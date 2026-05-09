import path from "node:path";
import type { NextConfig } from "next";
import createMDX from "@next/mdx";
import { remarkPluginSpecs } from "./src/mdx-plugins";

const exportToc = path.resolve(process.cwd(), "src/recma/exportToc.mjs");

const nextConfig: NextConfig = {
  output: "export",
  pageExtensions: ["ts", "tsx", "md", "mdx"],
};

const withMDX = createMDX({
  extension: /\.(md|mdx)$/,
  options: {
    format: "mdx",
    remarkPlugins: [...remarkPluginSpecs],
    rehypePlugins: [],
    recmaPlugins: [exportToc],
  },
});

export default withMDX(nextConfig);
