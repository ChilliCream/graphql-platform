"use client";

import { Card } from "@/src/design-system/Card";
import { SectionHeading } from "@/src/components/SectionHeading";

interface FaqSectionProps {
  readonly id: string;
  /** Outer section spacing utilities, e.g. "mt-24 sm:mt-28". */
  readonly className?: string;
  readonly eyebrow: string;
  readonly heading: string;
  readonly items: readonly {
    readonly question: string;
    readonly answer: string;
  }[];
}

/**
 * A FAQ as a single-column list of disclosure items. The whole header row
 * toggles the answer open, and clicking anywhere on the open answer collapses
 * it again. Shared across the marketing pages.
 */
export function FaqSection({
  id,
  className,
  eyebrow,
  heading,
  items,
}: FaqSectionProps) {
  return (
    <section aria-labelledby={`${id}-heading`} className={className} id={id}>
      <SectionHeading
        align="center"
        eyebrow={eyebrow}
        title={heading}
        titleId={`${id}-heading`}
      />

      <div className="mx-auto mt-10 flex max-w-3xl flex-col gap-4">
        {items.map((item) => (
          <FaqItem key={item.question} item={item} />
        ))}
      </div>
    </section>
  );
}

function FaqItem({
  item,
}: {
  readonly item: { readonly question: string; readonly answer: string };
}) {
  return (
    <Card className="bg-cc-card-bg/60" hoverBorder>
      <details className="group">
        <summary className="text-cc-heading font-heading flex cursor-pointer list-none items-start justify-between gap-4 p-5 text-base font-semibold">
          <span>{item.question}</span>
          <span
            aria-hidden="true"
            className="text-cc-accent mt-1 flex-none font-mono text-sm transition-transform group-open:rotate-45"
          >
            +
          </span>
        </summary>
        <div
          className="text-cc-ink cursor-pointer px-5 pb-5 text-sm leading-relaxed"
          onClick={(event) => {
            // Clicking the open answer collapses it, unless the user is selecting
            // text. The accessible keyboard toggle stays the <summary>.
            if (window.getSelection()?.toString()) {
              return;
            }
            event.currentTarget.closest("details")?.removeAttribute("open");
          }}
        >
          {item.answer}
        </div>
      </details>
    </Card>
  );
}
