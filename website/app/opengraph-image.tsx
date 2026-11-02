import { ImageResponse } from "next/og";
import { loadShareCardFonts } from "@/src/og/fonts";
import { enableSatoriHooks } from "@/src/og/satoriHooks";
import { ShareCard } from "@/src/og/ShareCard";

// Required under `output: export` for this paramless metadata route, which has
// no `generateStaticParams` to imply prerendering (unlike the per-doc card).
export const dynamic = "force-static";

export const alt = "ChilliCream GraphQL Platform";

export const size = {
  width: 1200,
  height: 630,
};

export const contentType = "image/png";

export default async function Image() {
  enableSatoriHooks();

  const fonts = await loadShareCardFonts();

  return new ImageResponse(<ShareCard />, {
    ...size,
    fonts,
  });
}
