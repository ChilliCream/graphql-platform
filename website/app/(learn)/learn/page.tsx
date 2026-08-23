import { LearnCollectionSection } from "@/src/components/learn/LearnCollectionSection";
import { LearnEditorialBand } from "@/src/components/learn/LearnEditorialBand";
import { LearnExplainerList } from "@/src/components/learn/LearnExplainerList";
import { LearnSubscribeBand } from "@/src/components/learn/LearnSubscribeBand";
import { LearnTopicRail } from "@/src/components/learn/LearnTopicRail";
import { LearnVideoSection } from "@/src/components/learn/LearnVideoSection";
import { popularTags, TOPICS, topicBrowseHref, topicsForBlogPost } from "@/src/components/learn/editorial";
import { learnItemHref } from "@/src/components/learn/learnItemHref";
import { findFeaturedTemplate, LEARN_SUMMARIES, TEMPLATE_SUMMARIES, VIDEO_ITEMS } from "@/src/data/learn/content";
import type { LearnItemSummary } from "@/src/data/learn/types";
import { listArticlesByKind } from "@/src/helpers/articles";
import { getLatestBlogPost, listBlogPostSummaries, type BlogPostSummary } from "@/src/helpers/blogPosts";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL, toAbsoluteUrl } from "@/src/helpers/siteUrl";
import { breadcrumbList, WEBSITE_ID } from "@/src/helpers/structuredData";

const LEARN_DESCRIPTION =
  "The GraphQL for .NET hub: the latest from the team, topic guides, starter templates, videos, and answer-first explainers on Hot Chocolate, Fusion, and the rest of the platform.";

export const metadata = pageMetadata({
  title: "Learn",
  description: LEARN_DESCRIPTION,
  path: "/learn",
  keywords: ["GraphQL", "Hot Chocolate", "Fusion", ".NET", "GraphQL tutorials", "GraphQL federation"],
});

/**
 * Builds the landing page's JSON-LD `@graph`: the page's own `WebPage` node,
 * its breadcrumb trail, and an `ItemList` of the featured catalog items shown
 * in the collection section below the fold.
 */
function buildStructuredData(collectionItems: readonly LearnItemSummary[]) {
  return {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "WebPage",
        "@id": `${SITE_URL}/learn#webpage`,
        url: `${SITE_URL}/learn`,
        name: "Learn",
        description: LEARN_DESCRIPTION,
        isPartOf: { "@id": WEBSITE_ID },
      },
      breadcrumbList([{ name: "Home", path: "/" }, { name: "Learn" }]),
      {
        "@type": "ItemList",
        name: "Featured on the Learn hub",
        numberOfItems: collectionItems.length,
        itemListElement: collectionItems.map((item, index) => ({
          "@type": "ListItem",
          position: index + 1,
          name: item.title,
          url: toAbsoluteUrl(learnItemHref(item)),
        })),
      },
    ],
  };
}

/**
 * Selects up to 4 posts for a topic rail (learn-editorial.md section 15.2),
 * newest first. Returns an empty array (the rail is then omitted) when fewer
 * than 3 posts remain after the editorial band's dedupe. Returning the exact
 * posts placed, rather than a count, is what the caller marks as consumed.
 */
function selectTopicPosts(pool: readonly BlogPostSummary[]): readonly BlogPostSummary[] {
  return pool.length >= 3 ? pool.slice(0, 4) : [];
}

export default function LearnPage() {
  const allPosts = listBlogPostSummaries();
  const featured = getLatestBlogPost();

  // Editorial band dedupe (learn-editorial.md section 15.1): a post appears
  // at most once in the band, and every post the band shows is excluded from
  // the topic sections below it.
  const postsExcludingFeatured = allPosts.filter((post) => post.stem !== featured?.stem);
  const latestPosts = postsExcludingFeatured.slice(0, 5);
  const bandStems = new Set<string>([...(featured ? [featured.stem] : []), ...latestPosts.map((post) => post.stem)]);

  const railPromoPost = allPosts.find(
    (post) => post.category === "Release" && post.featuredImage && !bandStems.has(post.stem),
  );
  if (railPromoPost) {
    bandStems.add(railPromoPost.stem);
  }

  const topicPostPool = allPosts.filter((post) => !bandStems.has(post.stem));
  const tags = popularTags(allPosts);
  const explainerArticles = [...listArticlesByKind("explainer"), ...listArticlesByKind("comparison")];

  const featuredTemplate = findFeaturedTemplate();
  const featuredTemplateSummary = TEMPLATE_SUMMARIES.find((t) => t.slug === featuredTemplate.slug);
  const otherTemplates = TEMPLATE_SUMMARIES.filter((t) => t.slug !== featuredTemplate.slug).slice(0, 2);
  const otherCatalogItems = LEARN_SUMMARIES.filter(
    (item): item is Extract<LearnItemSummary, { type: "tutorial" | "example" | "workshop" }> =>
      item.type === "tutorial" || item.type === "example" || item.type === "workshop",
  );
  const collectionItems = [
    ...(featuredTemplateSummary ? [featuredTemplateSummary] : []),
    ...otherTemplates,
    ...otherCatalogItems,
  ].slice(0, 6);

  const consumedStems = new Set<string>();
  const structuredData = buildStructuredData(collectionItems);

  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData) }} />
      {/* The subnav wordmark carries the visible section identity (learn-editorial.md section 15); this page leads with content. */}
      <h1 className="sr-only">Learn ChilliCream</h1>
      {featured ? (
        <LearnEditorialBand
          latestPosts={latestPosts}
          featuredPost={featured}
          railPromo={
            railPromoPost?.featuredImage
              ? {
                  href: railPromoPost.href,
                  image: railPromoPost.featuredImage,
                  kicker: railPromoPost.category ?? "Release",
                  title: railPromoPost.title,
                  author: railPromoPost.author ?? undefined,
                }
              : null
          }
          tags={tags}
        />
      ) : null}
      {TOPICS.map((topic) => {
        const topicPosts = topicPostPool.filter(
          (post) => !consumedStems.has(post.stem) && topicsForBlogPost(post).includes(topic.key),
        );
        const shown = selectTopicPosts(topicPosts);
        if (shown.length === 0) {
          return null;
        }
        for (const post of shown) {
          consumedStems.add(post.stem);
        }
        return <LearnTopicRail key={topic.key} heading={topic.label} moreHref={topicBrowseHref(topic)} posts={shown} />;
      })}
      <LearnCollectionSection
        items={collectionItems}
        subLinks={[
          { label: "Templates", href: "/learn/browse?type=template" },
          { label: "Tutorials", href: "/learn/browse?type=tutorial" },
          { label: "Examples", href: "/learn/browse?type=example" },
          { label: "Workshops", href: "/learn/browse?type=workshop" },
        ]}
      />
      <LearnExplainerList articles={explainerArticles} />
      <LearnVideoSection videos={VIDEO_ITEMS} />
      <LearnSubscribeBand />
    </>
  );
}
