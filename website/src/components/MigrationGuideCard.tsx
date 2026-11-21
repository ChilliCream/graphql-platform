import { Card } from "@/src/design-system/Card";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";

interface MigrationGuideCardProps {
  /** Root-relative path of the guide, e.g. `/docs/mocha/migration/coming-from-mediatr`. */
  readonly href: string;
  /** Name of the product the guide migrates from, e.g. "MediatR". */
  readonly from: string;
}

/**
 * A card in a comparison article that links to the migration guide for the
 * product being compared.
 */
export function MigrationGuideCard({ href, from }: MigrationGuideCardProps) {
  return (
    <Card
      as="a"
      href={href}
      variant="tile"
      className="group hover:border-cc-accent my-8 flex items-center gap-5 no-underline transition-colors"
    >
      <span className="flex min-w-0 flex-1 flex-col gap-1">
        <span className="font-heading text-cc-heading text-lg font-semibold">
          Migrate from {from}
        </span>{" "}
        <span className="text-cc-ink-dim text-sm">
          The guide walks you through the move step by step, with a migration
          prompt to copy into your coding agent.
        </span>
      </span>
      <ArrowRightIcon className="text-cc-accent size-5 shrink-0 transition-transform group-hover:translate-x-0.5" />
    </Card>
  );
}
