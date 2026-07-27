import { readFile } from "node:fs/promises";
import path from "node:path";

const FONT_DIR = path.join(process.cwd(), "src/og/fonts");

/**
 * Shape `ImageResponse` (from `next/og`) expects for the `fonts` option.
 */
export type OgFont = {
  name: string;
  data: Buffer;
  weight: 400 | 600 | 700;
  style: "normal";
};

/**
 * Loads the vendored share-card fonts as raw bytes for `ImageResponse`:
 * Inter for body/title copy and Josefin Sans for the display headline (matching
 * the landing hero). Reading from `process.cwd()` works at build time, which is
 * required for the static export (`output: "export"`).
 */
export async function loadShareCardFonts(): Promise<OgFont[]> {
  const [interRegular, interBold, josefinSemiBold, josefinBold] =
    await Promise.all([
      readFile(path.join(FONT_DIR, "Inter-Regular.ttf")),
      readFile(path.join(FONT_DIR, "Inter-Bold.ttf")),
      readFile(path.join(FONT_DIR, "JosefinSans-600.woff")),
      readFile(path.join(FONT_DIR, "JosefinSans-700.woff")),
    ]);

  return [
    { name: "Inter", data: interRegular, weight: 400, style: "normal" },
    { name: "Inter", data: interBold, weight: 700, style: "normal" },
    {
      name: "Josefin Sans",
      data: josefinSemiBold,
      weight: 600,
      style: "normal",
    },
    { name: "Josefin Sans", data: josefinBold, weight: 700, style: "normal" },
  ];
}
