import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { NodeHtmlMarkdown } from "node-html-markdown";
import { parse } from "node-html-parser";
import { parseSitemapUrls } from "./parse-sitemap.mjs";

const PROJECT_ROOT = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
);
const OUTPUT_ROOT = path.join(PROJECT_ROOT, "out");

const PRODUCT_NAMES = new Map([
  ["hotchocolate", "Hot Chocolate"],
  ["fusion", "Fusion"],
  ["strawberryshake", "Strawberry Shake"],
  ["nitro", "Nitro"],
  ["mocha", "Mocha"],
  ["skillz", "Skills"],
]);
const PRODUCT_ORDER = new Map(
  [...PRODUCT_NAMES.keys()].map((product, index) => [product, index]),
);

const STRIP_FROM_HTML = ["script", "style", "noscript", "template"];
const STRIP_FROM_CONTENT = [
  "svg",
  "canvas",
  "nav",
  "aside",
  "form",
  "button",
  "input",
  "select",
  "textarea",
  '[aria-hidden="true"]',
  "[data-llms-ignore]",
  ".heading-anchor",
];

const markdownConverter = new NodeHtmlMarkdown({
  bulletMarker: "-",
  codeBlockStyle: "fenced",
  keepDataImages: false,
  maxConsecutiveNewlines: 2,
  useInlineLinks: true,
});

function cleanInlineText(value) {
  return value.replace(/\s+/g, " ").trim();
}

function escapeLinkLabel(value) {
  return cleanInlineText(value).replaceAll("[", "\\[").replaceAll("]", "\\]");
}

function stripBrandSuffix(title) {
  return cleanInlineText(title)
    .replace(/\s+[-|]\s+ChilliCream$/i, "")
    .trim();
}

function stripBuildOnlyHtml(html) {
  return STRIP_FROM_HTML.reduce(
    (current, tag) =>
      current.replace(
        new RegExp(`<${tag}\\b[^>]*>[\\s\\S]*?<\\/${tag}>`, "gi"),
        "",
      ),
    html,
  );
}

function isDetailArticle(url) {
  const pathname = new URL(url).pathname;
  return (
    pathname.startsWith("/docs/") ||
    /^\/blog\/\d{4}-\d{2}-\d{2}-/.test(pathname)
  );
}

function contentRootFor(document, url) {
  if (isDetailArticle(url)) {
    const article = document.querySelector("main article");
    if (article) {
      for (const nestedArticle of article.querySelectorAll("article")) {
        nestedArticle.remove();
      }
      return article;
    }
  }

  return (
    document.querySelector("body > main") ?? document.querySelector("main")
  );
}

function makeAbsolute(value, baseUrl) {
  const trimmed = value.trim();
  if (
    trimmed === "" ||
    trimmed.startsWith("#") ||
    /^(?:data|javascript|mailto|tel):/i.test(trimmed)
  ) {
    return trimmed;
  }

  try {
    return new URL(trimmed, baseUrl).href;
  } catch {
    return trimmed;
  }
}

function prepareContent(root, canonicalUrl) {
  for (const selector of STRIP_FROM_CONTENT) {
    for (const node of root.querySelectorAll(selector)) {
      node.remove();
    }
  }

  for (const node of root.querySelectorAll("[href]")) {
    const href = node.getAttribute("href");
    if (href) {
      node.setAttribute("href", makeAbsolute(href, canonicalUrl));
    }
  }

  for (const attribute of ["src", "poster"]) {
    for (const node of root.querySelectorAll(`[${attribute}]`)) {
      const value = node.getAttribute(attribute);
      if (value) {
        node.setAttribute(attribute, makeAbsolute(value, canonicalUrl));
      }
    }
  }

  for (const image of root.querySelectorAll("img[srcset]")) {
    image.removeAttribute("srcset");
  }
}

