/**
 * The scene-illustration versions. Each version presents the same five homepage
 * scenes as a set of content hooks, but with its own coherent on-brand
 * illustration STRATEGY and copy ANGLE. Every version lives in the site's dark
 * cc-* design language (navy surfaces, cream/grey ink, teal accent, mono
 * labels); versions differ by how they illustrate and phrase the hooks, not by
 * switching to a foreign visual style. Creativity ramps with the number: v1 is
 * the literal product baseline, v5 the boldest take in the current set.
 *
 * Single source of truth for the version hub and every version page header.
 */
export interface VersionSpec {
  readonly v: number;
  readonly name: string;
  readonly summary: string;
  readonly route: string;
}

export const VERSIONS: readonly VersionSpec[] = [
  {
    v: 1,
    name: "Product Panels",
    summary:
      "The literal baseline: cropped Nitro product panels and code cards on deep navy. Shows the real UI behind each scene.",
    route: "/platform/scene-illustrations/v1",
  },
  {
    v: 2,
    name: "Flow Diagrams",
    summary:
      "Each hook drawn as a relationship diagram: labeled nodes and connectors that show how the pieces fit and flow together.",
    route: "/platform/scene-illustrations/v2",
  },
  {
    v: 3,
    name: "Signal & Metrics",
    summary:
      "Outcome-forward: big cream numerals, teal sparklines and bars that put the measurable result of each scene up front.",
    route: "/platform/scene-illustrations/v3",
  },
  {
    v: 4,
    name: "Generated Artifacts",
    summary:
      "The real emitted artifacts: small annotated SDL, code, and terminal snippets that show exactly what the platform produces.",
    route: "/platform/scene-illustrations/v4",
  },
  {
    v: 5,
    name: "Schematic Lines",
    summary:
      "Minimal monoline schematics: thin teal strokes and generous negative space distilling each scene to its essential structure.",
    route: "/platform/scene-illustrations/v5",
  },
  {
    v: 6,
    name: "Bespoke Hooks",
    summary:
      "Each hook individually designed for its own idea (no shared template), with sharpened, research-driven marketing copy. The production-candidate set.",
    route: "/platform/scene-illustrations/v6",
  },
];

/** Look up a single version by its number. Throws if the version is unknown. */
export function getVersion(v: number): VersionSpec {
  const found = VERSIONS.find((entry) => entry.v === v);
  if (!found) {
    throw new Error(`Unknown scene-illustration version: v${v}`);
  }
  return found;
}
