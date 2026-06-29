import type { Unlock } from "@/src/components/pricing/pricingData";
import { UNLOCKS } from "@/src/components/pricing/pricingData";

/**
 * "Unlock more as you grow": a vertical list of spend thresholds, each unlocking
 * more support or deployment options. Modeled on Railway's pricing progression.
 */
export function UnlockBand() {
  return (
    <section
      aria-labelledby="unlock-heading"
      className="mt-24 scroll-mt-24 sm:mt-28"
      id="unlock"
    >
      <div className="mx-auto max-w-2xl text-center">
        <h2
          id="unlock-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold"
        >
          Unlock more as you grow
        </h2>
        <p className="text-cc-ink mt-4 text-base text-pretty">
          Commit to a minimum monthly spend to unlock more support and
          deployment options, up to your spend.
        </p>
      </div>

      <ul className="mx-auto mt-10 flex max-w-3xl flex-col gap-3">
        {UNLOCKS.map((unlock, index) => (
          <UnlockRow key={unlock.title} unlock={unlock} index={index} />
        ))}
      </ul>
    </section>
  );
}

function UnlockRow({
  unlock,
  index,
}: {
  readonly unlock: Unlock;
  readonly index: number;
}) {
  const Glyph = UNLOCK_ICONS[index] ?? SupportGlyph;
  return (
    <li className="border-cc-card-border bg-cc-card-bg flex items-center gap-4 rounded-2xl border p-5 sm:gap-5 sm:p-6">
      <span className="border-cc-card-border bg-cc-surface text-cc-accent flex size-11 shrink-0 items-center justify-center rounded-xl border">
        <Glyph className="size-5" />
      </span>
      <div className="min-w-0 flex-1">
        <h3 className="font-heading text-cc-heading text-base font-semibold">
          {unlock.title}
        </h3>
        <p className="text-cc-ink-dim mt-1 text-sm text-pretty">
          {unlock.description}
        </p>
      </div>
      <span className="font-heading text-cc-heading shrink-0 text-lg font-semibold sm:text-xl">
        {unlock.spend}
      </span>
    </li>
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

/** Shield with a check, for the Enterprise Support unlock. */
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
