const { defineConfig } = require("@playwright/test");

module.exports = defineConfig({
  testDir: "tests/accessibility",
  outputDir: "TestResults/Accessibility/playwright",
  reporter: [
    ["list"],
    ["html", { outputFolder: "TestResults/Accessibility/report", open: "never" }]
  ],
  use: {
    baseURL: "http://127.0.0.1:5080",
    screenshot: "only-on-failure",
    trace: "retain-on-failure"
  },
  webServer: {
    command: "dotnet run --project Caf.Midden.Wasm/Caf.Midden.Wasm.csproj --configuration Release --no-launch-profile --urls http://127.0.0.1:5080",
    url: "http://127.0.0.1:5080",
    reuseExistingServer: !process.env.CI,
    timeout: 120000
  }
});