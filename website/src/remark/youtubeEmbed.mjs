/**
 * Rewrites a paragraph that contains nothing but a YouTube link into a
 * <YouTubeVideo> JSX element. Inline YouTube links inside surrounding text are
 * left alone, so raw markdown viewers (GitHub, etc.) still see a clickable link.
 */
const SUPPORTED_HOSTS = new Set([
  "youtube.com",
  "youtu.be",
  "youtube-nocookie.com",
]);

const ID_RE = /^[a-zA-Z0-9_-]{11}$/;

export default function remarkYouTubeEmbed() {
  return (tree) => {
    walk(tree, (node) => {
      if (!Array.isArray(node.children)) {
        return;
      }
      for (let i = 0; i < node.children.length; i++) {
        const child = node.children[i];
        if (child.type !== "paragraph") {
          continue;
        }
        const link = soleLinkChild(child);
        if (!link) {
          continue;
        }
        const videoId = extractYouTubeId(link.url);
        if (!videoId) {
          continue;
        }
        node.children[i] = toVideoNode(link, videoId);
      }
    });
  };
}

function soleLinkChild(paragraph) {
  const meaningful = paragraph.children.filter(
    (c) => !(c.type === "text" && (c.value ?? "").trim() === ""),
  );
  if (meaningful.length !== 1) {
    return null;
  }
  return meaningful[0].type === "link" ? meaningful[0] : null;
}

function extractYouTubeId(url) {
  if (typeof url !== "string") {
    return null;
  }
  let parsed;
  try {
    parsed = new URL(url.trim());
  } catch {
    return null;
  }
  const host = parsed.hostname.replace(/^(www\.|m\.)/, "");
  if (!SUPPORTED_HOSTS.has(host)) {
    return null;
  }
  if (host === "youtu.be") {
    const id = parsed.pathname.replace(/^\//, "").split("/")[0];
    return ID_RE.test(id) ? id : null;
  }
  const v = parsed.searchParams.get("v");
  if (v && ID_RE.test(v)) {
    return v;
  }
  const match = parsed.pathname.match(
    /^\/(?:embed|shorts|v)\/([a-zA-Z0-9_-]{11})/,
  );
  return match ? match[1] : null;
}

function toVideoNode(link, videoId) {
  const label = linkText(link).trim();
  const attributes = [
    { type: "mdxJsxAttribute", name: "videoId", value: videoId },
  ];
  if (label.length > 0) {
    attributes.push({
      type: "mdxJsxAttribute",
      name: "playlabel",
      value: label,
    });
  }
  return {
    type: "mdxJsxFlowElement",
    name: "YouTubeVideo",
    attributes,
    children: [],
  };
}

function linkText(node) {
  if (node.type === "text") {
    return node.value ?? "";
  }
  if (Array.isArray(node.children)) {
    return node.children.map(linkText).join("");
  }
  return "";
}

function walk(node, fn) {
  fn(node);
  if (Array.isArray(node.children)) {
    for (const child of node.children) {
      walk(child, fn);
    }
  }
}
