import Link from "next/link";
import { ButtonRow } from "@/src/components/ButtonRow";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import {
  COMPARISONS_PATH,
  formatCheckedDate,
  type ComparisonSummary,
} from "@/src/helpers/comparisons";
import { ArrowLeftIcon } from "@/src/icons/ArrowLeft";

interface ComparisonHeaderProps {
  readonly comparison: ComparisonSummary;
}

/**
 * Top of a comparison page: the title, the summary, the versions and check
 * date the comparison is based on, and the get started and migration links.
 */
export function ComparisonHeader({ comparison }: ComparisonHeaderProps) {
  const { product, competitor, versions } = comparison;

  return (
    <header>
      <Link
        href={COMPARISONS_PATH}
        prefetch={false}
        data-llms-ignore
        className="text-cc-ink-dim hover:text-cc-heading inline-flex items-center gap-1.5 text-sm no-underline transition-colors print:hidden"
      >
        <ArrowLeftIcon className="size-3.5" />
        All comparisons
      </Link>
      <Eyebrow color="ink-dim" className="mt-10">
        Compare
      </Eyebrow>
      <h1 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-4 tracking-tight text-balance">
        {comparison.title}
      </h1>
      <p className="text-cc-prose mt-6 max-w-3xl text-lg leading-relaxed">
        {comparison.description}
      </p>
      <p className="text-cc-ink-dim mt-4 text-sm">
        Based on {product.title} {versions.product} and {competitor}{" "}
        {versions.competitor}. Facts about {competitor} checked on{" "}
        <time dateTime={comparison.lastChecked}>
          {formatCheckedDate(comparison.lastChecked)}
        </time>
        .
      </p>
      <ButtonRow align="start" className="mt-8 print:hidden">
        <SolidButton href={`/docs/${product.slug}`}>
          Get started with {product.title}
        </SolidButton>
        {comparison.migrationGuide ? (
          <OutlineButton href={comparison.migrationGuide}>
            Migrate from {competitor}
          </OutlineButton>
        ) : null}
      </ButtonRow>
    </header>
  );
}
