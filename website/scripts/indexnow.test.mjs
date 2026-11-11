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
  fetchPreviousState,
  INDEXNOW_HOST,
  MAX_URLS_PER_REQUEST,
  parseManifest,
  run,
  runFetchPrevious,
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

async function runAgainstMock({
  status = 200,
  state,
  previous,
  current,
  fetchImpl,
}) {
  const directory = await mkdtemp(path.join(os.tmpdir(), "indexnow-"));
  const mock = await withMock(status, "");
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
    };
    mock.liveBody.current = JSON.stringify(current);
    const logs = await captureLogs(() => run(env, { fetchImpl }));
    const summary = await readFile(summaryPath, "utf8").catch(() => "");
    return {
      posts: mock.requests
        .filter((request) => request.method === "POST")
        .map((request) => JSON.parse(request.body)),
      logs,
      summary,
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
  assert.ok(logs.some((line) => line.startsWith("::notice::")));
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
  assert.ok(logs.some((line) => line.startsWith("::warning::IndexNow:")));
});

test("run_Should_ReportCountsInSummary_When_EndpointRejects", async () => {
  const { summary } = await runAgainstMock({
    status: 403,
    previous: { [url("edit")]: "1", [url("gone")]: "1" },
    current: { [url("edit")]: "2", [url("new")]: "1" },
  });

  assert.equal(
    summary,
    "IndexNow failed: HTTP 403 (1 added, 1 changed, 1 removed).\n",
  );
});

test("run_Should_ReportCountsInWarningAndSummary_When_ErrorIsThrown", async () => {
  const { logs, summary } = await runAgainstMock({
    previous: { [url("gone")]: "1" },
    current: { [url("new")]: "1" },
    fetchImpl: (input, init) => {
      if (init?.method === "POST") {
        throw new Error("boom");
      }
      return fetch(input, init);
    },
  });

  assert.deepEqual(
    logs.filter((line) => line.startsWith("::warning::")),
    [
      "::warning::IndexNow: Submission failed (boom) (1 added, 0 changed, 1 removed).",
    ],
  );
  assert.equal(
    summary,
    "IndexNow failed: boom (1 added, 0 changed, 1 removed).\n",
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
    "IndexNow submitted 2 added, 0 changed, 0 removed (first run), 1 request, HTTP 200.\n",
  );
});

test("run_Should_SkipWithWarning_When_StateFileIsMissing", async () => {
  const { posts, logs, summary } = await runAgainstMock({
    state: null,
    current: { [url("a")]: "1" },
  });

  assert.deepEqual(posts, []);
  assert.equal(logs.filter((line) => line.startsWith("::warning::")).length, 1);
  assert.ok(
    logs[0].startsWith(
      "::warning::IndexNow: Skipping submission, no diff computed: the previous state is missing or invalid",
    ),
  );
  assert.ok(
    summary.startsWith(
      "IndexNow skipped: no diff computed, the previous state is missing or invalid",
    ),
  );
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
