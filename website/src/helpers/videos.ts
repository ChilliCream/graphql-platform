import fs from "node:fs";
import path from "node:path";
import { AUTHOR_PROFILES, type AuthorProfile } from "@/src/data/authors";
import { getOptimizedImage } from "@/src/image-optimization/manifest";
import { readFrontmatter } from "./readFrontmatter";

export const VIDEOS_ROOT = path.join(process.cwd(), "content/videos");
export const VIDEOS_PATH = "/videos";
export const CHILLICREAM_TV_CHANNEL_URL =
  "https://www.youtube.com/@chillicreamtv";

const STEM_RE = /^(\d{4})-(\d{2})-(\d{2})-[a-z0-9]+(?:-[a-z0-9]+)*$/;
const YOUTUBE_ID_RE = /^[A-Za-z0-9_-]{11}$/;
const DURATION_RE = /^(?:(\d+):)?([0-5]?\d):([0-5]\d)$/;
const TAG_RE = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

const TOPIC_LABELS: Record<string, string> = {
  dataloader: "DataLoader",
  "entity-framework": "Entity Framework",
  greendonut: "GreenDonut",
  hotchocolate: "Hot Chocolate",
};

export type VideoSummary = {
  readonly stem: string;
  readonly href: string;
  /** Absolute path of the Markdown source. */
  readonly file: string;
  readonly title: string;
  readonly description: string;
  /** Publish date (`YYYY-MM-DD`), taken from the file name. */
  readonly date: string;
  readonly youtubeId: string;
  /** Running time in seconds. */
  readonly duration: number;
  /** Link to the example source code, or null when the video has none. */
  readonly example: string | null;
  readonly tags: readonly string[];
  readonly author: AuthorProfile | null;
  /** Position in publish order, starting at 1 for the oldest video. */
  readonly episode: number;
};

/**
 * Lists every video in `content/videos`, newest first. A video is a
 * `YYYY-MM-DD-slug.md` file; the date prefix is its publish date. Throws on the
 * first invalid file so the build fails.
 */
export function listVideos(): VideoSummary[] {
  if (!fs.existsSync(VIDEOS_ROOT)) {
    return [];
  }

  const videos = fs
    .readdirSync(VIDEOS_ROOT, { withFileTypes: true })
    .filter((entry) => entry.isFile() && entry.name.endsWith(".md"))
    .map((entry) => readVideo(entry.name))
    .sort((a, b) => b.stem.localeCompare(a.stem));

  return videos.map((video, index) => ({
    ...video,
    episode: videos.length - index,
  }));
}

/** Display name of a topic tag, e.g. `entity-framework` becomes "Entity Framework". */
export function topicLabel(tag: string): string {
  return (
    TOPIC_LABELS[tag] ??
    tag
      .split("-")
      .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
      .join(" ")
  );
}

/** The episode number padded to two digits, e.g. `07`. */
export function formatEpisode(episode: number): string {
  return String(episode).padStart(2, "0");
}

export function videoUrl(stem: string): string {
  return `${VIDEOS_PATH}/${stem}`;
}

export function youtubeWatchUrl(youtubeId: string): string {
  return `https://www.youtube.com/watch?v=${youtubeId}`;
}

/**
 * Absolute URL of the video's poster image: the self-hosted copy when the
 * image pipeline produced one, otherwise YouTube's always-available thumbnail.
 */
export function videoPosterUrl(youtubeId: string): string {
  const optimized = getOptimizedImage(
    `https://i.ytimg.com/vi/${youtubeId}/maxresdefault.jpg`,
  );
  return (
    optimized?.fallbackSrc ??
    `https://i.ytimg.com/vi/${youtubeId}/hqdefault.jpg`
  );
}

/**
 * Picks up to `limit` other videos, preferring the most shared tags and then
 * the newest. Falls back to the newest videos when none share a tag.
 */
export function findRelatedVideos(
  reference: VideoSummary,
  pool: readonly VideoSummary[],
  limit = 4,
): VideoSummary[] {
  const referenceTags = new Set(reference.tags);

  return pool
    .filter((video) => video.stem !== reference.stem)
    .map((video) => ({
      video,
      overlap: video.tags.filter((tag) => referenceTags.has(tag)).length,
    }))
    .sort(
      (a, b) =>
        b.overlap - a.overlap || b.video.stem.localeCompare(a.video.stem),
    )
    .slice(0, limit)
    .map(({ video }) => video);
}

