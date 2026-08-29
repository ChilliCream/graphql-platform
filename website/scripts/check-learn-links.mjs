#!/usr/bin/env node
// Link-integrity gate for /learn seed data. Extracts every http(s) URL found
// anywhere in src/data/learn/*.ts (link fields like githubUrl/demoUrl/
// externalUrl, but also URLs embedded in prose paragraphs and `cli` code
// strings), then checks each one is actually reachable. A GitHub repo/tree/
// blob URL is checked via `gh api` (repo existence, and path existence via
// the contents API at the ref named in the URL); every other host is
// checked with an HTTP HEAD, falling back to GET when the host rejects HEAD.
// 2xx and 3xx are a pass.
//
// Usage: node scripts/check-learn-links.mjs
//
// Requires the `gh` CLI to be installed and authenticated (`gh auth status`).
//
// Allowlist (scripts/check-learn-links.allowlist.json): a JSON array of
// { "url": "<exact URL as it appears in source>", "reason": "<why this
// can't be checked in CI>" }. Use it only for URLs that are legitimately
// unreachable from a CI runner (e.g. a localhost example in tutorial prose),
// never to silence a genuinely dead link.
import { execFileSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const WEBSITE_ROOT = process.cwd();
const LEARN_DATA_DIR = path.join(WEBSITE_ROOT, "src", "data", "learn");
const ALLOWLIST_PATH = path.join(WEBSITE_ROOT, "scripts", "check-learn-links.allowlist.json");
const FETCH_TIMEOUT_MS = 15_000;

/** Matches an http(s) URL up to the first character that can't legitimately be part of one in this source: whitespace, quotes, backtick, a trailing sentence/paren delimiter, or a backslash (source strings use "\n" for line breaks inside prose, not an actual newline character). */
const URL_PATTERN = /https?:\/\/[^\s"'`)\]}>,;\\]+/g;
/** Trailing punctuation a sentence can leave stuck to a URL match (e.g. "...at http://localhost:5095)."). */
const TRAILING_PUNCTUATION = /[.,;:)\]}]+$/;

/** Finds every `.ts` file directly in src/data/learn (non-recursive, matching the src/data/learn/*.ts scope this gate covers). */
function listLearnDataFiles() {
  return fs
    .readdirSync(LEARN_DATA_DIR, { withFileTypes: true })
    .filter((entry) => entry.isFile() && entry.name.endsWith(".ts"))
    .map((entry) => path.join(LEARN_DATA_DIR, entry.name))
    .sort();
}

/** Extracts every URL occurrence (with file + 1-based line number) from a source file's text. */
function extractUrlOccurrences(file, text) {
  const occurrences = [];
  const lines = text.split("\n");
  lines.forEach((line, index) => {
    for (const match of line.matchAll(URL_PATTERN)) {
      const url = match[0].replace(TRAILING_PUNCTUATION, "");
      if (url.length > 0) {
        occurrences.push({ url, file: path.relative(WEBSITE_ROOT, file), line: index + 1 });
      }
    }
  });
  return occurrences;
}

/** Groups occurrences by URL, so each distinct URL is checked once but every source location is still reported on failure. */
function groupByUrl(occurrences) {
  const byUrl = new Map();
  for (const occurrence of occurrences) {
    if (!byUrl.has(occurrence.url)) {
      byUrl.set(occurrence.url, []);
    }
    byUrl.get(occurrence.url).push({ file: occurrence.file, line: occurrence.line });
  }
  return byUrl;
}

function loadAllowlist() {
  if (!fs.existsSync(ALLOWLIST_PATH)) {
    return new Map();
  }
  const raw = JSON.parse(fs.readFileSync(ALLOWLIST_PATH, "utf8"));
  if (!Array.isArray(raw)) {
    throw new Error(`${path.relative(WEBSITE_ROOT, ALLOWLIST_PATH)} must be a JSON array of { url, reason }`);
  }
  const entries = new Map();
  for (const entry of raw) {
    if (typeof entry?.url !== "string" || typeof entry?.reason !== "string" || entry.reason.trim() === "") {
      throw new Error(
        `${path.relative(WEBSITE_ROOT, ALLOWLIST_PATH)} has an invalid entry: ${JSON.stringify(entry)} (each entry needs a string "url" and a non-empty string "reason")`,
      );
    }
    entries.set(entry.url, entry.reason);
  }
  return entries;
}

/** Parses a github.com URL into a repo check plus, for /tree/ or /blob/ URLs, a path-at-ref check. Returns null for a github.com URL shape this script doesn't recognize. */
function parseGithubUrl(url) {
  const parsed = new URL(url);
  if (parsed.hostname !== "github.com") {
    return null;
  }
  const segments = parsed.pathname.split("/").filter(Boolean);
  if (segments.length < 2) {
    return null;
  }
  const [owner, repoRaw, kind, ref, ...pathSegments] = segments;
  const repo = repoRaw.replace(/\.git$/, "");
  if (kind === undefined) {
    return { owner, repo, contentsPath: null, ref: null };
  }
  if ((kind === "tree" || kind === "blob") && ref !== undefined) {
    return { owner, repo, contentsPath: pathSegments.join("/") || null, ref };
  }
  // Some other github.com URL shape (issues, pulls, releases, ...): fall back to a plain HTTP check.
  return null;
}

