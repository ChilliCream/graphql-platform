import assert from "node:assert/strict";
import { mkdtemp, rm, writeFile } from "node:fs/promises";
import { createServer } from "node:http";
import os from "node:os";
import path from "node:path";
import { test } from "node:test";
import { hashPageContent } from "./generate-indexnow-manifest.mjs";
import {
  batchUrls,
  diffManifests,
  INDEXNOW_HOST,
  MAX_URLS_PER_REQUEST,
  parseManifest,
  run,
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

async function runAgainstMock({ status = 200, previous, current }) {
  const directory = await mkdtemp(path.join(os.tmpdir(), "indexnow-"));
  const mock = await withMock(status, "");
  const originalLog = console.log;
  const logs = [];
  console.log = (...args) => logs.push(args.join(" "));
  try {
    const manifestPath = path.join(directory, "new.json");
    await writeFile(manifestPath, JSON.stringify(current));
    const env = {
      INDEXNOW_MANIFEST: manifestPath,
      INDEXNOW_ENDPOINT: `${mock.origin}/indexnow`,
      INDEXNOW_LIVE_MANIFEST_URL: `${mock.origin}/indexnow-manifest.json`,
      INDEXNOW_POLL_INTERVAL_SECONDS: "0.01",
      INDEXNOW_POLL_TIMEOUT_SECONDS: "1",
    };
    if (previous) {
      const previousPath = path.join(directory, "previous.json");
      await writeFile(previousPath, JSON.stringify(previous));
      env.INDEXNOW_PREVIOUS_MANIFEST = previousPath;
    }
    mock.liveBody.current = JSON.stringify(current);
    await run(env);
    return {
      posts: mock.requests
        .filter((request) => request.method === "POST")
        .map((request) => JSON.parse(request.body)),
      logs,
    };
  } finally {
    console.log = originalLog;
    await mock.close();
    await rm(directory, { recursive: true, force: true });
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