function normalizeMarkdown(markdown) {
  return markdown
    .replace(/\[\s*#\s*\]\([^)]*\)/g, "")
    .replace(/\)\[/g, ") [")
    .replace(/[ \t]+$/gm, "")
    .replace(/\n{3,}/g, "\n\n")
    .trim();
}

function removeLeadingTitle(markdown) {
  const lines = markdown.split("\n");
  const firstContentLine = lines.findIndex((line) => line.trim() !== "");
  if (firstContentLine === -1) {
    return "";
  }

  if (/^#\s+/.test(lines[firstContentLine])) {
    lines.splice(firstContentLine, 1);
  }

  return normalizeMarkdown(lines.join("\n"));
}

function outputHtmlPath(url) {
  const pathname = decodeURIComponent(new URL(url).pathname);
  const relative =
    pathname === "/" ? "index.html" : `${pathname.slice(1)}.html`;
  const candidate = path.resolve(OUTPUT_ROOT, relative);
  const rootPrefix = `${path.resolve(OUTPUT_ROOT)}${path.sep}`;

  if (
    candidate !== path.join(OUTPUT_ROOT, "index.html") &&
    !candidate.startsWith(rootPrefix)
  ) {
    throw new Error(`Sitemap URL escaped the static output root: ${url}`);
  }

  return candidate;
}

function markdownRelativePath(url) {
  const pathname = decodeURIComponent(new URL(url).pathname);
  return pathname === "/" ? "index.md" : `${pathname.slice(1)}.md`;
}

function markdownUrl(url) {
  const parsed = new URL(url);
  parsed.pathname =
    parsed.pathname === "/" ? "/index.md" : `${parsed.pathname}.md`;
  parsed.search = "";
  parsed.hash = "";
  return parsed.href;
}

async function readSitemapUrls() {
  const sitemapPath = path.join(OUTPUT_ROOT, "sitemap.xml");
  const xml = await fs.readFile(sitemapPath, "utf8");
  const urls = parseSitemapUrls(xml);

  if (urls.length === 0) {
    throw new Error(`No URLs found in ${sitemapPath}`);
  }

  if (new Set(urls).size !== urls.length) {
    throw new Error("The sitemap contains duplicate canonical URLs.");
  }

  return urls;
}

async function readPage(url) {
  let htmlPath = outputHtmlPath(url);
  let html;

  try {
    html = await fs.readFile(htmlPath, "utf8");
  } catch (error) {
    if (error?.code !== "ENOENT") {
      throw error;
    }

    const pathname = decodeURIComponent(new URL(url).pathname).replace(
      /^\//,
      "",
    );
    const fallback = path.join(OUTPUT_ROOT, pathname, "index.html");
    htmlPath = fallback;
    html = await fs.readFile(fallback, "utf8");
  }

  const document = parse(stripBuildOnlyHtml(html), { comment: false });
  const root = contentRootFor(document, url);
  if (!root) {
    throw new Error(`No main content element found for ${url}`);
  }

  prepareContent(root, url);

  const contentHeading = root.querySelector("h1");
  const h1 = cleanInlineText(contentHeading?.textContent ?? "");
  const documentTitle = stripBrandSuffix(
    document.querySelector("title")?.textContent ?? "",
  );
  const title = documentTitle || h1;
  if (!title) {
    throw new Error(`No page title found for ${url}`);
  }
  contentHeading?.remove();

  const description = cleanInlineText(
    document
      .querySelector('meta[name="description"]')
      ?.getAttribute("content") ?? "",
  );
  const renderedMarkdown = normalizeMarkdown(
    markdownConverter.translate(root.innerHTML),
  );
  const body = removeLeadingTitle(renderedMarkdown);
  if (body.length < 40) {
    throw new Error(`Generated Markdown is unexpectedly short for ${url}`);
  }

  const page = {
    body,
    description,
    markdownUrl: markdownUrl(url),
    title,
    url,
  };
  const alternateLink = `<link rel="alternate" type="text/markdown" href="${page.markdownUrl}" data-llms-generated="true"/>`;
  if (!html.includes('data-llms-generated="true"')) {
    if (!html.includes("</head>")) {
      throw new Error(`No closing head element found for ${url}`);
    }
    await fs.writeFile(
      htmlPath,
      html.replace("</head>", `${alternateLink}</head>`),
      "utf8",
    );
  }

  return page;
}

function productForPage(page) {
  const segments = new URL(page.url).pathname.split("/").filter(Boolean);
  return segments[0] === "docs" && segments.length > 1 ? segments[1] : null;
}

function blogDate(pathname) {
  return pathname.match(/^\/blog\/(\d{4}-\d{2}-\d{2})-/)?.[1] ?? "";
}

function comparePages(left, right) {
  const leftPath = new URL(left.url).pathname;
  const rightPath = new URL(right.url).pathname;

  if (leftPath === "/") return -1;
  if (rightPath === "/") return 1;

  const leftBlogDate = blogDate(leftPath);
  const rightBlogDate = blogDate(rightPath);
  if (leftBlogDate || rightBlogDate) {
    if (!leftBlogDate) return -1;
    if (!rightBlogDate) return 1;
    return rightBlogDate.localeCompare(leftBlogDate);
  }

  const leftProduct = productForPage(left);
  const rightProduct = productForPage(right);
  if (leftProduct && rightProduct && leftProduct !== rightProduct) {
    return (
      (PRODUCT_ORDER.get(leftProduct) ?? Number.MAX_SAFE_INTEGER) -
      (PRODUCT_ORDER.get(rightProduct) ?? Number.MAX_SAFE_INTEGER)
    );
  }

  return leftPath.localeCompare(rightPath);
}

function pageListItem(page) {
  const description = page.description ? `: ${page.description}` : "";
  return `- [${escapeLinkLabel(page.title)}](${page.markdownUrl})${description}`;
}

function catalog(title, summary, sections, details = []) {
  const lines = [`# ${title}`, "", `> ${summary}`];

  if (details.length > 0) {
    lines.push("", ...details);
  }

  for (const section of sections) {
    if (section.items.length === 0) {
      continue;
    }
    lines.push("", `## ${section.title}`, "", ...section.items);
  }

  return `${lines.join("\n").trim()}\n`;
}

function pageDocument(page) {
  const lines = [`# ${page.title}`];
  if (page.description) {
    lines.push("", `> ${page.description}`);
  }
  lines.push("", `Canonical source: ${page.url}`, "", page.body);
  return `${lines.join("\n").trim()}\n`;
}

function fullCorpus(title, summary, pages) {
  const header = [
    `# ${title}`,
    "",
    `> ${summary}`,
    "",
    "This generated compatibility export can exceed many model context windows. Prefer the scoped llms.txt and llms-full.txt files when you only need one product or content area.",
  ].join("\n");

  return `${header}\n\n${pages
    .map((page) => `---\n\n${pageDocument(page).trim()}`)
    .join("\n\n")}\n`;
}

async function writeOutput(relativePath, content) {
  const outputPath = path.join(OUTPUT_ROOT, relativePath);
  await fs.mkdir(path.dirname(outputPath), { recursive: true });
  await fs.writeFile(outputPath, content, "utf8");
}

function pagesUnder(pages, prefix, includeRoot = true) {
  return pages.filter((page) => {
    const pathname = new URL(page.url).pathname;
    return pathname === prefix
      ? includeRoot
      : pathname.startsWith(`${prefix}/`);
  });
}

function scopeLinks(origin) {
  return [
    `- [Complete site context](${origin}/llms-full.txt): All public sitemap content in one large compatibility export.`,
    `- [Documentation context](${origin}/docs/llms-full.txt): All product documentation.`,
    `- [Blog context](${origin}/blog/llms-full.txt): All public ChilliCream blog posts.`,
  ];
}

async function writeScopedFiles(relativeRoot, title, summary, pages) {
  const sorted = [...pages].sort(comparePages);
  await writeOutput(
    `${relativeRoot}/llms.txt`,
    catalog(title, summary, [
      { title: "Pages", items: sorted.map(pageListItem) },
    ]),
  );
  await writeOutput(
    `${relativeRoot}/llms-full.txt`,
    fullCorpus(`${title} full context`, summary, sorted),
  );
}

async function writePerPageMarkdown(pages) {
  await Promise.all(
    pages.map((page) =>
      writeOutput(markdownRelativePath(page.url), pageDocument(page)),
    ),
  );
}

export async function generateLlmsFiles() {
  const urls = await readSitemapUrls();
  const pages = [];
  for (const url of urls) {
    pages.push(await readPage(url));
  }
  pages.sort(comparePages);

  const origin = new URL(pages[0].url).origin;
  const docs = pagesUnder(pages, "/docs");
  const blog = pagesUnder(pages, "/blog");
  const products = pagesUnder(pages, "/products");
  const platform = pagesUnder(pages, "/platform");
  const services = [
    ...pagesUnder(pages, "/services"),
    ...pagesUnder(pages, "/help"),
  ];
  const legal = pages.filter((page) => {
    const pathname = new URL(page.url).pathname;
    return pathname.startsWith("/legal/") || pathname.startsWith("/licensing/");
  });
  const usedByScopes = new Set(
    [...docs, ...blog, ...products, ...platform, ...services, ...legal].map(
      (page) => page.url,
    ),
  );
  const startHere = pages.filter((page) => !usedByScopes.has(page.url));

  await writePerPageMarkdown(pages);

  await Promise.all([
    writeScopedFiles(
      "docs",
      "ChilliCream documentation",
      "Technical documentation for Hot Chocolate, Fusion, Strawberry Shake, Nitro, Mocha, and Skills.",
      docs,
    ),
    writeScopedFiles(
      "blog",
      "ChilliCream blog",
      "Announcements, technical deep dives, and guides from the ChilliCream team, newest first.",
      blog,
    ),
    writeScopedFiles(
      "products",
      "ChilliCream products",
      "Product overviews for ChilliCream's open-source .NET runtimes and the commercial Nitro control plane.",
      products,
    ),
    writeScopedFiles(
      "platform",
      "ChilliCream platform",
      "GraphQL API lifecycle capabilities for development, federation, analytics, release safety, and agentic coding.",
      platform,
    ),
    writeScopedFiles(
      "services",
      "ChilliCream services and support",
      "Training, advisory, support, and self-service help for teams building GraphQL systems.",
      services,
    ),
    writeScopedFiles(
      "legal",
      "ChilliCream legal and licensing",
      "Public legal policies and the ChilliCream software license.",
      legal,
    ),
  ]);

  const docsByProduct = new Map();
  for (const page of docs) {
    const product = productForPage(page);
    if (product) {
      const productPages = docsByProduct.get(product) ?? [];
      productPages.push(page);
      docsByProduct.set(product, productPages);
    }
  }
  await Promise.all(
    [...docsByProduct.entries()].map(([product, productPages]) =>
      writeScopedFiles(
        `docs/${product}`,
        `${PRODUCT_NAMES.get(product) ?? product} documentation`,
        `Technical documentation for ${PRODUCT_NAMES.get(product) ?? product}.`,
        productPages,
      ),
    ),
  );

  const rootCatalog = catalog(
    "ChilliCream",
    "ChilliCream is a GraphQL platform for .NET teams, with open-source runtimes and the commercial Nitro control plane.",
    [
      { title: "Start Here", items: startHere.map(pageListItem) },
      {
        title: "Products and Platform",
        items: [
          `- [Product catalog](${origin}/products/llms.txt): Product pages for Hot Chocolate, Strawberry Shake, Mocha, and Nitro.`,
          `- [Platform catalog](${origin}/platform/llms.txt): Federation, analytics, release safety, ecosystem, and agentic coding pages.`,
        ],
      },
      {
        title: "Services and Support",
        items: [
          `- [Services catalog](${origin}/services/llms.txt): Training, advisory, support, and help pages.`,
        ],
      },
      {
        title: "Documentation",
        items: [
          `- [Documentation catalog](${origin}/docs/llms.txt): Every public documentation page, grouped into a generated machine-readable index.`,
          ...[...docsByProduct.keys()]
            .sort(
              (left, right) =>
                (PRODUCT_ORDER.get(left) ?? Number.MAX_SAFE_INTEGER) -
                (PRODUCT_ORDER.get(right) ?? Number.MAX_SAFE_INTEGER),
            )
            .map(
              (product) =>
                `- [${PRODUCT_NAMES.get(product) ?? product} documentation](${origin}/docs/${product}/llms.txt): Scoped documentation index.`,
            ),
        ],
      },
      {
        title: "Blog",
        items: [
          `- [Blog catalog](${origin}/blog/llms.txt): Every public blog post, newest first.`,
        ],
      },
      {
        title: "Optional",
        items: [
          ...scopeLinks(origin),
          `- [Legal and licensing](${origin}/legal/llms.txt): Policies and software licensing.`,
          `- [RSS feed](${origin}/blog/rss.xml): Blog updates in RSS format.`,
          `- [XML sitemap](${origin}/sitemap.xml): Canonical indexable HTML URLs.`,
        ],
      },
    ],
    [
      "Use the scoped catalogs when possible. Each catalog links to clean Markdown generated from the final rendered website during every production build.",
    ],
  );

  await Promise.all([
    writeOutput("llms.txt", rootCatalog),
    writeOutput(
      "llms-full.txt",
      fullCorpus(
        "ChilliCream full site context",
        "All public ChilliCream pages included in the XML sitemap, converted from final rendered HTML.",
        pages,
      ),
    ),
  ]);

  return {
    blogPages: blog.length,
    docsPages: docs.length,
    pages: pages.length,
    products: docsByProduct.size,
  };
}

if (
  process.argv[1] &&
  import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href
) {
  const result = await generateLlmsFiles();
  console.log(
    `[llms] Generated ${result.pages} page documents, ${result.docsPages} docs pages, ${result.blogPages} blog pages, and ${result.products} product scopes.`,
  );
}
