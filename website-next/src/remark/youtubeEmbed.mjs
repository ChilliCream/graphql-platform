/**
 * Rewrites a paragraph that contains nothing but a YouTube link into a
 * <Video> JSX element. Inline YouTube links inside surrounding text are left
 * alone, so raw markdown viewers (GitHub, etc.) still see a clickable link.
 */
const SUPPORTED_HOSTS = new Set([
  "youtube.com",
  "youtu.be",
  "youtube-nocookie.com",
]);

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
        if (!link || !isYouTubeUrl(link.url)) {
          continue;
        }
        node.children[i] = toVideoNode(link);
      }
    });
  };
}

function soleLinkChild(paragraph) {
  const meaningful = paragraph.children.filter(
    (c) => !(c.type === "text" && (c.value ?? "").trim() === "")
  );
  if (meaningful.length !== 1) {
    return null;
  }
  return meaningful[0].type === "link" ? meaningful[0] : null;
}

function isYouTubeUrl(url) {
  if (typeof url !== "string") {
    return false;
  }
  let parsed;
  try {
    parsed = new URL(url);
  } catch {
    return false;
  }
  const host = parsed.hostname.replace(/^(www\.|m\.)/, "");
  return SUPPORTED_HOSTS.has(host);
}

function toVideoNode(link) {
  const label = linkText(link).trim();
  const attributes = [
    { type: "mdxJsxAttribute", name: "src", value: link.url },
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
    name: "Video",
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
