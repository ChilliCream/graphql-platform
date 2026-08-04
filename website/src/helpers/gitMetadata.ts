import fs from "node:fs";
import path from "node:path";
import { formatDate } from "./formatDate";

const DISPLAY_DATE_OPTIONS: Intl.DateTimeFormatOptions = {
  year: "numeric",
  month: "long",
  day: "2-digit",
};

const WEBSITE_ROOT = process.cwd();
// Static manifest produced by `yarn generate-git-metadata` in the release
// workflow. Absent during development and builds outside the workflow, in which
// case every lookup falls back to a static placeholder.
const MANIFEST_PATH = path.join(WEBSITE_ROOT, "git-metadata.generated.json");

export interface GitMetadata {
  /** ISO 8601 timestamp of the last commit touching the file. */
  isoDate: string;
  /** Localized date string, e.g. "May 18, 2026". */
  displayDate: string;
  /** Author of the last commit, or "Unknown" when unavailable. */
  author: string;
}

type Entry = { date: string; author: string };

let cache: Record<string, Entry> | null | undefined;

function loadManifest(): Record<string, Entry> {
  if (cache === undefined) {
    try {
      cache = JSON.parse(fs.readFileSync(MANIFEST_PATH, "utf8"));
    } catch {
      cache = null; // dev / unoptimized / not generated yet
    }
  }
  return cache ?? {};
}

/**
 * Returns the raw last-commit date for a file, or `undefined` if the generated
 * manifest has no record of it. Used by callers (e.g. sitemap generation) that
 * want to fall back to filesystem mtime when git attribution is unavailable.
 */
export async function getLastModifiedFromGit(
  absoluteFilePath: string,
): Promise<Date | undefined> {
  const manifest = loadManifest();
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
  const manifest = loadManifest();
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
    displayDate: formatDate(date, DISPLAY_DATE_OPTIONS),
    author: entry.author || "Unknown",
  };
}

function fallback(): GitMetadata {
  const now = new Date();
  return {
    isoDate: now.toISOString(),
    displayDate: formatDate(now, DISPLAY_DATE_OPTIONS),
    author: "Unknown",
  };
}
