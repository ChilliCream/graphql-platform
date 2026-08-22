# Content strategy and IA: the /learn editorial hub

Strategy for evolving /learn from a faceted catalog into an editorial hub in
the style of IBM Think (ticket website-5yo.8, parent website-5yo). The landed
design spec `website/docs/design/learn-hub.md` (ticket website-5yo.1) governs
the catalog page, the unified card, and the template detail pages; this
document layers the editorial hub on top and adjusts the route map where the
two collide (section 4). No code changes belong to this ticket; implementation
is tickets website-5yo.9 through website-5yo.12.

**Open ruling:** section 5 (blog model) is explicitly NOT decided here. It
contains three options and one recommendation, and requires a user ruling
before website-5yo.11 may start.

---

## 1. Content inventory (verified against the repo, 2026-08-22)

### 1.1 Blog corpus: `content/blog/`

27 posts, 2022-01-13 through 2026-08-03. Frontmatter carries `date`, `title`,
optional `description`, `tags`, optional `category`, `featuredImage`, optional
`featuredVideoId`, and author fields (`src/helpers/blogPosts.ts` defines the
parsed `BlogPostSummary` shape).

Categories in use: `Release` (12 posts), `Newsletter` (3), `AI` (3); 9 posts
carry no category. 32 distinct tags; frequency of the head of the
distribution:

| Tag                                                                 | Posts |
| ------------------------------------------------------------------- | ----- |
| graphql                                                             | 23    |
| hotchocolate                                                        | 15    |
| dotnet                                                              | 11    |
| release                                                             | 10    |
| bananacakepop                                                       | 9     |
| fusion, cloud, aspnetcore                                           | 8     |
| ide, federation, ai                                                 | 5     |
| workshops, nitro                                                    | 4     |
| mcp, llm, openapi, open-telemetry, semantic-introspection, products | 2     |

The remaining 13 tags appear once each (`agents`, `apollo-federation`, `api`,
`community`, `deprecation`, `directives`, `event-streams`, `graphqlconf`,
`logging`, `micro-services`, `rest`, `subscriptions`, `telemetry`).

Routes: paginated index at `/blog` (`app/blog/page.tsx`), post pages at
`/blog/YYYY-MM-DD-slug` (`app/blog/[...slug]/page.tsx`, URL shape from
`blogUrlForStem` in `src/helpers/blogPaths.ts`), paginated tag pages at
`/blog/tags/[tag]` (`app/blog/tags/[tag]/page.tsx` and `[page]/page.tsx`),
and a static RSS feed at `/blog/rss.xml` (`app/blog/rss.xml/route.ts`).

### 1.2 Learn catalog: `src/data/learn/`

`src/data/learn/content.ts` seeds 18 items across the `LearnItem` union from
`src/data/learn/types.ts`:

- 8 templates (full detail-page payload: body sections, CLI commands, GitHub
  URL, stack, facets)
- 2 videos (YouTube URLs, duration, level)
- 3 tutorials (all `externalUrl` into docs:
  `/docs/hotchocolate/get-started-with-graphql-in-net-core`,
  `/docs/fusion/getting-started`, `/docs/strawberryshake/get-started`)
- 3 examples (GitHub repo links)
- 2 workshops (one links to the blog post
  `/blog/2024-04-01-fullstack-workshop`, one to
  `github.com/ChilliCream/graphql-workshop`)

`src/data/learn/facets.ts` defines the facet axes: `CONTENT_TYPE_OPTIONS`
(template, video, tutorial, example, workshop), the shared `PRODUCT_OPTIONS`
axis (hot-chocolate, mocha, fusion, nitro, strawberry-shake), and the
template-only `TEMPLATE_FILTER_AXES` (topology, use case, language, client,
agent-ready).

### 1.3 Other surfaces

- Docs tutorials live in `content/docs/` (hotchocolate, fusion, mocha, nitro,
  skillz, strawberryshake). Docs remain the canonical how-to surface; the hub
  points into them, it does not duplicate them.
- YouTube channel `youtube.com/c/ChilliCream` (linked from
  `src/components/Footer.tsx`); some posts embed videos via
  `featuredVideoId`.
- There is no newsletter subscribe component anywhere in `src/components/` or
  `app/(content)/`. The only subscription mechanisms today are the RSS feed
  and the social links in the footer. A subscribe band is a gap the hub
  landing should eventually fill (out of scope here; noted for
  website-5yo.9/.10).

### 1.4 Reference model: ibm.com/think (observed 2026-08-22)

