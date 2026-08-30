import { LearnArticleRows } from "@/src/components/learn/LearnArticleRows";
import { LearnFeaturedStory } from "@/src/components/learn/LearnFeaturedStory";
import { Pagination } from "@/src/design-system/Pagination";
import { Typography } from "@/src/design-system/Typography";
import { paginate } from "@/src/helpers/blogPaging";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { breadcrumbList } from "@/src/helpers/structuredData";

export const metadata = pageMetadata({
  title: "Articles",
  description: "All ChilliCream articles: announcements, deep dives, and how-tos, from the /learn hub.",
  path: "/learn/articles",
});

const STRUCTURED_DATA = {
  "@context": "https://schema.org",
  ...breadcrumbList([{ name: "Learn", path: "/learn" }, { name: "Articles" }]),
};

export default function ArticlesIndex() {
  const posts = listBlogPostSummaries();
  const slice = paginate(posts, 1);
  const items = slice?.posts ?? [];
  const [featured, ...rest] = items;

  return (
    <div className="cc-content-dark">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(STRUCTURED_DATA) }} />
      <Typography variant="h1">Articles</Typography>
      {featured ? (
        <LearnFeaturedStory
          post={featured}
          priority
          layout="split"
          sizes="(max-width: 1023px) 100vw, (max-width: 1695px) 45vw, 720px"
        />
      ) : null}
      <div className="mt-8 sm:mt-10">
        <LearnArticleRows posts={rest} />
      </div>
      {slice ? (
        <Pagination
          currentPage={slice.currentPage}
          totalPages={slice.totalPages}
          hrefForPage={(p) => (p === 1 ? "/learn/articles" : `/learn/articles/page/${p}`)}
        />
      ) : null}
    </div>
  );
}
