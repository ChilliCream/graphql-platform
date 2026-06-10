import { readFile } from "node:fs/promises";
import path from "node:path";

const FONT_DIR = path.join(process.cwd(), "src/og/fonts");

/**
 * Shape `ImageResponse` (from `next/og`) expects for the `fonts` option.
 */
export type OgFont = {
  name: string;
  data: Buffer;
  weight: 400 | 700;
  style: "normal";
};

/**
 * Loads the vendored Inter TTFs (regular + bold) as raw font bytes for
 * `ImageResponse`. Reading from `process.cwd()` works at build time, which is
 * required for the static export (`output: "export"`).
 */
export async function loadInterFonts(): Promise<OgFont[]> {
  const [regular, bold] = await Promise.all([
    readFile(path.join(FONT_DIR, "Inter-Regular.ttf")),
    readFile(path.join(FONT_DIR, "Inter-Bold.ttf")),
  ]);

  return [
    { name: "Inter", data: regular, weight: 400, style: "normal" },
    { name: "Inter", data: bold, weight: 700, style: "normal" },
  ];
}
