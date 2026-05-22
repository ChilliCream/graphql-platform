import { execFileSync, spawnSync } from "node:child_process";
import path from "node:path";
import { unstable_cache } from "next/cache";

const WEBSITE_ROOT = process.cwd();
const DOCS_DIR = path.join(WEBSITE_ROOT, "content", "docs");
const BLOGS_DIR = path.join(WEBSITE_ROOT, "content", "blogs");
const APP_CONTENT_DIR = path.join(WEBSITE_ROOT, "app", "(content)");
const HEADER = "@@COMMIT@@";

export interface GitMetadata {
  /** ISO 8601 timestamp of the last commit touching the file. */
  isoDate: string;
  /** Localized date string, e.g. "May 18, 2026". */
  displayDate: string;
  /** Author of the last commit, or "Unknown" when unavailable. */
  author: string;
}

type Entry = { date: string; author: string };

// HEAD SHA participates in the cache key so the manifest is reused while the
// commit hash is stable and rebuilt as soon as a new commit lands. Resolved
// once at module load (a cheap `git rev-parse`).
const HEAD_SHA = resolveHeadSha();

const loadManifest = unstable_cache(
  (): Promise<Record<string, Entry>> => Promise.resolve(buildManifest()),
  ["docs-git-metadata", HEAD_SHA],
  { revalidate: false },
);

/**
 * Returns the raw last-commit date for a file, or `undefined` if git has no
 * record of it. Used by callers (e.g. sitemap generation) that want to fall
 * back to filesystem mtime when git attribution is unavailable.
 */
export async function getLastModifiedFromGit(
  absoluteFilePath: string,
): Promise<Date | undefined> {
  const manifest = await loadManifest();
  const key = path.relative(WEBSITE_ROOT, absoluteFilePath);
  const entry = manifest[key];
  if (!entry) {
    return undefined;
  }
  const date = new Date(entry.date);
  return Number.isNaN(date.getTime()) ? undefined : date;
}

export async function getGitMetadata(
  absoluteFilePath: string,
): Promise<GitMetadata> {
  const manifest = await loadManifest();
  const key = path.relative(WEBSITE_ROOT, absoluteFilePath);
  const entry = manifest[key];

  if (!entry) {
    return fallback();
  }

  const date = new Date(entry.date);
  if (Number.isNaN(date.getTime())) {
    return fallback();
  }

  return {
    isoDate: date.toISOString(),
    displayDate: formatDate(date),
    author: entry.author || "Unknown",
  };
}

function buildManifest(): Record<string, Entry> {
  const repoRoot = tryGitRoot();
  if (!repoRoot) {
    return {};
  }
  warnIfShallow(repoRoot);

  const trackedPaths = [DOCS_DIR, BLOGS_DIR, APP_CONTENT_DIR].map((p) =>
    path.relative(repoRoot, p),
  );

  let stdout: string;
  try {
    stdout = execFileSync(
      "git",
      [
        "-c",
        "core.quotePath=false",
        "log",
        "--name-only",
        "--no-merges",
        "--diff-filter=AMR",
        `--format=${HEADER}%aI%x09%an`,
        "--",
        ...trackedPaths,
      ],
      {
        cwd: repoRoot,
        encoding: "utf-8",
        maxBuffer: 256 * 1024 * 1024,
        stdio: ["ignore", "pipe", "ignore"],
      },
    );
  } catch {
    return {};
  }

  const out: Record<string, Entry> = {};
  let currentDate: string | null = null;
  let currentAuthor: string | null = null;

  for (const line of stdout.split("\n")) {
    if (line === "") {
      continue;
    }
    if (line.startsWith(HEADER)) {
      const rest = line.slice(HEADER.length);
      const tab = rest.indexOf("\t");
      currentDate = tab === -1 ? rest : rest.slice(0, tab);
      currentAuthor = tab === -1 ? "" : rest.slice(tab + 1);
      continue;
    }
    if (currentDate === null || !isTrackedFile(line)) {
      continue;
    }
    const abs = path.join(repoRoot, line);
    const key = path.relative(WEBSITE_ROOT, abs);
    if (key.startsWith("..") || out[key]) {
      continue;
    }
    out[key] = { date: currentDate, author: currentAuthor ?? "" };
  }

  return out;
}

function isTrackedFile(repoRelativeFile: string): boolean {
  if (/\.mdx?$/.test(repoRelativeFile)) {
    return true;
  }
  return path.basename(repoRelativeFile) === "page.tsx";
}

function tryGitRoot(): string | null {
  const res = spawnSync("git", ["rev-parse", "--show-toplevel"], {
    cwd: WEBSITE_ROOT,
    encoding: "utf-8",
  });
  return res.status === 0 ? res.stdout.trim() : null;
}

function resolveHeadSha(): string {
  const res = spawnSync("git", ["rev-parse", "HEAD"], {
    cwd: WEBSITE_ROOT,
    encoding: "utf-8",
  });
  return res.status === 0 ? res.stdout.trim() : "no-git";
}

function warnIfShallow(repoRoot: string): void {
  const res = spawnSync("git", ["rev-parse", "--is-shallow-repository"], {
    cwd: repoRoot,
    encoding: "utf-8",
  });
  if (res.status === 0 && res.stdout.trim() === "true") {
    console.warn(
      "[git-metadata] shallow clone detected — doc attribution will be inaccurate. " +
        "Use `actions/checkout` with `fetch-depth: 0` in CI.",
    );
  }
}

function fallback(): GitMetadata {
  const now = new Date();
  return {
    isoDate: now.toISOString(),
    displayDate: formatDate(now),
    author: "Unknown",
  };
}

function formatDate(date: Date): string {
  return date.toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "2-digit",
  });
}