Topic subnav (Artificial intelligence, Cloud, Security, News) plus Subscribe;
sections in order: Latest (news grid), Featured (single big story), topic
rails, Feature stories, curated collections, learning hubs, Top insights and
explainers (what-is pages), Podcasts, Webinars, newsletter subscription.
Notably, IBM Think has no traditional blog: everything is an article inside
the hub. That is the structural question section 5 resolves for us.

---

## 2. Content types and detail-page policy

The hub recognizes nine content types. Five already exist in the `LearnItem`
union; article, comparison, explainer, and webinar are new editorial types
introduced by this strategy. The policy for each:

| Type           | Exists today                     | Detail page                           | Canonical URL                        | Notes                                                                                                                                                                                               |
| -------------- | -------------------------------- | ------------------------------------- | ------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Article / news | 27 posts in `content/blog/`      | Yes (owned by blog ruling, section 5) | `/blog/YYYY-MM-DD-slug` today        | Announcements, deep dives, newsletters, event reports.                                                                                                                                              |
| Comparison     | No                               | Yes, first-party                      | `/learn/articles/[slug]` (section 4) | "X vs Y" evaluations (e.g. gateway comparisons). Pipeline is ticket website-5yo.12.                                                                                                                 |
| Explainer      | No                               | Yes, first-party                      | `/learn/articles/[slug]`             | IBM-style "what is X" pages. Evergreen, SEO-oriented, updated in place (no date in URL). Pipeline is website-5yo.12.                                                                                |
| Tutorial       | 3 seeded, all pointing into docs | No, link to `content/docs/`           | docs path via `externalUrl`          | Docs stay canonical. A hub detail page would fork the content.                                                                                                                                      |
| Video          | 2 seeded                         | No, link to YouTube                   | `VideoItem.url`                      | Card opens YouTube in a new tab (per learn-hub.md section 4). A first-party watch page is a possible later addition, not part of this strategy.                                                     |
| Template       | 8 seeded                         | Yes (already specified)               | `/learn/templates/[slug]`            | Detail layout per learn-hub.md section 5.                                                                                                                                                           |
| Example        | 3 seeded                         | No, link to GitHub                    | `externalUrl`                        | The repo README is the detail page.                                                                                                                                                                 |
| Workshop       | 2 seeded                         | No, external link                     | `externalUrl`                        | Links to the workshop repo or announcement post until dedicated workshop landing pages exist (none planned in this wave).                                                                           |
| Webinar        | None                             | No, external link                     | `externalUrl`                        | Placeholder type for future live sessions; model as a `video`-like item with a registration or recording link. Do not build a surface for a type with zero items; the hub simply omits empty rails. |

Rule of thumb: a type gets a first-party detail page only when we own the
canonical content (templates, comparisons, explainers, blog articles).
Everything whose canonical home is elsewhere (docs, GitHub, YouTube) gets a
card that links out, exactly as `learn-hub.md` section 2 already established
for videos, tutorials, examples, and workshops.

Data model impact (for website-5yo.10/.12, not this ticket): extend
`LearnContentType` in `src/data/learn/facets.ts` with `article`,
`comparison`, `explainer` (and later `webinar`) following the file's own
rule: add options at the end, never reuse a key. Blog-sourced articles should
be projected into hub rails from `listBlogPostSummaries()` rather than
hand-duplicated into `content.ts`.

---

## 3. Topic taxonomy

IBM Think organizes by a handful of editorial topics, not by product SKU or
by the long tail of tags. We mirror that: topics are a curated editorial
axis, distinct from the existing `PRODUCT_OPTIONS` facet (which stays as the
precise filter in the catalog) and from blog tags (which stay as free
metadata). Every content item maps to one primary topic and optionally
secondary topics.

Five topics fit the corpus:

| Topic key       | Label                     | What it covers                                                               | Tag/product mapping (from section 1.1)                                                                                      |
| --------------- | ------------------------- | ---------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `graphql`       | GraphQL fundamentals      | Language, spec, schema design, directives, community                         | `graphql` (as primary subject), `directives`, `deprecation`, `community`, `graphqlconf`, `api`                              |
| `hot-chocolate` | Hot Chocolate             | Building GraphQL servers in .NET                                             | `hotchocolate`, `dotnet`, `aspnetcore`, product `hot-chocolate`, client work via `strawberry-shake`                         |
| `federation`    | Federation and Fusion     | Composite schemas, gateways, distributed GraphQL                             | `fusion`, `federation`, `apollo-federation`, `micro-services`, `subscriptions`, `event-streams`, products `fusion`, `mocha` |
| `tooling`       | Tooling and observability | Nitro, the former Banana Cake Pop, telemetry, logging, OpenAPI/REST adapters | `nitro`, `bananacakepop`, `ide`, `cloud`, `telemetry`, `open-telemetry`, `logging`, `openapi`, `rest`, product `nitro`      |
| `ai`            | AI and agents             | MCP, LLMs, semantic introspection, agent tooling                             | `ai`, `llm`, `mcp`, `agents`, `semantic-introspection`, blog category `AI`                                                  |

