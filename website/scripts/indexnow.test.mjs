import assert from "node:assert/strict";
import { mkdtemp, readFile, rm, writeFile } from "node:fs/promises";
import { createServer } from "node:http";
import os from "node:os";
import path from "node:path";
import { test } from "node:test";
import { hashPageContent } from "./generate-indexnow-manifest.mjs";
import {
  batchUrls,
  diffManifests,
  extractSitemapUrls,
  fetchPreviousState,
  INDEXNOW_HOST,
  MAX_URLS_PER_REQUEST,
  parseManifest,
  run,
  runFetchPrevious,
  runSubmitAll,
  waitForLiveManifest,
} from "./indexnow-submit.mjs";

const url = (name) => `https://${INDEXNOW_HOST}/${name}`;

function page({
  title = "T",
  main = "<p>Body</p>",
  chrome = "Menu",
  build = "a",
}) {
  return `<html><head><title>${title}</title><meta name="description" content="D"/><link rel="canonical" href="https://chillicream.com/x"/><script src="/_next/static/${build}/app.js"></script></head><body><header>${chrome}</header><main>${main}<script>self.__next_f=["${build}"]</script></main><footer>${chrome}</footer></body></html>`;
}

test("diffManifests_Should_ReportAllUrlsAsAdded_When_NoPreviousManifest", () => {
  const result = diffManifests(null, { [url("b")]: "1", [url("a")]: "2" });

  assert.deepEqual(result, {
    added: [url("a"), url("b")],
    changed: [],
    removed: [],
  });
});

test("diffManifests_Should_ReportNothing_When_ManifestsMatch", () => {
  const manifest = { [url("a")]: "1", [url("b")]: "2" };

  assert.deepEqual(diffManifests({ ...manifest }, manifest), {
    added: [],
    changed: [],
    removed: [],
  });
});

test("diffManifests_Should_ReportAddedChangedAndRemoved_When_ManifestsDiffer", () => {
  const previous = {
    [url("same")]: "1",
    [url("edit")]: "1",
    [url("gone")]: "1",
  };
  const current = { [url("same")]: "1", [url("edit")]: "2", [url("new")]: "1" };

  assert.deepEqual(diffManifests(previous, current), {
    added: [url("new")],
    changed: [url("edit")],
    removed: [url("gone")],
  });
});

test("batchUrls_Should_SplitAtTenThousandUrls", () => {
  const sizes = (count) =>
    batchUrls(Array.from({ length: count }, (_, index) => `u${index}`)).map(
      (batch) => batch.length,
    );

  assert.equal(MAX_URLS_PER_REQUEST, 10_000);
  assert.deepEqual(sizes(0), []);
  assert.deepEqual(sizes(10_000), [10_000]);
  assert.deepEqual(sizes(10_001), [10_000, 1]);
  assert.deepEqual(sizes(25_000), [10_000, 10_000, 5_000]);
});

test("parseManifest_Should_Throw_When_ManifestIsInvalid", () => {
  for (const text of ["", "[]", "{}", '{"a":1}', "null"]) {
    assert.throws(() => parseManifest(text), text);
  }
  assert.deepEqual(parseManifest('{"a":"1"}'), { a: "1" });
});

test("hashPageContent_Should_IgnoreBuildNoiseAndChrome", () => {
  const baseline = hashPageContent(page({}));

  assert.equal(
    hashPageContent(page({ build: "b", chrome: "Other" })),
    baseline,
  );
});

test("hashPageContent_Should_Change_When_ContentChanges", () => {
  const baseline = hashPageContent(page({}));

  assert.notEqual(hashPageContent(page({ main: "<p>Bodz</p>" })), baseline);
  assert.notEqual(hashPageContent(page({ title: "U" })), baseline);
});

test("hashPageContent_Should_FallBackToBodyWithoutChrome_When_NoMain", () => {
  const html = (chrome) =>
    `<html><head><title>T</title></head><body><header>${chrome}</header><div>Text</div><footer>${chrome}</footer></body></html>`;

  assert.equal(hashPageContent(html("A")), hashPageContent(html("B")));
});

