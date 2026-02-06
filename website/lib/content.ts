import fs from "fs";
import path from "path";
import matter from "gray-matter";

const WEBSITE_DIR = process.cwd();

export function getFilesRecursively(dir: string, ext = ".md"): string[] {
  const results: string[] = [];

  if (!fs.existsSync(dir)) return results;

  const entries = fs.readdirSync(dir, { withFileTypes: true });

  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...getFilesRecursively(fullPath, ext));
    } else if (entry.name.endsWith(ext)) {
      results.push(fullPath);
    }
  }

  return results;
}

export function readMarkdownFile(filePath: string) {
  const raw = fs.readFileSync(filePath, "utf-8");
  const { data: frontmatter, content } = matter(raw);
  return { frontmatter, content, filePath };
}

export function generateSlug(filePath: string, basePath: string): string {
  let relative = path.relative(basePath, filePath);
  // Remove extension
  relative = relative.replace(/\.mdx?$/, "");
  // Remove trailing /index
  relative = relative.replace(/\/index$/, "");
  // Normalize separators
  return "/" + relative.split(path.sep).join("/");
}

export function getContentDir(...segments: string[]): string {
  return path.join(WEBSITE_DIR, "src", ...segments);
}

interface BasicNavLink {
  path: string;
  title: string;
}

export function getBasicPageNavLinks(): BasicNavLink[] {
  const jsonPath = path.join(WEBSITE_DIR, "src", "basic", "basic.json");

  if (!fs.existsSync(jsonPath)) return [];

  const data = JSON.parse(fs.readFileSync(jsonPath, "utf-8"));

  return (data as BasicNavLink[]).map((link) => ({
    path: "/" + link.path,
    title: link.title,
  }));
}
