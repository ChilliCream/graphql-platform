import fs from "node:fs";
import path from "node:path";
import { notFound } from "next/navigation";

const CONTENT_ROOT = path.join(process.cwd(), "docs");

export const dynamicParams = false;

export function generateStaticParams(): { slug: string[] }[] {
  return walk(CONTENT_ROOT)
    .filter((f) => /\.mdx?$/.test(f))
    .map((f) => path.relative(CONTENT_ROOT, f).replace(/\.mdx?$/, ""))
    .map((rel) => rel.split(path.sep))
    .map((parts) =>
      parts[parts.length - 1] === "index" ? parts.slice(0, -1) : parts
    )
    .filter((slug) => slug.length > 0)
    .map((slug) => ({ slug }));
}

export default async function DocPage({
  params,
}: {
  params: Promise<{ slug: string[] }>;
}) {
  const { slug } = await params;
  const rel = resolveFile(slug);

  if (rel === null) {
    notFound();
  }

  const mod = await import(`@/docs/${rel}`);
  const Doc = mod.default;
  return <Doc />;
}

function resolveFile(slug: string[]): string | null {
  const joined = slug.join("/");
  const candidates = [
    `${joined}.md`,
    `${joined}.mdx`,
    `${joined}/index.md`,
    `${joined}/index.mdx`,
  ];

  for (const c of candidates) {
    if (fs.existsSync(path.join(CONTENT_ROOT, c))) {
      return c;
    }
  }
  return null;
}

function walk(dir: string): string[] {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  return entries.flatMap((e) => {
    const full = path.join(dir, e.name);
    return e.isDirectory() ? walk(full) : [full];
  });
}
