// Source-of-truth for the 3 Nitro tier cards on /pricing.
// Brewer eyebrows mirror Act5's mapping (French Press / Drip Brewer / Pour Over)
// but the surface is reframed as "what does this Nitro plan give me", with
// explicit price + sub-price + bullets + a single calibrated CTA.

export type TierKey = "nitro-free" | "nitro-hosted" | "nitro-self-hosted";

export interface Tier {
  readonly key: TierKey;
  readonly brewer: string;
  readonly name: string;
  readonly tagline: string;
  readonly price: string;
  readonly priceNote: string;
  readonly bullets: readonly string[];
  readonly cta: string;
  readonly ctaHref: string;
  readonly featured: boolean;
  readonly badge?: string;
}

export const TIERS: readonly Tier[] = [
  {
    key: "nitro-free",
    brewer: "FRENCH PRESS",
    name: "Nitro Free",
    tagline: "Hosted, shared, pay-per-request.",
    price: "$0",
    priceNote: "free forever",
    bullets: [
      "1M requests / mo included",
      "Schema registry + diffs",
      "Breaking-change detection in CI",
      "OpenTelemetry, 7-day retention",
      "Community support",
    ],
    cta: "Start free",
    ctaHref: "https://nitro.chillicream.com",
    featured: false,
  },
  {
    key: "nitro-hosted",
    brewer: "DRIP BREWER",
    name: "Nitro Hosted",
    tagline: "Single-tenant, reserved capacity.",
    price: "From $499",
    priceNote: "per month + usage",
    bullets: [
      "10M requests / mo included",
      "$0.40 / 1M requests overage",
      "Region pinning + reserved CPU",
      "OpenTelemetry, 30-day retention",
      "SSO + SCIM + audit logs",
      "Business-hours support, 99.9% SLA",
    ],
    cta: "Start with Hosted",
    ctaHref: "https://nitro.chillicream.com",
    featured: true,
    badge: "MOST POPULAR",
  },
  {
    key: "nitro-self-hosted",
    brewer: "POUR OVER",
    name: "Nitro Self-Hosted",
    tagline: "Your infra, your network, your security review.",
    price: "License",
    priceNote: "Helm, Docker, or air-gapped",
    bullets: [
      "Unlimited requests on your infra",
      "Helm + Docker + air-gapped install",
      "Bring-your-own observability",
      "Full data sovereignty",
      "Federation governance + RBAC",
      "Optional 24x7 support add-on",
    ],
    cta: "Talk to sales",
    ctaHref: "mailto:contact@chillicream.com?subject=Nitro%20Self-Hosted",
    featured: false,
  },
];
