import { ImageResponse } from "next/og";
import { loadInterFonts } from "@/src/og/fonts";
import { ShareCard } from "@/src/og/ShareCard";

// TODO: Proper styling and layout of share cards

// Required under `output: export` for this paramless metadata route, which has
// no `generateStaticParams` to imply prerendering (unlike the per-doc card).
export const dynamic = "force-static";

// Mirrors the brand strings in app/layout.tsx (TITLE is the headline).
const TITLE = "ChilliCream GraphQL Platform";

export const alt = TITLE;

export const size = {
  width: 1200,
  height: 630,
};

export const contentType = "image/png";

export default async function Image() {
  const fonts = await loadInterFonts();

  return new ImageResponse(
    <ShareCard badge="ChilliCream" eyebrow="chillicream.com" title={TITLE} />,
    {
      ...size,
      fonts,
    },
  );
}
