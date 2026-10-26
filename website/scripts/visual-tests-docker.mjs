import { spawn, spawnSync } from "node:child_process";
import { createHash, randomUUID } from "node:crypto";
import { readFileSync, realpathSync } from "node:fs";
import { createRequire } from "node:module";
import { constants } from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";

const website = realpathSync(fileURLToPath(new URL("..", import.meta.url)));
const root = path.dirname(website);
const require = createRequire(import.meta.url);
const readJson = (file) => JSON.parse(readFileSync(file, "utf8"));
const hash = (value) =>
  createHash("sha256").update(value).digest("hex").slice(0, 16);
const version = "1.62.1";
const platform = "linux/amd64";
const adapter = readFileSync(
  new URL("visual-tests.Dockerfile", import.meta.url),
);
const image = `website-visual:playwright-${version}-${hash(adapter)}`;
const lock = readFileSync(path.join(website, "package-lock.json"));
const workspace = hash(root);
const key = hash(
  Buffer.concat([
    Buffer.from(`${workspace}-${platform}-${version}`),
    lock,
    adapter,
    readFileSync(path.join(website, ".npmrc")),
    readFileSync(path.join(website, "package.json")),
  ]),
);
const invocation = `website-visual-${key}-${randomUUID()}`;
const modules = `${invocation}-modules`;
const cache = `website-visual-${key}-cache`;
const caller = process.getuid
  ? `${process.getuid()}:${process.getgid()}`
  : "0:0";
const labels = ["--label", `website-visual-workspace=${root}`];
if (process.env.GITHUB_RUN_ID) {
  labels.push(
    "--label",
    `website-visual-run=${process.env.GITHUB_RUN_ID}-${process.env.GITHUB_RUN_ATTEMPT}-${process.env.GITHUB_JOB}`,
  );
}
let child;
let container;
let interrupted;
let modulesCreated = false;

function docker(args, capture = false) {
  if (interrupted) {
    throw Object.assign(new Error("Visual tests interrupted."), {
      exitCode: interrupted,
    });
  }
  return new Promise((resolve, reject) => {
    child = spawn("docker", args, {
      stdio: capture ? ["ignore", "pipe", "inherit"] : "inherit",
    });
    let output = "";
    child.stdout?.on("data", (data) => {
      output += data;
    });
    child.on("error", reject);
    child.on("close", (code, signal) => {
      child = undefined;
      if (code === 0) {
        resolve(output.trim());
      } else {
        reject(
          Object.assign(
            new Error(
              `Docker ${args[0]} failed${signal ? ` (${signal})` : ""}.`,
            ),
            { exitCode: interrupted ?? code ?? 1 },
          ),
        );
      }
    });
  });
}

async function run(stage, args) {
  container = `${invocation}-${stage}`;
  try {
    return await docker([
      "run",
      "--rm",
      "--name",
      container,
      ...labels,
      "--platform",
      platform,
      ...args,
    ]);
  } finally {
    container = undefined;
  }
}

for (const signal of ["SIGINT", "SIGTERM", "SIGHUP"]) {
  process.on(signal, () => {
    interrupted = 128 + constants.signals[signal];
    if (container) {
      spawnSync("docker", ["rm", "--force", container], { stdio: "ignore" });
    }
    child?.kill(signal);
  });
}

try {
  const pkg = readJson(path.join(website, "package.json"));
  const locked =
    JSON.parse(lock).packages["node_modules/@playwright/test"].version;
  const installed = require(
    path.join(website, "node_modules/@playwright/test/package.json"),
  ).version;
  if (
    pkg.devDependencies["@playwright/test"] !== version ||
    locked !== version ||
    installed !== version
  ) {
    throw new Error(
      `Playwright image is pinned to ${version}; package, lockfile and installed @playwright/test must match. Update and verify visual-tests.Dockerfile and the runner pin together when upgrading Playwright.`,
    );
  }
  try {
    await docker(["info"], true);
  } catch (error) {
    throw Object.assign(
      new Error(
        "Docker with a running Linux-container daemon is required for visual tests. Install/start Docker Desktop (or Docker Engine on Linux), then rerun npm run test:visual. There is no host-rendering fallback.",
        { cause: error },
      ),
      { exitCode: interrupted ?? 1 },
    );
  }
  await docker([
    "build",
    "--platform",
    platform,
    "--tag",
    image,
    "--file",
    path.join(website, "scripts/visual-tests.Dockerfile"),
    path.join(website, "scripts"),
  ]);
  // Resolve the tag once so another invocation cannot replace the image mid-run.
  const imageId = await docker(
    ["image", "inspect", "--format", "{{.Id}}", image],
    true,
  );
  console.log(
    `Visual renderer: ${platform}, ${adapter.toString().split("\n")[0].slice(5)}, adapter ${imageId}`,
  );
  await docker(["volume", "create", ...labels, modules], true);
  modulesCreated = true;
  await docker(
    ["volume", "create", "--label", `website-visual-workspace=${root}`, cache],
    true,
  );
  // This root-only bootstrap mounts private volumes, never the checkout.
  await run("ownership", [
    "--user",
    "0:0",
    "--volume",
    `${modules}:/modules`,
    "--volume",
    `${cache}:/cache`,
    imageId,
    "chown",
    caller,
    "/modules",
    "/cache",
  ]);
  await run("tests", [
    "--init",
    "--ipc=host",
    "--user",
    caller,
    "--volume",
    `${root}:/repo`,
    "--mount",
    `type=volume,source=${modules},target=/repo/website/node_modules,volume-nocopy`,
    "--mount",
    `type=volume,source=${cache},target=/npm-cache,volume-nocopy`,
    "--workdir",
    "/repo/website",
    "--env",
    "HOME=/tmp",
    "--env",
    "npm_config_cache=/npm-cache",
    "--env",
    "CI",
    "--env",
    "PLAYWRIGHT_BROWSERS_PATH=/ms-playwright",
    "--env",
    `WEBSITE_VISUAL_DOCKER=playwright-${version}-noble-amd64`,
    imageId,
    "bash",
    "-c",
    `set -e
npm ci --no-audit --no-fund
node -e 'const {execFileSync}=require("node:child_process"); const {chromium}=require("playwright"); console.log("Visual runtime:", process.platform, process.arch, "Node", process.version, "npm", execFileSync("npm", ["--version"], {encoding:"utf8"}).trim(), "Playwright", require("@playwright/test/package.json").version, execFileSync(chromium.executablePath(), ["--version"], {encoding:"utf8"}).trim());'
exec node node_modules/@playwright/test/cli.js test --update-snapshots=none "$@"`,
    "visual-tests",
    ...process.argv.slice(2),
  ]);
} catch (error) {
  console.error(error.message);
  process.exitCode = interrupted ?? error.exitCode ?? 1;
} finally {
  if (modulesCreated) {
    const cleanup = spawnSync("docker", ["volume", "rm", modules], {
      stdio: "inherit",
    });
    if (cleanup.status !== 0) {
      console.error(`Could not remove owned dependency volume ${modules}.`);
      process.exitCode ||= 1;
    }
  }
}
