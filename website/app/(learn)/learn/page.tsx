import { LearnCollectionSection } from "@/src/components/learn/LearnCollectionSection";
import { LearnEditorialBand } from "@/src/components/learn/LearnEditorialBand";
import { LearnExplainerList } from "@/src/components/learn/LearnExplainerList";
import { LearnSubscribeBand } from "@/src/components/learn/LearnSubscribeBand";
import { LearnTopicRail, type TopicRailSlot } from "@/src/components/learn/LearnTopicRail";
import { LearnVideoSection } from "@/src/components/learn/LearnVideoSection";
import {
  popularTags,
  TOPICS,
  toBlogTeaserData,
  topicBrowseHref,
  topicsForBlogPost,
  topicsForLearnItem,
} from "@/src/components/learn/editorial";
import { findFeaturedTemplate, LEARN_SUMMARIES, TEMPLATE_SUMMARIES, VIDEO_ITEMS } from "@/src/data/learn/content";
import type { LearnItemSummary } from "@/src/data/learn/types";
import { listArticlesByKind } from "@/src/helpers/articles";
import { getLatestBlogPost, listBlogPostSummaries, type BlogPostSummary } from "@/src/helpers/blogPosts";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL } from "@/src/helpers/siteUrl";

export const metadata = pageMetadata({
  title: "Learn",
  description:
    "The GraphQL for .NET hub: the latest from the team, topic guides, starter templates, videos, and answer-first explainers on Hot Chocolate, Fusion, and the rest of the platform.",
  path: "/learn",
  keywords: ["GraphQL", "Hot Chocolate", "Fusion", ".NET", "GraphQL tutorials", "GraphQL federation"],
});

const STRUCTURED_DATA = {
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  itemListElement: [
    { "@type": "ListItem", position: 1, name: "Home", item: `${SITE_URL}/` },
    { "@type": "ListItem", position: 2, name: "Learn" },
  ],
};

/** Builds a topic rail's exactly-3-item mix (learn-editorial.md section 3.4): up to 2 articles then catalog items fill the remainder, falling back to more articles when the topic has fewer than 2 catalog items too. */
function buildTopicSlots(posts: readonly BlogPostSummary[], items: readonly LearnItemSummary[]): TopicRailSlot[] {
  const wantPosts = items.length > 0 ? 2 : 3;
  const postSlots: TopicRailSlot[] = posts
    .slice(0, wantPosts)
    .map((post) => ({ kind: "post", post: toBlogTeaserData(post) }));
  const remaining = 3 - postSlots.length;
  const itemSlots: TopicRailSlot[] = items.slice(0, remaining).map((item) => ({ kind: "catalog", item }));
  let slots: TopicRailSlot[] = [...postSlots, ...itemSlots];
  if (slots.length < 3) {
    const extraPosts = posts
      .slice(postSlots.length, postSlots.length + (3 - slots.length))
      .map((post): TopicRailSlot => ({ kind: "post", post: toBlogTeaserData(post) }));
    slots = [...slots, ...extraPosts];
  }
  return slots;
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

  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(STRUCTURED_DATA) }} />
      {/* The subnav wordmark carries the visible section identity (learn-editorial.md section 15); this page leads with content. */}
      <h1 className="sr-only">Learn ChilliCream</h1>
      {featured ? (
        <LearnEditorialBand
          latestPosts={latestPosts}
          featuredPost={featured}
          railPromo={
            railPromoPost
              ? {
                  href: railPromoPost.href,
                  image: railPromoPost.featuredImage as string,
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
        const topicPosts = topicPostPool.filter((post) => topicsForBlogPost(post).includes(topic.key));
        const topicItems = LEARN_SUMMARIES.filter((item) => topicsForLearnItem(item).includes(topic.key));
        if (topicPosts.length + topicItems.length < 3) {
          return null;
        }
        return (
          <LearnTopicRail
            key={topic.key}
            heading={topic.label}
            moreHref={topicBrowseHref(topic)}
            slots={buildTopicSlots(topicPosts, topicItems)}
          />
        );
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
