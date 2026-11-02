import { BlogIndexShell } from "@/src/components/BlogIndexShell";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { paginate } from "@/src/helpers/blogPaging";
import { listComparisonSummaries } from "@/src/helpers/comparisonCollection";
import {
  COMPARISON_DESCRIPTION,
  COMPARISON_ID,
  createComparisonCollectionNode,
  createComparisonItemListNode,
} from "@/src/helpers/comparisonStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { schemaRef } from "@/src/helpers/structuredData";

const PAGE = {
  title: "Comparison",
  description: COMPARISON_DESCRIPTION,
  path: "/comparison",
} as const;

export const metadata = pageMetadata(PAGE);

export default function ComparisonIndex() {
  const articles = listComparisonSummaries();
  const slice = paginate(articles, 1);
  if (slice === null) {
    return <BlogIndexShell title="Comparison" posts={[]} />;
  }

  const articleList = createComparisonItemListNode(
    PAGE.path,
    "Latest ChilliCream comparisons",
    slice.posts,
  );

  return (
    <>
      <PageStructuredData
        {...PAGE}
        pageType="CollectionPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Comparison" }]}
        mainEntity={schemaRef(articleList["@id"]!)}
        about={schemaRef(COMPARISON_ID)}
        additionalNodes={[createComparisonCollectionNode(), articleList]}
      />
      <BlogIndexShell
        title="Comparison"
        posts={slice.posts}
        pagination={{
          currentPage: slice.currentPage,
          totalPages: slice.totalPages,
          hrefForPage: (p) => (p === 1 ? "/comparison" : `/comparison/${p}`),
        }}
      />
    </>
  );
}
