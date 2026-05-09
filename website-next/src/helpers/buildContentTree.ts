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

/**
 * Walks a content root and produces an ordered navigation tree mirroring the
 * directory structure. Each directory must contain a `_meta.yml` describing its
 * display title and the ordered list of children with their display names.
 */
export function buildContentTree(rootAbs: string, urlPrefix: string): TreeNode[] {
  if (!fs.existsSync(rootAbs)) {
    return [];
  }
  return walk(rootAbs, urlPrefix);
}

function readMeta(dirAbs: string): Meta {
  const metaPath = path.join(dirAbs, META_FILE);
  if (!fs.existsSync(metaPath)) {
    throw new Error(`Missing ${META_FILE} in ${dirAbs}`);
  }
  const parsed = yaml.load(fs.readFileSync(metaPath, "utf-8")) as Meta | null;
  if (!parsed || typeof parsed.title !== "string" || !parsed.title.trim()) {
    throw new Error(`Invalid ${META_FILE} in ${dirAbs}: missing 'title'`);
  }
  return parsed;
}

function walk(dirAbs: string, urlPrefix: string): TreeNode[] {
  const meta = readMeta(dirAbs);
  const items = meta.items ?? [];
  const nodes: TreeNode[] = [];

  for (const item of items) {
    if (!item.path || !item.title) {
      throw new Error(
        `Invalid item in ${path.join(dirAbs, META_FILE)}: each item needs 'path' and 'title'`
      );
    }

    const fileExt = [".md", ".mdx"].find((ext) =>
      fs.existsSync(path.join(dirAbs, `${item.path}${ext}`))
    );

    if (fileExt) {
      const isIndex = item.path === "index";
      const href = isIndex ? urlPrefix : `${urlPrefix}/${item.path}`;
      nodes.push({ title: item.title, href, children: [] });
      continue;
    }

    const subAbs = path.join(dirAbs, item.path);
    if (fs.existsSync(subAbs) && fs.statSync(subAbs).isDirectory()) {
      const childUrl = `${urlPrefix}/${item.path}`;
      const indexFile = ["index.md", "index.mdx"]
        .map((n) => path.join(subAbs, n))
        .find((p) => fs.existsSync(p));
      const children = walk(subAbs, childUrl);
      nodes.push({
        title: item.title,
        href: indexFile ? childUrl : null,
        children,
      });
      continue;
    }

    throw new Error(
      `Item '${item.path}' referenced in ${path.join(dirAbs, META_FILE)} does not exist on disk`
    );
  }

  return nodes;
}
