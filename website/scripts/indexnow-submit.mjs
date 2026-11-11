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
const DEFAULT_SITEMAP_URL = `https://${INDEXNOW_HOST}/sitemap.xml`;
const REQUEST_TIMEOUT_MS = 30_000;
const MAX_RETRIES = 4;
const DEFAULT_RETRY_BASE_DELAY_SECONDS = 30;
const MAX_RETRY_AFTER_MS = 240_000;

const defaultSleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

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

function describeError(error) {
  const cause = error?.cause;
  const detail = cause?.code ?? cause?.errors?.[0]?.code ?? cause?.message;
  return detail ? `${error.message}, ${detail}` : (error?.message ?? error);
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

/**
 * Fetches the manifest of the live site before a deploy and classifies the
 * outcome. Only an HTTP 404 is a first run. A network error, any other
 * non-200 status or an invalid manifest is a skip with the reason.
 */
export async function fetchPreviousState({
  manifestUrl,
  fetchImpl = fetch,
  sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms)),
  attempts = 3,
  retryDelayMs = 5_000,
}) {
  let reason = "the previous manifest was never fetched";

  for (let attempt = 1; attempt <= attempts; attempt++) {
    if (attempt > 1) {
      await sleep(retryDelayMs);
    }
    try {
      const url = new URL(manifestUrl);
      url.searchParams.set("cache-bust", `${Date.now()}-${attempt}`);
      const response = await fetchImpl(url, {
        headers: { "cache-control": "no-cache" },
        signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
      });
      if (response.status === 404) {
        return { kind: "first-run" };
      }
      if (response.status !== 200) {
        reason = `the previous manifest request returned HTTP ${response.status}`;
        continue;
      }
      try {
        return {
          kind: "manifest",
          manifest: parseManifest(await response.text()),
        };
      } catch (error) {
        return {
          kind: "skip",
          reason: `the previous manifest is invalid (${error.message})`,
        };
      }
    } catch (error) {
      reason = `the previous manifest request failed (${describeError(error)})`;
    }
  }

  return { kind: "skip", reason };
}

/**
 * Fetches the previous manifest and persists the outcome to
 * `INDEXNOW_PREVIOUS_STATE` for the submit step. Never throws.
 */
export async function runFetchPrevious(env = process.env, options = {}) {
  const statePath = env.INDEXNOW_PREVIOUS_STATE;
  try {
    if (!statePath) {
      throw new Error("INDEXNOW_PREVIOUS_STATE is not set");
    }
    await fs.rm(statePath, { force: true });
    const retryDelay = Number(env.INDEXNOW_FETCH_RETRY_DELAY_SECONDS);
    const state = await fetchPreviousState({
      manifestUrl: env.INDEXNOW_LIVE_MANIFEST_URL || DEFAULT_LIVE_MANIFEST_URL,
      ...(Number.isFinite(retryDelay) && retryDelay >= 0
        ? { retryDelayMs: retryDelay * 1000 }
        : {}),
      ...options,
    });
    await fs.writeFile(statePath, JSON.stringify(state), "utf8");
    if (state.kind === "skip") {
      warn(`The next submit will be skipped, ${state.reason}.`);
    } else {
      console.log(
        `::notice::IndexNow: previous state is ${state.kind === "first-run" ? "a first run (HTTP 404)" : `a manifest with ${Object.keys(state.manifest).length} URLs`}.`,
      );
    }
  } catch (error) {
    warn(`Could not record the previous manifest (${describeError(error)}).`);
  }
}

async function readPreviousState(env) {
  const statePath = env.INDEXNOW_PREVIOUS_STATE;
  if (!statePath) {
    return { kind: "skip", reason: "no previous state file is configured" };
  }
  try {
    const state = JSON.parse(await fs.readFile(statePath, "utf8"));
    if (state?.kind === "first-run") {
      return { kind: "first-run" };
    }
    if (state?.kind === "manifest") {
      return {
        kind: "manifest",
        manifest: parseManifest(JSON.stringify(state.manifest)),
      };
    }
    if (state?.kind === "skip" && typeof state.reason === "string") {
      return state;
    }
    throw new Error("unknown state");
  } catch (error) {
    return {
      kind: "skip",
      reason: `the previous state is missing or invalid, the pre-deploy fetch did not complete (${error.message})`,
    };
  }
}

