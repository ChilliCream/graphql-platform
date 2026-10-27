import { spawn, spawnSync } from "node:child_process";
import { createHash, randomUUID } from "node:crypto";
import {
  lchownSync,
  lstatSync,
  mkdirSync,
  readFileSync,
  readdirSync,
  realpathSync,
} from "node:fs";
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
const contentHash = (value) => createHash("sha256").update(value).digest("hex");
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
const worktree = `${invocation}-worktree`;
const cache = `website-visual-${key}-cache`;
const callerUid = process.getuid?.();
const callerGid = process.getgid?.();
const caller =
  callerUid === undefined || callerGid === undefined
    ? "0:0"
    : `${callerUid}:${callerGid}`;
const labels = ["--label", `website-visual-workspace=${root}`];
const inputPaths = [
  "website/package.json",
  "website/package-lock.json",
  "website/.npmrc",
  "website/playwright.config.ts",
  "website/tsconfig.json",
  "website/tests/visual",
  "website/storybook-static",
  "src/Mocha/src/mocha-visualizer/package.json",
  "src/Mocha/src/mocha-visualizer/README.md",
  "src/Mocha/src/mocha-visualizer/dist",
];
const criticalInputPaths = inputPaths.slice(0, 5);
const outputRoots = ["test-results", "playwright-report"];
let child;
let container;
let interrupted;
let worktreeCreated = false;

if (process.env.GITHUB_RUN_ID) {
  labels.push(
    "--label",
    `website-visual-run=${process.env.GITHUB_RUN_ID}-${process.env.GITHUB_RUN_ATTEMPT}-${process.env.GITHUB_JOB}`,
  );
}

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

function requireInput(relative) {
  const input = path.join(root, relative);
  try {
    lstatSync(input);
  } catch {
    throw new Error(
      `Required visual test input is missing: ${relative}. Run npm run build-storybook:test before running the Docker renderer.`,
    );
  }
  return input;
}

function stageInputs() {
  const criticalHashes = Object.fromEntries(
    criticalInputPaths.map((relative) => [
      `/repo/${relative}`,
      contentHash(readFileSync(requireInput(relative))),
    ]),
  );
  return {
    inputs: inputPaths.map((relative) => {
      requireInput(relative);
      return {
        source: `/input/${relative}`,
        destination: `/repo/${relative}`,
      };
    }),
    criticalHashes,
  };
}

function outputMounts(args) {
  for (let i = 0; i < args.length; i++) {
    let value;
    if (args[i] === "--output") {
      value = args[++i];
    } else if (args[i].startsWith("--output=")) {
      value = args[i].slice("--output=".length);
    } else {
      continue;
    }
    if (!value) {
      throw new Error(
        "Playwright --output requires a path inside test-results or playwright-report.",
      );
    }
    const normalized = path.posix.normalize(value);
    if (
      value.includes("\\") ||
      path.posix.isAbsolute(value) ||
      normalized === "." ||
      normalized === ".." ||
      normalized.startsWith("../") ||
      !outputRoots.some(
        (root) => normalized === root || normalized.startsWith(`${root}/`),
      )
    ) {
      throw new Error(
        `Unsupported Playwright --output path: ${value}. Use a relative path inside test-results or playwright-report.`,
      );
    }
  }

  const outputs = [
    {
      source: path.join(website, "test-results"),
      target: "/repo/website/test-results",
    },
    {
      source: path.join(website, "playwright-report"),
      target: "/repo/website/playwright-report",
    },
    {
      source: path.join(website, "tests/visual/__screenshots__"),
      target: "/repo/website/tests/visual/__screenshots__",
    },
  ];
  for (const output of outputs) {
    mkdirSync(output.source, { recursive: true });
  }
  return outputs;
}

function snapshotOutputs(outputs) {
  const entries = new Map();

  const snapshot = (output) => {
    const info = lstatSync(output);
    entries.set(output, {
      ino: info.ino,
      size: info.size,
      mtimeMs: info.mtimeMs,
    });
    if (info.isDirectory()) {
      for (const entry of readdirSync(output)) {
        snapshot(path.join(output, entry));
      }
    }
  };

  for (const { source } of outputs) {
    snapshot(source);
  }
  return entries;
}

