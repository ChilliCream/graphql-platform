import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ArticleBreadcrumb } from "@/src/components/learn/ArticleLayout";
import { LearnCollectionSection } from "@/src/components/learn/LearnCollectionSection";
import { LearnFeaturedStory } from "@/src/components/learn/LearnFeaturedStory";
import { LearnMasthead } from "@/src/components/learn/LearnMasthead";
import { LearnSubscribeBand } from "@/src/components/learn/LearnSubscribeBand";
import { LearnTopicRail } from "@/src/components/learn/LearnTopicRail";
import { LearnVideoSection } from "@/src/components/learn/LearnVideoSection";
import { learnItemHref } from "@/src/components/learn/learnItemHref";
import { contentTypeLabel } from "@/src/data/learn/facets";
import { LEARN_SUMMARIES, VIDEO_ITEMS } from "@/src/data/learn/content";
import { findHub, HUBS, hubHref, hubsForLearnItem, hubsForPost, type Hub, type HubKey } from "@/src/data/learn/hubs";
import type { LearnItemSummary, VideoItem } from "@/src/data/learn/types";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import { breadcrumbList, WEBSITE_ID } from "@/src/helpers/structuredData";

interface PageProps {
  readonly params: Promise<{ readonly slug: string }>;
}

/** A "Latest" rail needs a lead story plus at least 2 secondary rows to fill its two-column layout; below that it is omitted, matching /learn's own topic rails (page.tsx's `selectTopicPosts`). */
const MIN_RAIL_POSTS = 3;

export const dynamicParams = false;

export function generateStaticParams(): { slug: string }[] {
  return HUBS.map((hub) => ({ slug: hub.key }));
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const hub = findHub(slug);
  if (!hub) {
    return {};
  }
  return pageMetadata({
    title: hub.label,
    description: hub.description,
    path: hubHref(hub.key),
    keywords: [hub.label, "GraphQL", ".NET"],
  });
}

/** Builds the hub's JSON-LD `@graph`: `CollectionPage`, its breadcrumb trail, and an `ItemList` of the catalog items shown in the curated collection below. */
function buildStructuredData(hub: Hub, items: readonly LearnItemSummary[]) {
  const url = toAbsoluteUrl(hubHref(hub.key));
  return {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "CollectionPage",
        "@id": `${url}#webpage`,
        url,
        name: hub.label,
        description: hub.description,
        isPartOf: { "@id": WEBSITE_ID },
      },
      breadcrumbList([{ name: "Home", path: "/" }, { name: "Learn", path: "/learn" }, { name: hub.label }]),
      {
        "@type": "ItemList",
        name: `${hub.label} on the Learn hub`,
        numberOfItems: items.length,
        itemListElement: items.map((item, index) => ({
          "@type": "ListItem",
          position: index + 1,
          name: item.title,
          url: toAbsoluteUrl(learnItemHref(item)),
        })),
      },
    ],
  };
}

/** Latest 4 videos scoped to the hub, newest `publishedAt` first (mirrors /learn's `selectLatestVideos`). */
function selectVideos(videos: readonly VideoItem[]): readonly VideoItem[] {
  return [...videos].sort((a, b) => (b.publishedAt ?? "").localeCompare(a.publishedAt ?? "")).slice(0, 4);
}

/** One browse sub-link per content type actually present among `items`, pre-filtered to the hub's product plus that type. Omitted (empty array) when only one type is present: a single-option filter row adds nothing. */
function typeSubLinks(
  hub: Hub,
  items: readonly LearnItemSummary[],
): { readonly label: string; readonly href: string }[] {
  const types = [...new Set(items.map((item) => item.type))];
  if (types.length < 2) {
    return [];
  }
  const [path, query] = hub.browseHref.split("?");
  return types.map((type) => {
    const params = new URLSearchParams(query);
    params.set("type", type);
    return { label: contentTypeLabel(type), href: `${path}?${params.toString()}` };
  });
}

export default async function LearnHubPage({ params }: PageProps) {
  const { slug } = await params;
  const hub = findHub(slug);
  if (!hub) {
    notFound();
  }

  const hubKey: HubKey = hub.key;
  const posts = listBlogPostSummaries().filter((post) => hubsForPost(post).includes(hubKey));
  const [featuredPost, ...railPostPool] = posts;
  const catalogItems = LEARN_SUMMARIES.filter(
    (item): item is Exclude<LearnItemSummary, VideoItem> =>
      item.type !== "video" && hubsForLearnItem(item).includes(hubKey),
  );
  const videos = selectVideos(VIDEO_ITEMS.filter((video) => hubsForLearnItem(video).includes(hubKey)));

  const collectionItems = catalogItems.slice(0, 6);
  const structuredData = buildStructuredData(hub, collectionItems);

  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData) }} />
      <div className="pt-6 sm:pt-8">
        <ArticleBreadcrumb items={[{ label: "Learn", href: "/learn" }, { label: hub.label }]} />
      </div>
      <LearnMasthead title={hub.label} teaser={hub.description} />
      {featuredPost ? <LearnFeaturedStory post={featuredPost} /> : null}
      {railPostPool.length >= MIN_RAIL_POSTS ? (
        <LearnTopicRail
          heading={`Latest in ${hub.label}`}
          moreHref={hub.browseHref}
          moreLabel={`More ${hub.label}`}
          posts={railPostPool.slice(0, 4)}
        />
      ) : null}
      <LearnCollectionSection
        items={collectionItems}
        subLinks={typeSubLinks(hub, collectionItems)}
        browseHref={hub.browseHref}
      />
      <LearnVideoSection videos={videos} />
      <LearnSubscribeBand />
    </>
  );
}
