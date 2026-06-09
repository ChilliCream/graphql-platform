import fs from "node:fs";
import path from "node:path";
import matter from "gray-matter";

const MD_RE = /\.(md|mdx)$/i;

// Matches an 11-char YouTube id in any of the supported URL forms or in a
// <Video src="..."> attribute (bare id or URL).
const YOUTUBE_RES = [
  /youtube\.com\/watch\?(?:[^"'\s]*&)?v=([A-Za-z0-9_-]{11})/g,
  /youtu\.be\/([A-Za-z0-9_-]{11})/g,
  /youtube-nocookie\.com\/embed\/([A-Za-z0-9_-]{11})/g,
  /youtube\.com\/shorts\/([A-Za-z0-9_-]{11})/g,
];

/**
 * Collects external images referenced by the content so the build can
 * self-host and optimize them.
 *
 * @param {string} cwd
 * @returns {Promise<Array<{ key: string, url: string, fallbackUrl?: string }>>}
 */
export async function collectRemoteImages(cwd) {
  const contentDir = path.resolve(cwd, "content");
  const files = walk(contentDir).filter((f) => MD_RE.test(f));

  /** @type {Map<string, { key: string, url: string, fallbackUrl?: string }>} */
  const byKey = new Map();

  for (const file of files) {
    let raw;
    try {
      raw = fs.readFileSync(file, "utf8");
    } catch {
      continue;
    }

    let parsed;
    try {
      parsed = matter(raw);
    } catch {
      continue;
    }

    // Author avatars from frontmatter.
    const avatar = parsed.data?.authorImageUrl;
    if (typeof avatar === "string" && avatar.startsWith("http")) {
      if (!byKey.has(avatar)) {
        byKey.set(avatar, { key: avatar, url: avatar, fallbackUrl: undefined });
      }
    }

    // YouTube ids from the body.
    for (const id of extractYouTubeIds(parsed.content)) {
      const key = `https://i.ytimg.com/vi/${id}/maxresdefault.jpg`;
      if (!byKey.has(key)) {
        byKey.set(key, {
          key,
          url: key,
          fallbackUrl: `https://i.ytimg.com/vi/${id}/hqdefault.jpg`,
        });
      }
    }
  }

  return [...byKey.values()];
}

function extractYouTubeIds(body) {
  const ids = new Set();

  for (const re of YOUTUBE_RES) {
    re.lastIndex = 0;
    let match;
    while ((match = re.exec(body)) !== null) {
      ids.add(match[1]);
    }
  }

  // <Video ... src="<id-or-url>"> — extract the 11-char id from the value.
  const videoRe = /<Video\b[^>]*\bsrc=["']([^"']+)["']/g;
  let videoMatch;
  while ((videoMatch = videoRe.exec(body)) !== null) {
    const id = idFromVideoSrc(videoMatch[1]);
    if (id) {
      ids.add(id);
    }
  }

  return ids;
}

function idFromVideoSrc(value) {
  const trimmed = value.trim();
  if (/^[A-Za-z0-9_-]{11}$/.test(trimmed)) {
    return trimmed;
  }
  const match = trimmed.match(/[A-Za-z0-9_-]{11}/);
  return match ? match[0] : null;
}

function walk(dir) {
  const out = [];
  let entries;
  try {
    entries = fs.readdirSync(dir, { withFileTypes: true });
  } catch {
    return out;
  }
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      out.push(...walk(full));
    } else if (entry.isFile()) {
      out.push(full);
    }
  }
  return out;
}