test("waitForLiveManifest_Should_ResolveTrue_When_LiveManifestEventuallyMatches", async () => {
  const expected = { [url("a")]: "2" };
  const bodies = [{ [url("a")]: "1" }, "not json", expected];
  let clock = 0;

  const live = await waitForLiveManifest({
    manifestUrl: url("indexnow-manifest.json"),
    expected,
    intervalMs: 10,
    timeoutMs: 1_000,
    fetchImpl: async () => {
      const body = bodies.shift();
      return new Response(
        typeof body === "string" ? body : JSON.stringify(body),
      );
    },
    sleep: async (ms) => {
      clock += ms;
    },
    now: () => clock,
  });

  assert.equal(live, true);
  assert.equal(bodies.length, 0);
});

test("waitForLiveManifest_Should_ResolveFalse_When_TimeoutElapses", async () => {
  let clock = 0;

  const live = await waitForLiveManifest({
    manifestUrl: url("indexnow-manifest.json"),
    expected: { [url("a")]: "2" },
    intervalMs: 100,
    timeoutMs: 450,
    fetchImpl: async () => new Response("{}", { status: 404 }),
    sleep: async (ms) => {
      clock += ms;
    },
    now: () => clock,
  });

  assert.equal(live, false);
  assert.equal(clock, 400);
});

async function withMock(status, body) {
  const requests = [];
  const server = createServer((request, response) => {
    const chunks = [];
    request.on("data", (chunk) => chunks.push(chunk));
    request.on("end", () => {
      const text = Buffer.concat(chunks).toString("utf8");
      requests.push({ method: request.method, body: text });
      if (request.method === "GET") {
        response.end(liveBody.current);
        return;
      }
      response.writeHead(status).end(body);
    });
  });
  const liveBody = { current: "{}" };
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
  const origin = `http://127.0.0.1:${server.address().port}`;
  return {
    origin,
    requests,
    liveBody,
    close: () => new Promise((resolve) => server.close(resolve)),
  };
}

async function captureLogs(action) {
  const originalLog = console.log;
  const logs = [];
  console.log = (...args) => logs.push(args.join(" "));
  try {
    await action();
  } finally {
    console.log = originalLog;
  }
  return logs;
}

function scriptedFetch(responses, sent) {
  const queue = [...responses];
  return async (input, init) => {
    if (init?.method !== "POST") {
      return fetch(input, init);
    }
    sent.push(init.body);
    const next = queue.shift();
    if (next === undefined) {
      throw new Error("no scripted response left");
    }
    if (next instanceof Error) {
      throw next;
    }
    return new Response(next.body ?? "", {
      status: next.status,
      headers: next.headers,
    });
  };
}

const SITE_VERIFICATION_PENDING = {
  status: 403,
  body: JSON.stringify({
    errorCode: "SiteVerificationNotCompleted",
    message: "Site verification is not completed.",
  }),
};

async function runAgainstMock({
  status = 200,
  responseBody = "",
  state,
  previous,
  current,
  fetchImpl,
  postResponses,
  now,
  env: extraEnv,
}) {
  const sleeps = [];
  const sleep = async (ms) => {
    sleeps.push(ms);
  };
  const directory = await mkdtemp(path.join(os.tmpdir(), "indexnow-"));
  const mock = await withMock(status, responseBody);
  try {
    const manifestPath = path.join(directory, "new.json");
    const statePath = path.join(directory, "state.json");
    const summaryPath = path.join(directory, "summary.md");
    await writeFile(manifestPath, JSON.stringify(current));
    if (previous) {
      await writeFile(
        statePath,
        JSON.stringify({ kind: "manifest", manifest: previous }),
      );
    } else if (state !== null) {
      await writeFile(
        statePath,
        JSON.stringify(state ?? { kind: "first-run" }),
      );
    }
    const env = {
      INDEXNOW_MANIFEST: manifestPath,
      INDEXNOW_PREVIOUS_STATE: statePath,
      GITHUB_STEP_SUMMARY: summaryPath,
      INDEXNOW_ENDPOINT: `${mock.origin}/indexnow`,
      INDEXNOW_LIVE_MANIFEST_URL: `${mock.origin}/indexnow-manifest.json`,
      INDEXNOW_POLL_INTERVAL_SECONDS: "0.01",
      INDEXNOW_POLL_TIMEOUT_SECONDS: "1",
      ...extraEnv,
    };
    mock.liveBody.current = JSON.stringify(current);
    const sent = [];
    if (postResponses) {
      fetchImpl = scriptedFetch(postResponses, sent);
    }
    const logs = await captureLogs(() => run(env, { fetchImpl, sleep, now }));
    const summary = await readFile(summaryPath, "utf8").catch(() => "");
    return {
      posts: postResponses
        ? sent.map((body) => JSON.parse(body))
        : mock.requests
            .filter((request) => request.method === "POST")
            .map((request) => JSON.parse(request.body)),
      logs,
      summary,
      statePath,
      sleeps,
    };
  } finally {
    await mock.close();
    await rm(directory, { recursive: true, force: true });
  }
}

