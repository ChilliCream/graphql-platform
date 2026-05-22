import path from "node:path";
import type { Pluggable } from "unified";
import remarkGfm from "remark-gfm";
import rehypeMermaid from "rehype-mermaid";
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
 *
 * `themeVariables` need real color values: mermaid parses them through a
 * color library at build time, so CSS `var(...)` references would fail.
 * Values mirror the Tailwind v4 palette. `themeCSS` is appended raw to the
 * SVG's <style> block, so CSS variables there resolve in the browser and
 * can be used for runtime overrides (e.g. theme switching).
 */
const mermaidOptions = {
  strategy: "inline-svg",
  mermaidConfig: {
    theme: "base",
    securityLevel: "strict",
    themeVariables: {
      background: "transparent",
      // Node fills (emerald-50, slate-100, slate-50).
      primaryColor: "#ecfdf5",
      secondaryColor: "#f1f5f9",
      tertiaryColor: "#f8fafc",
      // Node and label text (slate-900).
      primaryTextColor: "#0f172a",
      secondaryTextColor: "#0f172a",
      tertiaryTextColor: "#0f172a",
      textColor: "#0f172a",
      // Node borders (emerald-700, slate-300, slate-200).
      primaryBorderColor: "#047857",
      secondaryBorderColor: "#cbd5e1",
      tertiaryBorderColor: "#e2e8f0",
      // Edges between nodes (slate-500).
      lineColor: "#64748b",
      // Notes (amber-50, slate-900, amber-300).
      noteBkgColor: "#fffbeb",
      noteTextColor: "#0f172a",
      noteBorderColor: "#fcd34d",
    },
    // Note: do not override `font-family` here. Mermaid runs in headless
    // Chromium at build time to size each node, so changing the font after
    // measurement (or to a font not loaded in the headless browser) clips
    // the text in the live page.
    themeCSS: `
      .edgePath .path { stroke-width: 1.5px; }
    `,
  },
} as const;

const rehypePipeline: { spec: string | [string, unknown]; plugin: Pluggable }[] =
  [
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
