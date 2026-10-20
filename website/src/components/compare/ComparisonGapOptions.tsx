import { CardGrid } from "@/src/components/CardGrid";
import { LinkCard } from "@/src/components/LinkCard";
import { SectionHeading } from "@/src/components/SectionHeading";
import { CONTACT_HREF } from "@/src/components/header/navData";
import { GITHUB_REPO_URL } from "@/src/helpers/github";

interface ComparisonGapOptionsProps {
  /** Name of the ChilliCream product, e.g. "Mocha". */
  readonly product: string;
  readonly className?: string;
}

/**
 * What to do when the product lacks something the other tool has: build it,
 * request it on GitHub, or have the ChilliCream team build it.
 */
export function ComparisonGapOptions({
  product,
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
