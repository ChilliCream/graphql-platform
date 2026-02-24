import fs from "fs";
import path from "path";
import { serialize } from "next-mdx-remote/serialize";
import remarkGfm from "remark-gfm";
import rehypeSlug from "rehype-slug";
import rehypeAutolinkHeadings from "rehype-autolink-headings";
import rehypeExternalLinks from "rehype-external-links";
import rehypeRaw from "rehype-raw";

// Custom components that render as block-level elements (divs).
// When these appear in markdown, the parser wraps them in <p> tags,
// causing "<div> cannot be a descendant of <p>" hydration errors.
// This plugin unwraps them from <p> parents.
const BLOCK_COMPONENTS = [
  "video",
  "packageinstallation",
  "surveyprompt",
  "exampletabs",
  "inputchoicetabs",
  "inputchoicetabs-cli",
  "inputchoicetabs-visualstudio",
  "apichoicetabs",
  "apichoicetabs-minimalapis",
  "apichoicetabs-regular",
  "tabgroups",
  "warning",
];

function rehypeUnwrapBlockComponents() {
  return (tree: any) => {
    visit(tree);
  };

  function visit(node: any) {
    if (!node.children) return;

    const newChildren: any[] = [];
    for (const child of node.children) {
      if (
        child.type === "element" &&
        child.tagName === "p" &&
        child.children?.some(
          (c: any) =>
            c.type === "element" &&
            BLOCK_COMPONENTS.includes(c.tagName?.toLowerCase())
        )
      ) {
        // Unwrap: hoist block children out of the <p>, keep inline siblings
        let inlineBuf: any[] = [];
        for (const grandChild of child.children) {
          if (
            grandChild.type === "element" &&
            BLOCK_COMPONENTS.includes(grandChild.tagName?.toLowerCase())
          ) {
            if (inlineBuf.length > 0) {
              newChildren.push({ ...child, children: inlineBuf });
              inlineBuf = [];
            }
            newChildren.push(grandChild);
          } else {
            inlineBuf.push(grandChild);
          }
        }
        if (inlineBuf.length > 0) {
          // Only add a <p> wrapper if there's non-whitespace text
          const hasContent = inlineBuf.some(
            (n: any) =>
              (n.type === "text" && n.value.trim()) || n.type === "element"
          );
          if (hasContent) {
            newChildren.push({ ...child, children: inlineBuf });
          }
        }
      } else {
        newChildren.push(child);
      }
    }
    node.children = newChildren;

    for (const child of node.children) {
      visit(child);
    }
  }
}

// Load the optimized image map once at build time
let cachedImageMap: Record<string, any> | null = null;

function loadImageMap(): Record<string, any> {
  if (cachedImageMap) return cachedImageMap;

  const mapPath = path.join(process.cwd(), "public/optimized/image-map.json");
  try {
    if (fs.existsSync(mapPath)) {
      cachedImageMap = JSON.parse(fs.readFileSync(mapPath, "utf-8"));
      return cachedImageMap!;
    }
  } catch {
    // No image map available
  }

  cachedImageMap = {};
  return cachedImageMap;
}

/**
 * Rehype plugin that rewrites <img> elements to <picture> with optimized
 * AVIF/WebP sources at build time. This avoids loading the full-size original
 * image in the initial HTML.
 */
function rehypeOptimizedImages() {
  return (tree: any) => {
    const imageMap = loadImageMap();
    if (Object.keys(imageMap).length === 0) return;

    let imageIndex = 0;
    visitImages(tree, imageMap, () => imageIndex++);
  };

  function visitImages(
    node: any,
    imageMap: Record<string, any>,
    nextIndex: () => number
  ) {
    if (!node.children) return;

    for (let i = 0; i < node.children.length; i++) {
      const child = node.children[i];

      if (child.type === "element" && child.tagName === "img") {
        const src = child.properties?.src;
        if (!src || typeof src !== "string") continue;

        const entry = imageMap[src];
        if (!entry) continue;

        const isFirst = nextIndex() === 0;
        const sizes =
          "(max-width: 640px) 100vw, (max-width: 1024px) 75vw, 660px";

        // Build <picture> with <source> for AVIF and WebP, plus fallback <img>
        const sources: any[] = [];

        if (entry.a) {
          sources.push({
            type: "element",
            tagName: "source",
            properties: { type: "image/avif", srcSet: entry.a, sizes },
            children: [],
          });
        }

        if (entry.w) {
          sources.push({
            type: "element",
            tagName: "source",
            properties: { type: "image/webp", srcSet: entry.w, sizes },
            children: [],
          });
        }

        // First image gets fetchpriority="high" and eager loading for LCP;
        // subsequent images are lazy-loaded.
        const imgProps: Record<string, any> = {
          src: entry.s || src,
          alt: child.properties?.alt || "",
          width: entry.W,
          height: entry.H,
          sizes,
          style: "width:100%;height:auto",
        };

        if (isFirst) {
          imgProps.fetchPriority = "high";
          imgProps.decoding = "async";
        } else {
          imgProps.loading = "lazy";
          imgProps.decoding = "async";
        }

        sources.push({
          type: "element",
          tagName: "img",
          properties: imgProps,
          children: [],
        });

        // Replace <img> with <picture>
        node.children[i] = {
          type: "element",
          tagName: "picture",
          properties: {},
          children: sources,
        };
      } else {
        visitImages(child, imageMap, nextIndex);
      }
    }
  }
}

