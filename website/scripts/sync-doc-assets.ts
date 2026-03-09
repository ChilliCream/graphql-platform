import fs from "fs/promises";
import path from "path";

const sourceRoot = path.join(process.cwd(), "src/docs");
const targetRoot = path.join(process.cwd(), "public/docs");
const markdownExtensions = new Set([".md", ".mdx"]);

async function copyDocAssets(relativeDir = ""): Promise<number> {
  const sourceDir = path.join(sourceRoot, relativeDir);
  const entries = await fs.readdir(sourceDir, { withFileTypes: true });
  let copied = 0;

  for (const entry of entries) {
    const relativePath = path.join(relativeDir, entry.name);
    const sourcePath = path.join(sourceRoot, relativePath);
    const targetPath = path.join(targetRoot, relativePath);

    if (entry.isDirectory()) {
      copied += await copyDocAssets(relativePath);
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
  const copied = await copyDocAssets();
  console.log(`synced ${copied} doc asset files`);
}

main().catch((error) => {
  console.error("failed to sync doc assets", error);
  process.exitCode = 1;
});
