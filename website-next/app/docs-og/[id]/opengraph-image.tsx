import path from "node:path";
import { ImageResponse } from "next/og";
import { PRODUCTS } from "@/src/data/products";
import {
  CONTENT_ROOT,
  decodeDocId,
  encodeDocId,
  listDocSlugs,
  resolveFile,
} from "@/src/helpers/docsParams";
import { readFrontmatter } from "@/src/helpers/readFrontmatter";
import { loadInterFonts } from "@/src/og/fonts";
import { ShareCard } from "@/src/og/ShareCard";

// TODO: Proper styling and layout of share cards

export const dynamicParams = false;

export const alt = "ChilliCream documentation";

export const size = {
  width: 1200,
  height: 630,
};

export const contentType = "image/png";

type Params = {
  id: string;
};

export function generateStaticParams(): Params[] {
  return listDocSlugs().map((slug) => ({ id: encodeDocId(slug) }));
}

export default async function Image({ params }: { params: Promise<Params> }) {
  const { id } = await params;
  const slug = decodeDocId(id);
  const rel = resolveFile(slug);
  const frontmatter = rel
    ? readFrontmatter(path.join(CONTENT_ROOT, rel))
    : null;

  const productSlug = slug[0];
  const product = PRODUCTS.find((p) => p.slug === productSlug);
  const eyebrow = product?.title ?? "ChilliCream";
  const title = frontmatter?.title ?? product?.title ?? "ChilliCream";

  const fonts = await loadInterFonts();

  return new ImageResponse(
    <ShareCard badge={eyebrow} eyebrow={eyebrow} title={title} />,
    {
      ...size,
      fonts,
    },
  );
}
