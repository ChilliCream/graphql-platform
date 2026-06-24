import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Preview Hub",
  description: "Master index of every prototype variation for the site rework.",
  robots: { index: false, follow: false },
};

interface PreviewEntry {
  readonly href: string;
  readonly label: string;
  readonly note: string;
  readonly count: number;
}

interface PreviewGroup {
  readonly title: string;
  readonly entries: readonly PreviewEntry[];
}

const GROUPS: readonly PreviewGroup[] = [
  {
    title: "Landing",
    entries: [
      {
        href: "/landing/preview",
        label: "Landing page",
        note: "Product-Reel-First · .NET-Native Platform · Outcome-Led Story · The Dispatch · Ledger of Six (existing app/page.tsx untouched) · House Blend · Platform Pulse · Constellation Grid · Concentric Platform",
        count: 9,
      },
    ],
  },
  {
    title: "Platform",
    entries: [
      {
        href: "/platform/preview-hub",
        label: "Platform hub",
        note: "Capability Map · Developer Loop · Product-Led Hub · Annotated Source · The Field Guide · House Menu · Live Circuit · The Spectrum Hinge · Hatched Atlas",
        count: 9,
      },
      {
        href: "/platform/preview",
        label: "Platform scene pages",
        note: "5 sub-pages × 9 variations: Build · Agentic Coding · Observability · Workflows · Release Safety",
        count: 45,
      },
      {
        href: "/platform/analytics/preview",
        label: "Platform / Analytics",
        note: "Dashboard-First · Observability Story · Metric Catalogue · The Five-Step Trail · The Constellation · The Tasting Room · Waterfall in Motion · Switchback Telemetry · The Signal Column",
        count: 9,
      },
      {
        href: "/platform/continuous-integration/preview",
        label: "Platform / Continuous Integration",
        note: "Pipeline Story · Safety Net · CLI-First Engineering · The Registry Dispatch · Field Manual · Bean to Cup · Pipeline Pulse · Registry Wall · Registry Codex",
        count: 9,
      },
      {
        href: "/platform/ecosystem/preview",
        label: "Platform / Ecosystem",
        note: "IDE Showcase · Connected Workspace · Feature Library · Operator's Notebook · The Nitro Dispatch · Nitro Bar · Nitro Mesh in Motion · Departures Board · Thermal Receipt: ORDER #NITRO-001",
        count: 9,
      },
      {
        href: "/platform/scene-illustrations",
        label: "Scene illustrations gallery",
        note: "25 act-2 scroll-scene illustration variants (5 per scene)",
        count: 25,
      },
    ],
  },
  {
    title: "Products",
    entries: [
      {
        href: "/products/nitro/preview",
        label: "Nitro",
        note: "Railway Classic · Immersive Story · Developer Deep-Dive · The Verdict Ledger · Operator's Console (vendored animation system) · Cold Brew Control Bar · Live Cockpit · Blueprint Cockpit · nitro.manifest",
        count: 9,
      },
      {
        href: "/products/hotchocolate/preview",
        label: "Hot Chocolate",
        note: "Code-First Catalogue · Story-Led Build Loop · Why-.NET Comparison · Reference Manual · The Honest Diff · House Blend · Compiler Pulse · The Hot Chocolate Funnies · The Field Journal",
        count: 9,
      },
      {
        href: "/products/strawberryshake/preview",
        label: "Strawberry Shake",
        note: "Code-First Catalogue · End-to-End Story · For-.NET-UI Positioning · The Reference Manual · The Reference Card · The House Pour · Build-Time Forge · Field Postcards from the Build · Tag Atlas",
        count: 9,
      },
      {
        href: "/products/fusion/preview",
        label: "Fusion",
        note: "Composition-First Catalogue · Query-Plan Story · .NET-Native-Gateway Positioning · The Long Read · Reference Manual · House Blend Gateway · Plan in Motion · Ridge Walk · Composition Calendar",
        count: 9,
      },
      {
        href: "/products/mocha/preview",
        label: "Mocha",
        note: "Mediator-AND-Bus Catalogue · Message-Topology Story · Open-Modern Positioning · The Constellation · The Dispatch Essay · Order Up · Message in Flight · Signal on the Wire · Tilt: a pinball table for messages",
        count: 9,
      },
      {
        href: "/products/greendonut/preview",
        label: "Green Donut",
        note: "N+1 Killer Catalogue · Before/After Walkthrough · Inside-Hot-Chocolate Positioning · Six Ticks To Zero · The Coalescer · Behind the Bar · Tick Collapse · Dewey Decimal DataLoader · The Standup",
        count: 9,
      },
      {
        href: "/products/cookiecrumble/preview",
        label: "Cookie Crumble",
        note: "Test-Author Catalogue · Three-Snapshot-Styles Story · Built-for-Hot-Chocolate Positioning · The Snapshot Dispatch · The Six-Step Crumb Trail · Tasting Notes · Mismatch Reel · Crumb Cards · Heartbeat of the Suite",
        count: 9,
      },
    ],
  },
  {
    title: "Pricing",
    entries: [
      {
        href: "/pricing/preview",
        label: "Pricing",
        note: "Classic Three-Tier · Calculator-First · Story-Led Outcomes · The Pricing CLI · The Pricing Dispatch (with deep extended comparison) · The Nitro Menu · Tier Reveal · Counters at the Switch · Tier Cascade",
        count: 9,
      },
    ],
  },
  {
    title: "Services",
    entries: [
      {
        href: "/services/preview",
        label: "Services hub",
        note: "Tiered Routing · Engagement Journey · Embedded Specialists · The Split Ledger · The Service Manifest · House Blend · Routing Switchboard · Buoyant Routing · Magnetic Routing",
        count: 9,
      },
      {
        href: "/services/advisory/preview",
        label: "Services / Advisory",
        note: "Engagement Tiers · Outcome Patterns · Book a Conversation · Reference Sheet · The Ledger · Behind The Bar · First Hour, Live · Single Stroke Advisory · Dictated, Not Drafted",
        count: 9,
      },
      {
        href: "/services/support/preview",
        label: "Services / Support",
        note: "Clarity-First Tiers · SLA-Promise Forward · Engagement Funnel · The Handbook · SUPPORT.REGISTRY · Service Bar · Response Clock · Numbered Ascent · Ruled Ledger",
        count: 9,
      },
      {
        href: "/services/training/preview",
        label: "Services / Training",
        note: "Curriculum Catalog · Team Outcomes · Mixed-Level Workshop · The Long Read · The Course Reference · House Blend · Curriculum Constellation · The Kilometre Rail · Reel Six: The Training Filmstrip",
        count: 9,
      },
    ],
  },
  {
    title: "Help & Company",
    entries: [
      {
        href: "/help/preview",
        label: "Help",
        note: "Tiered Resource Hub · Self-Serve First · Decision-Tree · The Compass · The Long Read · The Help Bar · Routing Tree · The Triage Schematic · Help Console",
        count: 9,
      },
      {
        href: "/about/preview",
        label: "About",
        note: "Mission + Products · Open-Source-First · Customer Outcomes Narrative · Six Steps to a Platform · Constellation Diagram · House Blend · Constellation · The Platform Deck · Specimen Cabinet",
        count: 9,
      },
      {
        href: "/resources/preview",
        label: "Resources",
        note: "Builder Library · Brand & Company · Directory Index · The Builder Almanac · Field Manual · The Menu Board · Living Index · Resource Ticker · Card Catalog",
        count: 9,
      },
    ],
  },
];

