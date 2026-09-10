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

/** Terminal surface colours, shared by all six scenes so they read as one session. */
export const TERM = {
  /** Scene background behind the pane. */
  bg: "#080b12",
  /** Pane background. */
  pane: "#0b111c",
  /** Hairlines and box-drawing rules. */
  rule: "rgba(245, 241, 234, 0.14)",
  /** Ordinary output. */
  text: "#ded9d1",
  /** Secondary output: timestamps, column headers, comments. */
  dim: "#7c8798",
  /** Output that has not been written yet. */
  faint: "#3f4a5c",
  /** The prompt sigil and anything the operator typed. */
  prompt: "#16b9e4",
  /** Success: check marks, `pass`, `safe`. */
  ok: "#5eead4",
  /** Warning: `risky`, spinner. */
  warn: "#f6c177",
  /** Failure: `breaking`, conflicts, non-zero exits. */
  err: "#f87171",
  /** Specification directives; both specs use this one colour on purpose. */
  spec: "#c4b5fd",
  /** Block caret. */
  caret: "#f5f0ea",
} as const;

/** The one font stack every Terminal scene renders in. */
export const MONO =
  "ui-monospace, SFMono-Regular, Menlo, Consolas, 'Liberation Mono', monospace";

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
