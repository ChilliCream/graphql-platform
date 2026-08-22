# Design spec: /learn editorial hub, article and comparison pages

Design addendum for the editorial layer of the learn hub (ticket
website-5yo.9, parent website-5yo). It designs the `/learn` editorial landing,
the article reading page, the comparison-page layout, and the entry into the
faceted browse surface. It is a design document only; implementation is
tickets website-5yo.10 through website-5yo.12.

This document layers on top of two landed inputs:

- `docs/design/learn-hub.md` (website-5yo.1) still governs the faceted
  catalog, the unified `LearnCard`, the per-type accent system, the template
  detail pages, and the empty/loading states. Nothing here changes that
  design; the catalog simply moves to `/learn/browse` per the route map below.
- `docs/design/learn-content-strategy.md` (website-5yo.8) fixes the content
  plan: topic taxonomy (section 3), route map (section 4), landing anatomy as
  a content list (section 4.1), and content types (section 2). This document
  turns that content plan into visual design.

**Blog model neutrality:** strategy section 5 (how /blog relates to /learn)
is an open user ruling. Every design in this document works unchanged under
Option A (blog untouched, hub rails link to `/blog/...`) and Option B (blog
restyled in place as the hub archive). Nothing here assumes Option C (URL
migration), and nothing blocks it later. Section 4.4 states the per-option
differences explicitly.

Component inventory conventions: components cited without qualification exist
in the repo (verified 2026-08-23). Proposed new components are marked
**(proposed)** and live under `src/components/learn/`.

---

## 1. Design direction

The landing is an editorial front page in the IBM Think mold: one featured
story, a stream of the latest, topic-organized rails, and curated pointers
into the catalog. It must read as the same site as `/resources`,
`/platform`, and the catalog, so the rules from learn-hub.md section 1 carry
over verbatim:

- No page-level backdrop. The page sits on the global dark starfield surface;
  visual interest comes from imagery (blog `featuredImage`s), the per-type
  accent chips, and the drink iconography.
- Two card families, used deliberately:
  - **Editorial cards** are image-led: `BlogTeaser`
    (`src/components/BlogTeaser.tsx`), reused as-is. Its surface
    (`border-cc-ink-faint bg-cc-white/2.5`, aspect-video image, category
    chip, date, clamped description, "Read" arrow) already speaks the site
    token language and needs no restyle.
  - **Catalog cards** are type-led: `LearnCard`
    (`src/components/learn/LearnCard.tsx`) with its `ContentTypeBadge`
    accents, exactly as specified in learn-hub.md section 4.
    A rail either announces "read this" (teasers) or "use this" (learn cards);
    the two families make that distinction scannable without labels.
- All section headers on the landing use one recurring form, the
  header-plus-arrow row already established by `FromOurBlog` and
  `SimilarPosts`: heading on the left, `ArrowLink`
  (`src/components/ArrowLink.tsx`) on the right pointing at the fuller
  surface. That arrow row is the browse entry pattern (section 5).

---

## 2. Route map (restating the strategy, for reference)

| Route                     | Surface                                                       | Designed in            |
| ------------------------- | ------------------------------------------------------------- | ---------------------- |
| `/learn`                  | Editorial landing                                             | Section 3              |
| `/learn/browse`           | Faceted catalog, unchanged from learn-hub.md sections 3 and 6 | learn-hub.md           |
| `/learn/templates/[slug]` | Template detail                                               | learn-hub.md section 5 |
| `/learn/articles/[slug]`  | First-party comparisons and explainers                        | Sections 4 and 6       |
| `/blog/*`                 | Blog index and posts, owned by the strategy section 5 ruling  | Section 4.4            |

One catalog-facing adjustment falls out of the relocation: everywhere
learn-hub.md links to `/learn?type=...` (facet-bar resets, breadcrumbs like
"Templates" on the detail page, the `LearnEmptyState` reset), the target
becomes the equivalent `/learn/browse?...` URL. The template detail
breadcrumb becomes `Learn / Templates / {title}` with "Learn" linking to the
editorial landing and "Templates" to `/learn/browse?type=template`. No other
catalog change.

