// Per-integration accent colors for the Native grid. Approximates the partner
// brand color so the filled monogram tile reads as an intentional logo, not a
// generic placeholder. Where a partner color isn't well-known, we pick from a
// curated palette so the grid still has visible variety.
//
// The fallback accent is a neutral cream so unknown integrations degrade
// gracefully into the existing cream-on-navy treatment.
//
// Native cards use these on the monogram tile and the card's left edge.
// Community cards intentionally do NOT pick up these accents: the visual
// asymmetry IS the trust signal (Native = real color, Community = letters).

import type { Integration } from "./integrations";

export interface PartnerAccent {
  /** Tile fill color; carries the brand identity. */
  readonly fill: string;
  /** Letter color on top of the fill. White or near-black depending on luminance. */
  readonly ink: string;
  /** Card left-edge stripe color, derived from the fill. */
  readonly edge: string;
}

// Curated palette: real partner colors where well-known, otherwise distributed
// from a small set of hues so the grid has variety.
const PARTNER_ACCENTS: Readonly<Record<string, PartnerAccent>> = {
  // AI & Agents
  "model-context-protocol": {
    fill: "#22c97a",
    ink: "#0c1322",
    edge: "rgba(34, 201, 122, 0.95)",
  },
  // Observability
  opentelemetry: {
    fill: "#7c5cff",
    ink: "#ffffff",
    edge: "rgba(124, 92, 255, 0.95)",
  },
  jaeger: {
    fill: "#e8772e",
    ink: "#0c1322",
    edge: "rgba(232, 119, 46, 0.95)",
  },
  tempo: {
    fill: "#f46800",
    ink: "#ffffff",
    edge: "rgba(244, 104, 0, 0.95)",
  },
  datadog: {
    fill: "#632ca6",
    ink: "#ffffff",
    edge: "rgba(99, 44, 166, 0.95)",
  },
  honeycomb: {
    fill: "#f5a623",
    ink: "#0c1322",
    edge: "rgba(245, 166, 35, 0.95)",
  },
  // Auth
  auth0: {
    fill: "#eb5424",
    ink: "#ffffff",
    edge: "rgba(235, 84, 36, 0.95)",
  },
  "microsoft-entra-id": {
    fill: "#0078d4",
    ink: "#ffffff",
    edge: "rgba(0, 120, 212, 0.95)",
  },
  "openid-connect": {
    fill: "#f78c40",
    ink: "#0c1322",
    edge: "rgba(247, 140, 64, 0.95)",
  },
  // Messaging
  kafka: {
    fill: "#2bb3c0",
    ink: "#0c1322",
    edge: "rgba(43, 179, 192, 0.95)",
  },
  "azure-service-bus": {
    fill: "#0080d0",
    ink: "#ffffff",
    edge: "rgba(0, 128, 208, 0.95)",
  },
  // Data
  postgresql: {
    fill: "#336791",
    ink: "#ffffff",
    edge: "rgba(51, 103, 145, 0.95)",
  },
  "entity-framework-core": {
    fill: "#512bd4",
    ink: "#ffffff",
    edge: "rgba(81, 43, 212, 0.95)",
  },
  // Frontend
  nextjs: {
    fill: "#f5f1ea",
    ink: "#0c1322",
    edge: "rgba(245, 241, 234, 0.95)",
  },
};

const FALLBACK: PartnerAccent = {
  fill: "rgba(245, 241, 234, 0.92)",
  ink: "#0c1322",
  edge: "rgba(245, 241, 234, 0.85)",
};

/**
 * Resolve the partner accent for a Native integration. Community integrations
 * deliberately receive no accent so the grid asymmetry communicates the
 * native-vs-community trust tier without any badge work.
 */
export const partnerAccent = (integration: Integration): PartnerAccent => {
  if (integration.type !== "native") {
    return FALLBACK;
  }
  return PARTNER_ACCENTS[integration.slug] ?? FALLBACK;
};
