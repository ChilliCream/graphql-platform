import type { Metadata } from "next";
import { Fragment, type ReactNode } from "react";

import type { Cell, Tier, TierId } from "@/src/components/pricing/pricingData";
import {
  COMPARISON,
  FAQ,
  TIERS,
  UNLOCKS,
  UNLOCKS_NOTE,
} from "@/src/components/pricing/pricingData";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Pricing for Nitro by ChilliCream",
  description:
    "Nitro GraphQL pricing as a config file: start free on shared cloud, pay as you go at $20/mo, run a dedicated single-tenant instance from $400/mo with SSO, or self-host your own infra.",
  keywords: [
    "Nitro GraphQL pricing",
    "ChilliCream pricing",
    "GraphQL platform pricing",
    "GraphQL plans",
    "Hot Chocolate",
    "GraphQL observability pricing",
    "schema registry pricing",
  ],
  openGraph: {
    title: "Pricing for Nitro by ChilliCream",
    description:
      "Nitro GraphQL pricing: free shared cloud, pay as you go at $20/mo, dedicated single-tenant from $400/mo with SSO, or self-hosted on your own infra.",
  },
  robots: { index: false, follow: false },
};

// The three cloud tiers are presented as config panels; self-hosted sits below.
const CLOUD_TIERS = TIERS.filter((tier) => tier.id !== "self");
const SELF_HOSTED = TIERS.find((tier) => tier.id === "self");

// Config-file name shown in the terminal tab for each tier panel.
const TIER_FILENAME: Record<TierId, string> = {
  free: "free.toml",
  payg: "pay-as-you-go.toml",
  dedicated: "dedicated.toml",
  self: "self-hosted.toml",
};

// Terminal label + accent color for each tier in the hero `plans list` output.
const TIER_META: Record<
  TierId,
  { readonly label: string; readonly color: string }
> = {
  free: { label: "free", color: "#16b9e4" },
  payg: { label: "pay-as-you-go", color: "#7c92c6" },
  dedicated: { label: "dedicated", color: "#c792ea" },
  self: { label: "self-hosted", color: "#f0786a" },
};

function heroPrice(tier: Tier): string {
  return tier.priceNote === "per month" ? `${tier.price}/mo` : tier.price;
}

export default function PricingPreviewV4Page() {
  return (
    <div className="font-sans">
      <Hero />
      <PlanTriad />
      <WhyTeams />
      <CompareMatrix />
      <ProcurementBand />
      <UnlockBand />
      <FaqPanel />
      <ClosingCta />
    </div>
  );
}

// --- Terminal chrome --------------------------------------------------------

interface TerminalPanelProps {
  readonly filename: string;
  readonly children: ReactNode;
  readonly className?: string;
  readonly bodyClassName?: string;
  readonly accent?: boolean;
  readonly tabExtra?: ReactNode;
  readonly tabComment?: string;
}

function TerminalPanel({
  filename,
  children,
  className = "",
  bodyClassName = "",
  accent = false,
  tabExtra,
  tabComment,
}: TerminalPanelProps) {
  const wrapperClass = accent
    ? `rounded-2xl p-[1.5px] ${className}`
    : `border-cc-card-border overflow-hidden rounded-2xl border ${className}`;
  const wrapperStyle = accent
    ? { background: "var(--color-cc-accent)" }
    : undefined;
  const inner = (
    <div className="bg-cc-code-bg overflow-hidden rounded-[calc(1rem-1px)]">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-3 border-b px-4 py-2.5">
        <span className="flex gap-1.5" aria-hidden="true">
          <span className="block h-2.5 w-2.5 rounded-full bg-[#f0786a]" />
          <span className="block h-2.5 w-2.5 rounded-full bg-[#e4c46b]" />
          <span className="block h-2.5 w-2.5 rounded-full bg-[#5eead4]" />
        </span>
        <span className="text-cc-ink font-mono text-xs">{filename}</span>
        {tabComment ? (
          <span className="text-cc-ink-dim font-mono text-xs">
            {tabComment}
          </span>
        ) : null}
        {tabExtra ? (
          <span className="ml-auto flex items-center gap-2">{tabExtra}</span>
        ) : null}
      </div>
      <div className={`p-5 sm:p-6 ${bodyClassName}`}>{children}</div>
    </div>
  );
  return (
    <div className={wrapperClass} style={wrapperStyle}>
      {inner}
    </div>
  );
}

