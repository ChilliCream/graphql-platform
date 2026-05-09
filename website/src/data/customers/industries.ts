// Industry catalogue for the customers page. Each industry carries a
// monogram letter, an accent token name (which maps onto the existing
// --cc-col-* palette), and a short brewer-style descriptor used in
// AnonymousMonogram tiles and in the trust wall captions.
//
// Order matters: the trust-wall tabs render in this order, and the
// detail-page eyebrow uses `label` verbatim.

export type IndustryAccent =
  | "cat" // amber-orange — Retail
  | "bil" // honey-yellow — Public sector
  | "ord" // green — Healthcare
  | "shi" // blue — Banking & Insurance
  | "usr" // violet — Software & SaaS
  | "tel"; // teal — Telco & Media

export interface Industry {
  readonly key: string;
  readonly label: string;
  readonly short: string;
  readonly monogram: string;
  readonly accent: IndustryAccent;
  readonly accentVar: string;
}

export const INDUSTRIES: readonly Industry[] = [
  {
    key: "banking-insurance",
    label: "Banking & Insurance",
    short: "Banking",
    monogram: "B",
    accent: "shi",
    accentVar: "var(--cc-col-shi)",
  },
  {
    key: "retail-ecommerce",
    label: "Retail & E-commerce",
    short: "Retail",
    monogram: "R",
    accent: "cat",
    accentVar: "var(--cc-col-cat)",
  },
  {
    key: "healthcare",
    label: "Healthcare",
    short: "Health",
    monogram: "H",
    accent: "ord",
    accentVar: "var(--cc-col-ord)",
  },
  {
    key: "public-sector",
    label: "Public Sector",
    short: "Public",
    monogram: "P",
    accent: "bil",
    accentVar: "var(--cc-col-bil)",
  },
  {
    key: "software-saas",
    label: "Software & SaaS",
    short: "Software",
    monogram: "S",
    accent: "usr",
    accentVar: "var(--cc-col-usr)",
  },
  {
    key: "telco-media",
    label: "Telco & Media",
    short: "Telco",
    monogram: "T",
    accent: "tel",
    accentVar: "oklch(0.74 0.14 200)",
  },
];

export const findIndustry = (key: string): Industry =>
  INDUSTRIES.find((i) => i.key === key) ?? INDUSTRIES[0];
