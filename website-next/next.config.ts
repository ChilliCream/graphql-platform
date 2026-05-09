import path from "node:path";
import type { NextConfig } from "next";
import createMDX from "@next/mdx";

const rewriteMdLinks = path.resolve(
  process.cwd(),
  "src/remark/rewriteMdLinks.mjs"
);
const codeBlockMeta = path.resolve(
  process.cwd(),
  "src/remark/codeBlockMeta.mjs"
);
const extractToc = path.resolve(process.cwd(), "src/remark/extractToc.mjs");
const exportToc = path.resolve(process.cwd(), "src/recma/exportToc.mjs");

const nextConfig: NextConfig = {
  output: "export",
  pageExtensions: ["ts", "tsx", "md", "mdx"],
};

const withMDX = createMDX({
  extension: /\.(md|mdx)$/,
  options: {
    format: "mdx",
    remarkPlugins: [
      "remark-frontmatter",
      "remark-gfm",
      rewriteMdLinks,
      codeBlockMeta,
      extractToc,
    ],
    rehypePlugins: [],
    recmaPlugins: [exportToc],
  },
});

export default withMDX(nextConfig);
