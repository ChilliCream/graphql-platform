import assert from "node:assert/strict";
import { readdir, readFile } from "node:fs/promises";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { parse } from "node-html-parser";
import { isLlmsEligibleUrl } from "../../scripts/generate-llms-files.mjs";
import { parseSitemapUrls } from "../../scripts/parse-sitemap.mjs";

const root = fileURLToPath(new URL("../../", import.meta.url));
const output = path.join(root, "out");
const removedRoutes = Array.from(
  { length: 20 },
  (_, index) => `/platform/graphql-federation/v${index + 1}`,
);
const canonicalRoutes = [
  "/",
  "/platform/graphql-federation",
  "/products/fusion",
  "/comparison",
  ...[
    "apollo-federation",
    "bff",
    "individual-apis",
    "monolith",
    "schema-stitching",
  ].map((slug) => `/comparison/graphql-federation-vs-${slug}`),
];

async function filesUnder(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = await Promise.all(
    entries.map(async (entry) => {
      const file = path.join(directory, entry.name);
      return entry.isDirectory() ? filesUnder(file) : [file];
    }),
  );
  return files.flat();
}

const prerender = JSON.parse(
  await readFile(path.join(root, ".next/prerender-manifest.json"), "utf8"),
);
const appRoutes = JSON.parse(
  await readFile(
    path.join(root, ".next/app-path-routes-manifest.json"),
    "utf8",
  ),
);
const outputFiles = (await filesUnder(output)).map((file) =>
  path.relative(output, file).split(path.sep).join("/"),
);
const exportedPages = outputFiles
  .filter((file) => file.endsWith(".html") && file !== "404.html")
  .map((file) => (file === "index.html" ? "/" : `/${file.slice(0, -5)}`))
  .sort();
const sitemapUrls = parseSitemapUrls(
  await readFile(path.join(output, "sitemap.xml"), "utf8"),
);
const pages = await Promise.all(
  exportedPages.map(async (route) => {
    const file = route === "/" ? "index.html" : `${route.slice(1)}.html`;
    return {
      route,
      document: parse(await readFile(path.join(output, file), "utf8")),
    };
  }),
);

test("exports the complete production page inventory and canonical routes", async () => {
  // arrange
  const sourcePages = (await filesUnder(path.join(root, "app")))
    .filter((file) => path.basename(file) === "page.tsx")
    .map((file) =>
      path
        .relative(path.join(root, "app"), path.dirname(file))
        .split(path.sep)
        .filter((segment) => segment && !segment.startsWith("(")),
    )
    .filter((segments) => !segments.some((segment) => segment.startsWith("_")))
    .map((segments) => `/${segments.join("/")}`)
    .sort();

  // act
  const compiledPages = Object.entries(appRoutes)
    .filter(([file]) => file.endsWith("/page") && !file.startsWith("/_"))
    .map(([, route]) => route)
    .sort();
  const expectedExports = Object.entries(prerender.routes)
    .filter(
      ([route, entry]) =>
        entry.routeType === "page" && route !== "/_global-error",
    )
    .map(([route]) => route)
    .sort();

  // assert
  assert.deepEqual(compiledPages, sourcePages);
  assert.deepEqual(exportedPages, expectedExports);
  assert.deepEqual(
    canonicalRoutes.filter((route) => exportedPages.includes(route)),
    canonicalRoutes,
  );
  for (const route of sourcePages.filter((route) => !route.includes("["))) {
    assert.ok(exportedPages.includes(route), `Missing static export: ${route}`);
  }
});

test("removes every prototype route and its static navigation artifacts", () => {
  // act
  const remaining = removedRoutes.filter(
    (route) =>
      route in prerender.routes ||
      Object.values(appRoutes).includes(route) ||
      outputFiles.some((file) => {
        const relative = route.slice(1);
        return (
          file.startsWith(`${relative}/`) ||
          ["html", "txt", "md"].some(
            (extension) => file === `${relative}.${extension}`,
          )
        );
      }),
  );

  // assert
  assert.deepEqual(remaining, []);
});

test("indexes every self-canonical production page without prototype URLs", () => {
  // act
  const indexableUrls = pages.flatMap(({ route, document }) => {
    const canonical = document
      .querySelector('link[rel="canonical"]')
      ?.getAttribute("href");
    const robots =
      document.querySelector('meta[name="robots"]')?.getAttribute("content") ??
      "";
    return canonical &&
      new URL(canonical).pathname === route &&
      !/\bnoindex\b/i.test(robots)
      ? [new URL(canonical).href]
      : [];
  });
  const sitemapRoutes = sitemapUrls.map((url) => new URL(url).pathname);

  // assert
  assert.deepEqual([...sitemapUrls].sort(), indexableUrls.sort());
  assert.deepEqual(
    canonicalRoutes.filter((route) => sitemapRoutes.includes(route)),
    canonicalRoutes,
  );
  assert.deepEqual(
    sitemapRoutes.filter((route) => removedRoutes.includes(route)),
    [],
  );
});

test("retains the complete platform and global LLM page catalogs", async () => {
  // arrange
  const platformIndex = await readFile(
    path.join(output, "platform/llms.txt"),
    "utf8",
  );
  const globalCorpus = await readFile(
    path.join(output, "llms-full.txt"),
    "utf8",
  );
  const expectedUrls = sitemapUrls.filter(isLlmsEligibleUrl);

  // act
  const platformUrls = [
    ...platformIndex.matchAll(/\]\((https?:\/\/[^)]+)\.md\)/g),
  ].map((match) => match[1]);
  const globalUrls = [
    ...globalCorpus.matchAll(/^Canonical source: (.+)$/gm),
  ].map((match) => match[1]);
  const catalogs = await Promise.all(
    outputFiles
      .filter((file) => /(^|\/)llms(?:-full)?\.txt$/.test(file))
      .map((file) => readFile(path.join(output, file), "utf8")),
  );

  // assert
  assert.deepEqual(
    platformUrls.sort(),
    expectedUrls
      .filter((url) => new URL(url).pathname.startsWith("/platform"))
      .sort(),
  );
  assert.deepEqual(globalUrls.sort(), [...expectedUrls].sort());
  assert.deepEqual(
    catalogs.filter((catalog) =>
      /\/platform\/graphql-federation\/v\d+(?:[/.\s)#]|$)/.test(catalog),
    ),
    [],
  );
});
