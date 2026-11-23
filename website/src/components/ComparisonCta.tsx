import { DotGridSurface } from "@/src/components/DotGridSurface";
import { CONTACT_HREF } from "@/src/components/header/navData";
import { NextStepsSection } from "@/src/components/NextStepsSection";
import { Link } from "@/src/design-system/Link";
import type { ComparisonCallToAction } from "@/src/helpers/comparisonCollection";

interface ComparisonCtaProps {
  readonly cta: ComparisonCallToAction;
  /** Outer spacing utilities, e.g. "mt-16". */
  readonly className?: string;
}

/**
 * Closing call to action of a comparison article: two buttons and a link to
 * talk to the ChilliCream team.
 */
export function ComparisonCta({ cta, className }: ComparisonCtaProps) {
  return (
    <DotGridSurface className={`rounded-3xl ${className ?? ""}`.trim()}>
      <NextStepsSection
        skin="accent"
        // The accent panel carries its own padding.
        className="py-0"
        title={cta.title}
        text={
          <>
            {cta.text} <Link href={CONTACT_HREF}>{cta.contact}</Link>.
          </>
        }
        primaryLink={cta.primary.href}
        primaryLinkText={cta.primary.label}
        secondaryLink={cta.secondary.href}
        secondaryLinkText={cta.secondary.label}
      />
    </DotGridSurface>
  );
}