---

## 3. Editorial landing: `/learn`

Rendered by `app/(content)/learn/page.tsx`, inside the `(content)` layout
gutter (`max-w-7xl`, `px-5 sm:px-12`). The page is a vertical stack of
sections; every rail renders only when it has content (strategy 4.1 empty
rule: no placeholder cards, no empty rails). Order top to bottom:

### 3.1 Masthead and topic subnav

The landing does not use the full-height `PageHero` (that voice belongs to
the catalog and sibling content pages; an editorial front page leads with
content, not with a display title). Instead a compact masthead,
`LearnMasthead` **(proposed)**:

- Eyebrow-voice label "Learn" (`Eyebrow`, `src/design-system/Eyebrow.tsx`),
  an `text-h3 sm:text-h2 font-heading text-cc-heading` title ("Learn
  ChilliCream", final copy owned by website-5yo.10), and one
  `text-cc-ink-dim` sentence. Left-aligned, `py-10 sm:py-14`, roughly half
  the vertical weight of `PageHero`.
- Directly below, the topic subnav `LearnTopicNav` **(proposed)**: a single
  horizontal row of pill links, `overflow-x-auto` on mobile with no wrap.
  Pills use the `Tag` shape (`rounded-full border border-cc-card-border
text-cc-ink-dim hover:border-cc-accent hover:text-cc-ink`), matching the
  facet-bar pill styling from learn-hub.md section 3.2 so the two surfaces
  feel related. Content: the five topics from strategy section 3 (GraphQL
  fundamentals, Hot Chocolate, Federation and Fusion, Tooling and
  observability, AI and agents), then a visually distinct final item
  "Browse all" styled as an `ArrowLink` rather than a pill, linking to
  `/learn/browse`.
- Topic pill targets: `/learn/topics/[topic]` once that surface exists;
  until then, `/learn/browse` with the topic's mapped filters preapplied
  (strategy section 4). The nav takes hrefs as data, so retargeting is a
  data change, not a redesign.
- The subnav separates from the content below with a
  `border-cc-card-border` bottom border, echoing the section borders the
  catalog already uses.

### 3.2 Featured story hero

`LearnFeatureHero` **(proposed)**: the single editorially pinned story,
sourced initially via the existing `getLatestBlogPost()` heuristic
(`src/helpers/blogPosts.ts`, newest post with a `featuredImage`).

- Layout: a full-width linked panel, two-column on `lg`
  (`lg:grid-cols-[1.1fr_1fr]`), stacked image-first on mobile.
- Left column: mono caption row ("FEATURED" in the `Eyebrow` voice, plus the
  post category chip and date in the `BlogTeaser` metadata style), title at
  `text-h4 sm:text-h3 lg:text-h2 font-heading text-cc-heading text-balance`,
  description `text-cc-ink-dim text-base sm:text-lg` clamped to 3 lines,
  byline row reusing the `BlogMetadata` presentation (avatar, author, date),
  and a "Read story" affordance in the `ArrowLink` style.
- Right column: the `featuredImage` via `Picture`
  (`src/design-system/Picture.tsx`), `aspect-video`, `rounded-2xl`,
  `border border-cc-ink-faint`, `object-cover`.
- Surface: the whole panel is one link with the `BlogTeaser` interaction
  (`hover:-translate-y-0.5`, border brightening to
  `border-cc-card-border-hover`), on `bg-cc-white/2.5
border-cc-ink-faint rounded-2xl`. It is the `BlogTeaser` recipe scaled up,
  not a third card family.
- Fallback: if no post has a `featuredImage`, the hero renders the newest
  post without the image column (single column, larger type). The hero never
  renders empty.

### 3.3 Latest

Section header "Latest" with `ArrowLink` "All articles" (target per section
4.4: `/blog` today under both Option A and B). Content: the newest 4 to 6
posts from `listBlogPostSummaries()`, excluding the featured story so the
top of the page never shows the same post twice.

Layout: `LearnLatestSection` **(proposed)** composing the existing
`BlogTeaserGrid` (`src/components/BlogTeaserGrid.tsx`) with the first 3
posts, followed on `lg` by a compact headline list for posts 4 to 6: a
single-column list of rows (date in mono caption voice, title in
`text-cc-heading font-medium`, whole row a link with
`hover:text-cc-accent` on the title). On mobile the headline list is
dropped and the grid alone represents Latest. The mixed grid-plus-list form
is the IBM news-grid gesture at our corpus size; if it proves fussy in
implementation, the sanctioned simplification is `BlogTeaserGrid` with up to
6 posts and nothing else.

### 3.4 Topic rails

One rail per topic with 3 or more items (strategy 4.1), each a
`LearnTopicRail` **(proposed)**:

- Header row: topic label as the section heading, `ArrowLink` "More
  {topic}" pointing at the same target as the topic's subnav pill.
- Content: exactly 3 slots on `lg` (1 column stacked on mobile), filled with
  the topic's newest items in a fixed mix: up to 2 articles as `BlogTeaser`
  and 1 catalog item as `LearnCard` (the catalog item chosen
  featured-template-first, then newest). When the topic has no catalog
  items, 3 teasers; when it has fewer than 2 recent articles, the catalog
  fills the remainder. The fixed pattern makes the deliberate mixing of the
  two card families read as intent rather than inconsistency, and both
  cards are already equal-height flex columns so they share a row cleanly
  (`grid gap-5 sm:grid-cols-2 lg:grid-cols-3`, items stretched).
- Topic membership comes from the tag/product mapping table in strategy
  section 3; the rail component takes items as props and does not know
  about the mapping.

### 3.5 Curated collection: templates and the catalog

The "use this" band, `LearnCollectionSection` **(proposed)**:

- Header row: "Start building" (copy owned by website-5yo.10) with
  `ArrowLink` "Browse the catalog" to `/learn/browse`.
- Content: `CardGrid` (`src/components/CardGrid.tsx`) `cols={3}
step="progressive"` `itemsStretch` of `LearnCard`s: the featured template
  first (`findFeaturedTemplate()` in `src/data/learn/content.ts`), then 2
  more templates, then a second row mixing tutorials, examples, and
  workshops (newest/most prominent first, max 6 cards total). This is the
  catalog's own card and grid, so this band is visually a preview of
  `/learn/browse`.
- Sub-links under the grid, mono caption voice: "Templates", "Tutorials",
  "Examples", "Workshops", each linking to `/learn/browse?type=...`. These
  are the type-scoped browse entries (section 5).

### 3.6 Explainers and comparisons

Rendered only once website-5yo.12 seeds `kind: explainer` or
`kind: comparison` articles; omitted while empty (today it is omitted).
`LearnExplainerList` **(proposed)**:

- Header row: "Explainers" with `ArrowLink` "All explainers" to
  `/learn/browse?type=explainer` once the catalog knows the type (strategy
  section 2 data-model note), or hidden until then.
- Content: not cards. IBM's "top insights" reads as a reference list, and
  explainers have no imagery, so this is a two-column (on `lg`) list of
  rows: kind chip ("EXPLAINER" or "COMPARISON" as a `ContentTypeBadge`-style
  tinted chip, see section 6.1), title in `text-cc-heading font-medium`,
  one-line description `text-cc-ink-dim text-sm` clamped, row separated by
  `border-cc-ink-faint` bottom borders, whole row a link.

### 3.7 Videos

`LearnVideoSection` **(proposed)**:

- Header row: "Watch" with `ArrowLink` "YouTube channel" to
  `youtube.com/c/ChilliCream` (external, new tab).
- Content: the seeded `VIDEO_ITEMS` as `LearnCard`s (video accent,
  duration chip, opens YouTube in a new tab per learn-hub.md section 4) in
  the same 3-column grid. No inline player on the landing: the existing
  click-to-load facade (`YouTubeVideo`/`VideoFacade`) stays reserved for
  article bodies and docs; the landing keeps every tile a uniform card.

### 3.8 Subscribe band

`LearnSubscribeBand` **(proposed)**, the closing band of the landing:

- Uses the `Band` component (`src/components/Band.tsx`) with `skin="card"`,
  `layout="centered"`: `SectionHeading` ("Keep up with GraphQL in .NET",
  copy owned by implementation) plus a `ButtonRow`.
- Phase 1 (no newsletter mechanism exists; strategy 1.3): `SolidButton`
  "Subscribe via RSS" to `/blog/rss.xml` and `OutlineButton` "YouTube" to
  the channel. No fake email form: a form without a backend is worse than a
  link.
- When a newsletter provider lands, the buttons are replaced by a single
  email `Input` + `SolidButton` row (design-system `Input`); the band's
  shell does not change.
- Below the band, the landing ends with the standard next-steps pattern
  already used across content pages: `NextStepsSection`
  (`src/components/NextStepsSection.tsx`) with primary "Browse the catalog"
  to `/learn/browse` and secondary "Read the docs" to `/docs`. This
  replaces `LearnClosing` on the landing; `LearnClosing` remains the
  closing band of `/learn/browse` (learn-hub.md section 3.4), with its
  button retargeted per section 2.

### 3.9 Responsive and motion summary

- Every grid: 1 column base, 2 at `sm`, 3 at `lg`, `gap-5`.
- The subnav and any overflow-prone row scroll horizontally inside their own
  container; the page never scrolls horizontally.
- Motion is limited to the established card hovers (`-translate-y-0.5` for
  teasers, `-translate-y-1` for learn cards, arrow `translate-x-1`). No
  carousels, no auto-advancing rails: rails are static grids.

---

## 4. Article reading page

One layout serves three producers: blog posts (under either blog ruling),
comparisons, and explainers. The current blog post page
(`app/blog/[...slug]/page.tsx`) is already structurally right: featured
image, `Typography variant="h1"` title, `BlogMetadata` + `BlogShareBar` row,
`BlogTags`, MDX body from `compileDoc`, `SimilarPosts`, with
`TableOfContents` in a `2xl` side column. The design extracts that
composition into a shared, data-driven shell rather than inventing a new
page.

### 4.1 `ArticleLayout` (proposed)

`src/components/learn/ArticleLayout.tsx` **(proposed)**: a presentational
component that takes plain props and renders the article column. It contains
no blog imports and no filesystem reads, which is what makes it
ruling-neutral (section 4.4).

Props (all data, no loaders): breadcrumb items; optional kind chip (section
6.1); title; optional standfirst; meta (author, authorUrl, authorImageUrl,
published date, optional updated date, reading time); optional hero image
src; share url/title; tags; the compiled MDX `ReactNode`; a related-items
slot (`ReactNode`).

Composition, top to bottom, inside a `max-w-5xl` article column:

1. **Breadcrumb**: mono caption voice (`font-mono text-xs uppercase
tracking-wider text-cc-ink-dim`), e.g. `Learn / Articles` with each
   ancestor a link. Blog posts render `Blog` (Option A) or `Learn / Blog`
   (Option B) here; the shell just renders what it is given.
2. **Kind chip row** (comparisons/explainers only): the tinted chip per
   section 6.1, plus for evergreen articles an "Updated {date}" line in the
   same mono voice. Blog posts omit this row.
3. **Hero image**: `Picture`, `aspect-video rounded-lg object-cover`,
   `priority`, exactly the current blog treatment. Optional; explainers
   typically omit it.
4. **Title**: `Typography variant="h1"` (`src/design-system/Typography.tsx`),
   unchanged from the blog page.
5. **Standfirst** (optional): `text-cc-ink-dim text-lg leading-relaxed`,
   sourced from frontmatter `description`. Blog posts today skip it (their
   description is meta-only); comparisons and explainers render it, since a
   one-paragraph answer-first summary is the genre convention.
6. **Meta row**: `BlogMetadata` (`src/components/BlogMetadata.tsx`) left,
   `BlogShareBar` (`src/components/BlogShareBar.tsx`) right, both reused
   as-is. For evergreen articles the date shown is the updated date.
7. **Tags**: `BlogTags` (`src/components/BlogTags.tsx`) reused as-is; under
   `/learn/articles` the tag links keep pointing at `/blog/tags/[tag]`
   until a hub tag surface exists (tags are shared metadata per strategy
   section 3, so this is correct, not a compromise).
8. **Body**: the compiled MDX children. Typography, code, admonitions,
   tables, and images all come from the existing MDX component map used by
   `compileDoc` (design-system `CodeBlock`, `Admonition`, `Table`,
   `InlineCode`, `Quote`, `Typography` headings). No new prose styles: an
   article body and a docs body are the same voice.
9. **Related**: a slot, not a hardcoded component. Blog posts pass
   `SimilarPosts` (`src/components/SimilarPosts.tsx`) as today; comparisons
   and explainers pass a `CardGrid` of `LearnCard`s / `BlogTeaser`s chosen
   by topic overlap (wiring owned by website-5yo.12).

### 4.2 Page chrome around the shell

- **TOC**: `TableOfContents` in the `2xl:grid-cols-[1fr_20rem]` side
  column, as the blog and docs pages do today. Comparisons and explainers
  are long-form and get it; it collapses below `2xl` exactly as now.
- **Left sidebar**: the blog post page's `SidebarDrawer` + `BlogSidebar`
  ("Latest posts") is blog chrome, not article chrome. `/learn/articles`
  pages do not render it; they use the plain `(content)`-style full-width
  column plus TOC. Whether blog posts keep it is owned by the section 5
  ruling (Option A: yes, untouched; Option B: the restyle decides, and this
  spec recommends replacing it with the breadcrumb + TOC-only chrome for
  one consistent reading surface).
- **Structured data**: `/learn/articles` pages emit `Article` JSON-LD and a
  `BreadcrumbList`, mirroring the `BlogPosting` blocks the blog page
  already builds (implementation detail for website-5yo.12).

### 4.3 Typography rules

All from the `@theme` scale in `app/globals.css`; no ad-hoc sizes:

- Title: `Typography variant="h1"`; section headings inside the body map
  from MDX `h2`/`h3` through the existing components (`text-h4`/`text-h5`
  equivalents with permalink anchors).
- Standfirst: `text-lg`; body: `text-body` via the prose defaults; captions,
  breadcrumbs, and kind chips: mono caption voice
  (`font-mono text-xs uppercase tracking-wider`).
- Measure: the `max-w-5xl` column with the existing prose line-height. Wide
  elements (comparison tables, code) may extend to the full column width
  and scroll horizontally inside their own wrapper.

### 4.4 Behavior under each blog ruling

- **Option A** (blog untouched): `ArticleLayout` is used only by
  `/learn/articles/[slug]`. The blog keeps its current page exactly as is.
  Landing rails link into `/blog/...`. Nothing in sections 3 or 4 needs to
  change.
- **Option B** (blog restyled in place): `app/blog/[...slug]/page.tsx`
  adopts `ArticleLayout`, passing its existing frontmatter-derived data.
  Because the shell's props are a subset of what the blog page already
  computes (`BlogPostSummary` plus `compileDoc` output), adoption is a
  refactor of one page file, not a redesign. `/blog` index pages restyle by
  keeping `BlogIndexShell` and adding the hub breadcrumb and topic links
  (owned by website-5yo.11 after the ruling).
- **Option C** is out of scope by ruling status; nothing here depends on
  URLs, so the design would survive it regardless.

---

## 5. Browse entry points

The catalog at `/learn/browse` is entered from the landing in three
recurring shapes, all designed above and collected here for the acceptance
check:

1. **Persistent**: "Browse all" at the end of the topic subnav (section
   3.1), visible at every viewport, the primary route in.
2. **Scoped**: every rail's header `ArrowLink` deep-links a preapplied
   filter view: topic rails to the topic's mapped filters, the collection
   band to `/learn/browse` and its per-type sub-links to
   `/learn/browse?type=...`, the video section header pointing off-site to
   YouTube (its cards are catalog items already). Filtered catalog URLs are
   shareable by design (learn-hub.md section 3.2), so these links are plain
   hrefs with no state handoff.
3. **Closing**: the `NextStepsSection` at the page end (section 3.8) with
   "Browse the catalog" as the primary button, catching readers who
   scrolled through.

The reverse edge exists too: the catalog's breadcrumb/heading area on
`/learn/browse` links back to `/learn` ("Learn" breadcrumb), so the two
surfaces form an obvious pair.

---

## 6. Comparison page layout

Comparisons are `kind: comparison` articles at `/learn/articles/[slug]`
rendered through `ArticleLayout` (section 4) with genre-specific structure
inside the body. The genre promise: a reader with a decision to make gets
the answer early, the evidence in scannable form, and the nuance in prose.

### 6.1 Kind chips for editorial types

Extend the accent system of learn-hub.md section 4.1 to the editorial types,
in the same single source (`CONTENT_TYPE_META` in
`src/components/learn/contentTypeMeta.ts`, extended when the types land per
strategy section 2):

| Type       | Token     | Chip classes                                       |
| ---------- | --------- | -------------------------------------------------- |
| article    | `cc-note` | `text-cc-note bg-cc-note/10 ring-cc-note/30`       |
| comparison | `cc-tip`  | `text-cc-tip bg-cc-tip/10 ring-cc-tip/30`          |
| explainer  | `cc-note` | shares the article recipe (both are reading types) |

`cc-note` and `cc-tip` exist in `app/globals.css`. Learn-hub.md reserved
them for admonitions; that reservation is hereby narrowed: admonitions keep
them, and the two editorial reading types may also use them. The five
catalog types keep their landed accents unchanged. If implementation finds
the shared-token double duty confusing in practice, the fallback is a
neutral chip (`text-cc-ink ring-cc-card-border bg-cc-hover`) for all three
editorial types; the accent table above is the preferred form.

### 6.2 Page structure

Top matter comes from `ArticleLayout`: breadcrumb `Learn / Articles`,
"COMPARISON" chip, "Updated {date}" line, title in the "X vs. Y" form,
standfirst carrying the one-paragraph verdict. Then, in the body:

1. **Verdict cards**, `ComparisonVerdict` **(proposed)**, an MDX-usable
   component: one card per compared option in a `CardGrid`
   (`cols` matching the option count, 2 or 3), each card on the
   `HighlightCard` surface (`src/components/HighlightCard.tsx`): option
   name as `font-heading text-cc-heading` heading, a "Choose {X} when"
   list using `CheckList`/`CheckListItem`
   (`src/components/CheckList.tsx`, `CheckListItem.tsx`), and, where one
   option is ours, the `highlight` rainbow-border treatment with
   `badgeLabel="Our take"` so the vendor position is disclosed visually
   instead of smuggled into prose.
2. **At-a-glance matrix**: reuse `FeatureComparison`
   (`src/components/FeatureComparison.tsx`) as-is: columns are the compared
   products, grouped rows are capability areas, boolean cells render
   check/dash, string cells render values in mono. It already handles
   horizontal scroll in a focusable region, which is the required wide-table
   behavior inside the article column.
3. **Section-by-section prose**: standard MDX `h2` sections (one per
   capability area, mirroring the matrix groups), using the ordinary prose
   voice, `CodeBlock` for config/code contrasts, and the design-system
   `Table` (with `alternating`) for small in-prose tables where the full
   `FeatureComparison` panel would be too heavy.
4. **Methodology and disclosure**: a closing `Admonition` (note variant)
   stating what versions were compared and when, plus the vendor
   relationship. The "Updated" date in the top matter and this block are
   the honesty contract of the genre.
5. **Related**: `CardGrid` of related items (other comparisons, the
   relevant templates and tutorials) per section 4.1 item 9.

Explainers use the same top matter (with the explainer chip) and plain prose
sections; they need no components beyond the existing MDX set. A "Related
terms" list may reuse the section 3.6 list row form.

---

## 7. Existing blog components: reused, restyled, or replaced

| Component         | Path                                 | Disposition on the hub                                                                                                                          |
| ----------------- | ------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `BlogTeaser`      | `src/components/BlogTeaser.tsx`      | **Reused as-is.** The editorial card of the landing (sections 3.2 to 3.4). Its `BlogTeaserData` shape is already source-agnostic (plain hrefs). |
| `BlogTeaserGrid`  | `src/components/BlogTeaserGrid.tsx`  | **Reused as-is** in Latest; its zero-state line is never shown on the landing because empty rails are omitted upstream.                         |
| `FromOurBlog`     | `src/components/FromOurBlog.tsx`     | **Not used on /learn** (the landing composes rails itself with explicit data). Unchanged for its existing call sites elsewhere.                 |
| `BlogIndexShell`  | `src/components/BlogIndexShell.tsx`  | **Unchanged by this spec.** Stays the `/blog` index shell under Option A; Option B restyles around it (website-5yo.11, after the ruling).       |
| `BlogMetadata`    | `src/components/BlogMetadata.tsx`    | **Reused as-is** in `ArticleLayout` and the featured hero byline.                                                                               |
| `BlogShareBar`    | `src/components/BlogShareBar.tsx`    | **Reused as-is** in `ArticleLayout`.                                                                                                            |
| `BlogTags`        | `src/components/BlogTags.tsx`        | **Reused as-is** in `ArticleLayout`; tag links keep their `/blog/tags/` targets.                                                                |
| `BlogSidebar`     | `src/components/BlogSidebar.tsx`     | **Not used** on `/learn` surfaces (section 4.2); blog usage owned by the ruling.                                                                |
| `SimilarPosts`    | `src/components/SimilarPosts.tsx`    | **Reused as-is** as the blog's related slot in `ArticleLayout`.                                                                                 |
| `TableOfContents` | `src/components/TableOfContents.tsx` | **Reused as-is** on article pages.                                                                                                              |

Nothing blog-side is replaced or restyled by this spec; every replacement
decision that touches `/blog` routes belongs to the section 5 ruling and
ticket website-5yo.11.

---

## 8. Theme and token rules

The site is single-theme dark (verified: the `cc-*` palette is defined once
on `:root` in `app/globals.css` `@theme`; the only palette swap is
`@media print`; there is no `prefers-color-scheme` or `data-theme` handling
on the website). The rules from learn-hub.md section 7 apply to every
surface in this document:

- Every color is a `cc-*` token or a token with an opacity modifier. No raw
  hex, no `oklch` literals in components, so all pages stay correct under
  the print palette and any future light theme.
- Accent tints follow the `StatusChip` recipe (`/10`-`/15` background,
  `/30` ring, `/60` hover border), including the new editorial chips in
  section 6.1.
- The one sanctioned literal remains the `bg-[#f5f0ea]` chip behind
  `STACK_ICONS` brand marks inside `LearnCard`; no new literals.
- No section paints its own page background; panels use the established
  surfaces (`bg-cc-card-bg`, `bg-cc-white/2.5`, `Band` skins) on the global
  backdrop.
- Imagery (blog featured images) sits inside bordered, rounded containers
  (`border-cc-ink-faint`) so photos of any brightness stay framed on the
  dark surface.

---

## 9. Component inventory

### Reused as-is (all verified to exist)

| Component / module                                               | Path                                   | Role                                                                   |
| ---------------------------------------------------------------- | -------------------------------------- | ---------------------------------------------------------------------- |
| `BlogTeaser`, `BlogTeaserGrid`                                   | `src/components/`                      | Editorial cards and grids (sections 3.3, 3.4)                          |
| `BlogMetadata`, `BlogShareBar`, `BlogTags`, `SimilarPosts`       | `src/components/`                      | Article page (section 4)                                               |
| `TableOfContents`                                                | `src/components/TableOfContents.tsx`   | Article side column                                                    |
| `LearnCard`, `ContentTypeBadge`, `contentTypeMeta`               | `src/components/learn/`                | Catalog cards on the landing; chip system (section 6.1)                |
| `CardGrid`                                                       | `src/components/CardGrid.tsx`          | Collection band, verdict cards, related grids                          |
| `ArrowLink`                                                      | `src/components/ArrowLink.tsx`         | Rail header links, browse entries                                      |
| `SectionHeading`, `Band`, `ButtonRow`, `NextStepsSection`        | `src/components/`                      | Subscribe band and closing (section 3.8)                               |
| `Eyebrow`, `Tag`, `Picture`, `Typography`, `Input`               | `src/design-system/`                   | Masthead, subnav pills, imagery, article title, future newsletter form |
| `SolidButton`, `OutlineButton`                                   | `src/design-system/Button.tsx`         | Subscribe band CTAs                                                    |
| `FeatureComparison`                                              | `src/components/FeatureComparison.tsx` | Comparison matrix (section 6.2)                                        |
| `Table` family, `Admonition`, `CodeBlock`, `InlineCode`, `Quote` | `src/design-system/`                   | Article/comparison body                                                |
| `HighlightCard`, `CheckList`, `CheckListItem`                    | `src/components/`                      | Verdict cards (section 6.2)                                            |
| `YouTubeVideo` / `VideoFacade`                                   | `src/components/`                      | In-article embeds only (section 3.7)                                   |
| `listBlogPostSummaries`, `getLatestBlogPost`, `findSimilarPosts` | `src/helpers/blogPosts.ts`             | Landing and article data sources                                       |
| `findFeaturedTemplate`, `VIDEO_ITEMS`                            | `src/data/learn/content.ts`            | Collection and video rails                                             |

### Proposed new components (all under `src/components/learn/`)

| Proposed name                | Kind      | Responsibility                                   |
| ---------------------------- | --------- | ------------------------------------------------ |
| `LearnMasthead.tsx`          | component | Compact landing header (section 3.1)             |
| `LearnTopicNav.tsx`          | component | Topic pill subnav plus Browse link (section 3.1) |
| `LearnFeatureHero.tsx`       | component | Featured story panel (section 3.2)               |
| `LearnLatestSection.tsx`     | component | Latest grid plus headline list (section 3.3)     |
| `LearnTopicRail.tsx`         | component | Per-topic mixed rail (section 3.4)               |
| `LearnCollectionSection.tsx` | component | Curated catalog band (section 3.5)               |
| `LearnExplainerList.tsx`     | component | Explainer/comparison list rows (section 3.6)     |
| `LearnVideoSection.tsx`      | component | Video rail (section 3.7)                         |
| `LearnSubscribeBand.tsx`     | component | Subscribe band (section 3.8)                     |
| `ArticleLayout.tsx`          | component | Ruling-neutral article shell (section 4.1)       |
| `ComparisonVerdict.tsx`      | component | MDX verdict cards for comparisons (section 6.2)  |

All landing sections are server components (their data is static at build
time); nothing on the landing needs client state. The only client
components on these surfaces remain the ones that already are
(`BlogShareBar`, `VideoFacade`, TOC active tracking).

---

## 10. Non-goals

- Implementation of any route or component: website-5yo.10 (landing and
  browse relocation), website-5yo.11 (blog integration, gated on the
  section 5 ruling), website-5yo.12 (comparison/explainer pipeline and
  first article).
- Deciding the blog model: strategy section 5 remains the open user ruling.
- Newsletter subscription mechanics (section 3.8 designs the shell only).
- `/learn/topics/[topic]` surface design (phase 2; the landing degrades to
  filtered browse links until it exists).
- Any change to the landed catalog design beyond the `/learn/browse` link
  retargeting stated in section 2.
