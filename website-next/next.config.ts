import path from "node:path";
import type { NextConfig } from "next";
import createMDX from "@next/mdx";
import { rehypePluginSpecs, remarkPluginSpecs } from "./src/mdx-plugins";

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

// Image optimization and git metadata are generated as explicit pre-build
// steps in the release workflow (see `yarn optimize-images` /
// `yarn generate-git-metadata`), not during `next build`. The app falls back
// to unoptimized images and static git metadata when those artifacts are
// absent (development and builds outside the workflow).
const config = withMDX(nextConfig);

export default config;
