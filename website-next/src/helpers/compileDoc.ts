import "server-only";
import fs from "node:fs/promises";
import { compileMDX } from "next-mdx-remote/rsc";
import type { VFile } from "vfile";
import { remarkPlugins } from "@/src/mdx-plugins";
import { useMDXComponents as getMDXComponents } from "@/mdx-components";
import type { HeadingItem } from "@/src/design-system/TableOfContents";

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
  absPath: string
): Promise<CompiledDoc<T>> {
  const source = await fs.readFile(absPath, "utf-8");
  const captured: { toc: HeadingItem[] } = { toc: [] };

  const captureToc = () => (_tree: unknown, file: VFile) => {
    const data = file.data as { toc?: HeadingItem[] };
    captured.toc = data.toc ?? [];
  };

  const { content, frontmatter } = await compileMDX<T>({
    source,
    options: {
      parseFrontmatter: true,
      blockJS: false,
      mdxOptions: {
        remarkPlugins: [...remarkPlugins, captureToc],
      },
    },
    components: getMDXComponents(),
  });

  return { content, frontmatter, toc: captured.toc };
}
