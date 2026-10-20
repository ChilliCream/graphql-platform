import Link from "next/link";
import {
  COMPARISONS_PATH,
  type ComparisonSummary,
} from "@/src/helpers/comparisons";
import { ArrowLeftIcon } from "@/src/icons/ArrowLeft";

interface ComparisonHeaderProps {
  readonly comparison: ComparisonSummary;
}

/** Top of a comparison page: the title and the introduction. */
export function ComparisonHeader({ comparison }: ComparisonHeaderProps) {
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
      <h1 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-8 tracking-tight text-balance">
        {comparison.title}
      </h1>
      <p className="text-cc-prose mt-6 max-w-3xl text-lg leading-relaxed">
        {comparison.lead}
      </p>
    </header>
  );
}