async function fetchStateFromMock(status, body) {
  const server = createServer((request, response) => {
    response.writeHead(status).end(body);
  });
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
  try {
    return await fetchPreviousState({
      manifestUrl: `http://127.0.0.1:${server.address().port}/indexnow-manifest.json`,
      attempts: 2,
      retryDelayMs: 0,
    });
  } finally {
    await new Promise((resolve) => server.close(resolve));
  }
}

test("run_Should_PostChangedUrls_When_ManifestsDiffer", async () => {
  const { posts, logs } = await runAgainstMock({
    previous: { [url("edit")]: "1", [url("gone")]: "1" },
    current: { [url("edit")]: "2", [url("new")]: "1" },
  });

  assert.equal(posts.length, 1);
  assert.equal(posts[0].host, INDEXNOW_HOST);
  assert.equal(
    posts[0].keyLocation,
    `https://${INDEXNOW_HOST}/${posts[0].key}.txt`,
  );
  assert.deepEqual(posts[0].urlList, [url("new"), url("edit"), url("gone")]);
  assert.deepEqual(logs, [
    "::notice::IndexNow submitted 1 added, 1 changed, 1 removed, 1 request, HTTP 200, 0 retries.",
  ]);
});

test("run_Should_NotPost_When_NothingChanged", async () => {
  const manifest = { [url("a")]: "1" };

  const { posts } = await runAgainstMock({
    previous: manifest,
    current: manifest,
  });

  assert.deepEqual(posts, []);
});

test("run_Should_PostEveryUrlInBatches_When_NoPreviousManifest", async () => {
  const current = Object.fromEntries(
    Array.from({ length: 10_001 }, (_, index) => [url(`p${index}`), "1"]),
  );

  const { posts } = await runAgainstMock({ current });

  assert.deepEqual(
    posts.map((post) => post.urlList.length),
    [10_000, 1],
  );
});

test("run_Should_WarnAndNotThrow_When_EndpointRejects", async () => {
  const { posts, logs } = await runAgainstMock({
    status: 403,
    current: { [url("a")]: "1" },
  });

  assert.equal(posts.length, 1);
  assert.deepEqual(logs, [
    "::warning::IndexNow: Batch 1 of 1 failed with HTTP 403. Skipping the remaining batches (1 added, 0 changed, 0 removed (first run)).",
  ]);
});

test("run_Should_IncludeResponseBodyInWarning_When_EndpointRejectsWithBody", async () => {
  const { logs } = await runAgainstMock({
    status: 403,
    responseBody: "key not found",
    current: { [url("a")]: "1" },
  });

  assert.deepEqual(logs, [
    "::warning::IndexNow: Batch 1 of 1 failed with HTTP 403 key not found. Skipping the remaining batches (1 added, 0 changed, 0 removed (first run)).",
  ]);
});

