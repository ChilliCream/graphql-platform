import path from "node:path";
import type { Pluggable } from "unified";
import remarkGfm from "remark-gfm";
import rehypeMermaid, { RehypeMermaidOptions } from "rehype-mermaid";
import codeBlockMeta from "./remark/codeBlockMeta.mjs";
import demoteHeadings from "./remark/demoteHeadings.mjs";
import extractToc from "./remark/extractToc.mjs";
import rewriteMdLinks from "./remark/rewriteMdLinks.mjs";
import youtubeEmbed from "./remark/youtubeEmbed.mjs";
import styleStringToObject from "./rehype/styleStringToObject.mjs";

const remarkRoot = path.resolve(process.cwd(), "src/remark");
const rehypeRoot = path.resolve(process.cwd(), "src/rehype");

/**
 * Single source of truth for the docs/blog MDX pipeline.
 *
 * `spec` is what `@next/mdx` consumes (it resolves strings via `require.resolve`).
 * `plugin` is what `next-mdx-remote/rsc compileMDX` consumes at runtime.
 *
 * Keeping them paired in one array prevents drift.
 */
const remarkPipeline: { spec: string; plugin: Pluggable }[] = [
  { spec: "remark-gfm", plugin: remarkGfm },
  { spec: path.join(remarkRoot, "rewriteMdLinks.mjs"), plugin: rewriteMdLinks },
  { spec: path.join(remarkRoot, "codeBlockMeta.mjs"), plugin: codeBlockMeta },
  { spec: path.join(remarkRoot, "demoteHeadings.mjs"), plugin: demoteHeadings },
  { spec: path.join(remarkRoot, "extractToc.mjs"), plugin: extractToc },
  { spec: path.join(remarkRoot, "youtubeEmbed.mjs"), plugin: youtubeEmbed },
];

export const remarkPluginSpecs: string[] = remarkPipeline.map((p) => p.spec);
export const remarkPlugins: Pluggable[] = remarkPipeline.map((p) => p.plugin);

/**
 * Build-time mermaid rendering. Runs headless Chromium via playwright to
 * produce inline SVG, so no mermaid runtime ships to the client.
 */
const mermaidOptions: RehypeMermaidOptions = {
  strategy: "inline-svg",
  mermaidConfig: {
    theme: "base",
    securityLevel: "strict",
    // Note: do not override `font-family` here. Mermaid runs in headless
    // Chromium at build time to size each node, so changing the font after
    // measurement (or to a font not loaded in the headless browser) clips
    // the text in the live page.
    //
    // themeCSS is inlined as a <style> block inside the generated SVG, so
    // CSS custom properties from `app/globals.css` (`--color-primary-*`,
    // `--color-slate-*`) resolve in the browser. Mermaid sets fills/strokes
    // via inline `style=`, which is why every rule needs `!important`.
    themeCSS: `
      .edgePath .path { stroke-width: 1.5px; }

      /* Primary nodes */
      .node rect,
      .node circle,
      .node ellipse,
      .node polygon,
      .node path {
        fill: var(--color-primary-50) !important;
        stroke: var(--color-primary-700) !important;
      }

      /* Cluster / subgraph frames pick up the slate scale */
      .cluster rect {
        fill: var(--color-slate-50) !important;
        stroke: var(--color-slate-200) !important;
      }
    `,
  },
} as const;

const rehypePipeline: {
  spec: string | [string, unknown];
  plugin: Pluggable;
}[] = [
  {
    spec: ["rehype-mermaid", mermaidOptions],
    plugin: [rehypeMermaid, mermaidOptions],
  },
  // Must run after rehype-mermaid: mermaid emits raw CSS strings on SVG
  // nodes, which MDX rejects. Convert them to camelCased style objects.
  {
    spec: path.join(rehypeRoot, "styleStringToObject.mjs"),
    plugin: styleStringToObject,
  },
];

export const rehypePluginSpecs: (string | [string, unknown])[] =
  rehypePipeline.map((p) => p.spec);
export const rehypePlugins: Pluggable[] = rehypePipeline.map((p) => p.plugin);
