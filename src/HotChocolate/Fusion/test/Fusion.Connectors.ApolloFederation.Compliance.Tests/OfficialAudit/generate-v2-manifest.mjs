import { execFileSync, spawn } from "node:child_process";
import { createHash } from "node:crypto";
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const expectedRevision = "f59c05e3f48f4a4f8e8a731d67a1b71a9788a96f";
const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const auditDirectory = process.argv[2]
  ? resolve(process.argv[2])
  : resolve(scriptDirectory, "../../../../../../../../federation-gateway-audit");
const outputPath = resolve(scriptDirectory, "v2-manifest.json");
const deterministicRuntime = pathToFileURL(
  join(scriptDirectory, "official-audit-deterministic-runtime.mjs"),
).href;
const port = Number(process.argv[3] ?? 4217);

const revision = execFileSync("git", ["rev-parse", "HEAD"], {
  cwd: auditDirectory,
  encoding: "utf8",
}).trim();

if (revision !== expectedRevision) {
  throw new Error(
    `Expected federation-gateway-audit revision ${expectedRevision}, but found ${revision}.`,
  );
}

const server = spawn(
  join(auditDirectory, "node_modules", ".bin", "tsx"),
  [join(auditDirectory, "src", "cli.ts"), "serve", "--port", String(port)],
  {
    cwd: auditDirectory,
    env: {
      ...process.env,
      NODE_OPTIONS: [
        process.env.NODE_OPTIONS,
        `--import=${deterministicRuntime}`,
      ]
        .filter(Boolean)
        .join(" "),
    },
    stdio: ["ignore", "pipe", "inherit"],
  },
);

try {
  await waitForServer(server, `http://localhost:${port}/_health`);

  const ids = await fetchJson(`http://localhost:${port}/ids`);
  const suites = [];
  const excludedV1DependentSuites = [];

  for (const id of ids) {
    const [tests, subgraphs] = await Promise.all([
      fetchJson(`http://localhost:${port}/${id}/tests`),
      fetchJson(`http://localhost:${port}/${id}/subgraphs`),
    ]);
    const v1Sources = subgraphs
      .filter(({ sdl }) => isFederationV1Source(sdl))
      .map(({ name }) => name);

    if (v1Sources.length > 0) {
      excludedV1DependentSuites.push({ id, caseCount: tests.length, v1Sources });
      continue;
    }

    const suiteDirectory = join(auditDirectory, "src", "test-suites", id);
    const sources = await Promise.all(
      subgraphs.map(async ({ name, sdl, url }) => ({
        name,
        rawSdl: sdl,
        serviceSdl: await fetchServiceSdl(url),
      })),
    );
    const fixtureModules = readdirSync(suiteDirectory)
      .filter((file) => file === "data.ts" || file.endsWith(".subgraph.ts"))
      .sort()
      .map((file) => {
        const source = readFileSync(join(suiteDirectory, file), "utf8");

        return {
          path: file,
          sha256: createHash("sha256").update(source).digest("hex"),
          source,
        };
      });

    suites.push({
      id,
      cases: tests.map((test, index) => ({
        id: `${id}/${String(index).padStart(3, "0")}`,
        query: test.query,
        variables: test.variables ?? null,
        hasExpectedData: Object.hasOwn(test.expected, "data"),
        expectedData: Object.hasOwn(test.expected, "data")
          ? test.expected.data
          : null,
        hasExpectedErrors: Object.hasOwn(test.expected, "errors"),
        expectsErrors: Object.hasOwn(test.expected, "errors")
          ? test.expected.errors
          : null,
      })),
      sources,
      fixtureModules,
    });
  }

  const manifest = {
    source: "graphql-hive/federation-gateway-audit",
    revision,
    federationVersion: 2,
    selection:
      "Suites whose sources do not rely on linkless Federation directives.",
    excludedV1DependentSuites,
    suiteCount: suites.length,
    caseCount: suites.reduce((sum, suite) => sum + suite.cases.length, 0),
    suites,
  };

  writeFileSync(outputPath, `${JSON.stringify(manifest, null, 2)}\n`);
  process.stdout.write(
    `Wrote ${manifest.suiteCount} suites and ${manifest.caseCount} cases to ${outputPath}\n`,
  );
} finally {
  server.kill("SIGTERM");
}

async function fetchJson(url) {
  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`GET ${url} failed with ${response.status} ${response.statusText}.`);
  }

  return response.json();
}

async function fetchServiceSdl(url) {
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ query: "query AuditServiceSdl { _service { sdl } }" }),
  });

  if (!response.ok) {
    throw new Error(`POST ${url} failed with ${response.status} ${response.statusText}.`);
  }

  const result = await response.json();
  const sdl = result.data?._service?.sdl;

  if (typeof sdl !== "string" || sdl.length === 0) {
    throw new Error(`POST ${url} returned no _service.sdl.`);
  }

  return sdl;
}

async function waitForServer(process, url) {
  let failure;

  process.once("exit", (code) => {
    failure = new Error(`Audit server exited before startup with code ${code}.`);
  });

  for (let attempt = 0; attempt < 100; attempt++) {
    if (failure) {
      throw failure;
    }

    try {
      const response = await fetch(url);

      if (response.ok) {
        return;
      }
    } catch {
      // The server has not started accepting connections yet.
    }

    await new Promise((resolve) => setTimeout(resolve, 50));
  }

  throw new Error(`Audit server did not become healthy at ${url}.`);
}

function isFederationV1Source(sdl) {
  const linksFederation = sdl.includes("specs.apollo.dev/federation/");
  const usesFederationDirective =
    /@(key|external|requires|provides|extends)\b|\bextend\s+type\b/.test(sdl);

  return !linksFederation && usesFederationDirective;
}
