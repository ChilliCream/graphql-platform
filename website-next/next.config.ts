import path from "node:path";
import type { NextConfig } from "next";
import createMDX from "@next/mdx";

const rewriteMdLinks = path.resolve(
  process.cwd(),
  "src/remark/rewriteMdLinks.mjs"
);

const nextConfig: NextConfig = {
  output: "export",
  pageExtensions: ["ts", "tsx", "md", "mdx"],
};

const withMDX = createMDX({
  extension: /\.(md|mdx)$/,
  options: {
    remarkPlugins: ["remark-frontmatter", "remark-gfm", rewriteMdLinks],
    rehypePlugins: [],
  },
});

export default withMDX(nextConfig);
