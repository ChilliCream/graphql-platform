import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const PROJECT_ROOT = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
);

export const INDEXNOW_HOST = "chillicream.com";
export const MAX_URLS_PER_REQUEST = 10_000;

const DEFAULT_ENDPOINT = "https://api.indexnow.org/indexnow";
const DEFAULT_LIVE_MANIFEST_URL = `https://${INDEXNOW_HOST}/indexnow-manifest.json`;
const DEFAULT_MANIFEST_PATH = path.join(
  PROJECT_ROOT,
  "out",
  "indexnow-manifest.json",
);
const KEY_FILE_PATTERN = /^([0-9a-f]{32})\.txt$/;
const REQUEST_TIMEOUT_MS = 30_000;

/** Parses a manifest document and throws when it is not a URL to hash map. */
export function parseManifest(text) {
  const value = JSON.parse(text);
  if (
    value === null ||
    typeof value !== "object" ||
    Array.isArray(value) ||
    Object.keys(value).length === 0 ||
    !Object.values(value).every((hash) => typeof hash === "string")
  ) {
    throw new Error("The manifest is not a non-empty URL to hash map.");
  }
  return value;
}

/**
 * Compares two manifests. A null `previous` is a first run, where every
 * current URL counts as added.
 */
export function diffManifests(previous, current) {
  const added = [];
  const changed = [];
  const removed = [];

  for (const [url, hash] of Object.entries(current)) {
    if (!previous || !Object.hasOwn(previous, url)) {
      added.push(url);
    } else if (previous[url] !== hash) {
      changed.push(url);
    }
  }
  for (const url of Object.keys(previous ?? {})) {
    if (!Object.hasOwn(current, url)) {
      removed.push(url);
    }
  }

  return {
    added: added.sort(),
    changed: changed.sort(),
    removed: removed.sort(),
  };
}

/** Splits a list into consecutive chunks of at most `size` items. */
export function batchUrls(urls, size = MAX_URLS_PER_REQUEST) {
  const batches = [];
  for (let index = 0; index < urls.length; index += size) {
    batches.push(urls.slice(index, index + size));
  }
  return batches;
}

/** Reads the single `<key>.txt` file in `publicDir` and returns the key. */
export async function readKey(publicDir) {
  const names = (await fs.readdir(publicDir)).filter((name) =>
    KEY_FILE_PATTERN.test(name),
  );
  if (names.length !== 1) {
    throw new Error(
      `Expected exactly one IndexNow key file in ${publicDir}, found ${names.length}.`,
    );
  }
  const key = KEY_FILE_PATTERN.exec(names[0])[1];
  const content = (
    await fs.readFile(path.join(publicDir, names[0]), "utf8")
  ).trim();
  if (content !== key) {
    throw new Error(`${names[0]} must contain only its own key.`);
  }
  return key;
}

/**
 * Polls `manifestUrl` until it serves a manifest equal to `expected`. Resolves
 * true on a match and false once `timeoutMs` has elapsed.
 */
export async function waitForLiveManifest({
  manifestUrl,
  expected,
  intervalMs,
  timeoutMs,
  fetchImpl = fetch,
  sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms)),
  now = Date.now,
}) {
  const expectedText = JSON.stringify(expected);
  const deadline = now() + timeoutMs;

  for (;;) {
    try {
      const url = new URL(manifestUrl);
      url.searchParams.set("cache-bust", String(now()));
      const response = await fetchImpl(url, {
        headers: { "cache-control": "no-cache" },
        signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
      });
      if (response.ok) {
        const live = parseManifest(await response.text());
        if (JSON.stringify(live) === expectedText) {
          return true;
        }
      }
    } catch {
      // A transient or malformed response counts as not live yet.
    }

    if (now() + intervalMs > deadline) {
      return false;
    }
    await sleep(intervalMs);
  }
}

function oneLine(value) {
  return String(value).replace(/\s+/g, " ").trim().slice(0, 300);
}

function warn(message) {
  console.log(`::warning::IndexNow: ${oneLine(message)}`);
}

async function writeSummary(env, line) {
  if (!env.GITHUB_STEP_SUMMARY) {
    return;
  }
  try {
    await fs.appendFile(env.GITHUB_STEP_SUMMARY, `${line}\n`, "utf8");
  } catch {
    // The summary is best effort.
  }
}

async function readPreviousManifest(env) {
  const previousPath = env.INDEXNOW_PREVIOUS_MANIFEST;
  if (!previousPath) {
    warn("No previous manifest configured, treating this as a first run.");
    return null;
  }
  try {
    return parseManifest(await fs.readFile(previousPath, "utf8"));
  } catch (error) {
    warn(
      `Previous manifest is missing or invalid (${error.message}), treating this as a first run.`,
    );
    return null;
  }
}