test("run_Should_ReportCountsInSummary_When_EndpointRejects", async () => {
  const { summary } = await runAgainstMock({
    status: 403,
    previous: { [url("edit")]: "1", [url("gone")]: "1" },
    current: { [url("edit")]: "2", [url("new")]: "1" },
  });

  assert.equal(
    summary,
    "IndexNow failed: HTTP 403 at batch 1 of 1 (1 added, 1 changed, 1 removed), 0 retries.\n",
  );
});

test("run_Should_ReportCountsInWarningAndSummary_When_ErrorIsThrown", async () => {
  const { logs, summary } = await runAgainstMock({
    previous: { [url("gone")]: "1" },
    current: { [url("new")]: "1" },
    fetchImpl: (input, init) => {
      if (init?.method !== "POST") {
        return fetch(input, init);
      }
      throw new Error("boom");
    },
  });

  assert.deepEqual(logs, [
    "::warning::IndexNow: Batch 1 of 1 failed with a network error (boom) after 4 retries. Skipping the remaining batches (1 added, 0 changed, 1 removed).",
  ]);
  assert.equal(
    summary,
    "IndexNow failed: a network error (boom) at batch 1 of 1 (1 added, 0 changed, 1 removed), 4 retries.\n",
  );
});

test("run_Should_PostEveryUrl_When_StateIsFirstRun", async () => {
  const { posts, summary } = await runAgainstMock({
    state: { kind: "first-run" },
    current: { [url("a")]: "1", [url("b")]: "1" },
  });

  assert.deepEqual(posts[0].urlList, [url("a"), url("b")]);
  assert.equal(
    summary,
    "IndexNow submitted 2 added, 0 changed, 0 removed (first run), 1 request, HTTP 200, 0 retries.\n",
  );
});

test("run_Should_SkipWithWarning_When_StateFileIsMissing", async () => {
  const { posts, logs, summary, statePath } = await runAgainstMock({
    state: null,
    current: { [url("a")]: "1" },
  });

  const reason = `the previous state is missing or invalid, the pre-deploy fetch did not complete (ENOENT: no such file or directory, open '${statePath}')`;
  assert.deepEqual(posts, []);
  assert.deepEqual(logs, [
    `::warning::IndexNow: Skipping submission, no diff computed: ${reason}.`,
  ]);
  assert.equal(summary, `IndexNow skipped: no diff computed, ${reason}.\n`);
});

test("run_Should_SkipWithRecordedReason_When_StateIsSkip", async () => {
  const { posts, summary } = await runAgainstMock({
    state: {
      kind: "skip",
      reason: "the previous manifest request returned HTTP 503",
    },
    current: { [url("a")]: "1" },
  });

  assert.deepEqual(posts, []);
  assert.equal(
    summary,
    "IndexNow skipped: no diff computed, the previous manifest request returned HTTP 503.\n",
  );
});

test("fetchPreviousState_Should_ReturnFirstRun_When_Status404", async () => {
  assert.deepEqual(await fetchStateFromMock(404, "not found"), {
    kind: "first-run",
  });
});

test("fetchPreviousState_Should_ReturnSkip_When_ServerErrors", async () => {
  assert.deepEqual(await fetchStateFromMock(500, ""), {
    kind: "skip",
    reason: "the previous manifest request returned HTTP 500",
  });
  assert.deepEqual(await fetchStateFromMock(503, ""), {
    kind: "skip",
    reason: "the previous manifest request returned HTTP 503",
  });
});

test("fetchPreviousState_Should_ReturnSkip_When_ConnectionIsRefused", async () => {
  const server = createServer();
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
  const { port } = server.address();
  await new Promise((resolve) => server.close(resolve));

  const state = await fetchPreviousState({
    manifestUrl: `http://127.0.0.1:${port}/indexnow-manifest.json`,
    attempts: 2,
    retryDelayMs: 0,
  });

  assert.deepEqual(state, {
    kind: "skip",
    reason: "the previous manifest request failed (fetch failed, ECONNREFUSED)",
  });
});