const rehypePlugins = [
  rehypeRaw,
  rehypeUnwrapBlockComponents,
  rehypeOptimizedImages,
  rehypeSlug,
  [
    rehypeAutolinkHeadings,
    {
      behavior: "prepend",
      properties: { className: ["anchor"], "aria-hidden": "true" },
      content: {
        type: "element",
        tagName: "svg",
        properties: {
          xmlns: "http://www.w3.org/2000/svg",
          viewBox: "0 0 512 512",
          width: 16,
          height: 16,
          fill: "var(--cc-heading-text-color)",
        },
        children: [
          {
            type: "element",
            tagName: "path",
            properties: {
              d: "M326.612 185.391c59.747 59.809 58.927 155.698.36 214.59-.11.12-.24.25-.36.37l-67.2 67.2c-59.27 59.27-155.699 59.262-214.96 0-59.27-59.26-59.27-155.7 0-214.96l37.106-37.106c9.84-9.84 26.786-3.3 27.294 10.606.648 17.722 3.826 35.527 9.69 52.721 1.986 5.822.567 12.262-3.783 16.612l-13.087 13.087c-28.026 28.026-28.905 73.66-1.155 101.96 28.024 28.579 74.086 28.749 102.325.51l67.2-67.19c28.191-28.191 28.073-73.757 0-101.83-3.701-3.694-7.429-6.564-10.341-8.569a16.037 16.037 0 0 1-6.947-12.606c-.396-10.567 3.348-21.456 11.698-29.806l21.054-21.055c5.521-5.521 14.182-6.199 20.584-1.731a152.482 152.482 0 0 1 20.522 17.197zM467.547 44.449c-59.261-59.262-155.69-59.27-214.96 0l-67.2 67.2c-.12.12-.25.25-.36.37-58.566 58.892-59.387 154.781.36 214.59a152.454 152.454 0 0 0 20.521 17.196c6.402 4.468 15.064 3.789 20.584-1.731l21.054-21.055c8.35-8.35 12.094-19.239 11.698-29.806a16.037 16.037 0 0 0-6.947-12.606c-2.912-2.005-6.64-4.875-10.341-8.569-28.073-28.073-28.191-73.639 0-101.83l67.2-67.19c28.239-28.239 74.3-28.069 102.325.51 27.75 28.3 26.872 73.934-1.155 101.96l-13.087 13.087c-4.35 4.35-5.769 10.79-3.783 16.612 5.864 17.194 9.042 34.999 9.69 52.721.509 13.906 17.454 20.446 27.294 10.606l37.106-37.106c59.271-59.259 59.271-155.699.001-214.959z",
            },
            children: [],
          },
        ],
      },
    },
  ],
  [
    rehypeExternalLinks,
    {
      target: "_blank",
      rel: ["noopener", "noreferrer"],
    },
  ],
] as any;

