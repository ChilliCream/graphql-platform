import { EditOnGitHub } from "@/src/components/EditOnGitHub";
import { SectionHeading } from "@/src/components/SectionHeading";
import {
  formatCheckedDate,
  type ComparisonSummary,
} from "@/src/helpers/comparisons";
import { githubEditUrl } from "@/src/helpers/githubEditUrl";

interface ComparisonSourcesProps {
  readonly comparison: ComparisonSummary;
  readonly className?: string;
}

/**
 * The official sources behind the facts about the other tool, with the date
 * they were checked and a link to correct the page on GitHub.
 */
export function ComparisonSources({
  comparison,
  className,
}: ComparisonSourcesProps) {
  const { competitor } = comparison;

  return (
    <section aria-labelledby="sources-heading" className={className}>
      <SectionHeading
        titleId="sources-heading"
        eyebrow="Sources"
        title={`Where the ${competitor} facts come from`}
        description={
          <>
            Checked against {competitor}&apos;s own documentation and
            announcements on{" "}
            <time dateTime={comparison.lastChecked}>
              {formatCheckedDate(comparison.lastChecked)}
            </time>
            . If something is out of date, edit this page on GitHub.
          </>
        }
      />
      <ul className="mt-6 grid gap-x-8 gap-y-2 text-sm sm:grid-cols-2">
        {comparison.sources.map((source) => (
          <li key={source.url}>
            <a
              href={source.url}
              target="_blank"
              rel="noopener noreferrer"
              className="text-cc-ink hover:text-cc-accent decoration-cc-ink-faint underline underline-offset-4 transition-colors"
            >
              {source.title}
            </a>
          </li>
        ))}
      </ul>
      <EditOnGitHub
        href={githubEditUrl(`content/compare/${comparison.slug}.md`)}
      />
    </section>
  );
}