test("fetchPreviousState_Should_ReturnSkip_When_BodyIsNotAManifest", async () => {
  assert.deepEqual(await fetchStateFromMock(200, "not json"), {
    kind: "skip",
    reason:
      "the previous manifest is invalid (Unexpected token 'o', \"not json\" is not valid JSON)",
  });
});

test("fetchPreviousState_Should_ReturnManifest_When_Status200", async () => {
  assert.deepEqual(await fetchStateFromMock(200, '{"a":"1"}'), {
    kind: "manifest",
    manifest: { a: "1" },
  });
});

test("fetchPreviousState_Should_Retry_When_FirstAttemptFails", async () => {
  const responses = [
    new Response("", { status: 502 }),
    new Response('{"a":"1"}'),
  ];

  const state = await fetchPreviousState({
    manifestUrl: url("indexnow-manifest.json"),
    fetchImpl: async () => responses.shift(),
    sleep: async () => {},
  });

  assert.deepEqual(state, { kind: "manifest", manifest: { a: "1" } });
});

test("runFetchPrevious_Should_PersistTheOutcome_When_ManifestIsFirstRun", async () => {
  const directory = await mkdtemp(path.join(os.tmpdir(), "indexnow-"));
  const statePath = path.join(directory, "state.json");
  try {
    await runFetchPrevious(
      { INDEXNOW_PREVIOUS_STATE: statePath },
      { fetchImpl: async () => new Response("", { status: 404 }) },
    );

    assert.deepEqual(JSON.parse(await readFile(statePath, "utf8")), {
      kind: "first-run",
    });
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
});

const FIRST_RUN = { current: { [url("a")]: "1" } };
const ONE_URL_COUNTS = "1 added, 0 changed, 0 removed (first run)";

test("run_Should_SucceedAfterRetry_When_SiteVerificationNotCompletedThenOk", async () => {
  const { posts, logs, summary, sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    postResponses: [SITE_VERIFICATION_PENDING, { status: 200 }],
  });

  assert.equal(posts.length, 2);
  assert.deepEqual(sleeps, [30_000]);
  assert.deepEqual(logs, [
    `::notice::IndexNow submitted ${ONE_URL_COUNTS}, 1 request, HTTP 200, 1 retry.`,
  ]);
  assert.equal(
    summary,
    `IndexNow submitted ${ONE_URL_COUNTS}, 1 request, HTTP 200, 1 retry.\n`,
  );
});

test("run_Should_GiveUpWithWarning_When_SiteVerificationNeverCompletes", async () => {
  const { posts, logs, summary, sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    postResponses: Array.from({ length: 5 }, () => SITE_VERIFICATION_PENDING),
  });

  assert.equal(posts.length, 5);
  assert.deepEqual(sleeps, [30_000, 60_000, 120_000, 240_000]);
  assert.deepEqual(logs, [
    `::warning::IndexNow: Batch 1 of 1 failed with HTTP 403 ${SITE_VERIFICATION_PENDING.body} after 4 retries. Skipping the remaining batches (${ONE_URL_COUNTS}).`,
  ]);
  assert.equal(
    summary,
    `IndexNow failed: HTTP 403 at batch 1 of 1 (${ONE_URL_COUNTS}), 4 retries.\n`,
  );
});

test("run_Should_WaitRetryAfterSeconds_When_Status429", async () => {
  const { logs, sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    postResponses: [
      { status: 429, headers: { "retry-after": "7" } },
      { status: 200 },
    ],
  });

  assert.deepEqual(sleeps, [7_000]);
  assert.deepEqual(logs, [
    `::notice::IndexNow submitted ${ONE_URL_COUNTS}, 1 request, HTTP 200, 1 retry.`,
  ]);
});

test("run_Should_WaitUntilRetryAfterDate_When_Status429", async () => {
  const start = Date.parse("2026-11-11T12:00:00Z");

  const { sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    now: () => start,
    postResponses: [
      {
        status: 429,
        headers: { "retry-after": new Date(start + 12_000).toUTCString() },
      },
      { status: 200 },
    ],
  });

  assert.deepEqual(sleeps, [12_000]);
});

