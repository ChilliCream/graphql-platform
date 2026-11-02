/**
 * Zero-dependency fuzzy subsequence matcher with scoring.
 *
 * Returns a FuzzyResult with score and matched character indices,
 * or null if the query is not a subsequence of the target.
 */
export interface FuzzyResult {
  score: number;
  matchedIndices: number[];
}
/**
 * Fuzzy-match `query` against `target`.
 *
 * - Exact substring match gets highest score (~1000+).
 * - Subsequence matching with bonuses for consecutive chars, word
 *   boundaries (`.`, `-`, `_`, camelCase), early position, and
 *   length ratio.
 * - Case-insensitive.
 *
 * Returns `null` when the query is not a subsequence of the target.
 */
export declare function fuzzyMatch(
  query: string,
  target: string
): FuzzyResult | null;
