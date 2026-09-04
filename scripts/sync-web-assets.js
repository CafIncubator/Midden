const fs = require("node:fs");
const path = require("node:path");

const repositoryRoot = path.resolve(__dirname, "..");
const packageManifest = require(path.join(repositoryRoot, "package.json"));
const webRoot = path.join(repositoryRoot, "Caf.Midden.Wasm", "wwwroot", "lib");
const checkOnly = process.argv.includes("--check");

const assets = [
  {
    packageName: "leaflet",
    outputName: "leaflet",
    files: [
      ["LICENSE", "LICENSE"],
      ["dist/leaflet.css", "leaflet.css"],
      ["dist/leaflet.js", "leaflet.js"],
      ["dist/images", "images"]
    ]
  },
  {
    packageName: "@geoman-io/leaflet-geoman-free",
    outputName: "leaflet-geoman",
    files: [
      ["LICENSE", "LICENSE"],
      ["dist/leaflet-geoman.css", "leaflet-geoman.css"],
      ["dist/leaflet-geoman.min.js", "leaflet-geoman.min.js"]
    ]
  },
  {
    packageName: "leaflet.heat",
    outputName: "leaflet-heat",
    files: [
      ["LICENSE", "LICENSE"],
      ["dist/leaflet-heat.js", "leaflet-heat.js"]
    ]
  }
];

const differences = [];

for (const asset of assets) {
  const version = packageManifest.dependencies[asset.packageName];
  if (!version) {
    throw new Error(`Missing pinned dependency ${asset.packageName} in package.json.`);
  }

  const packageRoot = path.join(repositoryRoot, "node_modules", ...asset.packageName.split("/"));
  const outputRoot = path.join(webRoot, asset.outputName, version);

  if (checkOnly) {
    verifyAsset(asset, packageRoot, outputRoot);
    continue;
  }

  fs.rmSync(path.join(webRoot, asset.outputName), { recursive: true, force: true });
  for (const [source, destination] of asset.files) {
    fs.cpSync(path.join(packageRoot, source), path.join(outputRoot, destination), { recursive: true });
  }
  console.log(`Synced ${asset.packageName}@${version} to ${path.relative(repositoryRoot, outputRoot)}.`);
}

if (differences.length > 0) {
  console.error("Web assets are not synchronized with package-lock.json:");
  for (const difference of differences) {
    console.error(`- ${difference}`);
  }
  console.error("Run npm run sync:web-assets and commit the resulting changes.");
  process.exitCode = 1;
} else if (checkOnly) {
  console.log("Web assets match the pinned npm packages.");
}

function verifyAsset(asset, packageRoot, outputRoot) {
  const expectedFiles = new Map();
  for (const [source, destination] of asset.files) {
    collectFiles(path.join(packageRoot, source), destination, expectedFiles);
  }

  const actualFiles = new Map();
  collectFiles(outputRoot, "", actualFiles);

  for (const [relativePath, sourcePath] of expectedFiles) {
    const destinationPath = actualFiles.get(relativePath);
    if (!destinationPath) {
      differences.push(`missing ${path.relative(repositoryRoot, path.join(outputRoot, relativePath))}`);
    } else if (!filesMatch(sourcePath, destinationPath)) {
      differences.push(`changed ${path.relative(repositoryRoot, destinationPath)}`);
    }
  }

  for (const [relativePath, destinationPath] of actualFiles) {
    if (!expectedFiles.has(relativePath)) {
      differences.push(`stale ${path.relative(repositoryRoot, destinationPath)}`);
    }
  }
}

function filesMatch(sourcePath, destinationPath) {
  const source = fs.readFileSync(sourcePath);
  const destination = fs.readFileSync(destinationPath);
  if (source.equals(destination)) {
    return true;
  }

  const extension = path.extname(sourcePath).toLowerCase();
  const isTextAsset = path.basename(sourcePath) === "LICENSE" || extension === ".css" || extension === ".js";
  return isTextAsset
    && source.toString("utf8").replaceAll("\r\n", "\n")
      === destination.toString("utf8").replaceAll("\r\n", "\n");
}

function collectFiles(sourcePath, relativePath, files) {
  if (!fs.existsSync(sourcePath)) {
    throw new Error(`Required asset source does not exist: ${sourcePath}`);
  }

  const stats = fs.statSync(sourcePath);
  if (stats.isFile()) {
    files.set(relativePath.replaceAll("\\", "/"), sourcePath);
    return;
  }

  for (const entry of fs.readdirSync(sourcePath, { withFileTypes: true })) {
    collectFiles(
      path.join(sourcePath, entry.name),
      path.join(relativePath, entry.name),
      files
    );
  }
}