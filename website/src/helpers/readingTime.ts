const WORDS_PER_MINUTE = 200;

/**
 * Strips markdown / MDX scaffolding (frontmatter, code fences, JSX tags, image
 * and link syntax) before counting words. The result drives a coarse "N min
 * read" estimate at @WORDS_PER_MINUTE — matches the gatsby-remark-reading-time
 * heuristic the legacy site used.
 */
export function estimateReadingTime(source: string): {
  minutes: number;
  text: string;
  words: number;
} {
  const text = source
    .replace(/^---\n[\s\S]*?\n---\n?/, "")
    .replace(/```[\s\S]*?```/g, " ")
    .replace(/`[^`]*`/g, " ")
    .replace(/<[^>]+>/g, " ")
    .replace(/!\[[^\]]*]\([^)]*\)/g, " ")
    .replace(/\[([^\]]+)]\([^)]*\)/g, "$1")
    .replace(/[#>*_~|-]+/g, " ");

  const words = text.split(/\s+/).filter((w) => /[\p{L}\p{N}]/u.test(w)).length;

  const minutes = Math.max(1, Math.round(words / WORDS_PER_MINUTE));
  return {
    minutes,
    text: `${minutes} min read`,
    words,
  };
}
