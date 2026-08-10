import { parseSitemapUrls } from "./parse-sitemap.mjs";

const baseUrl = new URL(process.argv[2] ?? "http://localhost:3001");
const SITE_NODE_TYPES = new Set(["Organization", "ImageObject", "WebSite"]);
const DISCOVERABLE_ARCHIVES = /^\/blog\/(?:\d+|tags\/[^/?#]+(?:\/\d+)?)$/;

const sitemapResponse = await fetch(new URL("/sitemap.xml", baseUrl));
if (!sitemapResponse.ok) {
  throw new Error(
    `Could not load sitemap: ${sitemapResponse.status} ${sitemapResponse.statusText}`,
  );
}

const sitemap = await sitemapResponse.text();
const queue = [
  ...new Set(
    parseSitemapUrls(sitemap).map((url) => {
      const pathname = new URL(url).pathname;
      return pathname || "/";
    }),
  ),
];
const queued = new Set(queue);
const checked = [];
const errors = [];

for (let index = 0; index < queue.length; index += 4) {
  const batch = queue.slice(index, index + 4);
  await Promise.all(batch.map(validatePage));
}

await validateExcludedRoute("/services/support/thank-you");
await validateRedirect(
  "/platform/continuous-integration",
  "/platform/release-safety",
);

if (errors.length > 0) {
  console.error(`Structured-data validation failed (${errors.length} errors):`);
  for (const error of errors) {
    console.error(`- ${error}`);
  }
  process.exitCode = 1;
} else {
  console.log(
    `Validated ${checked.length} indexable pages, the noindex flow page, and the legacy redirect.`,
  );
}

async function validatePage(pathname) {
  try {
    const response = await fetch(new URL(pathname, baseUrl));
    if (!response.ok) {
      errors.push(`${pathname}: returned HTTP ${response.status}`);
      return;
    }

    const html = await response.text();
    discoverBlogArchives(html);
    const documents = parseJsonLd(html, pathname);
    if (documents === null) {
      return;
    }

    const nodes = documents.flatMap(
      (document) => document["@graph"] ?? [document],
    );
    const siteNodeTypes = new Set(
      nodes
        .filter((node) => typeof node?.["@type"] === "string")
        .map((node) => node["@type"]),
    );
    for (const required of ["Organization", "ImageObject", "WebSite"]) {
      if (!siteNodeTypes.has(required)) {
        errors.push(`${pathname}: missing site-wide ${required} node`);
      }
    }

    const pageUrl = new URL(pathname, baseUrl).toString().replace(/\/$/, "");
    const pageNode = nodes.find(
      (node) =>
        typeof node?.["@id"] === "string" && node["@id"].endsWith("#webpage"),
    );
    if (!pageNode) {
      errors.push(`${pathname}: missing page node`);
    } else if (String(pageNode.url).replace(/\/$/, "") !== pageUrl) {
      errors.push(
        `${pathname}: page node URL does not match the requested URL`,
      );
    }

    if (
      pathname !== "/" &&
      !nodes.some((node) => node?.["@type"] === "BreadcrumbList")
    ) {
      errors.push(`${pathname}: missing BreadcrumbList`);
    }

    const ids = nodes
      .map((node) => node?.["@id"])
      .filter((id) => typeof id === "string");
    const duplicateIds = [
      ...new Set(ids.filter((id, i) => ids.indexOf(id) !== i)),
    ];
    if (duplicateIds.length > 0) {
      errors.push(
        `${pathname}: duplicate @id values: ${duplicateIds.join(", ")}`,
      );
    }

    checked.push(pathname);
  } catch (error) {
    errors.push(
      `${pathname}: ${error instanceof Error ? error.message : error}`,
    );
  }
}

async function validateExcludedRoute(pathname) {
  const response = await fetch(new URL(pathname, baseUrl));
  const html = await response.text();
  const documents = parseJsonLd(html, pathname);
  if (documents === null) {
    return;
  }
  const nonSiteNodes = documents
    .flatMap((document) => document["@graph"] ?? [document])
    .filter((node) => !SITE_NODE_TYPES.has(node?.["@type"]));
  if (nonSiteNodes.length > 0) {
    errors.push(
      `${pathname}: noindex page contains page-specific structured data`,
    );
  }
}

async function validateRedirect(pathname, expectedLocation) {
  const response = await fetch(new URL(pathname, baseUrl), {
    redirect: "manual",
  });
  const location = response.headers.get("location");
  const locationPath = location ? new URL(location, baseUrl).pathname : null;
  if (
    ![307, 308].includes(response.status) ||
    locationPath !== expectedLocation
  ) {
    errors.push(
      `${pathname}: expected redirect to ${expectedLocation}, got ${response.status} ${location}`,
    );
  }
}

function parseJsonLd(html, pathname) {
  const matches = [
    ...html.matchAll(
      /<script\b[^>]*type=["']application\/ld\+json["'][^>]*>([\s\S]*?)<\/script>/g,
    ),
  ];
  if (matches.length === 0) {
    errors.push(`${pathname}: contains no JSON-LD`);
    return null;
  }

  try {
    return matches.map((match) => {
      if (match[1].includes("<")) {
        throw new Error("JSON-LD contains an unescaped less-than character");
      }
      return JSON.parse(match[1]);
    });
  } catch (error) {
    errors.push(
      `${pathname}: invalid JSON-LD (${error instanceof Error ? error.message : error})`,
    );
    return null;
  }
}

function discoverBlogArchives(html) {
  for (const match of html.matchAll(/<a\b[^>]*href=["']([^"']+)["']/g)) {
    const pathname = new URL(match[1], baseUrl).pathname;
    if (DISCOVERABLE_ARCHIVES.test(pathname) && !queued.has(pathname)) {
      queued.add(pathname);
      queue.push(pathname);
    }
  }
}
