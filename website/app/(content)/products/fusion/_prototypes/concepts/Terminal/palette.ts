import { BRAND, CC, FONTS } from "../../brand";

/**
 * Palette and cast for the Terminal concept (prototype v8).
 *
 * The concept reads the gateway as a shell session: there is no illustration
 * anywhere, only monospace text and box-drawing characters. Subgraphs are
 * files, so their language is a file extension; the federation specification a
 * source schema is written to is a directive on that file; composition is a
 * command that either prints a composite schema or exits non-zero; Nitro is a
 * second command that checks the operation registry.
 */

/**
 * Terminal surface colours, shared by all six scenes so they read as one
 * session. Every role is a site token or a brand accent from `../../brand`,
 * mixed with `color-mix` where the scene wants the hue at reduced strength;
 * the concept contributes the role names, never a colour of its own.
 */
export const TERM = {
  /** Scene background behind the pane: the page background. */
  bg: CC.bg,
  /** Pane background: the solid brand navy surface. */
  pane: CC.surface,
  /** Hairlines and box-drawing rules. */
  rule: CC.cardBorder,
  /** Ordinary output: the brightest text token, the way a shell prints. */
  text: CC.heading,
  /** Secondary output: timestamps, column headers, comments. */
  dim: CC.inkDim,
  /** Output that has not been written yet. */
  faint: CC.inkFaint,
  /** The prompt sigil and anything the operator typed. */
  prompt: CC.accent,
  /** Success: check marks, `pass`, `safe`. The phosphor glow, as brand teal. */
  ok: `color-mix(in srgb, ${BRAND.teal} 90%, transparent)`,
  /** Warning: `risky`, spinner. */
  warn: BRAND.amber,
  /** Failure: `breaking`, conflicts, non-zero exits. */
  err: CC.danger,
  /** Specification directives; both specs use this one colour on purpose. */
  spec: BRAND.violet,
  /** Block caret. */
  caret: CC.heading,
  /** The cursor bar Nitro drags down a scanned table row. */
  scan: `color-mix(in srgb, ${BRAND.cyan} 14%, transparent)`,
} as const;

/** The one font stack every Terminal scene renders in: the site mono face. */
export const MONO = FONTS.mono;

export type SubgraphSpec = "GraphQL Federation" | "Apollo Federation";

export interface Subgraph {
  /** Catalog, Billing, Ordering, Shipping, Accounts. */
  readonly name: string;
  /** The source schema as a file; the extension is the language tag. */
  readonly file: string;
  /** The language the extension stands for. */
  readonly language: string;
  readonly spec: SubgraphSpec;
  /** The specification written as a directive on the file. */
  readonly directive: string;
}

/** The five GraphQL subgraphs, in composition order; C# is never first. */
export const SUBGRAPHS: readonly Subgraph[] = [
  {
    name: "Catalog",
    file: "catalog.ts",
    language: "JS/TS",
    spec: "GraphQL Federation",
    directive: "@graphql-federation",
  },
  {
    name: "Billing",
    file: "billing.java",
    language: "Java",
    spec: "Apollo Federation",
    directive: "@apollo-federation",
  },
  {
    name: "Ordering",
    file: "ordering.go",
    language: "Go",
    spec: "GraphQL Federation",
    directive: "@graphql-federation",
  },
  {
    name: "Shipping",
    file: "shipping.rb",
    language: "Ruby",
    spec: "Apollo Federation",
    directive: "@apollo-federation",
  },
  {
    name: "Accounts",
    file: "accounts.cs",
    language: "C#",
    spec: "GraphQL Federation",
    directive: "@graphql-federation",
  },
];

export type SourceKind = "OpenAPI" | "gRPC";

export interface NonGraphQLSource {
  readonly name: string;
  readonly file: string;
  readonly kind: SourceKind;
}

/** Sources that are not GraphQL servers and compose into the same schema. */
export const SOURCES: readonly NonGraphQLSource[] = [
  { name: "Payments", file: "payments.openapi.yaml", kind: "OpenAPI" },
  { name: "Inventory", file: "inventory.proto", kind: "gRPC" },
];

/** Registered clients, as they appear in the operation registry. */
export const CLIENTS = ["web", "mobile", "partner", "agent"] as const;

/** Spinner frames; four is enough to read as motion and keeps the DOM small. */
export const SPINNER = ["⠋", "⠹", "⠼", "⠴"] as const;
