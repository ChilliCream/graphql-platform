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
import { ccAccent, ccBg, ccInk, ccSurface } from "@/src/theme/colors";

// TODO: Proper styling and layout of share cards

export const dynamicParams = false;

/** `#rrggbb` -> `rgba(r, g, b, a)`, so gradients derive from the same tokens. */
function rgba(hex: string, alpha: number): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

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
    <div
      style={{
        width: "100%",
        height: "100%",
        display: "flex",
        flexDirection: "column",
        justifyContent: "flex-end",
        padding: "72px",
        backgroundColor: ccBg,
        backgroundImage:
          `radial-gradient(1000px 600px at 85% 0%, ${rgba(ccAccent, 0.18)}, ${rgba(ccBg, 0)}), ` +
          `linear-gradient(135deg, ${ccSurface} 0%, ${ccBg} 60%)`,
        color: ccInk,
        fontFamily: "Inter",
      }}
    >
      <div
        style={{
          position: "absolute",
          top: "72px",
          right: "72px",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          padding: "16px 28px",
          borderRadius: "16px",
          border: `2px solid ${ccAccent}`,
          backgroundColor: rgba(ccAccent, 0.08),
          fontSize: "30px",
          fontWeight: 700,
          color: ccInk,
        }}
      >
        {eyebrow}
      </div>

      <div
        style={{
          display: "flex",
          flexDirection: "column",
          gap: "20px",
          maxWidth: "900px",
        }}
      >
        <div
          style={{
            display: "flex",
            fontSize: "28px",
            fontWeight: 700,
            letterSpacing: "2px",
            textTransform: "uppercase",
            color: ccAccent,
          }}
        >
          {eyebrow}
        </div>
        <div
          style={{
            display: "flex",
            fontSize: "68px",
            fontWeight: 700,
            lineHeight: 1.1,
            color: ccInk,
          }}
        >
          {title}
        </div>
      </div>
    </div>,
    {
      ...size,
      fonts,
    },
  );
}