function restoreOutputOwnership(outputs, snapshots) {
  if (callerUid === undefined || callerGid === undefined) {
    return;
  }

  const restore = (output) => {
    const info = lstatSync(output);
    const snapshot = snapshots.get(output);
    if (
      !info.isDirectory() &&
      (!snapshot ||
        snapshot.ino !== info.ino ||
        snapshot.size !== info.size ||
        snapshot.mtimeMs !== info.mtimeMs) &&
      (info.uid !== callerUid || info.gid !== callerGid)
    ) {
      lchownSync(output, callerUid, callerGid);
    }
    if (info.isDirectory()) {
      for (const entry of readdirSync(output)) {
        restore(path.join(output, entry));
      }
    }
  };

  for (const { source } of outputs) {
    restore(source);
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
  const stage = stageInputs();
  const outputs = outputMounts(process.argv.slice(2));
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
  await docker(["volume", "create", ...labels, worktree], true);
  worktreeCreated = true;
  await docker(
    ["volume", "create", "--label", `website-visual-workspace=${root}`, cache],
    true,
  );
  // This root-only bootstrap mounts private volumes, never the checkout or outputs.
  await run("ownership", [
    "--user",
    "0:0",
    "--mount",
    `type=volume,source=${worktree},target=/repo,volume-nocopy`,
    "--mount",
    `type=volume,source=${cache},target=/npm-cache,volume-nocopy`,
    imageId,
    "chown",
    caller,
    "/repo",
    "/npm-cache",
  ]);
  await run("staging", [
    "--user",
    caller,
    "--mount",
    `type=bind,source=${root},target=/input,readonly`,
    "--mount",
    `type=volume,source=${worktree},target=/repo,volume-nocopy`,
    "--workdir",
    "/repo",
    "--env",
    `VISUAL_STAGE_INPUTS=${JSON.stringify(stage.inputs)}`,
    "--env",
    `VISUAL_STAGE_HASHES=${JSON.stringify(stage.criticalHashes)}`,
    imageId,
    "node",
    "-e",
    `const { cpSync, mkdirSync, readFileSync } = require("node:fs");
const { createHash } = require("node:crypto");
const { dirname } = require("node:path");
for (const { source, destination } of JSON.parse(process.env.VISUAL_STAGE_INPUTS)) {
  mkdirSync(dirname(destination), { recursive: true });
  cpSync(source, destination, { preserveTimestamps: true, recursive: true, verbatimSymlinks: true });
}
for (const [destination, expected] of Object.entries(JSON.parse(process.env.VISUAL_STAGE_HASHES))) {
  const actual = createHash("sha256").update(readFileSync(destination)).digest("hex");
  if (actual !== expected) {
    throw new Error(\`Staged visual test input hash mismatch: \${destination}\`);
  }
}`,
  ]);
  const outputSnapshots = snapshotOutputs(outputs);
  let testError;
  try {
    await run("tests", [
      "--init",
      "--ipc=host",
      "--user",
      caller,
      "--mount",
      `type=volume,source=${worktree},target=/repo,volume-nocopy`,
      "--mount",
      `type=volume,source=${cache},target=/npm-cache,volume-nocopy`,
      ...outputs.flatMap(({ source, target }) => [
        "--mount",
        `type=bind,source=${source},target=${target}`,
      ]),
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
    testError = error;
  }

  let ownershipError;
  try {
    restoreOutputOwnership(outputs, outputSnapshots);
  } catch (error) {
    ownershipError = error;
  }

  if (testError && ownershipError) {
    const testMessage =
      testError instanceof Error ? testError.message : String(testError);
    const ownershipMessage =
      ownershipError instanceof Error
        ? ownershipError.message
        : String(ownershipError);
    throw Object.assign(
      new Error(
        `${testMessage} Output ownership normalization failed: ${ownershipMessage}`,
        { cause: new AggregateError([testError, ownershipError]) },
      ),
      { exitCode: testError.exitCode },
    );
  }
  if (testError) {
    throw testError;
  }
  if (ownershipError) {
    throw ownershipError;
  }
} catch (error) {
  console.error(error.message);
  process.exitCode = interrupted ?? error.exitCode ?? 1;
} finally {
  if (worktreeCreated) {
    const cleanup = spawnSync("docker", ["volume", "rm", worktree], {
      stdio: "inherit",
    });
    if (cleanup.status !== 0) {
      console.error(`Could not remove owned worktree volume ${worktree}.`);
      process.exitCode ||= 1;
    }
  }
}
