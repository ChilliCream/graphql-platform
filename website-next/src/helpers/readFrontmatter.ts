import fs from "node:fs";
import matter from "gray-matter";

export type DocFrontmatter = {
  title?: string;
  description?: string;
  [key: string]: unknown;
};

export function readFrontmatter(absPath: string): DocFrontmatter {
  const raw = fs.readFileSync(absPath, "utf-8");
  const { data } = matter(raw);
  return data as DocFrontmatter;
}
