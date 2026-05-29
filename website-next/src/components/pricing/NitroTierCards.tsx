import Link from "next/link";

import { TIERS, type TierKey } from "@/src/data/pricing/tiers";
import { CheckIcon } from "./CheckIcon";

// Brewer icons: line-stroke vocabulary so the pricing tiers read as siblings
// of the landing brew section (French Press / Drip Brewer / Pour Over).
const ICONS: Record<TierKey, React.ReactNode> = {
  "nitro-free": (
    <g
      stroke="var(--cc-ink)"
      strokeWidth={1.6}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <line x1="100" y1="20" x2="100" y2="34" />
      <circle cx="100" cy="20" r="4" />
      <rect x="50" y="34" width="100" height="14" rx="3" />
      <path d="M 56 48 L 56 192 L 144 192 L 144 48" />
      <path d="M 144 80 Q 168 80 168 110 Q 168 140 144 140" />
      <line x1="100" y1="48" x2="100" y2="120" />
      <line x1="68" y1="120" x2="132" y2="120" />
      <line x1="60" y1="160" x2="140" y2="160" opacity="0.4" />
    </g>
  ),
  "nitro-hosted": (
    <g
      stroke="var(--cc-ink)"
      strokeWidth={1.6}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M 36 28 L 164 28 L 164 70 L 132 70 L 132 90 L 68 90 L 68 70 L 36 70 Z" />
      <path d="M 76 90 L 124 90 L 110 132 L 90 132 Z" />
      <path d="M 70 138 L 130 138 L 138 198 L 62 198 Z" />
      <path d="M 130 150 Q 154 150 154 174 Q 154 196 138 196" />
      <line x1="68" y1="172" x2="132" y2="172" opacity="0.4" />
      <line x1="40" y1="200" x2="160" y2="200" />
    </g>
  ),
  "nitro-self-hosted": (
    <g
      stroke="var(--cc-ink)"
      strokeWidth={1.6}
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M 56 28 L 144 28 L 116 100 L 84 100 Z" />
      <line x1="76" y1="108" x2="124" y2="108" />
      <line x1="76" y1="118" x2="124" y2="118" />
      <path d="M 84 118 L 60 196 Q 60 204 70 204 L 130 204 Q 140 204 140 196 L 116 118" />
      <line x1="66" y1="178" x2="134" y2="178" opacity="0.4" />
      <path d="M 56 28 Q 64 22 72 28" />
    </g>
  ),
};

// Inline spend-controls strip co-located with the anxiety it calms: lives
// directly under the Hosted card's CTA.
function SpendControlsInline() {
  return (
    <ul className="mt-4 flex flex-wrap justify-center gap-x-3.5 gap-y-1 font-mono text-[10px] uppercase tracking-[0.12em] text-[var(--cc-ink-dim)]">
      {["Hard limits", "Budget alerts", "No surprise invoices"].map((item) => (
        <li key={item} className="relative pl-3.5">
          <span
            aria-hidden
            className="absolute left-0 top-1/2 size-1.5 -translate-y-1/2 rounded-full bg-fuchsia-400/70"
          />
          {item}
        </li>
      ))}
    </ul>
  );
}

