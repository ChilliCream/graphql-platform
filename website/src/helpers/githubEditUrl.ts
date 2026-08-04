const REPO = "ChilliCream/graphql-platform";
const BRANCH = "main";
const SITE_DIR = "website";

/**
 * Build a github.com edit URL for a content file. `repoRelPath` is the path
 * relative to the project root (e.g. `content/docs/example/getting-started.md`).
 */
export function githubEditUrl(repoRelPath: string): string {
  const normalized = repoRelPath.split("\\").join("/").replace(/^\/+/, "");
  return `https://github.com/${REPO}/edit/${BRANCH}/${SITE_DIR}/${normalized}`;
}