function positiveSeconds(value, fallback) {
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

/**
 * Diffs the previous and new manifests and submits the changed URLs to
 * IndexNow. Reports every failure as a workflow warning and never throws.
 */
export async function run(env = process.env, options = {}) {
  const { fetchImpl = fetch, sleep, now } = options;
  try {
    const dryRun = /^(1|true)$/i.test(env.INDEXNOW_DRY_RUN ?? "");
    const endpoint = env.INDEXNOW_ENDPOINT || DEFAULT_ENDPOINT;
    const manifestPath = env.INDEXNOW_MANIFEST || DEFAULT_MANIFEST_PATH;

    const current = parseManifest(await fs.readFile(manifestPath, "utf8"));
    const previous = await readPreviousManifest(env);
    const { added, changed, removed } = diffManifests(previous, current);
    const counts = `${added.length} added, ${changed.length} changed, ${removed.length} removed${previous ? "" : " (first run)"}`;

    const urlList = [...added, ...changed, ...removed].filter((url) => {
      try {
        return new URL(url).hostname === INDEXNOW_HOST;
      } catch {
        return false;
      }
    });
    if (urlList.length !== added.length + changed.length + removed.length) {
      warn(`Skipping URLs that are not on ${INDEXNOW_HOST}.`);
    }

    if (urlList.length === 0) {
      console.log(`::notice::IndexNow: nothing to submit (${counts}).`);
      await writeSummary(env, `IndexNow: nothing to submit (${counts}).`);
      return;
    }

    const key = await readKey(path.join(PROJECT_ROOT, "public"));
    const keyLocation = `https://${INDEXNOW_HOST}/${key}.txt`;
    const batches = batchUrls(urlList);

    if (dryRun) {
      console.log(`[indexnow] Dry run, not polling or POSTing to ${endpoint}.`);
      for (const [index, batch] of batches.entries()) {
        console.log(
          `[indexnow] Batch ${index + 1} of ${batches.length}:\n${JSON.stringify({ host: INDEXNOW_HOST, key, keyLocation, urlList: batch }, null, 2)}`,
        );
      }
      console.log(`::notice::IndexNow dry run: ${counts}.`);
      await writeSummary(env, `IndexNow dry run: ${counts}.`);
      return;
    }

    const live = await waitForLiveManifest({
      manifestUrl: env.INDEXNOW_LIVE_MANIFEST_URL || DEFAULT_LIVE_MANIFEST_URL,
      expected: current,
      intervalMs:
        positiveSeconds(env.INDEXNOW_POLL_INTERVAL_SECONDS, 20) * 1000,
      timeoutMs: positiveSeconds(env.INDEXNOW_POLL_TIMEOUT_SECONDS, 600) * 1000,
      fetchImpl,
      sleep,
      now,
    });
    if (!live) {
      warn(
        `The live manifest did not match the new build in time, skipping submission (${counts}).`,
      );
      await writeSummary(
        env,
        `IndexNow skipped: live manifest never matched (${counts}).`,
      );
      return;
    }

    const statuses = [];
    for (const [index, batch] of batches.entries()) {
      const response = await fetchImpl(endpoint, {
        method: "POST",
        headers: { "content-type": "application/json; charset=utf-8" },
        body: JSON.stringify({
          host: INDEXNOW_HOST,
          key,
          keyLocation,
          urlList: batch,
        }),
        signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
      });
      statuses.push(response.status);
      if (!response.ok) {
        const body = await response.text().catch(() => "");
        warn(
          `Batch ${index + 1} of ${batches.length} failed with HTTP ${response.status} ${body}. Skipping the remaining batches (${counts}).`,
        );
        await writeSummary(
          env,
          `IndexNow failed: HTTP ${response.status} (${counts}).`,
        );
        return;
      }
    }

    const outcome = `${counts}, ${batches.length} request${batches.length === 1 ? "" : "s"}, HTTP ${[...new Set(statuses)].join(", ")}`;
    console.log(`::notice::IndexNow submitted ${outcome}.`);
    await writeSummary(env, `IndexNow submitted ${outcome}.`);
  } catch (error) {
    warn(
      `Submission failed (${error?.cause?.code ?? error?.message ?? error}).`,
    );
    await writeSummary(env, "IndexNow failed, see the step log.");
  }
}

if (
  process.argv[1] &&
  import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href
) {
  await run();
}
