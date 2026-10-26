import assert from "node:assert/strict";
import { spawn, spawnSync } from "node:child_process";
import { once } from "node:events";
import {
  copyFileSync,
  mkdirSync,
  mkdtempSync,
  readFileSync,
  rmSync,
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

function fixture(t) {
  const scratch = path.join(website, "test-results");
  mkdirSync(scratch, { recursive: true });
  const root = mkdtempSync(path.join(scratch, "runner contract "));
  t.after(() => rmSync(root, { recursive: true, force: true }));
  const app = path.join(root, "website");
  const bin = path.join(root, "bin");
  mkdirSync(path.join(app, "scripts"), { recursive: true });
  mkdirSync(path.join(app, "node_modules/@playwright/test"), {
    recursive: true,
  });
  mkdirSync(bin);
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
  const log = path.join(root, "docker.jsonl");
  writeFileSync(
    path.join(bin, "docker"),
    `#!/usr/bin/env node
const fs = require("node:fs");
const args = process.argv.slice(2);
fs.appendFileSync(process.env.FAKE_DOCKER_LOG, JSON.stringify({args, CI: process.env.CI}) + "\\n");
const stage = args[0] === "run" ? (args[args.indexOf("--name") + 1].endsWith("-ownership") ? "ownership" : "tests") : args[0];
if (args[0] === "image") console.log("sha256:verified-adapter");
if (process.env.FAKE_DOCKER_FAIL === stage) process.exit(Number(process.env.FAKE_DOCKER_EXIT || 9));
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
    calls: () =>
      readFileSync(log, "utf8")
        .trim()
        .split("\n")
        .map((line) => JSON.parse(line)),
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
  return args.flatMap((arg, i) => (arg === name ? [args[i + 1]] : []));
}

test("canonical Docker argv isolates dependencies, preserves caller ownership and forwards arguments intact", (t) => {
  // arrange
  const f = fixture(t);
  const args = [
    "--grep",
    "Button with spaces|Card.*(Primary)",
    "--workers=2",
    "--update-snapshots=missing",
  ];

  // act
  const result = f.run(args, {
    CI: "1",
    GITHUB_RUN_ID: "42",
    GITHUB_RUN_ATTEMPT: "2",
    GITHUB_JOB: "website-tests",
    E2E_PLATFORM: "linux/arm64",
  });
  const calls = f.calls();
  const build = calls.find(({ args }) => args[0] === "build").args;
  const runs = calls.filter(({ args }) => args[0] === "run");
  const bootstrap = runs[0].args;
  const render = runs[1].args;

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
    runs.map(({ args, CI }) => ({
      platform: option(args, "--platform"),
      user: option(args, "--user"),
      CI,
    })),
    [
      { platform: "linux/amd64", user: "0:0", CI: "1" },
      {
        platform: "linux/amd64",
        user: process.getuid
          ? `${process.getuid()}:${process.getgid()}`
          : "0:0",
        CI: "1",
      },
    ],
  );
  const mounts = options(render, "--mount");
  const modules = mounts[0].split(",")[1].slice("source=".length);
  const cache = mounts[1].split(",")[1].slice("source=".length);
  assert.deepEqual(options(render, "--volume"), [`${f.root}:/repo`]);
  assert.match(modules, /^website-visual-.+-modules$/);
  assert.match(cache, /^website-visual-.+-cache$/);
  assert.deepEqual(mounts, [
    `type=volume,source=${modules},target=/repo/website/node_modules,volume-nocopy`,
    `type=volume,source=${cache},target=/npm-cache,volume-nocopy`,
  ]);
  assert.deepEqual(options(bootstrap, "--volume"), [
    `${modules}:/modules`,
    `${cache}:/cache`,
  ]);
  assert.deepEqual(options(render, "--label"), [
    `website-visual-workspace=${f.root}`,
    "website-visual-run=42-2-website-tests",
  ]);
  assert.equal(option(render, "--workdir"), "/repo/website");
  assert.deepEqual(
    render
      .slice(render.indexOf("sha256:verified-adapter"), -args.length)
      .slice(0, 3),
    ["sha256:verified-adapter", "bash", "-c"],
  );
  const shell = render[render.indexOf("-c") + 1];
  assert.match(shell, /npm ci --no-audit --no-fund/);
  assert.match(
    shell,
    /exec node node_modules\/@playwright\/test\/cli\.js test --update-snapshots=none "\$@"/,
  );
  assert.equal(shell.includes(args[1]), false);
  assert.deepEqual(calls.at(-1).args, ["volume", "rm", modules]);
  assert.equal(render.includes("--publish"), false);
  assert.match(
    readFileSync(path.join(f.app, "scripts/visual-tests.Dockerfile"), "utf8"),
    /^FROM mcr\.microsoft\.com\/playwright:v1\.62\.1-noble@sha256:dcc5531e97840b9b5e794f2814476b21571c5124a3fca2267d73041f56e7580e\n/,
  );
});

test("different workspaces and successive invocations never share mutable modules", (t) => {
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
        args.at(-1).endsWith("-modules"),
    )
    .map(({ args }) => args.at(-1));
  const secondVolume = second
    .calls()
    .find(({ args }) => args[0] === "volume")
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

for (const stage of ["build", "ownership", "tests"]) {
  test(`${stage} failure preserves Docker exit status and cleans only owned modules`, (t) => {
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
      assert.match(removals[0].args[2], /^website-visual-.+-modules$/);
    }
  });
}

for (const stage of ["ownership", "tests"]) {
  test(
    `SIGTERM during ${stage} removes only the invocation container and module volume`,
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
      const run = calls.findLast(({ args }) => args[0] === "run").args;
      // assert
      assert.equal(code, 143);
      assert.deepEqual(
        calls.filter(({ args }) => args[0] === "rm").map(({ args }) => args),
        [["rm", "--force", option(run, "--name")]],
      );
      assert.match(calls.at(-1).args[2], /^website-visual-.+-modules$/);
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
    const render = f.calls().findLast(({ args }) => args[0] === "run").args;
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
