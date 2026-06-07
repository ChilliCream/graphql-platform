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
    themeCSS: `
      /* Edges read as dim ink lines on the dark surface */
      .edgePath .path,
      .flowchart-link {
        stroke-width: 1.5px;
        stroke: var(--color-cc-ink-dim) !important;
      }
      marker,
      marker path,
      .arrowheadPath,
      #arrowhead path { fill: var(--color-cc-ink-dim) !important; stroke: var(--color-cc-ink-dim) !important; }

      /* Sequence diagram lifelines and message arrows (default to near-black) */
      .actor-line {
        stroke: var(--color-cc-ink-dim) !important;
      }
      .messageLine0,
      .messageLine1,
      line.messageLine0,
      line.messageLine1 {
        stroke: var(--color-cc-ink-dim) !important;
      }

      /* All diagram text in cream ink */
      .nodeLabel,
      .cluster-label,
      span.edgeLabel,
      text {
        fill: var(--color-cc-ink) !important;
        color: var(--color-cc-ink) !important;
      }

      /* Primary nodes: translucent dark fill, accent border, rounded corners
         so the boxes read as soft cards rather than hard outlines. */
      .node rect,
      .node circle,
      .node ellipse,
      .node polygon,
      .node path {
        fill: var(--color-cc-card-bg) !important;
        stroke: var(--color-cc-accent) !important;
        stroke-width: 1.25px !important;
        stroke-linejoin: round;
      }
      .node rect {
        rx: 12px !important;
        ry: 12px !important;
      }

      /* Edge labels sit on the page background so lines don't bleed through.
         Mermaid wraps the label text in a foreignObject <div>, so cover that
         (and any background rect) to kill the base theme's pink default. */
      .edgeLabel,
      .edgeLabel rect,
      .edgeLabel foreignObject div,
      .edgeLabel .labelBkg,
      .edgeLabel p {
        background-color: var(--color-cc-bg) !important;
        background: var(--color-cc-bg) !important;
        fill: var(--color-cc-bg) !important;
      }

      /* Cluster / subgraph frames as a subtle dark card matching the site */
      .cluster rect {
        fill: var(--color-cc-card-bg) !important;
        stroke: var(--color-cc-card-border) !important;
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
