import "server-only";
import fs from "node:fs/promises";
import { compileMDX } from "next-mdx-remote/rsc";
import type { VFile } from "vfile";
import { rehypePlugins, remarkPlugins } from "@/src/mdx-plugins";
import { useMDXComponents as getMDXComponents } from "@/mdx-components";
import type { HeadingItem } from "@/src/components/TableOfContents";

type Frontmatter = {
  title?: string;
  description?: string;
  [key: string]: unknown;
};

export type CompiledDoc<T extends Frontmatter = Frontmatter> = {
  content: React.ReactElement;
  frontmatter: T;
  toc: HeadingItem[];
};

export async function compileDoc<T extends Frontmatter = Frontmatter>(
  absPath: string,
): Promise<CompiledDoc<T>> {
  // MDX 3 rejects HTML comments (`<!-- ... -->`). They survive on disk for
  // tooling that reads them (e.g. cspell `<!-- spell-checker:ignore ... -->`)
  // but get stripped before MDX compilation.
  const source = (await fs.readFile(absPath, "utf-8")).replace(
    /<!--[\s\S]*?-->/g,
    "",
  );
  const captured: { toc: HeadingItem[] } = { toc: [] };

  const captureToc = () => (_tree: unknown, file: VFile) => {
    const data = file.data as { toc?: HeadingItem[] };
    captured.toc = data.toc ?? [];
  };

  // compileMDX builds the VFile from a raw string, so it has no path. Inject
  // the real source path up front so link-rewriting plugins can resolve
  // relative references (./foo.md, ../../../public/img.png) against it.
  const setSourcePath = () => (_tree: unknown, file: VFile) => {
    file.path = absPath;
  };

  const { content, frontmatter } = await compileMDX<T>({
    source,
    options: {
      parseFrontmatter: true,
      blockJS: false,
      mdxOptions: {
        remarkPlugins: [setSourcePath, ...remarkPlugins, captureToc],
        rehypePlugins: [...rehypePlugins],
      },
    },
    components: getMDXComponents(),
  });

  return { content, frontmatter, toc: captured.toc };
}