/** All tags used by `videos`, most used first. */
export function listVideoTags(videos: readonly VideoSummary[]): string[] {
  const counts = new Map<string, number>();
  for (const video of videos) {
    for (const tag of video.tags) {
      counts.set(tag, (counts.get(tag) ?? 0) + 1);
    }
  }

  return [...counts.entries()]
    .sort(([a, countA], [b, countB]) => countB - countA || a.localeCompare(b))
    .map(([tag]) => tag);
}

function readVideo(fileName: string): Omit<VideoSummary, "episode"> {
  const stem = fileName.slice(0, -".md".length);
  const match = STEM_RE.exec(stem);
  if (!match) {
    return invalid(
      fileName,
      "Expected the name format YYYY-MM-DD-slug.md with a lowercase, hyphenated slug.",
    );
  }

  const file = path.join(VIDEOS_ROOT, fileName);
  const frontmatter = readFrontmatter(file);

  const youtubeId = requiredString(frontmatter, "youtubeId", fileName);
  if (!YOUTUBE_ID_RE.test(youtubeId)) {
    invalid(fileName, `"youtubeId" must be an 11-character YouTube video id.`);
  }

  const example = optionalString(frontmatter, "example", fileName);
  if (example !== null && !example.startsWith("https://")) {
    invalid(fileName, `"example" must be an https:// link.`);
  }

  const authorSlug = optionalString(frontmatter, "author", fileName);
  const author =
    authorSlug === null
      ? null
      : (AUTHOR_PROFILES.find((profile) => profile.slug === authorSlug) ??
        invalid(
          fileName,
          `"author" must be one of: ${AUTHOR_PROFILES.map((profile) => profile.slug).join(", ")}.`,
        ));

  const [, year, month, day] = match;
  return {
    stem,
    href: videoUrl(stem),
    file,
    title: requiredString(frontmatter, "title", fileName),
    description: requiredString(frontmatter, "description", fileName),
    date: `${year}-${month}-${day}`,
    youtubeId,
    duration: readDuration(frontmatter, fileName),
    example,
    tags: readTags(frontmatter, fileName),
    author,
  };
}

function requiredString(
  frontmatter: Record<string, unknown>,
  key: string,
  fileName: string,
): string {
  return (
    optionalString(frontmatter, key, fileName) ??
    invalid(fileName, `"${key}" is required.`)
  );
}

function optionalString(
  frontmatter: Record<string, unknown>,
  key: string,
  fileName: string,
): string | null {
  const value = frontmatter[key];
  if (value === undefined || value === null) {
    return null;
  }
  if (typeof value !== "string" || value.trim().length === 0) {
    return invalid(fileName, `"${key}" must be a non-empty, quoted string.`);
  }
  return value.trim();
}

function readDuration(
  frontmatter: Record<string, unknown>,
  fileName: string,
): number {
  const match = DURATION_RE.exec(
    requiredString(frontmatter, "duration", fileName),
  );
  if (!match) {
    return invalid(
      fileName,
      `"duration" must be the running time as YouTube shows it (e.g. "12:18" or "1:02:03").`,
    );
  }
  const [, hours = "0", minutes, seconds] = match;
  return Number(hours) * 3600 + Number(minutes) * 60 + Number(seconds);
}

function readTags(
  frontmatter: Record<string, unknown>,
  fileName: string,
): string[] {
  const value = frontmatter.tags;
  if (value === undefined || value === null) {
    return [];
  }
  if (
    !Array.isArray(value) ||
    !value.every((tag) => typeof tag === "string" && TAG_RE.test(tag))
  ) {
    return invalid(
      fileName,
      `"tags" must be a list of lowercase, hyphenated tags (e.g. dataloader, entity-framework).`,
    );
  }
  return value;
}

function invalid(fileName: string, message: string): never {
  throw new Error(
    `[videos] Invalid video "content/videos/${fileName}": ${message}`,
  );
}
