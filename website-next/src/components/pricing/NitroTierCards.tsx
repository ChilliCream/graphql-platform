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
    <ul className="cc-tier-spend-strip" aria-label="Spend controls">
      <li>Hard limits</li>
      <li>Budget alerts</li>
      <li>No surprise invoices</li>
    </ul>
  );
}

export function NitroTierCards() {
  return (
    <div className="cc-tiers">
      <div className="cc-tiers-inner">
        <div className="cc-tiers-heading">
          <div className="eyebrow">Nitro plans</div>
          <h2 className="display">Brew it your way.</h2>
          <p>
            Same engine, same APIs, same DX. Pick the operational shape that
            fits your team. Move between them without re-architecting.
          </p>
          <p className="cc-tiers-subnote">
            Looking for the OSS stack? It&apos;s free, MIT-licensed, and runs
            without an account — see the open-source belt above. Need
            procurement-ready support?{" "}
            <Link href="mailto:contact@chillicream.com?subject=Enterprise">
              Talk to sales about Enterprise →
            </Link>
          </p>
        </div>

        <div className="cc-tiers-grid">
          {TIERS.map((tier) => (
            <article
              key={tier.key}
              className={
                "cc-tier-card" + (tier.featured ? " is-featured" : "")
              }
            >
              {tier.badge && (
                <div className="cc-tier-badge">{tier.badge}</div>
              )}

              <div className="cc-tier-icon">
                <svg
                  viewBox="0 0 200 220"
                  width="100%"
                  height="100%"
                  aria-hidden
                >
                  {ICONS[tier.key]}
                </svg>
              </div>

              <div className="cc-tier-brewer">{tier.brewer}</div>
              <h3 className="cc-tier-name">{tier.name}</h3>
              <p className="cc-tier-tagline">{tier.tagline}</p>

              <div className="cc-tier-price">
                <span className="cc-tier-price-amount">{tier.price}</span>
                <span className="cc-tier-price-note">{tier.priceNote}</span>
              </div>

              <ul className="cc-tier-bullets">
                {tier.bullets.map((bullet) => (
                  <li key={bullet}>
                    <CheckIcon />
                    <span>{bullet}</span>
                  </li>
                ))}
              </ul>

              <Link
                href={tier.ctaHref}
                {...(tier.ctaHref.startsWith("http")
                  ? { target: "_blank", rel: "noopener noreferrer" }
                  : {})}
                className={
                  "cc-btn " +
                  (tier.featured ? "cc-btn-primary" : "cc-btn-ghost")
                }
              >
                {tier.cta} →
              </Link>

              {tier.key === "nitro-hosted" && <SpendControlsInline />}
            </article>
          ))}
        </div>
      </div>
    </div>
  );
}
