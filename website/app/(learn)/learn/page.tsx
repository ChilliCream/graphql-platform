import { LearnCollectionSection } from "@/src/components/learn/LearnCollectionSection";
import { LearnEditorialBand } from "@/src/components/learn/LearnEditorialBand";
import { EXPLAINER_LIST_MIN_ITEMS, LearnExplainerList } from "@/src/components/learn/LearnExplainerList";
import type { LatestVideoRailItem } from "@/src/components/learn/LearnLatestVideos";
import { LearnSubscribeBand } from "@/src/components/learn/LearnSubscribeBand";
import { LearnTopicRail } from "@/src/components/learn/LearnTopicRail";
import { LearnVideoSection } from "@/src/components/learn/LearnVideoSection";
import { popularTags } from "@/src/components/learn/editorial";
import { learnItemHref } from "@/src/components/learn/learnItemHref";
import { findFeaturedTemplate, LEARN_SUMMARIES, TEMPLATE_SUMMARIES, VIDEO_ITEMS } from "@/src/data/learn/content";
import { HUBS, hubHref, hubsForPost, type Hub } from "@/src/data/learn/hubs";
import type { LearnItemSummary, VideoItem } from "@/src/data/learn/types";
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
 * Selects up to 4 posts for a hub rail (learn-editorial.md section 15.2,
 * amended by website-kbx.6 to source rails from the four canonical hubs
 * instead of the five legacy topics), newest first. Returns an empty array
 * (the rail is then omitted) when fewer than 3 posts remain after the
 * editorial band's dedupe. Returning the exact posts placed, rather than a
 * count, is what the caller marks as consumed.
 */
function selectTopicPosts(pool: readonly BlogPostSummary[]): readonly BlogPostSummary[] {
  return pool.length >= 3 ? pool.slice(0, 4) : [];
}

/**
 * Latest 4 videos for the Watch rail (website-8s5.4), from the pool the
 * caller passes minus the rail's picks: entries with a `publishedAt` sort
 * newest first; the 2 legacy entries seeded before the TV migration carry no
 * `publishedAt` and sort after, oldest-dated content last rather than first.
 */
function selectLatestVideos(videos: readonly VideoItem[]): readonly VideoItem[] {
  return [...videos]
    .sort((a, b) => {
      if (!a.publishedAt && !b.publishedAt) {
        return 0;
      }
      if (!a.publishedAt) {
        return 1;
      }
      if (!b.publishedAt) {
        return -1;
      }
      return b.publishedAt.localeCompare(a.publishedAt);
    })
    .slice(0, 4);
}

/**
 * Rail's "Latest videos" block (website-kbx.5): only videos with a
 * `youtubeId` qualify, since the rail links to the internal
 * `/learn/videos/<slug>` page, not out to YouTube; newest `publishedAt`
 * first, capped at 2. Resolves each row's self-hosted optimized poster here,
 * at the page's server boundary (website-kbx.4), so `LearnLatestVideos`
 * itself stays free of the `node:fs`-based image manifest.
 */
function selectRailVideos(videos: readonly VideoItem[]): readonly LatestVideoRailItem[] {
  return videos
    .filter((video): video is VideoItem & { youtubeId: string } => Boolean(video.youtubeId))
    .sort((a, b) => (b.publishedAt ?? "").localeCompare(a.publishedAt ?? ""))
    .slice(0, 2)
    .map(({ slug, title, youtubeId, duration, poster, products, hubs }) => ({
      slug,
      title,
      youtubeId,
      duration,
      poster,
      products,
      hubs,
    }));
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

  const topicPostPool = allPosts.filter((post) => !bandStems.has(post.stem));
  const tags = popularTags(allPosts);
  const explainerArticles = [...listArticlesByKind("explainer"), ...listArticlesByKind("comparison")];
  const explainerSectionRenders = explainerArticles.length >= EXPLAINER_LIST_MIN_ITEMS;

  const featuredTemplate = findFeaturedTemplate();
  const featuredTemplateSummary = featuredTemplate
    ? TEMPLATE_SUMMARIES.find((t) => t.slug === featuredTemplate.slug)
    : undefined;
  const otherTemplates = TEMPLATE_SUMMARIES.filter((t) => t.slug !== featuredTemplate?.slug).slice(0, 2);
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

  const railVideos = selectRailVideos(VIDEO_ITEMS);
  const railVideoSlugs = new Set(railVideos.map((v) => v.slug));

  // Rails that actually render, not every entry in HUBS. Sourced from the
  // four canonical hubs (website-kbx.6), not the five legacy TOPICS: a hub
  // with no posts left in the pool (e.g. Messaging, which has none at all)
  // simply contributes no rail, matching every other "only render sections
  // with content" rule on this page.
  const topicRails: { readonly hub: Hub; readonly posts: readonly BlogPostSummary[] }[] = [];
  for (const hub of HUBS) {
    const hubPosts = topicPostPool.filter(
      (post) => !consumedStems.has(post.stem) && hubsForPost(post).includes(hub.key),
    );
    const shown = selectTopicPosts(hubPosts);
    if (shown.length === 0) {
      continue;
    }
    for (const post of shown) {
      consumedStems.add(post.stem);
    }
    topicRails.push({ hub, posts: shown });
  }

  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData) }} />
      {/* The subnav wordmark carries the visible section identity (learn-editorial.md section 15); this page leads with content. */}
      <h1 className="sr-only">Learn ChilliCream</h1>
      <LearnEditorialBand
        latestPosts={latestPosts}
        featuredPost={featured ?? null}
        latestVideos={railVideos}
        tags={tags}
      />
      {topicRails.map(({ hub, posts }) => (
        <LearnTopicRail key={hub.key} heading={hub.label} moreHref={hubHref(hub.key)} posts={posts} />
      ))}
      <LearnCollectionSection items={collectionItems} subLinks={[]} />
      {explainerSectionRenders ? <LearnExplainerList articles={explainerArticles} /> : null}
      <LearnVideoSection videos={selectLatestVideos(VIDEO_ITEMS.filter((v) => !railVideoSlugs.has(v.slug)))} />
      <LearnSubscribeBand />
    </>
  );
}