function Prompt() {
  return <span className="text-cc-accent select-none">$</span>;
}

function Comment({ children }: { readonly children: ReactNode }) {
  return <span className="text-cc-ink-dim">{children}</span>;
}

// --- Hero -------------------------------------------------------------------

function Hero() {
  return (
    <section className="mx-auto max-w-5xl pt-10 pb-12 sm:pt-16 sm:pb-16">
      <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
        # nitro pricing
      </p>
      <h1 className="font-heading text-cc-heading text-h2 sm:text-hero mt-5 leading-[1.05] font-semibold">
        Pricing, as a config file.
      </h1>
      <p className="text-cc-ink text-lead mt-6 max-w-2xl text-pretty">
        Start free on shared cloud. Move to pay as you go when you outgrow the
        free limits. Run a dedicated single-tenant instance for SSO and your own
        region, or self-host on your own infrastructure when the policy demands
        it.
      </p>
      <div className="mt-8 flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Sales
        </OutlineButton>
      </div>

      <div className="mt-12">
        <TerminalPanel filename="pricing.sh">
          <pre className="text-cc-ink overflow-x-auto font-mono text-sm leading-relaxed">
            <code>
              <span>
                <Prompt /> <span className="text-cc-ink">nitro plans list</span>
              </span>
              {"\n\n"}
              <Comment># 4 plans available</Comment>
              {"\n"}
              <span className="text-cc-ink">NAME</span>
              {"             "}
              <span className="text-cc-ink">PRICE</span>
              {"          "}
              <span className="text-cc-ink">TAGLINE</span>
              {"\n"}
              {TIERS.map((tier) => {
                const meta = TIER_META[tier.id];
                const price = heroPrice(tier);
                return (
                  <span key={tier.id}>
                    <span style={{ color: meta.color }}>{meta.label}</span>
                    {" ".repeat(Math.max(1, 17 - meta.label.length))}
                    <span className="text-cc-accent">{price}</span>
                    {" ".repeat(Math.max(1, 15 - price.length))}
                    <Comment>{tier.tagline}</Comment>
                    {"\n"}
                  </span>
                );
              })}
              {"\n"}
              <Comment>
                # use `nitro plans show &lt;name&gt;` for the full config.
              </Comment>
            </code>
          </pre>
        </TerminalPanel>
      </div>
    </section>
  );
}

// --- Plan triad -------------------------------------------------------------

function PlanTriad() {
  return (
    <section
      aria-labelledby="plans-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <div className="mb-10 max-w-2xl">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # plans
        </p>
        <h2
          id="plans-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          Three cloud configs. Pick the one that fits.
        </h2>
        <p className="text-cc-ink mt-4 text-base">
          Each plan ships as a small config you can read in under a minute. The
          values match what we provision when you sign up. Need your own
          infrastructure? The self-hosted config sits below.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3 lg:items-stretch">
        {CLOUD_TIERS.map((tier) => (
          <PlanConfigPanel key={tier.id} tier={tier} />
        ))}
      </div>

      {SELF_HOSTED ? <SelfHostedStrip tier={SELF_HOSTED} /> : null}
    </section>
  );
}

function PlanConfigPanel({ tier }: { readonly tier: Tier }) {
  return (
    <div className="flex flex-col">
      <TerminalPanel
        filename={TIER_FILENAME[tier.id]}
        accent={tier.popular}
        tabComment={tier.popular ? "# most-popular" : undefined}
      >
        <pre className="text-cc-ink font-mono text-sm leading-relaxed">
          <code>
            <span className="text-cc-ink-dim">[plan]</span>
            {"\n"}
            <span style={{ color: "#16b9e4" }}>name</span>
            {"     = "}
            <span className="text-cc-accent">{`"${tier.name}"`}</span>
            {"\n"}
            <span style={{ color: "#16b9e4" }}>id</span>
            {"       = "}
            <span className="text-cc-accent">{`"${tier.id}"`}</span>
            {"\n"}
            <span style={{ color: "#16b9e4" }}>tagline</span>
            {"  = "}
            <span className="text-cc-accent">{`"${tier.tagline}"`}</span>
            {"\n\n"}
            <span className="text-cc-ink-dim">[price]</span>
            {"\n"}
            <span style={{ color: "#7c92c6" }}>amount</span>
            {"   = "}
          </code>
          <span className="font-heading text-cc-heading text-h4 align-baseline font-semibold">
            {tier.price}
          </span>
          <code>
            {"\n"}
            <span style={{ color: "#7c92c6" }}>unit</span>
            {"     = "}
            <span className="text-cc-accent">{`"${tier.priceNote}"`}</span>
            {"\n\n"}
            <span className="text-cc-ink-dim">[features]</span>
            {"\n"}
            {tier.features.map((feature, i) => (
              <span key={feature}>
                <span style={{ color: "#f0786a" }}>{`f${i + 1}`}</span>
                {"       = "}
                <span className="text-cc-accent">{`"${feature}"`}</span>
                {"\n"}
              </span>
            ))}
          </code>
        </pre>
      </TerminalPanel>
      {tier.popular ? (
        <SolidButton href={tier.ctaHref} className="mt-5 w-full">
          {tier.cta}
        </SolidButton>
      ) : (
        <OutlineButton href={tier.ctaHref} className="mt-5 w-full">
          {tier.cta}
        </OutlineButton>
      )}
    </div>
  );
}

