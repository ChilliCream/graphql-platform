#!/usr/bin/env node
// Standalone git metadata step, run as part of the release workflow (and the
// local `ci-local.sh` script). Walks the git history of the tracked content
// directories and writes `git-metadata.generated.json` mapping each doc/blog
// file to its last commit date and author. The app reads this file at build
// time; when it is absent it falls back to static placeholder metadata.
//
// A shallow checkout cannot produce accurate attribution, so this fails hard
// instead of silently degrading. Use `actions/checkout` with `fetch-depth: 0`.
import { execFileSync, spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const WEBSITE_ROOT = process.cwd();
const OUTPUT_PATH = path.join(WEBSITE_ROOT, "git-metadata.generated.json");
const HEADER = "@@COMMIT@@";

const TRACKED_DIRS = [
  path.join(WEBSITE_ROOT, "content", "docs"),
  path.join(WEBSITE_ROOT, "content", "blog"),
  path.join(WEBSITE_ROOT, "app", "(content)"),
];

function gitRoot() {
  const res = spawnSync("git", ["rev-parse", "--show-toplevel"], {
    cwd: WEBSITE_ROOT,
    encoding: "utf-8",
  });
  if (res.status !== 0) {
    throw new Error("not a git repository, cannot generate git metadata");
  }
  return res.stdout.trim();
}

function assertNotShallow(repoRoot) {
  const res = spawnSync("git", ["rev-parse", "--is-shallow-repository"], {
    cwd: repoRoot,
    encoding: "utf-8",
  });
  if (res.status === 0 && res.stdout.trim() === "true") {
    throw new Error(
      "shallow clone detected, doc attribution requires full history. " +
        "Use `actions/checkout` with `fetch-depth: 0` in CI.",
    );
  }
}

function isTrackedFile(repoRelativeFile) {
  if (/\.mdx?$/.test(repoRelativeFile)) {
    return true;
  }
  return path.basename(repoRelativeFile) === "page.tsx";
}

function buildManifest(repoRoot) {
  const trackedPaths = TRACKED_DIRS.map((p) => path.relative(repoRoot, p));

  const stdout = execFileSync(
    "git",
    [
      "-c",
      "core.quotePath=false",
      "log",
      "--name-only",
      "--no-merges",
      "--diff-filter=AMR",
      `--format=${HEADER}%aI%x09%an`,
      "--",
      ...trackedPaths,
    ],
    {
      cwd: repoRoot,
      encoding: "utf-8",
      maxBuffer: 256 * 1024 * 1024,
      stdio: ["ignore", "pipe", "ignore"],
    },
  );

  const out = {};
  let currentDate = null;
  let currentAuthor = null;

  for (const line of stdout.split("\n")) {
    if (line === "") {
      continue;
    }
    if (line.startsWith(HEADER)) {
      const rest = line.slice(HEADER.length);
      const tab = rest.indexOf("\t");
      currentDate = tab === -1 ? rest : rest.slice(0, tab);
      currentAuthor = tab === -1 ? "" : rest.slice(tab + 1);
      continue;
    }
    if (currentDate === null || !isTrackedFile(line)) {
      continue;
    }
    const abs = path.join(repoRoot, line);
    const key = path.relative(WEBSITE_ROOT, abs);
    if (key.startsWith("..") || out[key]) {
      continue;
    }
    out[key] = { date: currentDate, author: currentAuthor ?? "" };
  }

  return out;
}

try {
  const repoRoot = gitRoot();
  assertNotShallow(repoRoot);
  const manifest = buildManifest(repoRoot);
  fs.writeFileSync(OUTPUT_PATH, JSON.stringify(manifest, null, 2));
  console.log(
    `[git-metadata] wrote ${Object.keys(manifest).length} entries to ` +
      path.relative(WEBSITE_ROOT, OUTPUT_PATH),
  );
} catch (err) {
  console.error(`[git-metadata] ${err?.message ?? err}`);
  process.exit(1);
}
