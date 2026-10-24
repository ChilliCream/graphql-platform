import type { BenchmarkCatalogEntry } from "@/src/helpers/benchmarkReport";

export interface BenchmarkFilters {
  readonly technologies: readonly string[];
  readonly showPreviews: boolean;
  /** Whether each gateway shows only its best preview and best non-preview variant per composition. */
  readonly bestVersionOnly: boolean;
}

/** The runtime family, or language for platforms without a runtime. */
export function benchmarkTechnology(entry: BenchmarkCatalogEntry) {
  return (
    entry.variant.platform.runtime?.name ?? entry.variant.platform.language.name
  );
}

/** An empty technology selection includes every runtime family. */
export function matchesBenchmarkTechnology(
  entry: BenchmarkCatalogEntry,
  technologies: readonly string[],
) {
  return (
    technologies.length === 0 ||
    technologies.includes(benchmarkTechnology(entry))
  );
}
