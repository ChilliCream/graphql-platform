import { CardGrid } from "@/src/components/CardGrid";
import { LinkCard } from "@/src/components/LinkCard";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { SectionHeading } from "@/src/components/SectionHeading";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { PRODUCTS } from "@/src/data/products";
import { COMPARISONS_PATH, listComparisons } from "@/src/helpers/comparisons";
import { pageMetadata } from "@/src/helpers/pageMetadata";

const PAGE = {
  title: "Compare ChilliCream",
  description:
    "How Hot Chocolate, Fusion, Nitro, Mocha, and Strawberry Shake compare with the tools .NET teams use today, with sourced facts and migration guides.",
  path: COMPARISONS_PATH,
} as const;

export const metadata = pageMetadata(PAGE);

export default function ComparisonsIndex() {
  const comparisons = listComparisons();
  const groups = PRODUCTS.map((product) => ({
    product,
    comparisons: comparisons.filter(
      (comparison) => comparison.product === product,
    ),
  })).filter((group) => group.comparisons.length > 0);

  return (
    <div className="px-5 py-8 sm:px-12">
      <PageStructuredData
        {...PAGE}
        pageType="CollectionPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Compare" }]}
      />
      <div className="mx-auto max-w-5xl">
        <header className="py-16 sm:pt-20">
          <Eyebrow color="ink-dim">Compare</Eyebrow>
          <h1 className="font-heading text-h3 text-cc-heading sm:text-h2 mt-4 tracking-tight text-balance">
            How ChilliCream compares
          </h1>
          <p className="text-cc-prose mt-6 max-w-2xl text-lg leading-relaxed">
            Side-by-side comparisons with the tools you might be using today.
            Every fact about another tool links to its source, and each page
            says where the other tool is the better fit.
          </p>
        </header>

        {groups.length > 0 ? (
          groups.map(({ product, comparisons: items }) => (
            <section
              key={product.slug}
              aria-labelledby={`${product.slug}-heading`}
              className="pb-16"
            >
              <SectionHeading
                titleId={`${product.slug}-heading`}
                eyebrow={product.description}
                title={product.title}
              />
              <div className="mt-8">
                <CardGrid cols={2} gap={4} itemsStretch>
                  {items.map((comparison) => (
                    <LinkCard
                      key={comparison.slug}
                      href={comparison.href}
                      title={comparison.title}
                      description={comparison.description}
                      variant="trailing"
                    />
                  ))}
                </CardGrid>
              </div>
            </section>
          ))
        ) : (
          <p className="text-cc-ink-dim pb-16">
            No comparisons yet. Check back soon.
          </p>
        )}
      </div>
    </div>
  );
}
