import assert from "node:assert/strict";
import { spawn, spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import { once } from "node:events";
import {
  chownSync,
  copyFileSync,
  lstatSync,
  mkdirSync,
  mkdtempSync,
  readFileSync,
  readdirSync,
  readlinkSync,
  rmSync,
  symlinkSync,
  writeFileSync,
} from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "node:test";

const website = fileURLToPath(new URL("..", import.meta.url));
const source = path.join(website, "scripts");
const packageJson = JSON.parse(
  readFileSync(path.join(website, "package.json")),
);
const callerGid = process.getgid?.();
const secondaryGid = process.getgroups?.().find((group) => group !== callerGid);

function fixture(t) {
  const scratch = path.join(website, "test-results");
  mkdirSync(scratch, { recursive: true });
  const root = mkdtempSync(path.join(scratch, "runner contract "));
  t.after(() => rmSync(root, { recursive: true, force: true }));
  const app = path.join(root, "website");
  const bin = path.join(root, "bin");
  const mocha = path.join(root, "src/Mocha/src/mocha-visualizer");
  const volumes = path.join(root, "volumes");
  mkdirSync(path.join(app, "scripts"), { recursive: true });
  mkdirSync(path.join(app, "node_modules/@playwright/test"), {
    recursive: true,
  });
  mkdirSync(path.join(app, "node_modules/.store/vite"), { recursive: true });
  mkdirSync(path.join(app, "node_modules/.bin"), { recursive: true });
  mkdirSync(path.join(app, "tests/visual/__screenshots__"), {
    recursive: true,
  });
  mkdirSync(path.join(app, "storybook-static"), { recursive: true });
  mkdirSync(path.join(mocha, "dist"), { recursive: true });
  mkdirSync(bin);
  mkdirSync(volumes);
  for (const name of ["visual-tests-docker.mjs", "visual-tests.Dockerfile"]) {
    copyFileSync(path.join(source, name), path.join(app, "scripts", name));
  }
  writeFileSync(
    path.join(app, "package.json"),
    JSON.stringify({
      devDependencies: { "@playwright/test": "1.62.1" },
      scripts: packageJson.scripts,
    }),
  );
  writeFileSync(
    path.join(app, "package-lock.json"),
    JSON.stringify({
      packages: { "node_modules/@playwright/test": { version: "1.62.1" } },
    }),
  );
  writeFileSync(
    path.join(app, "node_modules/@playwright/test/package.json"),
    JSON.stringify({ version: "1.62.1" }),
  );
  writeFileSync(
    path.join(app, ".npmrc"),
    "install-strategy=linked\nstrict-allow-scripts=true\n",
  );
  writeFileSync(path.join(app, "playwright.config.ts"), "export default {};\n");
  writeFileSync(path.join(app, "tsconfig.json"), "{}\n");
  writeFileSync(
    path.join(app, "tests/visual/storybook.spec.ts"),
    "export {};\n",
  );
  writeFileSync(
    path.join(app, "tests/visual/__screenshots__/existing.png"),
    "baseline",
  );
  writeFileSync(path.join(app, "storybook-static/index.json"), "{}\n");
  writeFileSync(path.join(mocha, "package.json"), '{"name":"local-mocha"}\n');
  writeFileSync(path.join(mocha, "README.md"), "Local Mocha package.\n");
  writeFileSync(path.join(mocha, "dist/index.js"), "export {};\n");
  writeFileSync(path.join(app, "node_modules/.store/sentinel"), "host only");
  writeFileSync(path.join(app, "node_modules/.store/vite/package.json"), "{}");
  writeFileSync(
    path.join(app, "node_modules/.store/vite/native-host.node"),
    "host",
  );
  writeFileSync(
    path.join(app, "node_modules/.store/vite/bin.js"),
    "#!/usr/bin/env node\n",
  );
  symlinkSync(".store/vite", path.join(app, "node_modules/vite"));
  symlinkSync("../vite/bin.js", path.join(app, "node_modules/.bin/vite"));

  const log = path.join(root, "docker.jsonl");
  const audit = path.join(root, "stage-audit.json");
  writeFileSync(log, "");
  writeFileSync(
    path.join(bin, "docker"),
    `#!/usr/bin/env node
const { createHash } = require("node:crypto");
const fs = require("node:fs");
const path = require("node:path");
const args = process.argv.slice(2);
const option = (name) => args[args.indexOf(name) + 1];
const options = (name) => args.flatMap((arg, index) => arg === name ? [args[index + 1]] : []);
const environment = (name) => options("--env").find((value) => value.startsWith(name + "="))?.slice(name.length + 1);
const mounts = () => options("--mount").map((mount) => Object.fromEntries(mount.split(",").map((part) => {
  const [key, ...values] = part.split("=");
  return [key, values.length ? values.join("=") : true];
})));
const volumePath = (name) => path.join(process.env.FAKE_DOCKER_VOLUMES, name);
const hash = (file) => createHash("sha256").update(fs.readFileSync(file)).digest("hex");
const stage = args[0] === "run"
  ? (option("--name").endsWith("-ownership") ? "ownership" : option("--name").endsWith("-staging") ? "staging" : "tests")
  : args[0];
fs.appendFileSync(process.env.FAKE_DOCKER_LOG, JSON.stringify({ args, CI: process.env.CI, stage }) + "\\n");
if (args[0] === "image") console.log("sha256:verified-adapter");
if (args[0] === "volume" && args[1] === "create") fs.mkdirSync(volumePath(args.at(-1)), { recursive: true });
if (args[0] === "volume" && args[1] === "rm") fs.rmSync(volumePath(args[2]), { recursive: true, force: true });
if (process.env.FAKE_DOCKER_FAIL === stage && stage !== "tests") process.exit(Number(process.env.FAKE_DOCKER_EXIT || 9));
if (stage === "staging") {
  const input = mounts().find(({ target }) => target === "/input");
  const repo = mounts().find(({ target }) => target === "/repo");
  const inputs = JSON.parse(environment("VISUAL_STAGE_INPUTS"));
  const hashes = JSON.parse(environment("VISUAL_STAGE_HASHES"));
  const repoPath = volumePath(repo.source);
  for (const { source, destination } of inputs) {
    const stagedSource = path.join(input.source, source.slice("/input/".length));
    const stagedDestination = path.join(repoPath, destination.slice("/repo/".length));
    fs.mkdirSync(path.dirname(stagedDestination), { recursive: true });
    fs.cpSync(stagedSource, stagedDestination, { preserveTimestamps: true, recursive: true, verbatimSymlinks: true });
  }
  if (process.env.FAKE_DOCKER_CORRUPT_STAGE) {
    fs.appendFileSync(path.join(repoPath, "website/package.json"), "changed");
  }
  const failures = Object.entries(hashes).filter(([destination, expected]) =>
    hash(path.join(repoPath, destination.slice("/repo/".length))) !== expected,
  );
  fs.writeFileSync(process.env.FAKE_DOCKER_STAGE_AUDIT, JSON.stringify({
    inputs,
    hashes,
    failures,
    hostDependencySentinel: fs.existsSync(path.join(input.source, "website/node_modules/.store/sentinel")),
    stagedDependencySentinel: fs.existsSync(path.join(repoPath, "website/node_modules/.store/sentinel")),
    stagedFiles: [
      "website/package.json",
      "website/package-lock.json",
      "website/.npmrc",
      "website/playwright.config.ts",
      "website/tsconfig.json",
      "website/tests/visual/storybook.spec.ts",
      "website/storybook-static/index.json",
      "src/Mocha/src/mocha-visualizer/package.json",
      "src/Mocha/src/mocha-visualizer/README.md",
      "src/Mocha/src/mocha-visualizer/dist/index.js",
    ].map((file) => [file, hash(path.join(repoPath, file))]),
  }));
  if (failures.length) {
    console.error("Staged visual test input hash mismatch: " + failures[0][0]);
    process.exit(Number(process.env.FAKE_DOCKER_EXIT || 31));
  }
}
if (stage === "tests") {
  const output = mounts().find(({ target }) => target === "/repo/website/test-results");
  const testArgs = args.slice(args.indexOf("visual-tests") + 1);
  const outputIndex = testArgs.findIndex((arg) => arg === "--output" || arg.startsWith("--output="));
  const requestedOutput = outputIndex === -1 ? undefined : testArgs[outputIndex] === "--output" ? testArgs[outputIndex + 1] : testArgs[outputIndex].slice("--output=".length);
  const outputDirectory = requestedOutput?.startsWith("test-results/")
    ? path.join(output.source, requestedOutput.slice("test-results/".length))
    : output.source;
  fs.mkdirSync(outputDirectory, { recursive: true });
  const outputFile = path.join(outputDirectory, "docker-output.txt");
  fs.writeFileSync(outputFile, "output");
  if (process.env.FAKE_DOCKER_OUTPUT_GID) {
    fs.chownSync(outputFile, process.getuid(), Number(process.env.FAKE_DOCKER_OUTPUT_GID));
  }
  if (process.env.FAKE_DOCKER_OUTPUT_LINK_TARGET) {
    const outputLink = path.join(outputDirectory, "docker-output-link");
    fs.symlinkSync(process.env.FAKE_DOCKER_OUTPUT_LINK_TARGET, outputLink);
    if (process.env.FAKE_DOCKER_OUTPUT_GID) {
      fs.lchownSync(outputLink, process.getuid(), Number(process.env.FAKE_DOCKER_OUTPUT_GID));
    }
  }
  if (process.env.FAKE_DOCKER_FAIL === stage) process.exit(Number(process.env.FAKE_DOCKER_EXIT || 9));
}
if (process.env.FAKE_DOCKER_HOLD === stage) {
  console.log("FAKE_DOCKER_HOLD=" + stage);
  setInterval(() => {}, 1000);
}
`,
    { mode: 0o755 },
  );
  const runner = path.join(app, "scripts/visual-tests-docker.mjs");
  const env = {
    ...process.env,
    PATH: `${bin}${path.delimiter}${process.env.PATH}`,
    FAKE_DOCKER_LOG: log,
    FAKE_DOCKER_STAGE_AUDIT: audit,
    FAKE_DOCKER_VOLUMES: volumes,
    CI: "",
    GITHUB_RUN_ID: "",
    GITHUB_RUN_ATTEMPT: "",
    GITHUB_JOB: "",
  };
  return {
    root,
    app,
    runner,
    env,
    calls: () => {
      const contents = readFileSync(log, "utf8").trim();
      return contents
        ? contents.split("\n").map((line) => JSON.parse(line))
        : [];
    },
    stageAudit: () => JSON.parse(readFileSync(audit, "utf8")),
    run: (args = [], extra = {}) =>
      spawnSync(process.execPath, [runner, ...args], {
        cwd: app,
        env: { ...env, ...extra },
        encoding: "utf8",
      }),
  };
}

function option(args, name) {
  return args[args.indexOf(name) + 1];
}

function options(args, name) {
  return args.flatMap((arg, index) => (arg === name ? [args[index + 1]] : []));
}

function mounts(args) {
  return options(args, "--mount").map((mount) =>
    Object.fromEntries(
      mount.split(",").map((part) => {
        const [key, ...values] = part.split("=");
        return [key, values.length ? values.join("=") : true];
      }),
    ),
  );
}

function runForStage(calls, stage) {
  return calls.findLast(({ args }) =>
    option(args, "--name").endsWith(`-${stage}`),
  ).args;
}

function inventory(directory) {
  const entries = [];
  const visit = (current, relative) => {
    const info = lstatSync(current);
    const metadata = {
      relative,
      mode: info.mode,
      size: info.size,
      uid: info.uid,
      gid: info.gid,
      mtimeMs: info.mtimeMs,
      type: info.isSymbolicLink()
        ? "symlink"
        : info.isDirectory()
          ? "directory"
          : "file",
    };
    if (info.isSymbolicLink()) {
      metadata.target = readlinkSync(current);
    } else if (info.isFile()) {
      metadata.hash = createHash("sha256")
        .update(readFileSync(current))
        .digest("hex");
    }
    entries.push(metadata);
    if (info.isDirectory()) {
      for (const entry of readdirSync(current).sort()) {
        visit(path.join(current, entry), path.join(relative, entry));
      }
    }
  };
  visit(directory, ".");
  return entries;
}

test("canonical Docker argv stages a finite read-only input and isolates the Linux worktree", (t) => {
  // arrange
  const f = fixture(t);
  const args = [
    "--grep",
    "Button with spaces|Card.*(Primary)",
    "--workers=2",
    "--update-snapshots=missing",
  ];
  const hostDependenciesBefore = inventory(path.join(f.app, "node_modules"));

  // act
  const result = f.run(args, {
    CI: "1",
    GITHUB_RUN_ID: "42",
    GITHUB_RUN_ATTEMPT: "2",
    GITHUB_JOB: "website-tests",
    E2E_PLATFORM: "linux/arm64",
  });
  const calls = f.calls();
  const build = calls.find(
    ({ args: dockerArgs }) => dockerArgs[0] === "build",
  ).args;
  const ownership = runForStage(calls, "ownership");
  const staging = runForStage(calls, "staging");
  const render = runForStage(calls, "tests");
  const stageAudit = f.stageAudit();

  // assert
  assert.equal(result.status, 0, result.stderr);
  assert.deepEqual(build, [
    "build",
    "--platform",
    "linux/amd64",
    "--tag",
    option(build, "--tag"),
    "--file",
    path.join(f.app, "scripts/visual-tests.Dockerfile"),
    path.join(f.app, "scripts"),
  ]);
  assert.deepEqual(render.slice(-args.length), args);
  assert.deepEqual(options(render, "--env"), [
    "HOME=/tmp",
    "npm_config_cache=/npm-cache",
    "CI",
    "PLAYWRIGHT_BROWSERS_PATH=/ms-playwright",
    "WEBSITE_VISUAL_DOCKER=playwright-1.62.1-noble-amd64",
  ]);
  assert.deepEqual(
    [ownership, staging, render].map((dockerArgs) => ({
      platform: option(dockerArgs, "--platform"),
      user: option(dockerArgs, "--user"),
    })),
    [
      { platform: "linux/amd64", user: "0:0" },
      {
        platform: "linux/amd64",
        user: process.getuid
          ? `${process.getuid()}:${process.getgid()}`
          : "0:0",
      },
      {
        platform: "linux/amd64",
        user: process.getuid
          ? `${process.getuid()}:${process.getgid()}`
          : "0:0",
      },
    ],
  );
  const ownershipMounts = mounts(ownership);
  const worktree = ownershipMounts[0].source;
  const cache = ownershipMounts[1].source;
  assert.match(worktree, /^website-visual-.+-worktree$/);
  assert.match(cache, /^website-visual-.+-cache$/);
  assert.deepEqual(ownershipMounts, [
    {
      type: "volume",
      source: worktree,
      target: "/repo",
      "volume-nocopy": true,
    },
    {
      type: "volume",
      source: cache,
      target: "/npm-cache",
      "volume-nocopy": true,
    },
  ]);
  assert.deepEqual(mounts(staging), [
    { type: "bind", source: f.root, target: "/input", readonly: true },
    {
      type: "volume",
      source: worktree,
      target: "/repo",
      "volume-nocopy": true,
    },
  ]);
  assert.deepEqual(mounts(render), [
    {
      type: "volume",
      source: worktree,
      target: "/repo",
      "volume-nocopy": true,
    },
    {
      type: "volume",
      source: cache,
      target: "/npm-cache",
      "volume-nocopy": true,
    },
    {
      type: "bind",
      source: path.join(f.app, "test-results"),
      target: "/repo/website/test-results",
    },
    {
      type: "bind",
      source: path.join(f.app, "playwright-report"),
      target: "/repo/website/playwright-report",
    },
    {
      type: "bind",
      source: path.join(f.app, "tests/visual/__screenshots__"),
      target: "/repo/website/tests/visual/__screenshots__",
    },
  ]);
  assert.deepEqual(options(render, "--volume"), []);
  assert.deepEqual(
    options(staging, "--env")
      .find((value) => value.startsWith("VISUAL_STAGE_INPUTS="))
      .slice("VISUAL_STAGE_INPUTS=".length),
    JSON.stringify(stageAudit.inputs),
  );
  assert.equal(stageAudit.hostDependencySentinel, true);
  assert.equal(stageAudit.stagedDependencySentinel, false);
  assert.deepEqual(
    stageAudit.inputs.map(({ source: input }) => input),
    [
      "/input/website/package.json",
      "/input/website/package-lock.json",
      "/input/website/.npmrc",
      "/input/website/playwright.config.ts",
      "/input/website/tsconfig.json",
      "/input/website/tests/visual",
      "/input/website/storybook-static",
      "/input/src/Mocha/src/mocha-visualizer/package.json",
      "/input/src/Mocha/src/mocha-visualizer/README.md",
      "/input/src/Mocha/src/mocha-visualizer/dist",
    ],
  );
  assert.deepEqual(stageAudit.failures, []);
  assert.equal(stageAudit.stagedFiles.length, 10);
  const output = lstatSync(path.join(f.app, "test-results/docker-output.txt"));
  assert.equal(output.uid, process.getuid?.() ?? 0);
  assert.equal(output.gid, process.getgid?.() ?? 0);
  assert.deepEqual(
    hostDependenciesBefore,
    inventory(path.join(f.app, "node_modules")),
  );
  assert.deepEqual(calls.at(-1).args, ["volume", "rm", worktree]);
  assert.equal(render.includes("--publish"), false);
  assert.match(
    readFileSync(path.join(f.app, "scripts/visual-tests.Dockerfile"), "utf8"),
    /^FROM mcr\.microsoft\.com\/playwright:v1\.62\.1-noble@sha256:dcc5531e97840b9b5e794f2814476b21571c5124a3fca2267d73041f56e7580e\n/,
  );
});

test("custom output paths retain argv and stay inside explicit artifact roots", (t) => {
  // arrange
  const f = fixture(t);
  const args = [
    "--output",
    "test-results/custom path",
    "--grep",
    "Button|Card",
  ];

  // act
  const result = f.run(args);
  const render = runForStage(f.calls(), "tests");

  // assert
  assert.equal(result.status, 0, result.stderr);
  assert.deepEqual(render.slice(-args.length), args);
  assert.equal(mounts(render).filter(({ type }) => type === "bind").length, 3);
  assert.equal(
    mounts(render).some(({ source: mountSource }) => mountSource === f.root),
    false,
  );
});

test(
  "changed custom output files regain caller ownership without traversing output symlinks",
  { skip: secondaryGid === undefined },
  (t) => {
    // arrange
    const f = fixture(t);
    const callerUid = process.getuid?.() ?? 0;
    const baseline = path.join(
      f.app,
      "tests/visual/__screenshots__/existing.png",
    );
    const outside = path.join(f.root, "outside-output-target.txt");
    writeFileSync(outside, "outside");
    chownSync(baseline, callerUid, secondaryGid);
    chownSync(outside, callerUid, secondaryGid);
    const baselineBefore = lstatSync(baseline);

    // act
    const result = f.run(["--output=test-results/custom-output"], {
      FAKE_DOCKER_OUTPUT_GID: `${secondaryGid}`,
      FAKE_DOCKER_OUTPUT_LINK_TARGET: outside,
    });
    const output = lstatSync(
      path.join(f.app, "test-results/custom-output/docker-output.txt"),
    );
    const outputLink = lstatSync(
      path.join(f.app, "test-results/custom-output/docker-output-link"),
    );
    const baselineAfter = lstatSync(baseline);

    // assert
    assert.equal(result.status, 0, result.stderr);
    assert.deepEqual(
      { uid: output.uid, gid: output.gid },
      { uid: callerUid, gid: callerGid },
    );
    assert.deepEqual(
      { uid: outputLink.uid, gid: outputLink.gid },
      { uid: callerUid, gid: callerGid },
    );
    assert.equal(lstatSync(outside).gid, secondaryGid);
    assert.deepEqual(
      { gid: baselineAfter.gid, ctimeMs: baselineAfter.ctimeMs },
      { gid: baselineBefore.gid, ctimeMs: baselineBefore.ctimeMs },
    );
  },
);

test(
  "output ownership normalization preserves the Docker failure status",
  { skip: secondaryGid === undefined },
  (t) => {
    // arrange
    const f = fixture(t);

    // act
    const result = f.run([], {
      FAKE_DOCKER_EXIT: "23",
      FAKE_DOCKER_FAIL: "tests",
      FAKE_DOCKER_OUTPUT_GID: `${secondaryGid}`,
    });

    // assert
    assert.equal(result.status, 23, result.stderr);
    assert.match(result.stderr, /Docker run failed/);
    assert.equal(
      lstatSync(path.join(f.app, "test-results/docker-output.txt")).gid,
      callerGid,
    );
  },
);

for (const args of [
  ["--output", "../outside"],
  ["--output=/tmp/outside"],
  ["--output", "unmanaged-output"],
  ["--output"],
]) {
  test(`unsafe output path ${JSON.stringify(args)} fails before Docker`, (t) => {
    // arrange
    const f = fixture(t);

    // act
    const result = f.run(args);

    // assert
    assert.equal(result.status, 1);
    assert.match(result.stderr, /--output|Unsupported Playwright/);
    assert.deepEqual(f.calls(), []);
  });
}

test("stage hash mismatch is terminal before installation and cleans the owned worktree", (t) => {
  // arrange
  const f = fixture(t);

  // act
  const result = f.run([], { FAKE_DOCKER_CORRUPT_STAGE: "1" });
  const calls = f.calls();
  const worktree = mounts(runForStage(calls, "ownership"))[0].source;

  // assert
  assert.equal(result.status, 31, result.stderr);
  assert.match(result.stderr, /Staged visual test input hash mismatch/);
  assert.equal(
    calls.some(({ stage }) => stage === "tests"),
    false,
  );
  assert.equal(f.stageAudit().failures.length, 1);
  assert.deepEqual(calls.at(-1).args, ["volume", "rm", worktree]);
});

test("different workspaces and successive invocations never share mutable worktrees", (t) => {
  // arrange
  const first = fixture(t);
  const second = fixture(t);

  // act
  const results = [first.run(), first.run(), second.run()];
  const firstVolumes = first
    .calls()
    .filter(
      ({ args }) =>
        args[0] === "volume" &&
        args[1] === "create" &&
        args.at(-1).endsWith("-worktree"),
    )
    .map(({ args }) => args.at(-1));
  const secondVolume = second
    .calls()
    .find(
      ({ args }) =>
        args[0] === "volume" &&
        args[1] === "create" &&
        args.at(-1).endsWith("-worktree"),
    )
    .args.at(-1);

  // assert
  assert.deepEqual(
    results.map(({ status }) => status),
    [0, 0, 0],
  );
  assert.equal(new Set([...firstVolumes, secondVolume]).size, 3);
  assert.notEqual(firstVolumes[0].split("-")[2], secondVolume.split("-")[2]);
});

for (const dependency of ["package", "lockfile", "installed"]) {
  test(`Playwright ${dependency} mismatch fails before invoking Docker`, (t) => {
    // arrange
    const f = fixture(t);
    const file =
      dependency === "package"
        ? "package.json"
        : dependency === "lockfile"
          ? "package-lock.json"
          : "node_modules/@playwright/test/package.json";
    const target = path.join(f.app, file);
    writeFileSync(
      target,
      readFileSync(target, "utf8").replaceAll("1.62.1", "1.63.0"),
    );

    // act
    const result = f.run();

    // assert
    assert.equal(result.status, 1);
    assert.match(result.stderr, /Playwright image is pinned to 1\.62\.1/);
    assert.deepEqual(f.calls(), []);
  });
}

test("unavailable daemon fails with actionable guidance and no host fallback", (t) => {
  // arrange
  const f = fixture(t);

  // act
  const result = f.run([], { FAKE_DOCKER_FAIL: "info" });

  // assert
  assert.equal(result.status, 1);
  assert.match(result.stderr, /Install\/start Docker Desktop/);
  assert.match(result.stderr, /no host-rendering fallback/);
  assert.deepEqual(
    f.calls().map(({ args }) => args),
    [["info"]],
  );
});

test("missing Docker executable fails with actionable guidance", (t) => {
  // arrange
  const f = fixture(t);

  // act
  const result = f.run([], { PATH: "" });

  // assert
  assert.equal(result.status, 1);
  assert.match(
    result.stderr,
    /Docker with a running Linux-container daemon is required/,
  );
});

for (const stage of ["build", "ownership", "staging", "tests"]) {
  test(`${stage} failure preserves Docker exit status and cleans only the owned worktree`, (t) => {
    // arrange
    const f = fixture(t);

    // act
    const result = f.run([], {
      FAKE_DOCKER_FAIL: stage,
      FAKE_DOCKER_EXIT: "23",
    });

    // assert
    assert.equal(result.status, 23);
    const removals = f
      .calls()
      .filter(({ args }) => args[0] === "volume" && args[1] === "rm");
    assert.equal(removals.length, stage === "build" ? 0 : 1);
    if (removals.length) {
      assert.match(removals[0].args[2], /^website-visual-.+-worktree$/);
    }
    assert.equal(
      f.calls().some(({ stage: current }) => current === "tests"),
      stage === "tests",
    );
  });
}

for (const stage of ["ownership", "staging", "tests"]) {
  test(
    `SIGTERM during ${stage} removes only the invocation container and worktree volume`,
    { timeout: 15000 },
    async (t) => {
      // arrange
      const f = fixture(t);

      // act
      const child = spawn(process.execPath, [f.runner], {
        cwd: f.app,
        env: { ...f.env, FAKE_DOCKER_HOLD: stage },
        stdio: ["ignore", "pipe", "pipe"],
      });
      t.after(() => {
        if (child.exitCode === null) {
          child.kill("SIGKILL");
        }
      });
      const closed = once(child, "close");
      let output = "";
      for await (const chunk of child.stdout) {
        output += chunk;
        if (output.includes(`FAKE_DOCKER_HOLD=${stage}`)) {
          child.kill("SIGTERM");
          break;
        }
      }
      const [code] = await closed;
      const calls = f.calls();
      const run = runForStage(calls, stage);

      // assert
      assert.equal(code, 143);
      assert.deepEqual(
        calls.filter(({ args }) => args[0] === "rm").map(({ args }) => args),
        [["rm", "--force", option(run, "--name")]],
      );
      assert.match(calls.at(-1).args[2], /^website-visual-.+-worktree$/);
      assert.equal(calls.at(-1).args[1], "rm");
    },
  );
}

test("both public npm entry points invoke the Docker runner, including updates", (t) => {
  // arrange
  const f = fixture(t);
  // Replace only the build in this disposable fixture, keeping npm's real argv forwarding.
  writeFileSync(
    path.join(f.app, "package.json"),
    JSON.stringify({
      ...JSON.parse(readFileSync(path.join(f.app, "package.json"))),
      scripts: {
        ...packageJson.scripts,
        "build-storybook:test": 'node -e "process.exit(0)"',
      },
    }),
  );

  for (const name of ["test:visual", "test:visual:update"]) {
    // act
    const result = spawnSync(
      "npm",
      ["run", name, "--", "--grep", "literal spaces|regex.*"],
      { cwd: f.app, env: f.env, encoding: "utf8" },
    );

    // assert
    assert.equal(result.status, 0, result.stderr);
    const render = runForStage(f.calls(), "tests");
    assert.deepEqual(render.slice(render.indexOf("visual-tests") + 1), [
      ...(name.endsWith(":update") ? ["--update-snapshots"] : []),
      "--grep",
      "literal spaces|regex.*",
    ]);
  }
});

test("direct host Playwright is rejected by the real configuration before discovery or browser launch", () => {
  // act
  const result = spawnSync(
    process.execPath,
    [
      path.join(website, "node_modules/@playwright/test/cli.js"),
      "test",
      "--list",
    ],
    {
      cwd: website,
      env: { ...process.env, WEBSITE_VISUAL_DOCKER: "" },
      encoding: "utf8",
    },
  );

  // assert
  assert.equal(result.status, 1);
  assert.match(
    result.stderr,
    /Visual tests must run in the pinned Docker renderer/,
  );
});
