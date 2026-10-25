import Link from "next/link";
import { CardGrid } from "@/src/components/CardGrid";
import { PageHero } from "@/src/components/PageHero";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { Section } from "@/src/components/Section";
import { Picture } from "@/src/design-system/Picture";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";
import {
  AUTHOR_PROFILES,
  AUTHORS_PATH,
  authorPageUrl,
} from "@/src/data/authors";
import { pageMetadata } from "@/src/helpers/pageMetadata";

const PAGE = {
  title: "ChilliCream authors",
  description: "Browse the authors of the ChilliCream blog.",
  path: AUTHORS_PATH,
} as const;

export const metadata = pageMetadata(PAGE);

export default function AuthorsPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="CollectionPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Authors" }]}
      />
      <PageHero
        title="The people behind the blog."
        teaser="Browse ChilliCream blog authors and their published articles."
      />
      <Section title="Authors">
        <CardGrid cols={3} step="progressive" itemsStretch>
          {AUTHOR_PROFILES.map((author) => (
            <Link
              key={author.slug}
              href={authorPageUrl(author)}
              className="border-cc-card-border bg-cc-surface hover:border-cc-accent flex h-full flex-col rounded-2xl border p-6 no-underline transition-colors"
            >
              <span className="flex items-center gap-4">
                <Picture
                  src={author.imageUrl}
                  alt=""
                  width={64}
                  height={64}
                  className="border-cc-card-border size-16 rounded-full border object-cover"
                />
                <span className="text-cc-heading text-lg font-semibold">
                  {author.name}
                </span>
              </span>
              <span className="text-cc-accent mt-auto inline-flex items-center gap-1.5 pt-5 text-sm font-medium">
                View profile
                <ArrowRightIcon className="size-3.5" />
              </span>
            </Link>
          ))}
        </CardGrid>
      </Section>
    </>
  );
}
