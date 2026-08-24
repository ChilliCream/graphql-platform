import { parse } from "node-html-parser";
import { parseSitemapUrls } from "./parse-sitemap.mjs";

const baseUrl = new URL(process.argv[2] ?? "http://localhost:3001");
const SITE_NODE_TYPES = new Set(["Organization", "ImageObject", "WebSite"]);
const BLOG_PAGINATION_PATH = /^\/blog\/\d+$/;
const TAG_ARCHIVE_PATH = /^\/blog\/tags\/[^/?#]+(?:\/\d+)?$/;
const EXCLUDED_SITEMAP_PATHS = new Set([
  "/platform/continuous-integration",
  "/services/support/thank-you",
]);
const LEGACY_DOCS_PATH = /^\/docs\/skillz(?:\/|$)/;
const REDUNDANT_ARCHIVE_PATH = /^\/blog\/(?:\d+|tags\/[^/]+(?:\/\d+)?)$/;
const MISSING_TEST_PATHS = [
  "/__seo-validator-missing-page__",
  "/docs/__seo-validator-missing-page__",
  "/blog/__seo-validator-missing-page__",
];

const sitemapResponse = await fetch(new URL("/sitemap.xml", baseUrl));
if (!sitemapResponse.ok) {
  throw new Error(
    `Could not load sitemap: ${sitemapResponse.status} ${sitemapResponse.statusText}`,
  );
}

const sitemap = await sitemapResponse.text();
const sitemapUrls = parseSitemapUrls(sitemap);
const canonicalByPath = validateSitemap(sitemap, sitemapUrls);
const canonicalOrigin = new URL(sitemapUrls[0]).origin;
const queue = [...canonicalByPath.keys()];
const queued = new Set(queue);
const noindexArchiveQueue = [];
const queuedNoindexArchives = new Set();
const checked = [];
const errors = [];
const crawlGraph = new Map();
const internalLinkTargets = new Map();
const pageMetadata = new Map();
const markdownAlternates = new Map();
const jsonLdIds = new Set();
const jsonLdReferencesByPath = new Map();

await validateRobots([
  ...new Set(sitemapUrls.map((url) => new URL(url).origin)),
]);

for (let index = 0; index < queue.length; index += 4) {
  const batch = queue.slice(index, index + 4);
  await Promise.all(batch.map(validatePage));
}

for (let index = 0; index < noindexArchiveQueue.length; index += 4) {
  const batch = noindexArchiveQueue.slice(index, index + 4);
  await Promise.all(batch.map(validateNoindexArchive));
}

validateJsonLdReferences();
await validateInternalLinks();
validateHomepageReachability();
validateMetadataUniqueness();
await validateNotFoundResponses();
await validateMachineReadableRepresentations();

await validateExcludedRoute("/services/support/thank-you");
await validateRedirect(
  "/platform/continuous-integration",
  "/platform/release-safety",
);
await validateRedirect("/docs/skillz", "/docs/skills");
await validateRedirect(
  "/docs/skillz/getting-started",
  "/docs/skills/getting-started",
);

if (errors.length > 0) {
  console.error(`Structured-data validation failed (${errors.length} errors):`);
  for (const error of errors) {
    console.error(`- ${error}`);
  }
  process.exitCode = 1;
} else {
  console.log(
    `Validated ${checked.length} indexable pages, ${noindexArchiveQueue.length} noindex tag archives, robots.txt, sitemap.xml, the web manifest, the noindex flow page, and three permanent redirects.`,
  );
}

function validateSitemap(xml, urls) {
  if (urls.length === 0 || new Set(urls).size !== urls.length) {
    throw new Error("Sitemap must contain a nonempty set of unique URLs.");
  }

  const document = parse(xml);
  const urlNodes = document.querySelectorAll("url");
  if (urlNodes.length !== urls.length) {
    throw new Error("Sitemap URL parsing produced inconsistent results.");
  }

  const byPath = new Map();
  const origins = new Set();
  const violations = [];
  for (const node of urlNodes) {
    const value = node.querySelector("loc")?.textContent.trim();
    if (!value) {
      violations.push("entry is missing <loc>");
      continue;
    }

    let url;
    try {
      url = new URL(value);
    } catch {
      violations.push(`${value}: is not an absolute URL`);
      continue;
    }

    origins.add(url.origin);
    if (url.protocol !== "https:" && url.hostname !== "localhost") {
      violations.push(`${value}: canonical sitemap URLs must use HTTPS`);
    }
    if (url.username || url.password || url.search || url.hash) {
      violations.push(`${value}: canonical URL contains extra URL components`);
    }
    if (url.pathname !== "/" && url.pathname.endsWith("/")) {
      violations.push(`${value}: canonical URL has a trailing slash`);
    }
    if (EXCLUDED_SITEMAP_PATHS.has(url.pathname)) {
      violations.push(`${value}: excluded flow or redirect is in the sitemap`);
    }
    if (LEGACY_DOCS_PATH.test(url.pathname)) {
      violations.push(`${value}: legacy Skills URL is in the sitemap`);
    }
    if (TAG_ARCHIVE_PATH.test(url.pathname)) {
      violations.push(`${value}: thin tag archive is in the sitemap`);
    }
    if (byPath.has(url.pathname)) {
      violations.push(`${value}: duplicate canonical pathname`);
    }
    byPath.set(url.pathname, url.href);

    const lastModified = node.querySelector("lastmod")?.textContent.trim();
    if (lastModified) {
      const date = new Date(lastModified);
      if (Number.isNaN(date.getTime())) {
        violations.push(`${value}: invalid <lastmod> value ${lastModified}`);
      } else if (date.getTime() > Date.now() + 5 * 60 * 1000) {
        violations.push(`${value}: <lastmod> is in the future`);
      }
    }

    if (node.querySelector("changefreq") || node.querySelector("priority")) {
      violations.push(
        `${value}: contains ignored changefreq/priority sitemap hints`,
      );
    }
  }

  if (origins.size !== 1) {
    violations.push("all sitemap URLs must use one canonical origin");
  }
  if (violations.length > 0) {
    throw new Error(`Invalid sitemap:\n- ${violations.join("\n- ")}`);
  }
  return byPath;
}

async function validateRobots(sitemapOrigins) {
  const response = await fetch(new URL("/robots.txt", baseUrl));
  if (!response.ok) {
    errors.push(`robots.txt: returned HTTP ${response.status}`);
    return;
  }
  if (!response.headers.get("content-type")?.includes("text/plain")) {
    errors.push("robots.txt: response is not text/plain");
  }

  const content = await response.text();
  if (!/^User-Agent:\s*\*\s*$/im.test(content)) {
    errors.push("robots.txt: missing wildcard user-agent policy");
  }
  if (!/^Allow:\s*\/\s*$/im.test(content)) {
    errors.push("robots.txt: production policy must allow the public site");
  }
  if (/^Disallow:\s*\/\s*$/im.test(content)) {
    errors.push("robots.txt: production policy blocks the public site");
  }
  const expectedSitemap = `${sitemapOrigins[0]}/sitemap.xml`;
  if (
    !content
      .split("\n")
      .some((line) => line.trim() === `Sitemap: ${expectedSitemap}`)
  ) {
    errors.push(`robots.txt: missing ${expectedSitemap}`);
  }
  if (/^Host:/im.test(content)) {
    errors.push("robots.txt: contains a nonstandard Host directive");
  }
}

function validatePageMetadata(html, pathname) {
  const document = parse(html);
  const expectedCanonical = canonicalByPath.get(pathname);
  const canonicalLinks = document.querySelectorAll('link[rel="canonical"]');
  if (canonicalLinks.length !== 1) {
    errors.push(
      `${pathname}: expected exactly one canonical link, found ${canonicalLinks.length}`,
    );
  } else {
    const href = canonicalLinks[0].getAttribute("href");
    if (
      !href ||
      normalizeUrl(new URL(href, expectedCanonical).href) !==
        normalizeUrl(expectedCanonical)
    ) {
      errors.push(`${pathname}: canonical link does not match the sitemap`);
    }
  }

  const titles = document.querySelectorAll("title");
  const title = titles[0]?.textContent.trim() ?? "";
  if (titles.length !== 1 || !titles[0].textContent.trim()) {
    errors.push(`${pathname}: expected exactly one nonempty document title`);
  }
  const descriptions = document.querySelectorAll('meta[name="description"]');
  const description = descriptions[0]?.getAttribute("content")?.trim() ?? "";
  if (
    descriptions.length !== 1 ||
    !descriptions[0].getAttribute("content")?.trim()
  ) {
    errors.push(`${pathname}: expected exactly one nonempty meta description`);
  }
  const h1s = document.querySelectorAll("h1");
  if (h1s.length !== 1 || !h1s[0].textContent.trim()) {
    errors.push(`${pathname}: expected exactly one nonempty H1`);
  }
  const language = document.querySelector("html")?.getAttribute("lang");
  if (!language?.toLowerCase().startsWith("en")) {
    errors.push(`${pathname}: HTML language is not English`);
  }

  for (const robots of document.querySelectorAll(
    'meta[name="robots"], meta[name="googlebot"]',
  )) {
    if (/\bnoindex\b/i.test(robots.getAttribute("content") ?? "")) {
      errors.push(`${pathname}: sitemap URL is marked noindex`);
    }
  }

  const openGraphUrls = document.querySelectorAll('meta[property="og:url"]');
  if (
    openGraphUrls.length !== 1 ||
    normalizeUrl(openGraphUrls[0].getAttribute("content")) !==
      normalizeUrl(expectedCanonical)
  ) {
    errors.push(`${pathname}: og:url does not match the canonical URL`);
  }

  if (title && description) {
    pageMetadata.set(pathname, { title, description });
  }
  recordLinks(document, pathname);

  const markdownLinks = document.querySelectorAll(
    'link[rel="alternate"][type="text/markdown"]',
  );
  if (markdownLinks.length > 1) {
    errors.push(`${pathname}: has multiple Markdown alternate links`);
  } else if (markdownLinks.length === 1) {
    try {
      markdownAlternates.set(
        pathname,
        new URL(markdownLinks[0].getAttribute("href") ?? "", expectedCanonical)
          .href,
      );
    } catch {
      errors.push(`${pathname}: Markdown alternate link is invalid`);
    }
  }
}

async function validatePage(pathname) {
  try {
    const response = await fetch(new URL(pathname, baseUrl), {
      redirect: "manual",
    });
    if (!response.ok) {
      errors.push(`${pathname}: returned HTTP ${response.status}`);
      return;
    }
    if (!response.headers.get("content-type")?.includes("text/html")) {
      errors.push(`${pathname}: response is not HTML`);
    }

    const html = await response.text();
    discoverBlogArchives(html);
    validatePageMetadata(html, pathname);
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

    const pageUrl = canonicalByPath.get(pathname);
    if (!pageUrl) {
      errors.push(`${pathname}: indexable page is missing from the sitemap`);
      return;
    }
    const pageNode = nodes.find(
      (node) =>
        typeof node?.["@id"] === "string" && node["@id"].endsWith("#webpage"),
    );
    if (!pageNode) {
      errors.push(`${pathname}: missing page node`);
    } else if (normalizeUrl(pageNode.url) !== normalizeUrl(pageUrl)) {
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

    validateProducts(nodes, pathname);
    validateArticles(nodes, pathname);
    validateBreadcrumbs(nodes, pathname);
    validateFaqs(nodes, pathname);
    validateItemLists(nodes, pathname);
    validateReferences(nodes, pathname);

    checked.push(pathname);
  } catch (error) {
    errors.push(
      `${pathname}: ${error instanceof Error ? error.message : error}`,
    );
  }
}

function validateProducts(nodes, pathname) {
  const nodesById = new Map(
    nodes
      .filter((node) => typeof node?.["@id"] === "string")
      .map((node) => [node["@id"], node]),
  );

  for (const product of nodes.filter((node) => node?.["@type"] === "Product")) {
    if (
      !product.brand ||
      !["Brand", "Organization"].includes(product.brand["@type"])
    ) {
      errors.push(
        `${pathname}: Product.brand must be an object with @type "Brand" or "Organization"`,
      );
    }

    const offers = Array.isArray(product.offers)
      ? product.offers
      : product.offers
        ? [product.offers]
        : [];

    if (offers.length === 0 && !product.aggregateRating && !product.review) {
      errors.push(
        `${pathname}: Product requires an offer, aggregate rating, or review`,
      );
    }

    for (const offerReference of offers) {
      const offer =
        typeof offerReference?.["@id"] === "string"
          ? nodesById.get(offerReference["@id"])
          : offerReference;
      if (!offer) {
        errors.push(`${pathname}: Product references an unknown Offer`);
        continue;
      }

      const specification = offer.priceSpecification;
      const price = offer.price ?? specification?.price;
      const currency = offer.priceCurrency ?? specification?.priceCurrency;
      if (price === undefined) {
        errors.push(`${pathname}: Product Offer is missing a price`);
      }
      if (!currency) {
        errors.push(`${pathname}: Product Offer is missing a price currency`);
      }
      if (
        specification?.["@type"] === "UnitPriceSpecification" &&
        specification.price === undefined
      ) {
        errors.push(`${pathname}: UnitPriceSpecification is missing its price`);
      }
    }
  }
}

function validateArticles(nodes, pathname) {
  for (const article of nodes.filter((node) =>
    typesOf(node).some((type) =>
      ["Article", "BlogPosting", "NewsArticle", "TechArticle"].includes(type),
    ),
  )) {
    if (typeof article.headline !== "string" || !article.headline.trim()) {
      errors.push(`${pathname}: Article is missing a headline`);
    }
    if (!article.image) {
      errors.push(`${pathname}: Article is missing a representative image`);
    }
    if (!article.publisher) {
      errors.push(`${pathname}: Article is missing its publisher`);
    }
    if (!article.mainEntityOfPage) {
      errors.push(`${pathname}: Article is missing mainEntityOfPage`);
    }

    for (const property of ["datePublished", "dateModified"]) {
      if (
        article[property] &&
        Number.isNaN(new Date(article[property]).getTime())
      ) {
        errors.push(`${pathname}: Article has invalid ${property}`);
      }
    }

    if (typesOf(article).includes("BlogPosting")) {
      if (!article.datePublished) {
        errors.push(`${pathname}: BlogPosting is missing datePublished`);
      }
      if (!article.author) {
        errors.push(`${pathname}: BlogPosting is missing its visible author`);
      }
    }
  }
}

function validateBreadcrumbs(nodes, pathname) {
  for (const breadcrumb of nodes.filter((node) =>
    typesOf(node).includes("BreadcrumbList"),
  )) {
    const items = breadcrumb.itemListElement;
    if (!Array.isArray(items) || items.length < 2) {
      errors.push(
        `${pathname}: BreadcrumbList must contain at least two items`,
      );
      continue;
    }
    for (let index = 0; index < items.length; index++) {
      const item = items[index];
      if (item?.["@type"] !== "ListItem") {
        errors.push(`${pathname}: breadcrumb item is not a ListItem`);
      }
      if (item?.position !== index + 1) {
        errors.push(`${pathname}: breadcrumb positions are not contiguous`);
      }
      if (typeof item?.name !== "string" || !item.name.trim()) {
        errors.push(`${pathname}: breadcrumb item is missing its name`);
      }
      if (index < items.length - 1 && !isAbsoluteHttpUrl(item?.item)) {
        errors.push(`${pathname}: non-final breadcrumb is missing its URL`);
      }
    }
  }
}

function validateFaqs(nodes, pathname) {
  for (const faq of nodes.filter((node) => typesOf(node).includes("FAQPage"))) {
    if (!Array.isArray(faq.mainEntity) || faq.mainEntity.length === 0) {
      errors.push(`${pathname}: FAQPage has no questions`);
      continue;
    }
    for (const question of faq.mainEntity) {
      if (
        question?.["@type"] !== "Question" ||
        typeof question.name !== "string" ||
        !question.name.trim()
      ) {
        errors.push(`${pathname}: FAQPage contains an invalid Question`);
      }
      if (
        question?.acceptedAnswer?.["@type"] !== "Answer" ||
        typeof question.acceptedAnswer.text !== "string" ||
        !question.acceptedAnswer.text.trim()
      ) {
        errors.push(`${pathname}: FAQ question has no accepted answer text`);
      }
    }
  }
}

function validateItemLists(nodes, pathname) {
  for (const list of nodes.filter((node) =>
    typesOf(node).includes("ItemList"),
  )) {
    const items = list.itemListElement;
    if (!Array.isArray(items)) {
      errors.push(`${pathname}: ItemList has no itemListElement array`);
      continue;
    }
    if (list.numberOfItems !== items.length) {
      errors.push(`${pathname}: ItemList numberOfItems is inconsistent`);
    }
    for (let index = 1; index < items.length; index++) {
      if (items[index].position !== items[index - 1].position + 1) {
        errors.push(`${pathname}: ItemList positions are not contiguous`);
        break;
      }
    }
  }
}

function validateReferences(nodes, pathname) {
  for (const id of nodes
    .map((node) => node?.["@id"])
    .filter((id) => typeof id === "string")) {
    jsonLdIds.add(id);
  }

  const references = [];
  for (const node of nodes) {
    collectReferences(node, references);
  }

  jsonLdReferencesByPath.set(pathname, new Set(references));
}

function validateJsonLdReferences() {
  for (const [pathname, references] of jsonLdReferencesByPath) {
    for (const reference of references) {
      if (jsonLdIds.has(reference)) {
        continue;
      }

      let url;
      try {
        url = new URL(reference);
      } catch {
        errors.push(`${pathname}: invalid JSON-LD reference ${reference}`);
        continue;
      }

      // External entity identifiers are allowed. Same-site identifiers must
      // resolve somewhere in the complete graph assembled from every
      // indexable page, including valid cross-page Product references.
      if (url.origin === canonicalOrigin) {
        errors.push(`${pathname}: unresolved JSON-LD reference ${reference}`);
      }
    }
  }
}

function collectReferences(value, references) {
  if (!value || typeof value !== "object") {
    return;
  }
  if (
    !Array.isArray(value) &&
    Object.keys(value).length === 1 &&
    typeof value["@id"] === "string"
  ) {
    references.push(value["@id"]);
    return;
  }
  for (const nested of Array.isArray(value) ? value : Object.values(value)) {
    collectReferences(nested, references);
  }
}

function typesOf(node) {
  const value = node?.["@type"];
  return Array.isArray(value)
    ? value
    : typeof value === "string"
      ? [value]
      : [];
}

function isAbsoluteHttpUrl(value) {
  if (typeof value !== "string") {
    return false;
  }
  try {
    return /^https?:$/.test(new URL(value).protocol);
  } catch {
    return false;
  }
}

function normalizeUrl(value) {
  try {
    return new URL(String(value)).href;
  } catch {
    return String(value);
  }
}

function recordLinks(document, sourcePath) {
  const crawlTargets = new Set();
  const sourceUrl =
    canonicalByPath.get(sourcePath) ?? new URL(sourcePath, baseUrl);
  const canonicalOrigin = new URL(sitemapUrls[0]).origin;

  for (const anchor of document.querySelectorAll("a[href]")) {
    const href = anchor.getAttribute("href")?.trim();
    if (
      !href ||
      href.startsWith("#") ||
      /^(?:data|javascript|mailto|tel):/i.test(href)
    ) {
      continue;
    }

    let url;
    try {
      url = new URL(href, sourceUrl);
    } catch {
      errors.push(`${sourcePath}: contains an invalid link target ${href}`);
      continue;
    }
    if (
      !/^https?:$/.test(url.protocol) ||
      (url.origin !== canonicalOrigin && url.origin !== baseUrl.origin)
    ) {
      continue;
    }

    url.hash = "";
    const target = `${url.pathname}${url.search}`;
    const entry = internalLinkTargets.get(target) ?? {
      pathname: url.pathname,
      sources: new Set(),
    };
    entry.sources.add(sourcePath);
    internalLinkTargets.set(target, entry);

    const rel = new Set(
      (anchor.getAttribute("rel") ?? "")
        .toLowerCase()
        .split(/\s+/)
        .filter(Boolean),
    );
    if (!rel.has("nofollow")) {
      crawlTargets.add(url.pathname);
    }
  }

  crawlGraph.set(sourcePath, crawlTargets);
}

async function validateInternalLinks() {
  const links = [...internalLinkTargets.entries()];
  for (let index = 0; index < links.length; index += 12) {
    await Promise.all(
      links.slice(index, index + 12).map(async ([target, entry]) => {
        try {
          const response = await fetch(new URL(target, baseUrl), {
            redirect: "manual",
          });
          await response.body?.cancel();
          const sources = [...entry.sources].slice(0, 4).join(", ");
          if (response.status >= 400) {
            errors.push(
              `${target}: internal link from ${sources} returned HTTP ${response.status}`,
            );
          } else if (response.status >= 300) {
            errors.push(
              `${target}: internal link from ${sources} hits redirect source (HTTP ${response.status})`,
            );
          }
        } catch (error) {
          errors.push(
            `${target}: internal link request failed (${error instanceof Error ? error.message : error})`,
          );
        }
      }),
    );
  }
}

function validateHomepageReachability() {
  const reached = new Set(["/"]);
  const pending = ["/"];
  while (pending.length > 0) {
    const source = pending.shift();
    for (const target of crawlGraph.get(source) ?? []) {
      if (!reached.has(target) && crawlGraph.has(target)) {
        reached.add(target);
        pending.push(target);
      }
    }
  }

  const unreachable = [...canonicalByPath.keys()].filter(
    (pathname) => !reached.has(pathname),
  );
  if (unreachable.length > 0) {
    errors.push(
      `sitemap pages not reachable from / through crawlable anchors: ${unreachable.join(", ")}`,
    );
  }
}

function validateMetadataUniqueness() {
  for (const property of ["title", "description"]) {
    const pagesByValue = new Map();
    for (const [pathname, metadata] of pageMetadata) {
      const value = metadata[property]
        .replace(/\s+/g, " ")
        .trim()
        .toLowerCase();
      const paths = pagesByValue.get(value) ?? [];
      paths.push(pathname);
      pagesByValue.set(value, paths);
    }
    for (const paths of pagesByValue.values()) {
      if (paths.length > 1) {
        errors.push(
          `duplicate ${property} across canonical pages: ${paths.join(", ")}`,
        );
      }
    }
  }
}

async function validateNotFoundResponses() {
  await Promise.all(
    MISSING_TEST_PATHS.map(async (pathname) => {
      const response = await fetch(new URL(pathname, baseUrl), {
        redirect: "manual",
      });
      await response.body?.cancel();
      if (response.status !== 404) {
        errors.push(
          `${pathname}: representative missing route returned HTTP ${response.status}, expected 404`,
        );
      }
    }),
  );
}

async function validateMachineReadableRepresentations() {
  const manifestResponse = await fetch(
    new URL("/manifest.webmanifest", baseUrl),
  );
  if (!manifestResponse.ok) {
    errors.push(
      `/manifest.webmanifest: returned HTTP ${manifestResponse.status}`,
    );
  }
  if (
    !manifestResponse.headers
      .get("content-type")
      ?.includes("application/manifest+json")
  ) {
    errors.push(
      `/manifest.webmanifest: expected application/manifest+json, got ${manifestResponse.headers.get("content-type") ?? "no content type"}`,
    );
  }
  try {
    const manifest = await manifestResponse.json();
    if (
      typeof manifest.name !== "string" ||
      !manifest.name.trim() ||
      !Array.isArray(manifest.icons) ||
      manifest.icons.length === 0
    ) {
      errors.push(
        "/manifest.webmanifest: missing a nonempty name or icon list",
      );
    }
  } catch {
    errors.push("/manifest.webmanifest: response is not valid JSON");
  }

  const firstCatalog = await fetch(new URL("/llms.txt", baseUrl));
  const servedByNginx =
    /nginx/i.test(firstCatalog.headers.get("server") ?? "") ||
    firstCatalog.headers.has("x-robots-tag");
  if (!servedByNginx) {
    await firstCatalog.body?.cancel();
    console.warn(
      "Skipped HTTP header checks for Markdown and LLM files because the target is not served through nginx.",
    );
    return;
  }

  const catalogs = [
    ["/llms.txt", firstCatalog],
    ["/llms-full.txt"],
    ["/docs/llms.txt"],
    ["/docs/llms-full.txt"],
    ["/blog/llms.txt"],
    ["/blog/llms-full.txt"],
  ];
  for (const [pathname, existingResponse] of catalogs) {
    const response =
      existingResponse ?? (await fetch(new URL(pathname, baseUrl)));
    const robots = response.headers.get("x-robots-tag") ?? "";
    if (!response.ok) {
      errors.push(`${pathname}: LLM catalog returned HTTP ${response.status}`);
    }
    if (!/^googlebot:\s*noindex,\s*follow$/i.test(robots.trim())) {
      errors.push(
        `${pathname}: expected X-Robots-Tag "googlebot: noindex, follow", got ${robots || "no header"}`,
      );
    }
    await response.body?.cancel();
  }

  for (const [pathname, canonical] of canonicalByPath) {
    if (REDUNDANT_ARCHIVE_PATH.test(pathname)) {
      if (markdownAlternates.has(pathname)) {
        errors.push(`${pathname}: redundant archive advertises Markdown`);
      }
      continue;
    }

    const expectedMarkdown =
      pathname === "/"
        ? new URL("/index.md", canonical).href
        : new URL(`${pathname}.md`, canonical).href;
    if (markdownAlternates.get(pathname) !== expectedMarkdown) {
      errors.push(`${pathname}: missing its generated Markdown alternate link`);
      continue;
    }

    const response = await fetch(
      new URL(new URL(expectedMarkdown).pathname, baseUrl),
    );
    const link = response.headers.get("link") ?? "";
    const robots = response.headers.get("x-robots-tag") ?? "";
    if (!response.ok) {
      errors.push(
        `${pathname}: Markdown alternate returned HTTP ${response.status}`,
      );
    }
    if (!response.headers.get("content-type")?.includes("text/markdown")) {
      errors.push(`${pathname}: Markdown alternate is not text/markdown`);
    }
    if (
      !link
        .split(",")
        .some((value) => value.trim() === `<${canonical}>; rel="canonical"`)
    ) {
      errors.push(
        `${pathname}: Markdown response has an invalid HTTP canonical`,
      );
    }
    if (/\bnoindex\b/i.test(robots)) {
      errors.push(`${pathname}: page-level Markdown is incorrectly noindex`);
    }
    await response.body?.cancel();
  }
}

async function validateExcludedRoute(pathname) {
  const response = await fetch(new URL(pathname, baseUrl));
  if (!response.ok) {
    errors.push(`${pathname}: excluded route returned HTTP ${response.status}`);
    return;
  }
  const html = await response.text();
  const document = parse(html);
  const robots = document
    .querySelector('meta[name="robots"]')
    ?.getAttribute("content");
  if (!/\bnoindex\b/i.test(robots ?? "")) {
    errors.push(`${pathname}: excluded flow page is not marked noindex`);
  }
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

async function validateNoindexArchive(pathname) {
  try {
    if (canonicalByPath.has(pathname)) {
      errors.push(`${pathname}: noindex tag archive is present in the sitemap`);
    }
    const response = await fetch(new URL(pathname, baseUrl));
    if (!response.ok) {
      errors.push(`${pathname}: tag archive returned HTTP ${response.status}`);
      return;
    }
    const html = await response.text();
    discoverBlogArchives(html);
    const document = parse(html);
    recordLinks(document, pathname);
    const robots = document
      .querySelector('meta[name="robots"]')
      ?.getAttribute("content");
    if (!/\bnoindex\b/i.test(robots ?? "")) {
      errors.push(`${pathname}: thin tag archive is not marked noindex`);
    }
    if (/\bnofollow\b/i.test(robots ?? "")) {
      errors.push(`${pathname}: tag archive must allow link following`);
    }

    const canonicalLinks = document.querySelectorAll('link[rel="canonical"]');
    const expected = new URL(pathname, sitemapUrls[0]).href;
    if (
      canonicalLinks.length !== 1 ||
      normalizeUrl(
        new URL(canonicalLinks[0].getAttribute("href") ?? "", baseUrl).href,
      ) !== normalizeUrl(expected)
    ) {
      errors.push(`${pathname}: tag archive has an invalid self-canonical`);
    }
  } catch (error) {
    errors.push(
      `${pathname}: ${error instanceof Error ? error.message : error}`,
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
    ![301, 308].includes(response.status) ||
    locationPath !== expectedLocation
  ) {
    errors.push(
      `${pathname}: expected a permanent redirect to ${expectedLocation}, got ${response.status} ${location}`,
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
    if (BLOG_PAGINATION_PATH.test(pathname) && !queued.has(pathname)) {
      if (!canonicalByPath.has(pathname)) {
        errors.push(
          `${pathname}: discoverable canonical archive is absent from the sitemap`,
        );
      }
      queued.add(pathname);
      queue.push(pathname);
    } else if (
      TAG_ARCHIVE_PATH.test(pathname) &&
      !queuedNoindexArchives.has(pathname)
    ) {
      queuedNoindexArchives.add(pathname);
      noindexArchiveQueue.push(pathname);
    }
  }
}