test("run_Should_CapRetryAfter_When_ItExceedsTheLongestBackoff", async () => {
  const { sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    postResponses: [
      { status: 429, headers: { "retry-after": "9999" } },
      { status: 200 },
    ],
  });

  assert.deepEqual(sleeps, [240_000]);
});

test("run_Should_UseBackoff_When_429HasNoUsableRetryAfter", async () => {
  const { sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    postResponses: [
      { status: 429, headers: { "retry-after": "soon" } },
      { status: 429 },
      { status: 200 },
    ],
  });

  assert.deepEqual(sleeps, [30_000, 60_000]);
});

test("run_Should_SucceedAfterRetry_When_ServerErrorThenOk", async () => {
  const { posts, logs, sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    postResponses: [{ status: 500 }, { status: 200 }],
  });

  assert.equal(posts.length, 2);
  assert.deepEqual(sleeps, [30_000]);
  assert.deepEqual(logs, [
    `::notice::IndexNow submitted ${ONE_URL_COUNTS}, 1 request, HTTP 200, 1 retry.`,
  ]);
});

test("run_Should_SucceedAfterRetry_When_NetworkErrorThenOk", async () => {
  const { posts, logs, sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    postResponses: [new Error("socket hang up"), { status: 200 }],
  });

  assert.equal(posts.length, 2);
  assert.deepEqual(sleeps, [30_000]);
  assert.deepEqual(logs, [
    `::notice::IndexNow submitted ${ONE_URL_COUNTS}, 1 request, HTTP 200, 1 retry.`,
  ]);
});

test("run_Should_NotRetry_When_PlainForbidden", async () => {
  const { posts, logs, sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    postResponses: [{ status: 403, body: "Forbidden" }, { status: 200 }],
  });

  assert.equal(posts.length, 1);
  assert.deepEqual(sleeps, []);
  assert.deepEqual(logs, [
    `::warning::IndexNow: Batch 1 of 1 failed with HTTP 403 Forbidden. Skipping the remaining batches (${ONE_URL_COUNTS}).`,
  ]);
});

test("run_Should_NotRetry_When_ForbiddenBodyIsNotJson", async () => {
  const { posts, sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    postResponses: [{ status: 403, body: "SiteVerificationNotCompleted" }],
  });

  assert.equal(posts.length, 1);
  assert.deepEqual(sleeps, []);
});

test("run_Should_NotRetry_When_Status422", async () => {
  const { posts, sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    postResponses: [{ status: 422 }, { status: 200 }],
  });

  assert.equal(posts.length, 1);
  assert.deepEqual(sleeps, []);
});

test("run_Should_ScaleBackoff_When_RetryBaseDelayIsConfigured", async () => {
  const { sleeps } = await runAgainstMock({
    ...FIRST_RUN,
    env: { INDEXNOW_RETRY_BASE_DELAY_SECONDS: "0.5" },
    postResponses: [{ status: 500 }, { status: 500 }, { status: 200 }],
  });

  assert.deepEqual(sleeps, [500, 1_000]);
});

test("run_Should_CountRetriesAcrossBatches_When_EachBatchRetries", async () => {
  const current = Object.fromEntries(
    Array.from({ length: 10_001 }, (_, index) => [url(`p${index}`), "1"]),
  );

  const { posts, logs } = await runAgainstMock({
    current,
    postResponses: [
      { status: 500 },
      { status: 200 },
      { status: 503 },
      { status: 200 },
    ],
  });

  assert.equal(posts.length, 4);
  assert.deepEqual(logs, [
    "::notice::IndexNow submitted 10001 added, 0 changed, 0 removed (first run), 2 requests, HTTP 200, 2 retries.",
  ]);
});