Two deliberate non-topics:

- **News/Releases is a stream, not a topic.** The `Release` and `Newsletter`
  categories (15 of 27 posts) cut across all five topics. The hub surfaces
  them as the "Latest" section (IBM's news grid), filterable by topic, rather
  than as a sixth topic that would swallow half the corpus.
- **Products are a facet, not a topic.** The catalog keeps `PRODUCT_OPTIONS`
  for precise filtering; topics are broader (e.g. the `federation` topic
  spans Fusion and Mocha content plus vendor-neutral federation articles).

Every current tag maps into this scheme with no orphans (`products`,
`workshops`, and `release` are metadata tags, not subjects; `products` and
`workshops` map to content types, and `release` marks the Release stream
handled as a category above). The mapping table should live next to the facet definitions
(`src/data/learn/facets.ts` or a sibling module) when website-5yo.10
implements it.

---

## 4. Route map

The landed `learn-hub.md` spec placed the faceted catalog at `/learn` itself.
The editorial hub takes that slot; the catalog relocates to `/learn/browse`.
This is the one place this document supersedes the design spec (relocation is
implemented by website-5yo.10).

| Route                                      | Surface                                                                                                                                                                | Status                                                                                                                                                                                                                                                                             |
| ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `/learn`                                   | Editorial hub landing (section 4.1)                                                                                                                                    | New (website-5yo.10). Currently `app/(content)/learn/page.tsx` renders the catalog.                                                                                                                                                                                                |
| `/learn/browse`                            | Faceted catalog, exactly as specified in `learn-hub.md` sections 3 and 6, including all URL params (`?type=`, `?product=`, `?q=`, template axes)                       | Relocated from `/learn` (website-5yo.10)                                                                                                                                                                                                                                           |
| `/learn/templates/[slug]`                  | Template detail pages                                                                                                                                                  | Unchanged (`app/(content)/learn/...` per learn-hub.md section 2)                                                                                                                                                                                                                   |
| `/learn/topics/[topic]`                    | Topic surfaces for the five topic keys of section 3: topic intro, latest articles in topic, relevant catalog rails                                                     | Phase 2. Until built, topic links resolve to `/learn/browse` with the topic's product/type filters preapplied.                                                                                                                                                                     |
| `/learn/articles/[slug]`                   | First-party comparisons and explainers (website-5yo.12), sourced from a new `content/learn/articles/` directory using the same frontmatter pipeline as `content/blog/` | New. Undated slugs (`fusion-vs-apollo-router`, `what-is-graphql-federation`) because these are evergreen and updated in place. A frontmatter `kind: comparison \| explainer` field distinguishes them; no separate route namespaces, so the URL scheme does not multiply per type. |
| `/blog/*`, `/blog/tags/*`, `/blog/rss.xml` | Blog index, posts, tags, feed                                                                                                                                          | Owned by the section 5 ruling                                                                                                                                                                                                                                                      |

Redirect adjustments caused by the relocation:

- `/templates` and `/templates/[slug]` are specified (learn-hub.md section 2,
  implemented by website-5yo.5) to redirect to `/learn?type=template` and
  `/learn/templates/[slug]`. The first target becomes
  `/learn/browse?type=template`.
- `/learn?type=...` (and any other catalog params on `/learn`) must redirect
  to `/learn/browse` with params preserved, so links shared during the
  catalog-at-`/learn` window keep working.

### 4.1 Hub landing anatomy (content plan, not visual design)

Mapping the observed IBM Think structure onto our inventory. Visual design is
ticket website-5yo.9; this list fixes what content each section draws from.

1. **Topic subnav**: the five topics of section 3 plus a Browse link to
   `/learn/browse`. (IBM: topic subnav + Subscribe.)
2. **Latest**: newest 4-6 items from `listBlogPostSummaries()`
   (`src/helpers/blogPosts.ts`), regardless of topic. (IBM: news grid.)
3. **Featured**: one editorially pinned story, initially the newest post with
   a `featuredImage` (the `getLatestBlogPost()` heuristic already exists).
4. **Topic rails**: one rail per topic that has 3 or more items, mixing
   articles and catalog items. (IBM: "Latest in artificial intelligence".)
5. **Catalog rails**: templates (featured first, per
   `findFeaturedTemplate()` in `src/data/learn/content.ts`), then a mixed
   rail of tutorials/examples/workshops linking into `/learn/browse`. (IBM:
   curated collections and learning hubs.)
6. **Explainers**: rail of `kind: explainer` articles once website-5yo.12
   seeds them; section is omitted while empty. (IBM: "Top insights and
   explainers".)
7. **Videos**: rail from `VIDEO_ITEMS` plus a link to the YouTube channel.
   (IBM: Podcasts/Webinars slot.)
8. **Subscribe band**: RSS link now; a newsletter form when one exists
   (section 1.3 gap). (IBM: newsletter subscription.)

Empty-state rule: rails render only when they have content. With today's
inventory, sections 6 and a webinar rail are omitted; nothing on the landing
may show placeholder cards.

---

## 5. Blog model: options and recommendation

> **NEEDS USER RULING. This section intentionally does not decide.** The
> orchestrator must surface the three options below to the user and record
> the ruling as a comment on ticket website-5yo.11 before that ticket starts.

The question: IBM Think has no traditional blog; everything is a hub article.
We have 27 posts at stable `/blog/YYYY-MM-DD-slug` URLs, tag pages, and an
RSS feed with subscribers. How does /blog relate to /learn?

### Option A: keep /blog as-is, surface posts in hub rails

The hub's Latest/Featured/topic rails link to existing `/blog/...` URLs.
No blog code changes at all.

- Cheapest; zero URL or feed risk; website-5yo.11 shrinks to "wire rails".
- Cost: two front doors with two visual languages. The blog index
  (`src/components/BlogIndexShell.tsx`) competes with `/learn` as the place
  to read, which is exactly the fragmentation the hub is meant to end.

### Option B: restyle /blog in place as the hub's article archive

Keep every URL (`/blog`, `/blog/YYYY-MM-DD-slug`, `/blog/tags/[tag]`,
`/blog/rss.xml`) and all frontmatter. Restyle the blog index and post pages
into the hub's visual language, add hub navigation (breadcrumb into /learn,
topic links per section 3), and demote `/blog` from front door to archive:
the hub landing is the reading entry point, `/blog` is the complete
chronological index it links to ("All articles").

- Zero redirect and RSS risk: no inbound link, bookmark, or feed subscription
  changes; `app/blog/rss.xml/route.ts` is untouched.
- One editorial voice: readers cannot tell where hub ends and blog begins.
- Cost: article URLs stay under `/blog/...` rather than `/learn/...`, so the
  hub's content lives under two path prefixes. This is cosmetic; IBM itself
  serves Think articles under several prefixes.

### Option C: migrate posts into the hub IBM-style

Move all 27 posts to `/learn/articles/[slug]` (or `/learn/news/...`), add
permanent redirects from every `/blog/...` URL, and either redirect
`/blog/rss.xml` to a regenerated feed or keep emitting it with new item
URLs. Retire the standalone blog index.

- Purest IBM Think shape: one namespace, one surface.
- Cost: 27 redirects plus tag-page and pagination redirects to get right,
  RSS item GUIDs churn (`app/blog/rss.xml/route.ts` uses the post URL as
  item identity, so every item reappears as unread in feed readers), and
  external links accrue a permanent redirect hop. Highest effort of the
  three, and the only one that can break something.

### Recommendation (one, per ticket): Option B

Option B captures the editorial win of Option C (one voice, hub as the single
front door) at Option A's risk level (no URL, redirect, or feed churn). The
`/blog` prefix remaining is a cosmetic impurity, not a user-facing seam, and
Option C stays available later since B changes no URLs. Option B also keeps
website-5yo.11 scoped to styling and navigation rather than migration
machinery.

**This remains a recommendation only. Do not start website-5yo.11 until the
user's ruling is recorded on that ticket.**

---

## 6. Non-goals

- Visual design of the hub landing and article templates: ticket
  website-5yo.9.
- Implementation of any route, component, or redirect: tickets
  website-5yo.10/.11/.12.
- Newsletter subscription mechanics (no component exists today; section 1.3).
- Webinar and podcast programs: type reserved in section 2, no surface built
  until content exists.
- Any change to `content/blog/`, `src/data/learn/`, or `app/` in this
  ticket.