function SelfHostedStrip({ tier }: { readonly tier: Tier }) {
  return (
    <div className="mt-6">
      <TerminalPanel filename={TIER_FILENAME[tier.id]}>
        <div className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-cc-nav-label text-caption font-mono">
              {"// self-hosted"}
            </p>
            <h3 className="font-heading text-cc-heading text-h5 mt-2 font-semibold">
              {tier.name}
            </h3>
            <p className="text-cc-ink mt-2 max-w-2xl text-sm text-pretty">
              {tier.tagline} Run on your own infrastructure, air-gapped or
              on-prem, with configurable retention, priority engineering
              support, and a long-term release channel.
            </p>
          </div>
          <OutlineButton href={tier.ctaHref} className="shrink-0 sm:w-auto">
            {tier.cta}
          </OutlineButton>
        </div>
      </TerminalPanel>
    </div>
  );
}

// --- Why teams pick Nitro ---------------------------------------------------

interface Blurb {
  readonly tag: string;
  readonly title: string;
  readonly body: string;
}

const BLURBS: readonly Blurb[] = [
  {
    tag: "// hosting",
    title: "Same Nitro, different runway.",
    body: "Free, pay as you go, dedicated, or your own infra. The control plane, registry, and telemetry are the same. What changes is where it runs and who you share it with.",
  },
  {
    tag: "// lifecycle",
    title: "Schema changes you can read.",
    body: "CI checks classify every change as safe, dangerous, or breaking, and report the published clients affected before you deploy. Rollback is republishing an earlier tag.",
  },
  {
    tag: "// observability",
    title: "OpenTelemetry, native.",
    body: "Traces, metrics, and logs flow through standard OTLP once Nitro is configured. Distributed tracing across Fusion subgraphs and any .NET service comes with it.",
  },
];