// Strip import statements from MDX source since components are provided via MDX context
function stripImports(source: string): string {
  return source.replace(/^import\s+.*?from\s+["'].*?["'];?\s*$/gm, "");
}

// Replace dotted JSX component names with hyphenated names for HTML parser compatibility.
// HTML element names cannot contain dots, so rehype-raw fails to parse them.
// e.g. <InputChoiceTabs.CLI> → <InputChoiceTabs-CLI>
const DOTTED_COMPONENT_MAP: Record<string, string> = {
  "InputChoiceTabs.CLI": "InputChoiceTabs-CLI",
  "InputChoiceTabs.VisualStudio": "InputChoiceTabs-VisualStudio",
  "ApiChoiceTabs.MinimalApis": "ApiChoiceTabs-MinimalApis",
  "ApiChoiceTabs.Regular": "ApiChoiceTabs-Regular",
};

// Convert self-closing custom component tags to explicit open+close pairs.
// HTML parsing (used by rehype-raw in format:"md") doesn't recognize self-closing
// syntax for non-void elements: <Video videoId="x" /> is parsed as an opening tag,
// causing all subsequent content to become children. Converting to
// <Video videoId="x"></Video> fixes this.
const SELF_CLOSING_COMPONENTS = [
  "Video",
  "PackageInstallation",
  "SurveyPrompt",
];

function expandSelfClosingTags(source: string): string {
  let result = source;
  for (const name of SELF_CLOSING_COMPONENTS) {
    // Match <ComponentName ... /> with optional attributes
    // Handles both <Component /> and <Component/>
    const regex = new RegExp(`<${name}(\\s[^>]*)?\\/\\s*>`, "g");
    result = result.replace(regex, (_match, attrs) => {
      return `<${name}${attrs || ""}></${name}>`;
    });
  }
  return result;
}

// Fix JSX-style attribute values that HTML parser can't handle.
// In format:"md", rehype-raw parses HTML per spec:
//   1. defaultValue={"string"} → braces are literal text, not JS expressions
//   2. Attribute names are lowercased: defaultValue → defaultvalue
// This function converts e.g. defaultValue={"connection"} → defaultValue="connection"
function fixJsxAttributes(source: string): string {
  // Replace attr={"value"} with attr="value" (strip JSX braces around string literals)
  return source.replace(/(\w+)=\{"([^"]+)"\}/g, '$1="$2"');
}

function replaceDottedComponents(source: string): string {
  let result = source;
  for (const [dotted, hyphenated] of Object.entries(DOTTED_COMPONENT_MAP)) {
    // Replace both opening and closing tags
    result = result.replaceAll(`<${dotted}>`, `<${hyphenated}>`);
    result = result.replaceAll(`</${dotted}>`, `</${hyphenated}>`);
    result = result.replaceAll(`<${dotted} `, `<${hyphenated} `);
    result = result.replaceAll(`<${dotted}/>`, `<${hyphenated}/>`);
  }
  return result;
}

export function extractHeadings(
  source: string
): Array<{ depth: number; value: string }> {
  const headings: Array<{ depth: number; value: string }> = [];
  const lines = source.split("\n");
  let inCodeBlock = false;

  for (const line of lines) {
    if (line.trim().startsWith("```")) {
      inCodeBlock = !inCodeBlock;
      continue;
    }
    if (inCodeBlock) continue;

    const match = line.match(/^(#{1,6})\s+(.+)$/);
    if (match) {
      // Strip inline markdown formatting (bold, italic, code, links)
      let value = match[2].trim();
      value = value.replace(/\*\*(.+?)\*\*/g, "$1"); // bold
      value = value.replace(/\*(.+?)\*/g, "$1"); // italic
      value = value.replace(/__(.+?)__/g, "$1"); // bold alt
      value = value.replace(/_(.+?)_/g, "$1"); // italic alt
      value = value.replace(/`(.+?)`/g, "$1"); // inline code
      value = value.replace(/\[([^\]]+)\]\([^)]+\)/g, "$1"); // links

      headings.push({
        depth: match[1].length,
        value,
      });
    }
  }

  return headings;
}

// Resolve relative image paths in markdown to absolute URLs.
// Images in src/docs/ are copied to public/docs/, images in src/images/ are in public/images/.
function resolveImagePaths(source: string, originPath: string): string {
  const docDir = path.dirname(originPath); // e.g. "hotchocolate/v15"

  return source.replace(
    /(!\[[^\]]*\]\()([^)]+)(\))/g,
    (_match, prefix, imgPath, suffix) => {
      if (imgPath.startsWith("http://") || imgPath.startsWith("https://")) {
        return prefix + imgPath + suffix;
      }

      // Resolve relative path against the doc file's directory within src/docs/
      const resolved = path.normalize(path.join(docDir, imgPath));

      // Check if path escapes out of docs directory (e.g. ../../../images/foo.webp)
      if (resolved.startsWith("..")) {
        // Goes above src/docs/ - these are in src/images/ which maps to public/images/
        // Strip leading "../" segments to get the path relative to src/
        const cleaned = resolved.replace(/^(\.\.\/?)+/, "");
        return prefix + "/" + cleaned + suffix;
      }

      // Path stays within docs directory - served from public/docs/
      return prefix + "/docs/" + resolved + suffix;
    }
  );
}

export async function compileMdxContent(source: string, originPath = "") {
  const cleaned = resolveImagePaths(
    fixJsxAttributes(
      expandSelfClosingTags(replaceDottedComponents(stripImports(source)))
    ),
    originPath
  );

  try {
    const mdxSource = await serialize(cleaned, {
      parseFrontmatter: true,
      mdxOptions: {
        format: "md",
        remarkPlugins: [remarkGfm],
        rehypePlugins,
      },
    });

    return { mdxSource, frontmatter: mdxSource.frontmatter || {} };
  } catch (error) {
    console.error(
      "MD compilation failed:",
      (error as Error).message?.substring(0, 200)
    );
    const mdxSource = await serialize("*Content could not be rendered.*");
    return { mdxSource, frontmatter: {} };
  }
}
