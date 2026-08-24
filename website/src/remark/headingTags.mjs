/**
 * Turns a version line that directly follows a heading into <HeadingTags> badges
 * rendered next to that heading:
 *
 *   # `nitro fusion publish`
 *
 *   Since: 16.6.0, Nitro: 10.3.0
 *
 * The same line as the first block of a document tags the page title instead;
 * those tags are handed to the layout via `file.data.headingTags`.
 *
 * The line stays plain, readable markdown for raw viewers (GitHub, editors).
 * A paragraph is only consumed when it directly follows a heading, contains
 * nothing but text, and every `key: value` pair uses a known key. Anything
 * else (for example a prose paragraph starting with "Note: ...") is left
 * untouched.
 */
const KEYS = new Map([
  ["since", "since"],
  ["nitro", "requiresNitro"],
]);

// Rendering order of the tags, independent of the authored order.
const PROP_ORDER = ["since", "requiresNitro"];

const DATA_ATTRIBUTES = {
  since: "data-since",
  requiresNitro: "data-requires-nitro",
};

export default function remarkHeadingTags() {
  return (tree, file) => {
    // A tag line as the very first block of a document tags the page title
    // (which the layout renders from the frontmatter, not from the body).
    const first = tree.children?.[0];
    if (first?.type === "paragraph") {
      const tags = parseTagLine(first);
      if (tags) {
        file.data ??= {};
        file.data.headingTags = Object.fromEntries(
          PROP_ORDER.filter((prop) => tags.has(prop)).map((prop) => [prop, tags.get(prop)]),
        );
        tree.children.shift();
      }
    }

    walk(tree, (node) => {
      if (!Array.isArray(node.children)) {
        return;
      }
      for (let i = node.children.length - 1; i > 0; i--) {
        const heading = node.children[i - 1];
        const paragraph = node.children[i];
        if (heading.type !== "heading" || paragraph.type !== "paragraph") {
          continue;
        }
        const tags = parseTagLine(paragraph);
        if (!tags) {
          continue;
        }
        applyTags(heading, tags);
        node.children.splice(i, 1);
      }
    });
  };
}

function parseTagLine(paragraph) {
  if (!paragraph.children.every((child) => child.type === "text")) {
    return null;
  }
  const text = paragraph.children.map((child) => child.value ?? "").join("");
  if (!text.includes(":")) {
    return null;
  }

  const tags = new Map();
  for (const part of text.split(",")) {
    const match = part.match(/^\s*([^:]+?)\s*:\s*(.+?)\s*$/);
    if (!match) {
      return null;
    }
    const prop = KEYS.get(match[1].toLowerCase());
    if (!prop || tags.has(prop)) {
      return null;
    }
    tags.set(prop, match[2]);
  }
  return tags.size > 0 ? tags : null;
}

/**
 * Tags travel as `data-*` attributes on the heading itself rather than as
 * extra children, so heading text, anchor ids and the TOC stay untouched and
 * the heading component decides where to place the badges.
 */
function applyTags(heading, tags) {
  heading.data ??= {};
  heading.data.hProperties ??= {};
  for (const prop of PROP_ORDER) {
    if (tags.has(prop)) {
      heading.data.hProperties[DATA_ATTRIBUTES[prop]] = tags.get(prop);
    }
  }
}

function walk(node, fn) {
  fn(node);
  if (Array.isArray(node.children)) {
    for (const child of node.children) {
      walk(child, fn);
    }
  }
}
