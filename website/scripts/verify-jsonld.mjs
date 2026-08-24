#!/usr/bin/env node
/**
 * Curls each learn/article route type against a running dev server, extracts
 * every `application/ld+json` block from the rendered HTML, and asserts the
 * required properties per schema.org type are present.
 *
 * Usage: BASE_URL=http://localhost:3230 node scripts/verify-jsonld.mjs
 *
 * This checks structural presence of required fields only. It cannot
 * validate against Google's Rich Results Test, which requires a publicly
 * reachable URL and cannot run locally.
 */

const BASE_URL = process.env.BASE_URL ?? "http://localhost:3230";

/** Required property assertions per schema.org `@type`. */
const REQUIRED_PROPS = {
  Organization: ["@id", "name", "url"],
  WebSite: ["@id", "name", "url"],
  WebPage: ["@id", "url", "name"],
  CollectionPage: ["@id", "url", "name"],
  BreadcrumbList: ["itemListElement"],
  ItemList: ["itemListElement"],
  Article: ["headline", "datePublished", "publisher"],
  BlogPosting: ["headline", "datePublished", "publisher"],
  // No `programmingLanguage` here: templates always carry one (from their
  // `language` axis) but examples don't have an equivalent field, so it's
  // only ever emitted when available rather than required.
  SoftwareSourceCode: ["name", "url", "codeRepository"],
  // Tutorial detail pages (website-kbx.25): a technical how-to, not a dated
  // news article, so no `datePublished` requirement either.
  TechArticle: ["headline", "url", "description"],
  // Workshop detail pages (website-kbx.25).
  Course: ["name", "url", "description", "provider"],
};

/**
 * Routes to audit, one per route type in the ticket's schema list. `type` is
 * a human label; `expect` lists the schema.org `@type`s that MUST appear
 * somewhere in the page's combined `@graph` (across all ld+json blocks).
 */
const ROUTES = [
  { path: "/", type: "root layout (Organization/WebSite)", expect: ["Organization", "WebSite"] },
  { path: "/learn", type: "learn landing", expect: ["BreadcrumbList", "ItemList", "WebPage"] },
  { path: "/learn/browse", type: "learn browse", expect: ["BreadcrumbList", "ItemList"] },
  {
    path: "/learn/articles/fusion-16-5",
    type: "article page (blog post)",
    expect: ["BreadcrumbList", "BlogPosting"],
  },
  {
    path: "/learn/articles/fusion-vs-apollo-router",
    type: "article page (comparison)",
    expect: ["BreadcrumbList", "Article"],
  },
  { path: "/learn/articles", type: "articles index", expect: ["BreadcrumbList"] },
  { path: "/learn/articles/page/2", type: "articles index (page 2)", expect: ["BreadcrumbList"] },
  { path: "/learn/articles/tags/fusion", type: "articles tag index", expect: ["BreadcrumbList"] },
  {
    path: "/learn/templates/fusion-3-service-federation",
    type: "template page",
    expect: ["BreadcrumbList", "SoftwareSourceCode"],
  },
  {
    path: "/learn/tutorials/get-started-with-graphql-in-net-core",
    type: "tutorial page",
    expect: ["BreadcrumbList", "TechArticle"],
  },
  {
    path: "/learn/examples/fusion-demo",
    type: "example page",
    expect: ["BreadcrumbList", "SoftwareSourceCode"],
  },
  {
    path: "/learn/workshops/graphql-workshop-repo",
    type: "workshop page",
    expect: ["BreadcrumbList", "Course"],
  },
  {
    path: "/learn/topics/graphql-federation",
    type: "topic hub (GraphQL & Federation)",
    expect: ["BreadcrumbList", "CollectionPage", "ItemList"],
  },
  {
    path: "/learn/topics/messaging",
    type: "topic hub (Messaging)",
    expect: ["BreadcrumbList", "CollectionPage", "ItemList"],
  },
  {
    path: "/learn/topics/tooling-observability",
    type: "topic hub (Tooling & Observability)",
    expect: ["BreadcrumbList", "CollectionPage", "ItemList"],
  },
  {
    path: "/learn/topics/agents",
    type: "topic hub (Agents)",
    expect: ["BreadcrumbList", "CollectionPage", "ItemList"],
  },
];

let failures = 0;
let checkedGraphNodes = 0;

/** Extracts every JSON-LD block's parsed content, flattening `@graph` arrays into a single node list. */
function extractNodes(html) {
  const nodes = [];
  const re = /<script type="application\/ld\+json"[^>]*>([\s\S]*?)<\/script>/g;
  let match;
  while ((match = re.exec(html)) !== null) {
    const parsed = JSON.parse(match[1].replace(/\\u003c/g, "<"));
    if (Array.isArray(parsed["@graph"])) {
      nodes.push(...parsed["@graph"]);
    } else {
      nodes.push(parsed);
    }
  }
  return nodes;
}

for (const route of ROUTES) {
  const url = `${BASE_URL}${route.path}`;
  let html;
  try {
    const res = await fetch(url);
    if (!res.ok) {
      console.error(`FAIL ${route.type} (${route.path}): HTTP ${res.status}`);
      failures++;
      continue;
    }
    html = await res.text();
  } catch (err) {
    console.error(`FAIL ${route.type} (${route.path}): fetch error ${err.message}`);
    failures++;
    continue;
  }

  const nodes = extractNodes(html);
  if (nodes.length === 0) {
    console.error(`FAIL ${route.type} (${route.path}): no ld+json blocks found`);
    failures++;
    continue;
  }

  for (const expectedType of route.expect) {
    const node = nodes.find((n) => n["@type"] === expectedType);
    if (!node) {
      console.error(`FAIL ${route.type} (${route.path}): missing @type "${expectedType}"`);
      failures++;
      continue;
    }
    checkedGraphNodes++;
    const required = REQUIRED_PROPS[expectedType] ?? [];
    for (const prop of required) {
      if (!(prop in node) || node[prop] === null || node[prop] === undefined) {
        console.error(`FAIL ${route.type} (${route.path}): ${expectedType} missing required property "${prop}"`);
        failures++;
      }
    }
  }

  console.log(`OK   ${route.type} (${route.path}): found ${nodes.map((n) => n["@type"]).join(", ")}`);
}

console.log("");
console.log(`Checked ${ROUTES.length} routes, ${checkedGraphNodes} required @type assertions.`);
if (failures > 0) {
  console.log(`${failures} assertion(s) FAILED.`);
  process.exit(1);
}
console.log("All assertions passed.");
console.log(
  "Note: this only checks structural presence of required fields. Google's Rich Results Test " +
    "(https://search.google.com/test/rich-results) requires a publicly reachable URL and cannot run locally.",
);
