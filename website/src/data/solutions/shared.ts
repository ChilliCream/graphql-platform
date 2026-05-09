import type { FeatureCardId, IconKind, LogoEntry } from "./types";

// Shared library of feature cards. Every solution page references six IDs
// out of this list. This is the template's superpower: 70% of the page is
// the same vocabulary, only hero/metrics/testimonials change. Updating a
// shared card here propagates across all seven solution pages.

export interface FeatureCard {
  readonly id: FeatureCardId;
  readonly title: string;
  readonly body: string;
  readonly icon: IconKind;
}

export const FEATURE_CARDS: Record<FeatureCardId, FeatureCard> = {
  performance: {
    id: "performance",
    title: "Federation-aware performance",
    body: "Sub-millisecond gateway overhead, cold-start under 200ms, persisted queries on the edge.",
    icon: "speed",
  },
  security: {
    id: "security",
    title: "RBAC + audit, baked in",
    body: "Field-level authorization, signed schemas, replay-safe audit log via Mocha.",
    icon: "shield",
  },
  observability: {
    id: "observability",
    title: "OpenTelemetry, no glue",
    body: "Traces, metrics, logs across resolvers, transports, and the Fusion gateway. No bespoke wiring.",
    icon: "graph",
  },
  dx: {
    id: "dx",
    title: "Hot Chocolate DX, end to end",
    body: "Same schema-first ergonomics from a single service to a federated fleet. No second framework to learn.",
    icon: "compose",
  },
  scale: {
    id: "scale",
    title: "Horizontal scale on Nitro",
    body: "Multi-region gateway, per-tenant rate limits, schema rollouts that never page anyone.",
    icon: "scale",
  },
  openness: {
    id: "openness",
    title: "MIT-licensed core, forever",
    body: "Hot Chocolate, Strawberry Shake, and Mocha are MIT and will stay MIT. No license rug pull.",
    icon: "globe",
  },
};

// Logo library. ~16 entries: 6 named (public references), 10 anonymous
// monograms covering the regulated/financial customer segments where we
// can name the sector but never the brand. Solution pages compose 8-12 of
// these into a wall, mixing named + anonymous so the wall reads as honest.
//
// The id is the stable reference key. The monogram is the 2-letter wordmark
// rendered into the tile when there is no real logo image (which, for now,
// is always — the trust wall is a typographic tile, not a raster wall).
export const LOGOS: Record<string, LogoEntry> = {
  microsoft: {
    id: "microsoft",
    label: "Microsoft",
    named: true,
    monogram: "MS",
  },
  adidas: { id: "adidas", label: "Adidas", named: true, monogram: "AD" },
  sbb: {
    id: "sbb",
    label: "SBB",
    named: true,
    monogram: "SB",
  },
  allianz: {
    id: "allianz",
    label: "Allianz",
    named: true,
    monogram: "AZ",
  },
  swissgrid: {
    id: "swissgrid",
    label: "Swissgrid",
    named: true,
    monogram: "SG",
  },
  publicSector: {
    id: "publicSector",
    label: "Public-sector cloud",
    named: true,
    monogram: "PS",
  },
  euTier1Bank: {
    id: "euTier1Bank",
    label: "EU Tier-1 Bank",
    named: false,
    monogram: "EB",
  },
  top3EuInsurer: {
    id: "top3EuInsurer",
    label: "Top-3 EU Insurer",
    named: false,
    monogram: "EI",
  },
  naHealthNetwork: {
    id: "naHealthNetwork",
    label: "NA Health Network",
    named: false,
    monogram: "NH",
  },
  logisticsPaaS: {
    id: "logisticsPaaS",
    label: "Logistics PaaS",
    named: false,
    monogram: "LP",
  },
  fsiGroup: {
    id: "fsiGroup",
    label: "FSI Group",
    named: false,
    monogram: "FG",
  },
  iberianRetailBank: {
    id: "iberianRetailBank",
    label: "Iberian Retail Bank",
    named: false,
    monogram: "IB",
  },
  dachReinsurer: {
    id: "dachReinsurer",
    label: "DACH Reinsurer",
    named: false,
    monogram: "DR",
  },
  nordicTelco: {
    id: "nordicTelco",
    label: "Nordic Telco",
    named: false,
    monogram: "NT",
  },
  ukChallengerBank: {
    id: "ukChallengerBank",
    label: "UK Challenger Bank",
    named: false,
    monogram: "UC",
  },
  globalCardNetwork: {
    id: "globalCardNetwork",
    label: "Global Card Network",
    named: false,
    monogram: "GC",
  },
};
