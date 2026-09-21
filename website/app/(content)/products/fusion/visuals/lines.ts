/**
 * Text-splitting helpers shared by the Fusion page's console visuals, for
 * breaking a label's copy across several stacked `<tspan>` lines. Not
 * `tokens.ts`: these operate on strings, not design values.
 */

/**
 * Splits `text` at every ` · ` separator, each line keeping its leading
 * separator so the lines' concatenated content is `text` again, byte for
 * byte.
 */
export function dotLines(text: string): readonly string[] {
  const parts = text.split(" · ");
  return parts.map((part, i) => (i === 0 ? part : ` · ${part}`));
}

/**
 * Greedily wraps `text` at word boundaries so each line stays within
 * `maxChars` characters (an estimate of the mono face's natural width at
 * the target render size). The break space is kept as the next line's
 * leading character, so the lines' concatenated content is `text` again,
 * byte for byte.
 */
export function wrapWords(text: string, maxChars: number): readonly string[] {
  const words = text.split(" ");
  const lines: string[] = [];
  let line = words[0] ?? "";
  for (let i = 1; i < words.length; i++) {
    const word = words[i];
    if (line.length + 1 + word.length <= maxChars) {
      line += ` ${word}`;
    } else {
      lines.push(line);
      line = ` ${word}`;
    }
  }
  lines.push(line);
  return lines;
}