const TOTAL_VARIATIONS = GROUPS.reduce(
  (sum, g) => sum + g.entries.reduce((s, e) => s + e.count, 0),
  0,
);

export default function PreviewHubPage() {
  return (
    <section className="mx-auto max-w-5xl px-5 py-16 sm:px-12 sm:py-24">
      <header className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Internal preview index
        </p>
        <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 leading-[1.1] font-semibold text-balance">
          Every prototype in one place.
        </h1>
        <p className="text-cc-ink mt-5 text-base sm:text-lg">
          {TOTAL_VARIATIONS} variations across{" "}
          {GROUPS.reduce((n, g) => n + g.entries.length, 0)} surfaces. All
          routes are noindex; existing live pages are untouched.
        </p>
      </header>

      <div className="mt-16 flex flex-col gap-14">
        {GROUPS.map((group) => (
          <section key={group.title} aria-labelledby={`group-${group.title}`}>
            <h2
              id={`group-${group.title}`}
              className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase"
            >
              {group.title}
            </h2>
            <div className="mt-5 grid gap-3">
              {group.entries.map((entry) => (
                <Link
                  key={entry.href}
                  href={entry.href}
                  className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover group flex items-center gap-5 rounded-xl border p-5 no-underline backdrop-blur-sm transition-colors"
                >
                  <div className="border-cc-card-border bg-cc-surface text-cc-accent flex h-10 w-12 shrink-0 items-center justify-center rounded-md border font-mono text-sm tabular-nums">
                    {entry.count}
                  </div>
                  <div className="min-w-0 flex-1">
                    <div className="text-cc-heading group-hover:text-cc-accent flex items-baseline gap-3 font-semibold transition-colors">
                      <span>{entry.label}</span>
                      <span className="text-cc-ink-dim font-mono text-[11px] tracking-wide">
                        {entry.href}
                      </span>
                    </div>
                    <p className="text-cc-ink-dim mt-1.5 text-sm">
                      {entry.note}
                    </p>
                  </div>
                  <span
                    aria-hidden="true"
                    className="text-cc-ink-faint group-hover:text-cc-accent shrink-0 transition-colors"
                  >
                    &rarr;
                  </span>
                </Link>
              ))}
            </div>
          </section>
        ))}
      </div>

      <p className="text-cc-ink-dim mt-16 text-center text-xs">
        This page is unlisted. Bookmark{" "}
        <span className="font-mono">/preview</span> to keep returning here.
      </p>
    </section>
  );
}
