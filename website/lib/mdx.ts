import path from "path";
import { serialize } from "next-mdx-remote/serialize";
import remarkGfm from "remark-gfm";
import rehypeSlug from "rehype-slug";
import rehypeAutolinkHeadings from "rehype-autolink-headings";
import rehypeExternalLinks from "rehype-external-links";
import rehypeRaw from "rehype-raw";

const rehypePlugins = [
  rehypeRaw,
  rehypeSlug,
  [
    rehypeAutolinkHeadings,
    {
      behavior: "prepend",
      properties: { className: ["anchor"], "aria-hidden": "true" },
      content: {
        type: "text",
        value: "#",
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
      headings.push({
        depth: match[1].length,
        value: match[2].trim(),
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
    replaceDottedComponents(stripImports(source)),
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
