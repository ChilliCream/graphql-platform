import fs from "node:fs";
import path from "node:path";
import { notFound } from "next/navigation";

const CONTENT_ROOT = path.join(process.cwd(), "blogs");

export const dynamicParams = false;

export function generateStaticParams(): { slug: string[] }[] {
  if (!fs.existsSync(CONTENT_ROOT)) {
    return [];
  }

  return walk(CONTENT_ROOT)
    .filter((f) => f.endsWith(".md"))
    .map((f) => path.relative(CONTENT_ROOT, f).replace(/\.md$/, ""))
    .map((rel) => rel.split(path.sep))
    .map((parts) =>
      parts[parts.length - 1] === "index" ? parts.slice(0, -1) : parts
    )
    .filter((slug) => slug.length > 0)
    .map((slug) => ({ slug }));
}

export default async function BlogPage({
  params,
}: {
  params: Promise<{ slug: string[] }>;
}) {
  const { slug } = await params;
  const joined = slug.join("/");

  let mod;
  try {
    mod = await import(`@/blogs/${joined}.md`);
  } catch {
    try {
      mod = await import(`@/blogs/${joined}/index.md`);
    } catch {
      notFound();
    }
  }

  const Post = mod.default;
  return <Post />;
}

function walk(dir: string): string[] {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  return entries.flatMap((e) => {
    const full = path.join(dir, e.name);
    return e.isDirectory() ? walk(full) : [full];
  });
}
