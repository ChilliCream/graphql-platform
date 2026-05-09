// Typed schema for the /solutions/[slug] system. Every page is a record of
// this shape, rendered by the shared SolutionPageRenderer. Adding a new
// solution page is "add another SolutionRecord, not another component".
//
// The content axis splits into use-case pages (the volume play, includes a
// code snippet) and industry pages (the lean, audit-friendly variant, no
// code snippet, fewer pillars allowed). The renderer respects the absence
// of optional fields so industry pages naturally drop sections.

import type { AccentTokens } from "@/components/redesign-system/AccentThread";

export type SolutionCategory = "use-case" | "industry";

// Page-level hero motif. Each solution page picks one so the hero canvas
// reads as solution-specific even though the layout is shared. The renderer
// maps the chosen motif to one of the abstract spatial primitives.
export type HeroMotif =
  | "orbit"
  | "hemisphere"
  | "perspective-grid"
  | "ray-burst"
  | "light-cone"
  | "dashboard"
  | "typographic";

export type IconKind =
  | "stack"
  | "shield"
  | "graph"
  | "bus"
  | "agent"
  | "lock"
  | "scale"
  | "audit"
  | "speed"
  | "globe"
  | "compose"
  | "schema";

export type DiagramKind =
  | "polyglot"
  | "federation"
  | "single-graph"
  | "agents"
  | "event-bus"
  | "compliance";

export type FeatureCardId =
  | "performance"
  | "security"
  | "observability"
  | "dx"
  | "scale"
  | "openness";

export type CollateralKind = "playbook" | "starter" | "workshop";

export interface CtaLink {
  readonly label: string;
  readonly href: string;
}

export interface ProofMetric {
  readonly value: string;
  readonly outcome: string;
  readonly customer: string;
}

export interface Pillar {
  readonly title: string;
  readonly body: string;
  readonly icon: IconKind;
}

export interface Testimonial {
  readonly quote: string;
  readonly author: string;
  readonly title: string;
  readonly company: string;
  readonly monogram?: string;
}

export interface Collateral {
  readonly title: string;
  readonly href: string;
  readonly kind: CollateralKind;
}

export interface CodeSnippet {
  readonly language: string;
  readonly fileName: string;
  readonly source: string;
}

export interface LogoEntry {
  readonly id: string;
  readonly label: string;
  readonly named: boolean;
  // Two-letter monogram. For named brands, the wordmark; for anonymous
  // tier-coded customers, an industry initial pair (e.g. "EB" for "EU
  // Tier-1 Bank").
  readonly monogram: string;
}

export interface SolutionHero {
  readonly eyebrow: string;
  readonly headline: string;
  readonly headlineAccent?: string;
  readonly sub: string;
  readonly primaryCta: CtaLink;
  readonly secondaryCta: CtaLink;
}

export interface SolutionPillars {
  readonly headline: string;
  readonly sub?: string;
  readonly items: readonly Pillar[];
}

export interface SolutionFinalCta {
  readonly headline: string;
  readonly sub: string;
  readonly primary: CtaLink;
  readonly secondary: CtaLink;
  readonly tertiary?: CtaLink;
}

export interface SolutionRecord {
  readonly slug: string;
  readonly category: SolutionCategory;
  readonly title: string;
  readonly metaDescription: string;
  readonly hero: SolutionHero;
  readonly proofMetrics: readonly ProofMetric[];
  readonly pillars: SolutionPillars;
  readonly diagram: DiagramKind;
  readonly codeSnippet?: CodeSnippet;
  readonly testimonials: readonly Testimonial[];
  readonly featureCards: readonly FeatureCardId[];
  readonly collateral?: Collateral;
  readonly logos: readonly string[];
  readonly logoCaption: string;
  readonly finalCta: SolutionFinalCta;
  readonly related: readonly string[];
  /**
   * Per-slug accent override. Layered onto the shared "solutions" page
   * accent so each slug has a distinct chromatic identity that threads
   * through hero, diagram, proof, and final CTA.
   */
  readonly accent?: Partial<AccentTokens>;
  /**
   * Page-level hero motif, picked from the abstract-spatial primitives.
   * The renderer resolves this to the matching illustration so the hero
   * reads as solution-specific even though the layout is shared.
   */
  readonly heroMotif?: HeroMotif;
}
