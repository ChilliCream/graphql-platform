import {
  getDocsConfig,
  getAllDocPages,
  type DocsProduct,
  type DocsNavItem,
} from "./docs";
import { getAllBlogPosts } from "./blog";

const SITE_URL = "https://chillicream.com";

function buildDocUrl(
  product: DocsProduct,
  versionPath: string,
  itemPath: string
): string {
  const parts = ["/docs", product.path];
  if (versionPath) {
    parts.push(versionPath);
  }
  if (itemPath && itemPath !== "index") {
    parts.push(itemPath);
  }
  return parts.join("/");
}

function flattenNavItems(
  product: DocsProduct,
  versionPath: string,
  items: DocsNavItem[],
  parentPath = ""
): { url: string; title: string }[] {
  const result: { url: string; title: string }[] = [];
  for (const item of items) {
    const itemPath = parentPath ? `${parentPath}/${item.path}` : item.path;
    // Skip nested "index" sub-items — the parent already covers this URL
    if (!(parentPath && item.path === "index")) {
      result.push({
        url: buildDocUrl(product, versionPath, itemPath),
        title: item.title,
      });
    }
    if (item.items) {
      result.push(
        ...flattenNavItems(product, versionPath, item.items, itemPath)
      );
    }
  }
  return result;
}

function getLatestVersion(product: DocsProduct) {
  if (product.latestStableVersion) {
    return product.versions.find((v) => v.path === product.latestStableVersion);
  }
  return product.versions[0];
}

const PRODUCT_DESCRIPTIONS: Record<string, string> = {
  hotchocolate: "GraphQL server framework for .NET",
  strawberryshake: "Type-safe GraphQL client for .NET",
  fusion: "Distributed GraphQL gateway",
  nitro: "GraphQL IDE and API management",
};

export function generateLlmsTxt(): string {
  const products = getDocsConfig();
  const blogPosts = getAllBlogPosts();

  const lines: string[] = [
    "# ChilliCream GraphQL Platform",
    "",
    "> The ChilliCream GraphQL Platform is an open-source ecosystem for building GraphQL APIs in .NET, including Hot Chocolate (server), Strawberry Shake (client), Green Donut (DataLoader), Fusion (distributed GraphQL), and Nitro (GraphQL IDE).",
    "",
    `For complete documentation in a single file, see [Full Documentation](${SITE_URL}/llms-full.txt)`,
    "",
    "## Documentation",
    "",
  ];

  // Product overview links
  for (const product of products) {
    const version = getLatestVersion(product);
    const versionPath = version?.path ?? "";
    const url = buildDocUrl(product, versionPath, "index");
    const desc = PRODUCT_DESCRIPTIONS[product.path] || product.description;
    lines.push(`- [${product.title}](${url}): ${desc}`);
  }
  lines.push("");

  // Per-product nav items
  for (const product of products) {
    const version = getLatestVersion(product);
    if (!version) continue;

    lines.push(`### ${product.title}`, "");
    const navItems = flattenNavItems(product, version.path, version.items);
    for (const item of navItems) {
      lines.push(`- [${item.title}](${item.url})`);
    }
    lines.push("");
  }

  // Blog
  lines.push("## Blog", "");
  const recentPosts = blogPosts.slice(0, 20);
  for (const post of recentPosts) {
    const desc = post.description ? `: ${post.description}` : "";
    lines.push(`- [${post.title}](${post.path})${desc}`);
  }
  lines.push("");

  return lines.join("\n");
}

export function generateLlmsFullTxt(): string {
  const products = getDocsConfig();
  const allPages = getAllDocPages();

  const lines: string[] = [
    "# ChilliCream GraphQL Platform - Full Documentation",
    "",
    "> The ChilliCream GraphQL Platform is an open-source ecosystem for building GraphQL APIs in .NET, including Hot Chocolate (server), Strawberry Shake (client), Green Donut (DataLoader), Fusion (distributed GraphQL), and Nitro (GraphQL IDE).",
    "",
  ];

  for (const product of products) {
    const version = getLatestVersion(product);
    if (!version) continue;

    const versionPath = version.path;
    const navItems = flattenNavItems(product, versionPath, version.items);

    for (const navItem of navItems) {
      // Find matching page content
      const page = allPages.find((p) => p.slug === navItem.url);
      if (!page || !page.content.trim()) continue;

      lines.push(`## ${navItem.url}`, "");
      const title = page.frontmatter?.title || navItem.title;
      lines.push(`# ${title}`, "");
      lines.push(page.content.trim(), "");
      lines.push("---", "");
    }
  }

  return lines.join("\n");
}