function positiveSeconds(value, fallback) {
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

function plural(count, noun) {
  if (count === 1) {
    return `${count} ${noun}`;
  }
  return `${count} ${noun.endsWith("y") ? `${noun.slice(0, -1)}ies` : `${noun}s`}`;
}

function parseRetryAfterMs(value, now) {
  const text = value?.trim();
  if (!text) {
    return null;
  }
  if (/^\d+$/.test(text)) {
    return Number(text) * 1000;
  }
  const date = Date.parse(text);
  return Number.isNaN(date) ? null : Math.max(0, date - now());
}

function isSiteVerificationPending(body) {
  try {
    return JSON.parse(body)?.errorCode === "SiteVerificationNotCompleted";
  } catch {
    return false;
  }
}

function isRetryable(status, body) {
  return (
    status === 429 ||
    status >= 500 ||
    (status === 403 && isSiteVerificationPending(body))
  );
}

/**
 * Posts one batch, retrying on 403 SiteVerificationNotCompleted, 429, 5xx and
 * network errors with a doubling backoff. Returns the final status or error,
 * the response body and the number of retries used.
 */
async function postBatch({
  endpoint,
  payload,
  fetchImpl,
  sleep,
  now,
  baseDelayMs,
}) {
  let retries = 0;
  for (;;) {
    let outcome;
    try {
      const response = await fetchImpl(endpoint, {
        method: "POST",
        headers: { "content-type": "application/json; charset=utf-8" },
        body: payload,
        signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
      });
      const body = response.ok
        ? ""
        : (await response.text().catch(() => "")).trim();
      outcome = {
        status: response.status,
        ok: response.ok,
        body,
        retryable: !response.ok && isRetryable(response.status, body),
        retryAfterMs: parseRetryAfterMs(
          response.headers.get("retry-after"),
          now,
        ),
      };
    } catch (error) {
      outcome = {
        ok: false,
        error: describeError(error),
        retryable: true,
        retryAfterMs: null,
      };
    }

    if (outcome.ok || !outcome.retryable || retries >= MAX_RETRIES) {
      return { ...outcome, retries };
    }
    const backoffMs = baseDelayMs * 2 ** retries;
    await sleep(
      outcome.retryAfterMs === null
        ? backoffMs
        : Math.min(outcome.retryAfterMs, MAX_RETRY_AFTER_MS),
    );
    retries++;
  }
}

/**
 * Submits `urlList` to IndexNow in batches and reports the outcome as workflow
 * annotations and a summary line. `beforeSend` returns false to skip sending.
 * Never throws on an IndexNow failure.
 */
async function deliver({
  urlList,
  counts,
  env,
  fetchImpl,
  sleep = defaultSleep,
  now = Date.now,
  beforeSend,
}) {
  const dryRun = /^(1|true)$/i.test(env.INDEXNOW_DRY_RUN ?? "");
  const endpoint = env.INDEXNOW_ENDPOINT || DEFAULT_ENDPOINT;
  const key = await readKey(path.join(PROJECT_ROOT, "public"));
  const keyLocation = `https://${INDEXNOW_HOST}/${key}.txt`;
  const batches = batchUrls(urlList);
  const requests = plural(batches.length, "request");

  if (dryRun) {
    console.log(`[indexnow] Dry run, not polling or POSTing to ${endpoint}.`);
    for (const [index, batch] of batches.entries()) {
      console.log(
        `[indexnow] Batch ${index + 1} of ${batches.length}:\n${JSON.stringify({ host: INDEXNOW_HOST, key, keyLocation, urlList: batch }, null, 2)}`,
      );
    }
    console.log(`::notice::IndexNow dry run: ${counts}, ${requests}.`);
    await writeSummary(env, `IndexNow dry run: ${counts}, ${requests}.`);
    return;
  }

  if (beforeSend && !(await beforeSend())) {
    return;
  }

  const baseDelayMs =
    positiveSeconds(
      env.INDEXNOW_RETRY_BASE_DELAY_SECONDS,
      DEFAULT_RETRY_BASE_DELAY_SECONDS,
    ) * 1000;
  const statuses = [];
  let retries = 0;
  for (const [index, batch] of batches.entries()) {
    const result = await postBatch({
      endpoint,
      payload: JSON.stringify({
        host: INDEXNOW_HOST,
        key,
        keyLocation,
        urlList: batch,
      }),
      fetchImpl,
      sleep,
      now,
      baseDelayMs,
    });
    retries += result.retries;
    if (!result.ok) {
      const batchRetries = result.retries;
      const failure = result.error
        ? `a network error (${result.error})`
        : `HTTP ${result.status}`;
      const detail = result.body ? ` ${result.body}` : "";
      warn(
        `Batch ${index + 1} of ${batches.length} failed with ${failure}${detail}${batchRetries > 0 ? ` after ${plural(batchRetries, "retry")}` : ""}. Skipping the remaining batches (${counts}).`,
      );
      await writeSummary(
        env,
        `IndexNow failed: ${failure} at batch ${index + 1} of ${batches.length} (${counts}), ${plural(retries, "retry")}.`,
      );
      return;
    }
    statuses.push(result.status);
  }

  const outcome = `${counts}, ${requests}, HTTP ${[...new Set(statuses)].join(", ")}, ${plural(retries, "retry")}`;
  console.log(`::notice::IndexNow submitted ${outcome}.`);
  await writeSummary(env, `IndexNow submitted ${outcome}.`);
}

/**
 * Diffs the previous and new manifests and submits the changed URLs to
 * IndexNow. Reports every failure as a workflow warning and never throws.
 */
export async function run(env = process.env, options = {}) {
  const { fetchImpl = fetch, sleep, now } = options;
  let counts = "no diff computed";
  try {
    const manifestPath = env.INDEXNOW_MANIFEST || DEFAULT_MANIFEST_PATH;

    const state = await readPreviousState(env);
    if (state.kind === "skip") {
      warn(`Skipping submission, no diff computed: ${state.reason}.`);
      await writeSummary(
        env,
        `IndexNow skipped: no diff computed, ${state.reason}.`,
      );
      return;
    }

    const current = parseManifest(await fs.readFile(manifestPath, "utf8"));
    const previous = state.kind === "manifest" ? state.manifest : null;
    const { added, changed, removed } = diffManifests(previous, current);
    counts = `${added.length} added, ${changed.length} changed, ${removed.length} removed${previous ? "" : " (first run)"}`;

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

    await deliver({
      urlList,
      counts,
      env,
      fetchImpl,
      sleep,
      beforeSend: async () => {
        const live = await waitForLiveManifest({
          manifestUrl:
            env.INDEXNOW_LIVE_MANIFEST_URL || DEFAULT_LIVE_MANIFEST_URL,
          expected: current,
          intervalMs:
            positiveSeconds(env.INDEXNOW_POLL_INTERVAL_SECONDS, 20) * 1000,
          timeoutMs:
            positiveSeconds(env.INDEXNOW_POLL_TIMEOUT_SECONDS, 600) * 1000,
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
        }
        return live;
      },
      now,
    });
  } catch (error) {
    warn(`Submission failed (${describeError(error)}) (${counts}).`);
    await writeSummary(
      env,
      `IndexNow failed: ${oneLine(describeError(error))} (${counts}).`,
    );
  }
}

/** Extracts the unique `<loc>` URLs of a sitemap document in document order. */
export function extractSitemapUrls(xml) {
  const urls = new Set();
  for (const match of xml.matchAll(/<loc>\s*([^<]*?)\s*<\/loc>/g)) {
    const url = match[1]
      .replaceAll("&lt;", "<")
      .replaceAll("&gt;", ">")
      .replaceAll("&quot;", '"')
      .replaceAll("&apos;", "'")
      .replaceAll("&amp;", "&");
    if (url) {
      urls.add(url);
    }
  }
  return [...urls];
}

/**
 * Submits every URL of the live sitemap to IndexNow. Returns false, after an
 * error annotation, when the sitemap cannot be fetched or has no URLs. An
 * IndexNow failure is a warning and still returns true.
 */
export async function runSubmitAll(env = process.env, options = {}) {
  const { fetchImpl = fetch, sleep, now } = options;
  let urls;
  try {
    const sitemapUrl = env.INDEXNOW_SITEMAP_URL || DEFAULT_SITEMAP_URL;
    const response = await fetchImpl(sitemapUrl, {
      signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    });
    if (!response.ok) {
      throw new Error(`the sitemap request returned HTTP ${response.status}`);
    }
    urls = extractSitemapUrls(await response.text());
    if (urls.length === 0) {
      throw new Error("the sitemap contains no URLs");
    }
  } catch (error) {
    const message = `Could not read the sitemap (${describeError(error)}).`;
    console.log(`::error::IndexNow: ${oneLine(message)}`);
    await writeSummary(env, `IndexNow failed: ${oneLine(message)}`);
    return false;
  }

  const counts = plural(urls.length, "URL");
  try {
    const urlList = urls.filter((url) => {
      try {
        return new URL(url).hostname === INDEXNOW_HOST;
      } catch {
        return false;
      }
    });
    if (urlList.length !== urls.length) {
      warn(`Skipping URLs that are not on ${INDEXNOW_HOST}.`);
    }
    if (urlList.length === 0) {
      warn(`No sitemap URLs are on ${INDEXNOW_HOST}, nothing to submit.`);
      await writeSummary(env, `IndexNow: nothing to submit (${counts}).`);
      return true;
    }
    await deliver({
      urlList,
      counts: plural(urlList.length, "URL"),
      env,
      fetchImpl,
      sleep,
      now,
    });
  } catch (error) {
    warn(`Submission failed (${describeError(error)}) (${counts}).`);
    await writeSummary(
      env,
      `IndexNow failed: ${oneLine(describeError(error))} (${counts}).`,
    );
  }
  return true;
}

if (
  process.argv[1] &&
  import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href
) {
  if (process.argv[2] === "fetch-previous") {
    await runFetchPrevious();
  } else if (process.argv[2] === "submit-all") {
    process.exitCode = (await runSubmitAll()) ? 0 : 1;
  } else {
    await run();
  }
}
