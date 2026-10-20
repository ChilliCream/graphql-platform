import { CardGrid } from "@/src/components/CardGrid";
import { LinkCard } from "@/src/components/LinkCard";
import { SectionHeading } from "@/src/components/SectionHeading";
import { CONTACT_HREF } from "@/src/components/header/navData";
import { GITHUB_REPO_URL } from "@/src/helpers/github";

interface ComparisonGapOptionsProps {
  /** Name of the ChilliCream product, e.g. "Mocha". */
  readonly product: string;
  /** What the product does not have yet; the list is left out when empty. */
  readonly gaps: readonly string[];
  readonly className?: string;
}

/**
 * What the product does not have yet, and the ways to get it: build it,
 * request it on GitHub, or have the ChilliCream team build it.
 */
export function ComparisonGapOptions({
  product,
  gaps,
  className,
}: ComparisonGapOptionsProps) {
  return (
    <section
      id="missing-something"
      aria-labelledby="missing-something-heading"
      className={`scroll-mt-24 ${className ?? ""}`.trim()}
    >
      <SectionHeading
        titleId="missing-something-heading"
        eyebrow="Missing something?"
        title={`Need something ${product} does not have yet?`}
        description={`A missing transport, store, or feature does not have to block your move to ${product}.`}
      />
      {gaps.length > 0 ? (
        <ul className="text-cc-ink mt-6 grid max-w-3xl list-disc gap-1.5 pl-5 text-sm leading-relaxed">
          {gaps.map((gap) => (
            <li key={gap}>{gap}</li>
          ))}
        </ul>
      ) : null}
      <div className="mt-8">
        <CardGrid cols={3} gap={4} itemsStretch>
          <LinkCard
            variant="plain"
            href={GITHUB_REPO_URL}
            external
            title="Build it"
            description={`${product}'s extension points are public, so you can add your own transport, store, or middleware, starting from the implementations in the repository.`}
          />
          <LinkCard
            variant="plain"
            href={`${GITHUB_REPO_URL}/issues`}
            external
            title="Request it"
            description="Open an issue or a pull request on GitHub. Requests from teams moving over decide what we build next."
          />
          <LinkCard
            variant="plain"
            href={CONTACT_HREF}
            title="Have us build it"
            description="Get in touch, and the ChilliCream team builds it for you, from a new transport to a store for your database."
          />
        </CardGrid>
      </div>
    </section>
  );
}
