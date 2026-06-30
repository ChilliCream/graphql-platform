/**
 * The Nitro pricing tiers, shared by the landing "Brew it your Way" selector
 * (NitroPricing) and the pricing page (PlanSelector, CompareTable). The rest of
 * the pricing content (comparison matrix, FAQ, unlocks) lives inline in its own
 * component.
 *
 * Tiers, in order: Free (shared) -> Pay as you go (shared) -> Dedicated
 * (single-tenant, volume based) -> Self-Hosted (your infrastructure).
 */

export type TierId = "free" | "payg" | "dedicated" | "self";

export interface Tier {
  readonly id: TierId;
  readonly name: string;
  readonly tagline: string;
  /** Display price, e.g. "$0", "$20", "from $400", "Custom". */
  readonly price: string;
  /** Small note under the price, e.g. "forever", "per month", "talk to us". */
  readonly priceNote: string;
  /** Headline bullets for the plan card. */
  readonly features: readonly string[];
  readonly cta: string;
  readonly ctaHref: string;
  /** The highlighted / recommended tier. */
  readonly popular?: boolean;
}

export const TIERS: readonly Tier[] = [
  {
    id: "free",
    name: "Free",
    tagline: "Shared cloud, fully managed.",
    price: "$0",
    priceNote: "",
    features: [
      "Shared multi-tenant cloud",
      "Schemas & environments included",
      "1M operations / month",
      "2 GB ingest / month",
      "3-day log & trace retention",
      "Community support",
    ],
    cta: "Start for Free",
    ctaHref: "https://nitro.chillicream.com",
  },
  {
    id: "payg",
    name: "Pay as you go",
    tagline: "Shared cloud, usage based.",
    price: "$20",
    priceNote: "per month",
    features: [
      "Shared multi-tenant cloud",
      "5M operations included, then $2 / million",
      "2 GB ingest per 1M ops, then $1.15 / GB",
      "60-day log & trace retention",
      "Email support",
    ],
    cta: "Start for Free",
    ctaHref: "https://nitro.chillicream.com",
  },
  {
    id: "dedicated",
    name: "Dedicated",
    tagline: "Single-tenant, volume based.",
    price: "from $400",
    priceNote: "per month",
    features: [
      "Single-tenant cloud or BYOC",
      "Priced by instance size",
      "Configurable retention",
      "Private networking",
      "SSO, audit log, role-based access",
    ],
    cta: "Talk to Us",
    ctaHref: "/services/support/contact?subject=Sales",
    popular: true,
  },
  {
    id: "self",
    name: "Self-Hosted",
    tagline: "Your infrastructure.",
    price: "Custom",
    priceNote: "talk to us",
    features: [
      "Run on your own infrastructure",
      "Air-gapped & on-prem supported",
      "Configurable retention",
      "Priority engineering support",
      "Long-term release channel",
    ],
    cta: "Talk to Us",
    ctaHref: "/services/support/contact?subject=Sales",
  },
];
