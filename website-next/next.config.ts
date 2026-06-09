import path from "node:path";
import type { NextConfig } from "next";
import createMDX from "@next/mdx";
import { PHASE_PRODUCTION_BUILD } from "next/constants";
import { rehypePluginSpecs, remarkPluginSpecs } from "./src/mdx-plugins";
import optimizeImages from "./src/image-optimization/generate.mjs";

// Build-time image optimization config (quality is configurable; default 90).
const imageOptimization = {
  quality: 90,
  widths: [640, 1080, 1920],
  formats: ["avif", "webp"],
  sourceDir: "public/images",
  outputDir: "public/_optimized/images",
  manifestPath: "public/_optimized/manifest.json",
};

const exportToc = path.resolve(process.cwd(), "src/recma/exportToc.mjs");

const nextConfig: NextConfig = {
  output: "export",
  images: {
    unoptimized: true,
  },
  pageExtensions: ["ts", "tsx", "md", "mdx"],
  serverExternalPackages: [
    "rehype-mermaid",
    "mermaid-isomorphic",
    "playwright",
    "playwright-core",
  ],
};

const withMDX = createMDX({
  extension: /\.(md|mdx)$/,
  options: {
    format: "mdx",
    remarkPlugins: [...remarkPluginSpecs],
    rehypePlugins: [...rehypePluginSpecs],
    recmaPlugins: [exportToc],
  },
});

const config = withMDX(nextConfig);

async function nextConfigForPhase(phase: string) {
  if (phase === PHASE_PRODUCTION_BUILD) {
    await optimizeImages(imageOptimization);
  }
  return config;
}

export default nextConfigForPhase;
