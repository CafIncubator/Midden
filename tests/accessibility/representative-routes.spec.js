const AxeBuilder = require("@axe-core/playwright").default;
const { expect, test } = require("@playwright/test");
const reviewedBaseline = require("./axe-reviewed-baseline.json");

const routes = [
  { name: "dataset editor", path: "/editor/dataset", readyText: "Dataset Editor" },
  { name: "dataset catalog", path: "/catalog/datasets", readyText: "Dataset Catalog" }
];

for (const route of routes) {
  test(`${route.name} does not exceed the reviewed axe baseline`, async ({ page }, testInfo) => {
    await page.goto(route.path);
    await expect(page.getByText(route.readyText, { exact: true }).first()).toBeVisible();

    const results = await new AxeBuilder({ page }).analyze();
    const blockingViolations = results.violations.filter(
      violation => violation.impact === "serious" || violation.impact === "critical"
    );

    await testInfo.attach("axe-results.json", {
      body: JSON.stringify(results, null, 2),
      contentType: "application/json"
    });

    const unreviewedViolations = blockingViolations.filter(violation => {
      const maximumAffectedNodes = reviewedBaseline[route.name][violation.id];

      return maximumAffectedNodes === undefined || violation.nodes.length > maximumAffectedNodes;
    });

    if (unreviewedViolations.length > 0) {
      throw new Error(formatViolations(unreviewedViolations));
    }
  });
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