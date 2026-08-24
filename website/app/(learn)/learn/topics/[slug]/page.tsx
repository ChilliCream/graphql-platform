import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ArticleBreadcrumb } from "@/src/components/learn/ArticleLayout";
import { popularTags } from "@/src/components/learn/editorial";
import { LearnCollectionSection } from "@/src/components/learn/LearnCollectionSection";
import { LearnEditorialBand } from "@/src/components/learn/LearnEditorialBand";
import type { LatestVideoRailItem } from "@/src/components/learn/LearnLatestVideos";
import { LearnMasthead } from "@/src/components/learn/LearnMasthead";
import { LearnSubscribeBand } from "@/src/components/learn/LearnSubscribeBand";
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

/**
 * Hero band's "Latest videos" rail, scoped to the hub (website-kbx.6, mirrors
 * /learn's own `selectRailVideos`): only videos with a `youtubeId` qualify,
 * since the rail links to the internal `/learn/videos/<slug>` page, newest
 * `publishedAt` first, capped at 2 per the kbx.5 hero-band composition.
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

  // Scoped hero band (website-kbx.6): the same Latest / Featured / rail
  // composition as /learn's editorial band, parameterized to this hub's own
  // posts, videos, and tags instead of the sitewide pool. Mirrors /learn's
  // own featured/latestPosts split (page.tsx): the newest post leads as
  // Featured, the next up to 5 fill the Latest column.
  const posts = listBlogPostSummaries().filter((post) => hubsForPost(post).includes(hubKey));
  const [featuredPost = null, ...restPosts] = posts;
  const latestPosts = restPosts.slice(0, 5);

  const catalogItems = LEARN_SUMMARIES.filter(
    (item): item is Exclude<LearnItemSummary, VideoItem> =>
      item.type !== "video" && hubsForLearnItem(item).includes(hubKey),
  );
  const hubVideos = VIDEO_ITEMS.filter((video) => hubsForLearnItem(video).includes(hubKey));
  const railVideos = selectRailVideos(hubVideos);
  const railVideoSlugs = new Set(railVideos.map((video) => video.slug));
  const tags = popularTags(posts);

  const collectionItems = catalogItems.slice(0, 6);
  const structuredData = buildStructuredData(hub, collectionItems);

  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(structuredData) }} />
      <ArticleBreadcrumb items={[{ label: "Learn", href: "/learn" }, { label: hub.label }]} />
      <LearnMasthead title={hub.label} teaser={hub.description} showEyebrow={false} />
      <LearnEditorialBand
        latestPosts={latestPosts}
        featuredPost={featuredPost}
        latestVideos={railVideos}
        tags={tags}
        allArticlesHref={hub.browseHref}
      />
      <LearnCollectionSection
        items={collectionItems}
        subLinks={typeSubLinks(hub, collectionItems)}
        browseHref={hub.browseHref}
      />
      <LearnVideoSection videos={selectVideos(hubVideos.filter((video) => !railVideoSlugs.has(video.slug)))} />
      <LearnSubscribeBand />
    </>
  );
}
