const AxeBuilder = require("@axe-core/playwright").default;
const { expect, test } = require("@playwright/test");
const fs = require("node:fs");
const path = require("node:path");
const reviewedBaseline = require("./axe-reviewed-baseline.json");

const routes = [
  {
    name: "dataset editor",
    path: "/editor/dataset",
    waitUntilReady: page => expect(page.locator("#name").first()).toBeVisible()
  },
  {
    name: "dataset catalog",
    path: "/catalog/datasets",
    waitUntilReady: page => expect(page.locator(".catalog-summary-bar > div").first()).toContainText(/^\d+ datasets$/)
  }
];

for (const route of routes) {
  test(`${route.name} does not exceed the reviewed axe baseline`, async ({ page }, testInfo) => {
    await page.goto(route.path);
    await route.waitUntilReady(page);

    const results = await new AxeBuilder({ page }).analyze();
    const blockingViolations = results.violations.filter(
      violation => violation.impact === "serious" || violation.impact === "critical"
    );

    const actualBaseline = Object.fromEntries(blockingViolations.map(violation => [
      violation.id,
      countTargetSignatures(violation.nodes)
    ]));
    const resultsDirectory = "TestResults/Accessibility/axe-results";
    fs.mkdirSync(resultsDirectory, { recursive: true });
    fs.writeFileSync(
      path.join(resultsDirectory, `${route.name.replaceAll(" ", "-")}.json`),
      JSON.stringify({ baseline: actualBaseline, results }, null, 2)
    );

    await testInfo.attach("axe-results.json", {
      body: JSON.stringify(results, null, 2),
      contentType: "application/json"
    });

    const unreviewedViolations = blockingViolations
      .map(violation => {
        const configuredTargets = reviewedBaseline[route.name][violation.id];
        const reviewedTargets = typeof configuredTargets === "object" ? { ...configuredTargets } : {};
        const nodes = violation.nodes.filter(node => {
          const signature = normalizeTarget(node.target);
          const remaining = reviewedTargets[signature] ?? 0;
          reviewedTargets[signature] = Math.max(0, remaining - 1);
          return remaining === 0;
        });
        return { ...violation, nodes };
      })
      .filter(violation => violation.nodes.length > 0);

    if (unreviewedViolations.length > 0) {
      throw new Error(formatViolations(unreviewedViolations));
    }
  });
}

test("visible autosave status meets color contrast requirements", async ({ page }) => {
  await page.goto("/editor/dataset");
  await routes[0].waitUntilReady(page);

  const autosaveStatus = page.locator(".autosave-status");
  await autosaveStatus.evaluate(element => {
    element.textContent = "All changes saved";
  });

  const results = await new AxeBuilder({ page })
    .include(".autosave-status")
    .withRules(["color-contrast"])
    .analyze();

  expect(results.violations).toEqual([]);
});

function countTargetSignatures(nodes) {
  return nodes.reduce((counts, node) => {
    const signature = normalizeTarget(node.target);
    counts[signature] = (counts[signature] ?? 0) + 1;
    return counts;
  }, {});
}

function normalizeTarget(target) {
  return JSON.stringify(target)
    .replaceAll(/#ant-blazor-[0-9a-f-]+/gi, "#ant-blazor-*")
    .replaceAll(/b-[a-z0-9]{10}/gi, "b-*")
    .replaceAll(/_bl_\d+/g, "_bl_*");
}

function formatViolations(violations) {
  return violations
    .map(violation => {
      const targets = violation.nodes
        .flatMap(node => node.target)
        .join(", ");

      return `${violation.id} (${violation.impact}, ${violation.nodes.length} affected): ${violation.help}\n${targets}`;
    })
    .join("\n\n");
}