export function NitroTierCards() {
  return (
    <section className="py-16">
      <div className="mx-auto mb-14 max-w-2xl text-center">
        <div className="mb-2 font-mono text-xs font-semibold uppercase tracking-widest text-[var(--cc-ink-dim)]">
          Nitro plans
        </div>
        <h2 className="text-3xl font-semibold tracking-tight text-[var(--cc-ink)] sm:text-4xl">
          Brew it your way.
        </h2>
        <p className="mx-auto mt-4 max-w-xl text-base text-[var(--cc-ink-dim)]">
          Same engine, same APIs, same DX. Pick the operational shape that fits
          your team. Move between them without re-architecting.
        </p>
        <p className="mx-auto mt-4 max-w-xl text-sm text-[var(--cc-ink-dim)]">
          Looking for the OSS stack? It&apos;s free, MIT-licensed, and runs
          without an account — see the open-source belt above. Need
          procurement-ready support?{" "}
          <Link
            href="mailto:contact@chillicream.com?subject=Enterprise"
            className="border-b border-fuchsia-400/40 text-fuchsia-400 no-underline transition-colors hover:border-fuchsia-400"
          >
            Talk to sales about Enterprise →
          </Link>
        </p>
      </div>

      <div className="mx-auto grid max-w-md items-stretch gap-6 lg:max-w-none lg:grid-cols-3">
        {TIERS.map((tier) => (
          <article
            key={tier.key}
            className={
              "relative flex flex-col rounded-2xl border p-8 backdrop-blur-sm transition-[transform,border-color] duration-200 hover:-translate-y-0.5 " +
              (tier.featured
                ? "border-fuchsia-400/40 bg-white/[0.06] shadow-[0_30px_80px_-40px_rgba(0,0,0,0.6),0_0_80px_-20px_rgba(217,70,239,0.25)]"
                : "border-[var(--cc-card-border)] bg-[var(--cc-card-bg)] hover:border-[var(--cc-card-border-hover)]")
            }
          >
            {tier.badge && (
              <div className="absolute -top-3 left-1/2 -translate-x-1/2 whitespace-nowrap rounded-full bg-fuchsia-500 px-3 py-1.5 font-mono text-[10px] font-semibold uppercase leading-none tracking-[0.18em] text-[#0c1322]">
                {tier.badge}
              </div>
            )}

            <div className="mx-auto mb-5 h-[122px] w-[110px]">
              <svg viewBox="0 0 200 220" width="100%" height="100%" aria-hidden>
                {ICONS[tier.key]}
              </svg>
            </div>

            <div className="text-center font-mono text-[10px] uppercase tracking-[0.18em] text-[var(--cc-ink-dim)]">
              {tier.brewer}
            </div>
            <h3 className="mt-1.5 text-center text-2xl font-medium tracking-tight text-[var(--cc-ink)]">
              {tier.name}
            </h3>
            <p className="mx-auto mt-1.5 max-w-[28ch] text-center text-sm leading-relaxed text-[var(--cc-ink-dim)]">
              {tier.tagline}
            </p>

            <div className="mb-6 mt-5 border-b border-[var(--cc-card-border)] pb-6 text-center">
              <span className="block text-3xl font-medium tracking-tight text-[var(--cc-ink)]">
                {tier.price}
              </span>
              <span className="mt-1 block font-mono text-[11px] uppercase tracking-[0.08em] text-[var(--cc-ink-dim)]">
                {tier.priceNote}
              </span>
            </div>

            <ul className="mb-7 flex flex-1 flex-col gap-2.5 text-sm text-[var(--cc-ink)]">
              {tier.bullets.map((bullet) => (
                <li key={bullet} className="flex items-start gap-2.5">
                  <span aria-hidden className="mt-1 shrink-0 text-fuchsia-400">
                    <CheckIcon />
                  </span>
                  <span className="leading-relaxed">{bullet}</span>
                </li>
              ))}
            </ul>

            <Link
              href={tier.ctaHref}
              {...(tier.ctaHref.startsWith("http")
                ? { target: "_blank", rel: "noopener noreferrer" }
                : {})}
              className={
                "inline-flex w-full items-center justify-center rounded-full px-6 py-3 text-sm font-medium no-underline transition-colors " +
                (tier.featured
                  ? "bg-[var(--cc-ink)] text-[#0c1322] hover:bg-white"
                  : "border border-[var(--cc-card-border)] text-[var(--cc-ink)] hover:border-[var(--cc-card-border-hover)]")
              }
            >
              {tier.cta} →
            </Link>

            {tier.key === "nitro-hosted" && <SpendControlsInline />}
          </article>
        ))}
      </div>
    </section>
  );
}
