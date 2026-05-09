const ID_RE = /^[a-zA-Z0-9_-]{11}$/;
const SUPPORTED_HOSTS = new Set([
  "youtube.com",
  "youtu.be",
  "youtube-nocookie.com",
]);

/**
 * Extracts an 11-char YouTube video ID from a bare ID or any common
 * YouTube URL form (watch, youtu.be, embed, shorts, v). Returns null
 * if the input doesn't reference a YouTube video.
 */
export function extractYouTubeId(input: string): string | null {
  const trimmed = input.trim();
  if (ID_RE.test(trimmed)) {
    return trimmed;
  }

  let url: URL;
  try {
    url = new URL(trimmed);
  } catch {
    return null;
  }

  const host = url.hostname.replace(/^(www\.|m\.)/, "");
  if (!SUPPORTED_HOSTS.has(host)) {
    return null;
  }

  if (host === "youtu.be") {
    const id = url.pathname.replace(/^\//, "").split("/")[0];
    return ID_RE.test(id) ? id : null;
  }

  const v = url.searchParams.get("v");
  if (v && ID_RE.test(v)) {
    return v;
  }

  const match = url.pathname.match(/^\/(?:embed|shorts|v)\/([a-zA-Z0-9_-]{11})/);
  return match ? match[1] : null;
}
