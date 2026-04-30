import fs from "fs/promises";
import path from "path";

const markdownExtensions = new Set([".md", ".mdx"]);

interface SyncTarget {
  source: string;
  target: string;
}

const targets: SyncTarget[] = [
  {
    source: path.join(process.cwd(), "src/docs"),
    target: path.join(process.cwd(), "public/docs"),
  },
  {
    source: path.join(process.cwd(), "src/blog"),
    target: path.join(process.cwd(), "public/images/blog"),
  },
];

async function copyAssets(
  sourceRoot: string,
  targetRoot: string,
  relativeDir = ""
): Promise<number> {
  const sourceDir = path.join(sourceRoot, relativeDir);
  const entries = await fs.readdir(sourceDir, { withFileTypes: true });
  let copied = 0;

  for (const entry of entries) {
    const relativePath = path.join(relativeDir, entry.name);
    const sourcePath = path.join(sourceRoot, relativePath);
    const targetPath = path.join(targetRoot, relativePath);

    if (entry.isDirectory()) {
      copied += await copyAssets(sourceRoot, targetRoot, relativePath);
      continue;
    }

    if (!entry.isFile()) {
      continue;
    }

    const ext = path.extname(entry.name).toLowerCase();
    if (markdownExtensions.has(ext)) {
      continue;
    }

    await fs.mkdir(path.dirname(targetPath), { recursive: true });
    await fs.copyFile(sourcePath, targetPath);
    copied++;
  }

  return copied;
}

async function main() {
  let total = 0;
  for (const { source, target } of targets) {
    total += await copyAssets(source, target);
  }
  console.log(`synced ${total} asset files`);
}

main().catch((error) => {
  console.error("failed to sync assets", error);
  process.exitCode = 1;
});
