import type { ReactNode } from "react";
import { Eyebrow } from "@/src/design-system/Eyebrow";

interface MarketingHeroProps {
  readonly eyebrow?: string;
  readonly title: ReactNode;
  /** Supporting lead paragraph under the title. */
  readonly lead?: ReactNode;
  /** Extra content between the lead and the actions (e.g. a card grid). */
  readonly children?: ReactNode;
  /** Call-to-action buttons, typically a `ButtonRow`. */
  readonly actions?: ReactNode;
  /** Fine-print caption shown under the actions. */
  readonly footnote?: ReactNode;
}

/**
 * The centered hero for the marketing pages (pricing, support, training,
 * advisory): a mono eyebrow, a heading-font title, a lead, and optional slots
 * for extra content, call-to-action buttons, and a caption. Distinct from the
 * larger `PageHero` used on the content pages.
 */
export function MarketingHero({
  eyebrow,
  title,
  lead,
  children,
  actions,
  footnote,
}: MarketingHeroProps) {
  return (
    <section className="pt-16 pb-12 text-center sm:pt-24 sm:pb-16">
      {eyebrow && <Eyebrow color="ink-dim">{eyebrow}</Eyebrow>}
      <h1 className="font-heading text-cc-heading sm:text-h2 mx-auto mt-5 max-w-3xl text-4xl leading-tight font-semibold tracking-tight text-balance">
        {title}
      </h1>
      {lead && (
        <p className="text-cc-ink-dim mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
          {lead}
        </p>
      )}
      {children}
      {actions && <div className="mt-9">{actions}</div>}
      {footnote && (
        <Eyebrow color="ink-dim" size="2xs" className="mt-5">
          {footnote}
        </Eyebrow>
      )}
    </section>
  );
}
