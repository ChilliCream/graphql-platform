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
  experimental: {
    inlineCss: true,
  },
  serverExternalPackages: [
    "rehype-mermaid",
    "mermaid-isomorphic",
    "playwright",
    "playwright-core",
  ],
  allowedDevOrigins: ["192.168.1.10"],
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

export default config;
