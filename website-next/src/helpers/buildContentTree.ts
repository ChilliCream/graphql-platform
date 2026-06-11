import fs from "node:fs";
import path from "node:path";
import yaml from "js-yaml";

export type TreeNode = {
  title: string;
  href: string | null;
  children: TreeNode[];
};

type MetaItem = { path: string; title: string };
type Meta = { title: string; items?: MetaItem[] };

const META_FILE = "structure.yaml";

function abs(rel: string): string {
  return path.join(process.cwd(), "content", rel);
}

/**
 * Walks a content root and produces an ordered navigation tree mirroring the
 * directory structure. Each directory must contain a `structure.yaml` describing
 * its display title and the ordered list of children with their display names.
 *
 * `rootRel` is relative to the project's `content/` directory (e.g. `docs/fusion`).
 */
export function buildContentTree(
  rootRel: string,
  urlPrefix: string
): TreeNode[] {
  if (!fs.existsSync(abs(rootRel))) {
    return [];
  }
  return walk(rootRel, urlPrefix);
}

function readMeta(dirRel: string): Meta {
  const metaPath = abs(`${dirRel}/${META_FILE}`);
  if (!fs.existsSync(metaPath)) {
    throw new Error(`Missing ${META_FILE} in ${dirRel}`);
  }
  const parsed = yaml.load(fs.readFileSync(metaPath, "utf-8")) as Meta | null;
  if (!parsed || typeof parsed.title !== "string" || !parsed.title.trim()) {
    throw new Error(`Invalid ${META_FILE} in ${dirRel}: missing 'title'`);
  }
  return parsed;
}

function walk(dirRel: string, urlPrefix: string): TreeNode[] {
  const meta = readMeta(dirRel);
  const items = meta.items ?? [];
  const nodes: TreeNode[] = [];

  for (const item of items) {
    if (!item.path || !item.title) {
      throw new Error(
        `Invalid item in ${dirRel}/${META_FILE}: each item needs 'path' and 'title'`
      );
    }

    const fileExt = [".md", ".mdx"].find((ext) =>
      fs.existsSync(abs(`${dirRel}/${item.path}${ext}`))
    );

    if (fileExt) {
      const isIndex = item.path === "index";
      const href = isIndex ? urlPrefix : `${urlPrefix}/${item.path}`;
      nodes.push({ title: item.title, href, children: [] });
      continue;
    }

    const subRel = `${dirRel}/${item.path}`;
    const subAbs = abs(subRel);
    if (fs.existsSync(subAbs) && fs.statSync(subAbs).isDirectory()) {
      const childUrl = `${urlPrefix}/${item.path}`;
      const indexFile = ["index.md", "index.mdx"].find((n) =>
        fs.existsSync(abs(`${subRel}/${n}`))
      );
      const children = walk(subRel, childUrl);
      nodes.push({
        title: item.title,
        href: indexFile ? childUrl : null,
        children,
      });
      continue;
    }

    throw new Error(
      `Item '${item.path}' referenced in ${dirRel}/${META_FILE} does not exist on disk`
    );
  }

  return nodes;
}

export type Breadcrumb = { name: string; href: string | null };

/**
 * Resolves the navigation titles along a docs slug into breadcrumb entries by
 * locating the page in the product's navigation tree. The first entry is the
 * product itself (titled by its `structure.yaml`); the rest are the tree nodes
 * on the path to the page. `href` is null for groups without an index page.
 */
export function docBreadcrumbs(slug: string[]): Breadcrumb[] {
  const product = slug[0];
  const rootRel = `docs/${product}`;
  if (!fs.existsSync(abs(`${rootRel}/${META_FILE}`))) {
    return [];
  }

  const productCrumb: Breadcrumb = {
    name: readMeta(rootRel).title,
    href: `/docs/${product}`,
  };
  if (slug.length === 1) {
    return [productCrumb];
  }

  const tree = buildContentTree(rootRel, `/docs/${product}`);
  const trail = findTrail(tree, `/docs/${slug.join("/")}`);
  return trail ? [productCrumb, ...trail] : [productCrumb];
}

function findTrail(nodes: TreeNode[], targetHref: string): Breadcrumb[] | null {
  for (const node of nodes) {
    if (node.href === targetHref) {
      return [{ name: node.title, href: node.href }];
    }
    const sub = findTrail(node.children, targetHref);
    if (sub !== null) {
      return [{ name: node.title, href: node.href }, ...sub];
    }
  }
  return null;
}