function WhyTeams() {
  return (
    <section
      aria-labelledby="why-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <div className="mb-10 max-w-2xl">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # why teams pick nitro
        </p>
        <h2
          id="why-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          What the configs share.
        </h2>
      </div>

      <div className="grid gap-10 md:grid-cols-3">
        {BLURBS.map((blurb) => (
          <div key={blurb.title}>
            <p className="text-cc-nav-label text-caption font-mono">
              {blurb.tag}
            </p>
            <h3 className="font-heading text-cc-heading text-h5 mt-3 font-semibold">
              {blurb.title}
            </h3>
            <p className="text-cc-ink mt-3 text-base">{blurb.body}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

// --- Compare plans (terminal diff) ------------------------------------------

function CompareMatrix() {
  return (
    <section
      aria-labelledby="compare-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <div className="mb-10 max-w-2xl">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # compare plans
        </p>
        <h2
          id="compare-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          Every capability, side by side.
        </h2>
        <p className="text-cc-ink mt-4 text-base">
          A single diff across all four tiers. Same Nitro platform, different
          deployment, usage, and support shape.
        </p>
      </div>

      <TerminalPanel filename="plans-diff.txt">
        <p className="text-cc-ink-dim mb-4 font-mono text-xs">
          # nitro plans diff --all
        </p>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] border-collapse text-left font-mono text-[0.78rem] leading-relaxed sm:text-xs">
            <thead>
              <tr>
                <th
                  scope="col"
                  className="text-cc-heading py-2 pr-4 font-normal"
                >
                  capability
                </th>
                {TIERS.map((tier) => (
                  <th
                    key={tier.id}
                    scope="col"
                    className="text-cc-heading px-3 py-2 align-bottom font-normal"
                  >
                    {tier.name.toLowerCase()}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {COMPARISON.map((group) => (
                <Fragment key={group.title}>
                  <tr>
                    <th
                      scope="colgroup"
                      colSpan={TIERS.length + 1}
                      className="text-cc-nav-label pt-6 pb-2 font-normal"
                    >
                      {`## ${group.title}`}
                    </th>
                  </tr>
                  {group.rows.map((row) => (
                    <tr
                      key={row.label}
                      className="border-cc-ink-faint border-t border-dashed"
                    >
                      <th
                        scope="row"
                        className="text-cc-ink max-w-[22rem] py-2 pr-4 align-top font-normal"
                      >
                        {row.label}
                      </th>
                      {TIERS.map((tier) => (
                        <CompareCell key={tier.id} value={row[tier.id]} />
                      ))}
                    </tr>
                  ))}
                </Fragment>
              ))}
            </tbody>
          </table>
        </div>
      </TerminalPanel>
    </section>
  );
}

function CompareCell({ value }: { readonly value: Cell }) {
  if (typeof value === "boolean") {
    return (
      <td
        className={`px-3 py-2 align-top ${value ? "text-cc-accent" : "text-cc-ink-faint"}`}
      >
        {value ? "[x]" : "[ ]"}
      </td>
    );
  }
  return <td className="text-cc-ink-dim px-3 py-2 align-top">{value}</td>;
}

// --- Procurement band -------------------------------------------------------

function ProcurementBand() {
  return (
    <section
      aria-labelledby="procurement-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <TerminalPanel
        filename="procurement.md"
        tabExtra={
          <>
            <SolidButton
              href="/services/support/contact"
              className="!px-4 !py-1.5 text-xs"
            >
              Contact Sales
            </SolidButton>
            <OutlineButton href="/platform" className="!px-4 !py-1.5 text-xs">
              Explore the platform
            </OutlineButton>
          </>
        }
      >
        <div className="text-cc-ink font-mono text-sm leading-relaxed">
          <p className="text-cc-nav-label">{"### procurement"}</p>
          <h2
            id="procurement-heading"
            className="font-heading text-cc-heading text-h4 mt-3 font-semibold"
          >
            Custom volume, procurement, or air-gapped?
          </h2>
          <p className="text-cc-ink mt-4 text-base">
            We work directly with platform and security teams on bespoke
            commercial terms, on-prem rollouts, and migrations from existing
            GraphQL gateways. Engineers, not gatekeepers, run the call.
          </p>
          <ul className="mt-6 grid gap-3 sm:grid-cols-2">
            <ChecklistItem>Dedicated solution architect</ChecklistItem>
            <ChecklistItem>Annual contracts and POs</ChecklistItem>
            <ChecklistItem>Security and DPA review</ChecklistItem>
            <ChecklistItem>Migration playbooks</ChecklistItem>
            <ChecklistItem>BYOC and on-prem rollouts</ChecklistItem>
          </ul>
        </div>
      </TerminalPanel>
    </section>
  );
}

function ChecklistItem({ children }: { readonly children: ReactNode }) {
  return (
    <li className="text-cc-ink flex items-start gap-3 text-sm">
      <span className="text-cc-accent mt-[5px] flex-none font-mono">-</span>
      <span className="font-sans">{children}</span>
    </li>
  );
}

// --- Unlock more as you grow ------------------------------------------------

function UnlockBand() {
  return (
    <section
      aria-labelledby="unlock-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # growth
        </p>
        <h2
          id="unlock-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          Unlock more as you grow
        </h2>
        <p className="text-cc-ink mt-4 text-base text-pretty">
          Commit to a minimum monthly spend to unlock more, up to your spend.
        </p>
      </div>

      <TerminalPanel filename="growth.toml">
        <p className="text-cc-ink-dim mb-2 font-mono text-xs">
          # nitro plans unlocks
        </p>
        <ul className="flex flex-col">
          {UNLOCKS.map((unlock, i) => {
            const Glyph = UNLOCK_ICONS[i] ?? SupportGlyph;
            return (
              <li
                key={unlock.title}
                className={`flex items-center gap-4 py-4 sm:gap-5 ${
                  i > 0 ? "border-cc-ink-faint border-t border-dashed" : ""
                }`}
              >
                <span className="border-cc-card-border bg-cc-code-header text-cc-accent flex size-10 shrink-0 items-center justify-center rounded-lg border">
                  <Glyph className="size-5" />
                </span>
                <div className="min-w-0 flex-1">
                  <h3 className="font-heading text-cc-heading text-base font-semibold">
                    {unlock.title}
                  </h3>
                  <p className="text-cc-ink-dim mt-1 font-mono text-xs text-pretty">
                    {unlock.description}
                  </p>
                </div>
                <span className="text-cc-accent shrink-0 font-mono text-sm font-semibold sm:text-base">
                  {unlock.spend}
                </span>
              </li>
            );
          })}
        </ul>
      </TerminalPanel>
      <p className="text-cc-nav-label mt-4 font-mono text-[0.72rem]">
        {UNLOCKS_NOTE}
      </p>
    </section>
  );
}

interface GlyphProps {
  readonly className?: string;
}

/** Lifebuoy, for the Business Support unlock. */
function SupportGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      aria-hidden="true"
      className={className}
    >
      <circle cx="12" cy="12" r="9" />
      <circle cx="12" cy="12" r="3.4" />
      <path d="M5.2 5.2l4.4 4.4M18.8 5.2l-4.4 4.4M5.2 18.8l4.4-4.4M18.8 18.8l-4.4-4.4" />
    </svg>
  );
}

/** Shield with a check, for the priority support unlock. */
function ShieldGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M12 3l7 3v5c0 4-3 6.6-7 8-4-1.4-7-4-7-8V6z" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  );
}

/** Cloud, for the BYOC unlock. */
function CloudGlyph({ className }: GlyphProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={className}
    >
      <path d="M7 18h10a4 4 0 0 0 .5-7.97A5.5 5.5 0 0 0 6.5 9 4 4 0 0 0 7 18z" />
    </svg>
  );
}

const UNLOCK_ICONS = [SupportGlyph, ShieldGlyph, CloudGlyph];

// --- FAQ --------------------------------------------------------------------

function FaqPanel() {
  return (
    <section
      aria-labelledby="faq-heading"
      className="mx-auto max-w-5xl pb-16 sm:pb-24"
    >
      <div className="mb-10 max-w-2xl">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # faq
        </p>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-h3 mt-3 font-semibold"
        >
          Honest answers about pricing.
        </h2>
      </div>

      <TerminalPanel filename="faq.md">
        <dl>
          {FAQ.map((item, i) => (
            <div key={item.question}>
              {i > 0 ? (
                <div
                  aria-hidden="true"
                  className="border-cc-ink-faint my-6 border-t border-dashed"
                />
              ) : null}
              <dt className="text-cc-heading text-h6 font-mono">
                <span className="text-cc-accent">##</span> {item.question}
              </dt>
              <dd className="text-cc-ink mt-3 font-sans text-base leading-relaxed">
                {item.answer}
              </dd>
            </div>
          ))}
        </dl>
      </TerminalPanel>
    </section>
  );
}

// --- Closing CTA ------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="mx-auto max-w-5xl pb-16 sm:pb-24">
      <div className="mb-8 max-w-2xl">
        <p className="text-cc-nav-label text-caption font-mono tracking-[0.18em] uppercase">
          # quickstart
        </p>
        <h2 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
          Ship your GraphQL platform with Nitro.
        </h2>
        <p className="text-cc-ink mt-4 text-base">
          Start on the free tier in minutes. Move to pay as you go when you
          outgrow the limits, or a dedicated region when you need SSO and
          isolation. The docs walk you through every step.
        </p>
      </div>

      <TerminalPanel filename="quickstart.sh">
        <pre className="text-cc-ink font-mono text-sm leading-relaxed">
          <code>
            <Comment># pick a plan, then run:</Comment>
            {"\n"}
            <Prompt />{" "}
            <span className="text-cc-ink">nitro start --plan free</span>
            <span
              aria-hidden="true"
              className="bg-cc-accent ml-1 inline-block h-[1.1em] w-[0.55em] translate-y-[2px] animate-pulse"
            />
          </code>
        </pre>
      </TerminalPanel>

      <div className="mt-8 flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the docs</OutlineButton>
      </div>
    </section>
  );
}
