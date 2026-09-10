import type { CSSProperties } from "react";

/**
 * Palette, cast and drawing helpers for the Editorial concept (prototype v10).
 *
 * The concept reads the page as a printed magazine feature that is being
 * annotated by hand: the gateway is a sketched hub, subgraphs are sticky notes
 * with handwritten language and specification labels, clients are doodled
 * devices, composition is a red-pen correction on the proof sheet, and Nitro is
 * a highlighter drawn over the operations real clients run.
 */

/** Ink-on-paper colours, kept out of the visuals so the six scenes match. */
export const PAPER = {
  /** The sheet every scene is drawn on. */
  sheet: "#f4efe3",
  sheetEdge: "#e7dfcd",
  /** Warm shadow under a taped or pinned element. */
  shade: "rgba(28, 27, 24, 0.12)",
  /** Main marker ink. */
  ink: "#1f1d1a",
  /** Second pass of the same marker, lighter. */
  inkSoft: "#4b463d",
  /** Pencil marginalia. */
  pencil: "#7c7466",
  /** Editor's red pen. */
  red: "#c0392b",
  /** Proof-reader's blue pencil. */
  blue: "#2f5d8c",
  /** Green pen used for a passing check. */
  green: "#3f7d52",
  /** Highlighter wash. */
  highlight: "rgba(244, 200, 63, 0.55)",
  highlightEdge: "rgba(214, 160, 20, 0.75)",
  /** Sticky note stocks, in cast order. */
  noteA: "#f6e39c",
  noteB: "#f2c9a0",
  noteC: "#cfe3b8",
  noteD: "#bfd8ea",
  noteE: "#e6cde4",
} as const;

/**
 * CSS variable overrides applied to the concept root so the shared
 * design-system link and button primitives print as ink on paper instead of
 * the site's dark chrome. Every token the concept renders is listed.
 */
export const PAPER_TOKENS = {
  "--color-cc-bg": PAPER.sheet,
  "--color-cc-surface": PAPER.sheet,
  "--color-cc-heading": PAPER.ink,
  "--color-cc-ink": PAPER.inkSoft,
  "--color-cc-prose": "rgba(31, 29, 26, 0.86)",
  "--color-cc-ink-dim": PAPER.pencil,
  "--color-cc-ink-faint": "rgba(31, 29, 26, 0.18)",
  "--color-cc-nav-label": PAPER.pencil,
  "--color-cc-card-border": "rgba(31, 29, 26, 0.22)",
  "--color-cc-card-border-hover": "rgba(31, 29, 26, 0.5)",
  "--color-cc-accent": PAPER.blue,
  "--color-cc-white": PAPER.ink,
} as const;

/** Handwritten marginalia and note labels. */
export const HAND = {
  fontFamily:
    "'Bradley Hand', 'Segoe Print', 'Chalkboard SE', 'Comic Sans MS', cursive",
} as const;

/** Larger marker lettering for headings drawn inside a scene. */
export const MARKER = {
  fontFamily:
    "'Bradley Hand', 'Segoe Print', 'Chalkboard SE', 'Comic Sans MS', cursive",
  letterSpacing: "0.01em",
} as const;

/** Set in type for folio marks and small caps captions inside a scene. */
export const FOLIO = {
  fontFamily:
    "ui-monospace, SFMono-Regular, Menlo, Consolas, 'Liberation Mono', monospace",
  letterSpacing: "0.16em",
} as const;

export type SubgraphSpec = "GraphQL Federation" | "Apollo Federation";

export interface Note {
  /** The subgraph the sticky note stands for. */
  readonly name: string;
  /** Handwritten under the name: the language its server is written in. */
  readonly language: string;
  /** The federation specification the source schema is written to. */
  readonly spec: SubgraphSpec;
  /** Short handwritten spec label; both specs are drawn the same way. */
  readonly specLabel: string;
  /** Note stock colour. */
  readonly stock: string;
}

/** The five GraphQL subgraphs, in reading order. */
export const NOTES: readonly Note[] = [
  {
    name: "Catalog",
    language: "JS/TS",
    spec: "GraphQL Federation",
    specLabel: "GraphQL Fed",
    stock: PAPER.noteA,
  },
  {
    name: "Billing",
    language: "Java",
    spec: "Apollo Federation",
    specLabel: "Apollo Fed",
    stock: PAPER.noteB,
  },
  {
    name: "Ordering",
    language: "Go",
    spec: "GraphQL Federation",
    specLabel: "GraphQL Fed",
    stock: PAPER.noteC,
  },
  {
    name: "Shipping",
    language: "Ruby",
    spec: "Apollo Federation",
    specLabel: "Apollo Fed",
    stock: PAPER.noteD,
  },
  {
    name: "Accounts",
    language: "C#",
    spec: "GraphQL Federation",
    specLabel: "GraphQL Fed",
    stock: PAPER.noteE,
  },
];

export type SourceKind = "OpenAPI" | "gRPC";

export interface Source {
  readonly name: string;
  readonly kind: SourceKind;
}

/** Index cards taped to the same spread: sources that are not GraphQL servers. */
export const SOURCES: readonly Source[] = [
  { name: "Payments", kind: "OpenAPI" },
  { name: "Inventory", kind: "gRPC" },
];

export interface Client {
  readonly name: string;
  /** Doodle drawn for it. */
  readonly device: "laptop" | "phone" | "card" | "robot";
}

/** The four doodled clients that send queries to the one endpoint. */
export const CLIENTS: readonly Client[] = [
  { name: "Web", device: "laptop" },
  { name: "Mobile", device: "phone" },
  { name: "Partner API", device: "card" },
  { name: "Agent", device: "robot" },
];

/**
 * Style for a stroke that draws itself: the path length the keyframes count
 * down from, plus the delay that stages it after the lines drawn before it.
 * A generous length is fine - the stroke simply finishes a little early.
 */
export function drawStyle(length: number, delay = 0): CSSProperties {
  return { "--len": length, animationDelay: `${delay}s` } as CSSProperties;
}

/** Style for an annotation that fades in after the lines around it. */
export function fadeStyle(delay: number): CSSProperties {
  return { animationDelay: `${delay}s` };
}