function ghApi(apiPath) {
  execFileSync("gh", ["api", apiPath], { stdio: ["ignore", "ignore", "pipe"] });
}

/** Checks a github.com URL via `gh api`: the repo must exist, and if the URL names a /tree/ or /blob/ path, that path must exist at the given ref. */
function checkGithubUrl(githubTarget) {
  const { owner, repo, contentsPath, ref } = githubTarget;
  try {
    ghApi(`repos/${owner}/${repo}`);
  } catch (err) {
    return { ok: false, detail: `repo ${owner}/${repo} not found (gh api repos/${owner}/${repo}): ${firstLine(err)}` };
  }
  if (contentsPath === null) {
    if (ref === null) {
      return { ok: true, detail: null };
    }
    const commitApiPath = `repos/${owner}/${repo}/commits/${encodeURIComponent(ref)}`;
    try {
      ghApi(commitApiPath);
    } catch (err) {
      return {
        ok: false,
        detail: `ref "${ref}" not found (gh api ${commitApiPath}): ${firstLine(err)}`,
      };
    }
    return { ok: true, detail: null };
  }
  const apiPath = `repos/${owner}/${repo}/contents/${contentsPath}?ref=${encodeURIComponent(ref)}`;
  try {
    ghApi(apiPath);
  } catch (err) {
    return {
      ok: false,
      detail: `path "${contentsPath}" not found at ref "${ref}" (gh api ${apiPath}): ${firstLine(err)}`,
    };
  }
  return { ok: true, detail: null };
}

function firstLine(err) {
  const text = (err.stderr ?? err.message ?? "").toString().trim();
  return text.split("\n")[0] ?? text;
}

async function fetchWithTimeout(url, method) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), FETCH_TIMEOUT_MS);
  try {
    return await fetch(url, { method, redirect: "follow", signal: controller.signal });
  } finally {
    clearTimeout(timeout);
  }
}

function isPassingStatus(status) {
  return status >= 200 && status < 400;
}

/** Checks a non-github URL with HTTP HEAD, falling back to GET when the host errors or rejects HEAD (e.g. nuget.org returns 404 on HEAD but 200 on GET). */
async function checkHttpUrl(url) {
  let headStatus = null;
  try {
    const headResponse = await fetchWithTimeout(url, "HEAD");
    headStatus = headResponse.status;
    if (isPassingStatus(headStatus)) {
      return { ok: true, detail: null };
    }
  } catch (err) {
    headStatus = `error (${err.message})`;
  }

  try {
    const getResponse = await fetchWithTimeout(url, "GET");
    if (isPassingStatus(getResponse.status)) {
      return { ok: true, detail: null };
    }
    return { ok: false, detail: `HEAD ${headStatus}, GET ${getResponse.status}` };
  } catch (err) {
    return { ok: false, detail: `HEAD ${headStatus}, GET error (${err.message})` };
  }
}

async function checkUrl(url) {
  const githubTarget = parseGithubUrl(url);
  if (githubTarget !== null) {
    return checkGithubUrl(githubTarget);
  }
  return checkHttpUrl(url);
}

async function main() {
  const files = listLearnDataFiles();
  const occurrences = files.flatMap((file) => extractUrlOccurrences(file, fs.readFileSync(file, "utf8")));
  const byUrl = groupByUrl(occurrences);
  const allowlist = loadAllowlist();

  const failures = [];
  let checkedCount = 0;
  let skippedCount = 0;

  for (const [url, locations] of [...byUrl.entries()].sort(([a], [b]) => a.localeCompare(b))) {
    if (allowlist.has(url)) {
      console.log(`SKIP ${url} (allowlisted: ${allowlist.get(url)})`);
      skippedCount++;
      continue;
    }
    checkedCount++;
    const result = await checkUrl(url);
    if (result.ok) {
      console.log(`OK   ${url}`);
    } else {
      console.error(`FAIL ${url}: ${result.detail}`);
      failures.push({ url, detail: result.detail, locations });
    }
  }

  console.log("");
  console.log(
    `Checked ${checkedCount} URL(s), skipped ${skippedCount} allowlisted, found in ${files.length} file(s) under ${path.relative(WEBSITE_ROOT, LEARN_DATA_DIR)}.`,
  );

  if (failures.length > 0) {
    console.log("");
    console.log(`${failures.length} URL(s) FAILED:`);
    for (const failure of failures) {
      console.log(`  ${failure.url}`);
      console.log(`    reason: ${failure.detail}`);
      for (const location of failure.locations) {
        console.log(`    at ${location.file}:${location.line}`);
      }
    }
    process.exitCode = 1;
    return;
  }

  console.log("All links passed.");
}

main().catch((err) => {
  console.error(err.stack ?? String(err));
  process.exitCode = 1;
});
