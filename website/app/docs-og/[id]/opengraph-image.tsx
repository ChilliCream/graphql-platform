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
import { DocsShareCard } from "@/src/og/DocsShareCard";
import { loadShareCardFonts } from "@/src/og/fonts";
import { enableSatoriHooks } from "@/src/og/satoriHooks";

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
  enableSatoriHooks();

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

  const fonts = await loadShareCardFonts();

  return new ImageResponse(
    <DocsShareCard
      eyebrow={eyebrow}
      title={title}
      productSlug={product?.slug}
    />,
    {
      ...size,
      fonts,
    },
  );
}
