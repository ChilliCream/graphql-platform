import fs from "node:fs";
import path from "node:path";
import { readFrontmatter } from "./readFrontmatter";

export type TreeNode = {
  title: string;
  href: string | null;
  children: TreeNode[];
};

/**
 * Walks a content root (docs/ or blogs/) and produces a navigation tree
 * mirroring the directory structure. Title comes from frontmatter `title`,
 * falling back to the slug segment.
 */
export function buildContentTree(rootAbs: string, urlPrefix: string): TreeNode[] {
  if (!fs.existsSync(rootAbs)) {
    return [];
  }
  return walk(rootAbs, urlPrefix);
}

function walk(dirAbs: string, urlPrefix: string): TreeNode[] {
  const entries = fs
    .readdirSync(dirAbs, { withFileTypes: true })
    .sort((a, b) => a.name.localeCompare(b.name));

  const folders = entries.filter((e) => e.isDirectory());
  const files = entries.filter(
    (e) => e.isFile() && /\.mdx?$/i.test(e.name) && !/^index\.mdx?$/i.test(e.name)
  );

  const nodes: TreeNode[] = [];

  for (const folder of folders) {
    const indexFile = ["index.md", "index.mdx"]
      .map((n) => path.join(dirAbs, folder.name, n))
      .find((p) => fs.existsSync(p));

    const href = `${urlPrefix}/${folder.name}`;
    const title = indexFile
      ? readFrontmatter(indexFile).title ?? folder.name
      : folder.name;
    const children = walk(path.join(dirAbs, folder.name), href);
    nodes.push({ title, href: indexFile ? href : null, children });
  }

  for (const file of files) {
    const slug = file.name.replace(/\.mdx?$/i, "");
    const href = `${urlPrefix}/${slug}`;
    const title =
      readFrontmatter(path.join(dirAbs, file.name)).title ?? slug;
    nodes.push({ title, href, children: [] });
  }

  return nodes;
}