test("run_Should_ReportOwnRetriesPerBatchAndTotalInSummary_When_LaterBatchFails", async () => {
  const current = Object.fromEntries(
    Array.from({ length: 10_001 }, (_, index) => [url(`p${index}`), "1"]),
  );

  const { posts, logs, summary } = await runAgainstMock({
    current,
    postResponses: [
      SITE_VERIFICATION_PENDING,
      SITE_VERIFICATION_PENDING,
      { status: 200 },
      { status: 403, body: "Forbidden" },
    ],
  });

  assert.equal(posts.length, 4);
  assert.deepEqual(logs, [
    "::warning::IndexNow: Batch 2 of 2 failed with HTTP 403 Forbidden. Skipping the remaining batches (10001 added, 0 changed, 0 removed (first run)).",
  ]);
  assert.equal(
    summary,
    "IndexNow failed: HTTP 403 at batch 2 of 2 (10001 added, 0 changed, 0 removed (first run)), 2 retries.\n",
  );
});

test("extractSitemapUrls_Should_ReturnUniqueLocs_When_XmlHasWhitespaceAndEntities", () => {
  const xml = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
<url>
<loc>https://chillicream.com/</loc>
</url>
<url><loc>
  https://chillicream.com/a?x=1&amp;y=2
</loc><lastmod>2026-01-01</lastmod></url>
<url><loc>https://chillicream.com/</loc></url>
<url><loc></loc></url>
</urlset>`;

  assert.deepEqual(extractSitemapUrls(xml), [
    "https://chillicream.com/",
    "https://chillicream.com/a?x=1&y=2",
  ]);
  assert.deepEqual(extractSitemapUrls("<urlset></urlset>"), []);
});

function sitemapXml(urls) {
  return `<?xml version="1.0"?><urlset>${urls.map((u) => `<url><loc>${u}</loc></url>`).join("\n")}</urlset>`;
}

async function runSubmitAllAgainst({
  sitemap,
  postResponses = [],
  env: extraEnv,
}) {
  const directory = await mkdtemp(path.join(os.tmpdir(), "indexnow-"));
  try {
    const summaryPath = path.join(directory, "summary.md");
    const sleeps = [];
    const sent = [];
    const scripted = scriptedFetch(postResponses, sent);
    const fetchImpl = async (input, init) => {
      if (init?.method === "POST") {
        return scripted(input, init);
      }
      assert.equal(String(input), "https://chillicream.com/sitemap.xml");
      if (sitemap instanceof Error) {
        throw sitemap;
      }
      return new Response(sitemap.body, { status: sitemap.status ?? 200 });
    };
    let ok;
    const logs = await captureLogs(async () => {
      ok = await runSubmitAll(
        { GITHUB_STEP_SUMMARY: summaryPath, ...extraEnv },
        {
          fetchImpl,
          sleep: async (ms) => {
            sleeps.push(ms);
          },
        },
      );
    });
    return {
      ok,
      logs,
      sleeps,
      posts: sent.map((body) => JSON.parse(body)),
      summary: await readFile(summaryPath, "utf8").catch(() => ""),
    };
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
}

test("runSubmitAll_Should_PostEverySitemapUrl_When_SitemapIsValid", async () => {
  const { ok, posts, logs, summary } = await runSubmitAllAgainst({
    sitemap: { body: sitemapXml([url("a"), url("b")]) },
    postResponses: [{ status: 200 }],
  });

  assert.equal(ok, true);
  assert.equal(posts.length, 1);
  assert.equal(posts[0].host, INDEXNOW_HOST);
  assert.deepEqual(posts[0].urlList, [url("a"), url("b")]);
  assert.deepEqual(logs, [
    "::notice::IndexNow submitted 2 URLs, 1 request, HTTP 200, 0 retries.",
  ]);
  assert.equal(
    summary,
    "IndexNow submitted 2 URLs, 1 request, HTTP 200, 0 retries.\n",
  );
});

test("runSubmitAll_Should_PostInBatches_When_SitemapExceedsTheBatchSize", async () => {
  const urls = Array.from({ length: 10_001 }, (_, index) => url(`p${index}`));

  const { posts, summary } = await runSubmitAllAgainst({
    sitemap: { body: sitemapXml(urls) },
    postResponses: [{ status: 200 }, { status: 200 }],
  });

  assert.deepEqual(
    posts.map((post) => post.urlList.length),
    [10_000, 1],
  );
  assert.equal(
    summary,
    "IndexNow submitted 10001 URLs, 2 requests, HTTP 200, 0 retries.\n",
  );
});

test("runSubmitAll_Should_RetryAndSucceed_When_SiteVerificationNotCompletedThenOk", async () => {
  const { ok, posts, sleeps, logs } = await runSubmitAllAgainst({
    sitemap: { body: sitemapXml([url("a")]) },
    postResponses: [SITE_VERIFICATION_PENDING, { status: 200 }],
  });

  assert.equal(ok, true);
  assert.equal(posts.length, 2);
  assert.deepEqual(sleeps, [30_000]);
  assert.deepEqual(logs, [
    "::notice::IndexNow submitted 1 URL, 1 request, HTTP 200, 1 retry.",
  ]);
});

test("runSubmitAll_Should_WarnAndSucceed_When_IndexNowKeepsRejecting", async () => {
  const { ok, logs, summary } = await runSubmitAllAgainst({
    sitemap: { body: sitemapXml([url("a")]) },
    postResponses: [{ status: 403, body: "Forbidden" }],
  });

  assert.equal(ok, true);
  assert.deepEqual(logs, [
    "::warning::IndexNow: Batch 1 of 1 failed with HTTP 403 Forbidden. Skipping the remaining batches (1 URL).",
  ]);
  assert.equal(
    summary,
    "IndexNow failed: HTTP 403 at batch 1 of 1 (1 URL), 0 retries.\n",
  );
});

test("runSubmitAll_Should_NotPost_When_DryRun", async () => {
  const { ok, posts, logs, summary } = await runSubmitAllAgainst({
    sitemap: { body: sitemapXml([url("a")]) },
    env: { INDEXNOW_DRY_RUN: "true" },
  });

  assert.equal(ok, true);
  assert.deepEqual(posts, []);
  assert.deepEqual(
    logs.at(-1),
    "::notice::IndexNow dry run: 1 URL, 1 request.",
  );
  assert.equal(summary, "IndexNow dry run: 1 URL, 1 request.\n");
});

test("runSubmitAll_Should_Fail_When_SitemapRequestReturnsErrorStatus", async () => {
  const { ok, posts, logs, summary } = await runSubmitAllAgainst({
    sitemap: { status: 503, body: "" },
  });

  assert.equal(ok, false);
  assert.deepEqual(posts, []);
  assert.deepEqual(logs, [
    "::error::IndexNow: Could not read the sitemap (the sitemap request returned HTTP 503).",
  ]);
  assert.equal(
    summary,
    "IndexNow failed: Could not read the sitemap (the sitemap request returned HTTP 503).\n",
  );
});

test("runSubmitAll_Should_Fail_When_SitemapRequestThrows", async () => {
  const { ok, logs } = await runSubmitAllAgainst({
    sitemap: new Error("getaddrinfo ENOTFOUND"),
  });

  assert.equal(ok, false);
  assert.deepEqual(logs, [
    "::error::IndexNow: Could not read the sitemap (getaddrinfo ENOTFOUND).",
  ]);
});

test("runSubmitAll_Should_Fail_When_SitemapHasNoUrls", async () => {
  const { ok, posts, logs } = await runSubmitAllAgainst({
    sitemap: { body: "<urlset></urlset>" },
  });

  assert.equal(ok, false);
  assert.deepEqual(posts, []);
  assert.deepEqual(logs, [
    "::error::IndexNow: Could not read the sitemap (the sitemap contains no URLs).",
  ]);
});

test("runSubmitAll_Should_SkipForeignHosts_When_SitemapListsThem", async () => {
  const { posts, logs } = await runSubmitAllAgainst({
    sitemap: { body: sitemapXml([url("a"), "https://example.com/x"]) },
    postResponses: [{ status: 200 }],
  });

  assert.deepEqual(posts[0].urlList, [url("a")]);
  assert.deepEqual(logs, [
    "::warning::IndexNow: Skipping URLs that are not on chillicream.com.",
    "::notice::IndexNow submitted 1 URL, 1 request, HTTP 200, 0 retries.",
  ]);
});
