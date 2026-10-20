import { ImageResponse } from "next/og";
import { listComparisons } from "@/src/helpers/comparisons";
import { DocsShareCard } from "@/src/og/DocsShareCard";
import { loadShareCardFonts } from "@/src/og/fonts";
import {
  shareCardContentType,
  shareCardSize,
} from "@/src/og/pageShareCardImage";
import { enableSatoriHooks } from "@/src/og/satoriHooks";

export const dynamicParams = false;

export const alt = "ChilliCream product comparison";
export const size = shareCardSize;
export const contentType = shareCardContentType;

type Params = { slug: string };

export function generateStaticParams(): Params[] {
  const params = listComparisons().map((comparison) => ({
    slug: comparison.slug,
  }));
  return params.length > 0 ? params : [{ slug: "__empty__" }];
}

export default async function Image({ params }: { params: Promise<Params> }) {
  enableSatoriHooks();

  const { slug } = await params;
  const comparison = listComparisons().find(
    (candidate) => candidate.slug === slug,
  );
  const fonts = await loadShareCardFonts();

  return new ImageResponse(
    <DocsShareCard
      eyebrow="Compare"
      title={comparison?.title ?? "Compare"}
      productSlug={comparison?.product.slug}
    />,
    { ...shareCardSize, fonts },
  );
}
