# Design spec: /learn editorial hub, article and comparison pages

> **Status (2026-08-23):** Part I below (sections 1 to 10) is the v1 spec; it
> shipped via website-5yo.10 through .12. User review of the shipped pages
> rejected the uniform-card landing ("this way everything looks the same") and
> asked for the IBM Think editorial layout. **Part II (sections 11 to 19,
> epic website-c6w, plus section 20, epic website-hnm) is the binding v2
> spec: where Part II conflicts with
> Part I, Part II wins.** Section 11 states the disposition of every Part I
> section explicitly. Part I is kept intact as the record of the shipped v1
> and for everything Part II leaves standing.

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

# Part I: v1 spec (shipped; superseded per section 11)

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

Composition, top to bottom, spanning the full `1fr` main column of the
shared `[1fr_20rem]` grid (the TOC rail keeps its `20rem` track where it
renders). **Amended by website-kbx.18 (2026-08-24), superseding
website-kbx.15's `max-w-5xl` shell / `max-w-2xl` reading column** (see the
amendment after item 9): no article-shell or reading-column width cap
applies; items 1 to 9, including `Related`, all render at the full main
column width.

1. **Breadcrumb**: mono caption voice (`font-mono text-xs uppercase
tracking-wider text-cc-ink-dim`), e.g. `Learn / Articles` with each
   ancestor a link. Blog posts render `Blog` (Option A) or `Learn / Blog`
   (Option B) here; the shell just renders what it is given.
2. **Kind chip row** (comparisons/explainers only): the tinted chip per
   section 6.1, plus for evergreen articles an "Updated {date}" line in the
   same mono voice. Blog posts omit this row.
3. **Hero image**: `Picture`, `aspect-video rounded-lg object-cover`,
   `priority`. Optional; explainers typically omit it.

   **Amended by website-kbx.7 (2026-08-24), width numbers superseded by
   website-kbx.15 (2026-08-24), kbx.18's height-cap treatment reverted
   pending a design call (2026-08-24), design call made and the width cap
   removed by website-kbx.22 (2026-08-24):** kbx.18 removed the
   reading-column width cap for the header and body, but a `max-h-[26rem]`
   plus `object-cover` height-cap treatment for the hero was found to crop
   roughly 11 of the 27 article heroes (title text or focal subjects cut
   off, in both directions depending on where the source art places them),
   regressing the same "must compose, not regress" requirement website-kbx.7
   was filed under, so kbx.18 shipped with the hero kept at kbx.7's original
   `max-w-3xl` width cap instead, pending Pascal's design call. **USER
   RULING (website-kbx.22, 2026-08-24) supersedes that pending state and
   the same-day 'keep 768px cap' answer recorded on kbx.18:** the hero goes
   full content column width like every other element in this list. The
   `max-w-3xl` cap is removed; the hero is a direct, unwrapped child of the
   full `1fr` main column,
   `aspect-video max-h-[27rem] w-full object-cover rounded-lg`. The
   `max-h-[27rem]` (432px) cap matches the pre-change rendered height
   exactly (a 768px-wide 16:9 hero was 432px tall; user ruling 2026-08-24)
   and keeps a 16:9 image usable once its width can reach 1344px
   (aspect-video alone would put it at roughly 756px tall). The crop consequence kbx.18
   reverted for is now accepted by the ruling: at full column width,
   `object-cover` center-crops the same roughly 11 of 27 heroes with
   baked-in title art. See the kbx.22 amendment after item 9 for the
   measurements and the accepted-consequence rationale.

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

**Amendment (website-kbx.15, 2026-08-24): reading measure and centering.**
User review of the shipped page flagged the `max-w-[46rem]` prose cap
(kbx.7/D5) as stale for the wide-container layout, with wrong-looking
padding. Measured on `/learn/articles/fusion-16-5` (a `kind: article` post,
the article shell's `2xl:grid-cols-[1fr_20rem]` TOC column present at 1920
and 2560, absent below `2xl`) with a standalone Playwright script (own
Chromium, explicit viewport), before any change:

| Viewport | Article shell (`max-w-5xl`)  | Old prose column (`max-w-[46rem]`, no `mx-auto`) | Side padding (viewport edge → prose)                      |
| -------- | ---------------------------- | ------------------------------------------------ | --------------------------------------------------------- |
| 1440     | 1024px, left=208, right=1232 | 736px, left=208 (flush with shell, not centered) | left 208px / right 208px (shell only; no TOC below `2xl`) |
| 1920     | 1024px, left=288, right=1312 | 736px, left=288 (flush)                          | left 288px / right 608px (includes the 320px TOC column)  |
| 2560     | 1024px, left=608, right=1632 | 736px, left=608 (flush)                          | left 608px / right 928px (includes the 320px TOC column)  |

Header/meta/body gaps (unaffected by this amendment, recorded for
completeness): breadcrumb → hero 24px (`Picture` `mt-6`), hero → title 40px,
title → meta row 16px (`h1 mb-4`), meta row → tags → body 75px aggregate
(`BlogTags` `my-6` plus its own row height). The old prose column was left
flush against the shell's left edge (no `mx-auto`), leaving a fixed 288px
dead gutter on the right of every paragraph, the concrete shape of the
"wrong padding" complaint.

**Measure math.** Body paragraphs render at `text-base leading-7`: 16px
font-size (`--text-body: 1rem` in `app/globals.css`), `system-ui` stack. The
`ch` unit for that font, measured in-browser via
`getComputedStyle` after setting an element to `width: 1ch`, resolves to
**9.140625px**. The old 736px (`max-w-[46rem]`) cap is 736 / 9.140625 ≈
**80.5ch**, above the 65 to 75ch target band this ticket set and above the
"~70 to 80ch" band D5 originally cited. The new cap is Tailwind's
`max-w-2xl` (42rem, 672px): 672 / 9.140625 ≈ **73.5ch**, inside the target
band, and a standard scale token rather than an arbitrary value.

**Layout change.** Items 1 to 8 above (breadcrumb through body) now render
inside one `<div className="mx-auto max-w-2xl">` wrapper nested in the
`max-w-5xl` article shell, instead of the shell rendering breadcrumb/kind
chip/title/meta/tags at the full 1024px shell width with only standfirst,
hero, and body separately (and inconsistently, none `mx-auto`) capped at
46rem. Effects:

- The reading column (breadcrumb through body, including the hero image)
  is one measure, `max-w-2xl` (672px, ~73.5ch), centered inside the shell:
  176px indent on each side at every measured viewport (672px column
  centered in the 1024px shell), instead of flush-left with a 288px dead
  gutter on the right only.
- The hero image no longer carries its own `max-w-[…]` cap (kbx.7's
  amendment above): it is a direct child of the `max-w-2xl` wrapper and so
  is always exactly the reading-column width, closing the "hero cap and
  prose cap can drift apart" gap that a duplicated arbitrary value left
  open.
- `Related` (item 9, `SimilarPosts` or a `CardGrid`) stays outside the
  `max-w-2xl` wrapper, at the full `max-w-5xl` (1024px) shell width, since
  it is a card grid, not running text, and needs the room (a `CardGrid
cols={3}` at 672px would run roughly 200px cards, well under the design
  system's card sizing elsewhere). This is the one place the shell's outer
  1024px width still does work; the two-tier shell (wide shell /
  narrower body measure) from D5 is kept for exactly this case, not
  removed.
- Vertical rhythm (the header/meta/body/related gaps in the before table)
  is unchanged: all values were already on the existing 4px spacing
  scale (`mt-10`/`mb-4`/`my-4`/`mt-6`/`mb-6`/`my-6`), so nothing there
  needed correction.

After, measured the same way at the same three viewports (all figures
identical across 1440/1920/2560 since the reading column's width no longer
depends on viewport, only its centering offset within the shell does):

| Viewport | Reading column (`max-w-2xl`, centered) | Indent inside shell (both sides) | Hero width |
| -------- | -------------------------------------- | -------------------------------- | ---------- |
| 1440     | 672px, left=384, right=1056            | 176px                            | 672px      |
| 1920     | 672px, left=464, right=1136            | 176px                            | 672px      |
| 2560     | 672px, left=784, right=1456            | 176px                            | 672px      |

TOC and share bar are unchanged by this amendment: `TableOfContents` stays
the `2xl` side rail (section 4.2, untouched), and `BlogShareBar` stays
inline in the meta row next to `BlogMetadata` (item 6) rather than becoming
a separate rail; the design doc never specified a standalone share rail, so
none was added.

**Amendment (website-kbx.18, 2026-08-24): full-width ruling, superseding
the kbx.15 reading measure above.** User ruling: article detail pages must
use the same width every other learn page gives its content, not a boxed
reading column. kbx.15's `max-w-2xl` (672px) column centered inside the
`max-w-5xl` shell is removed entirely, and so is the `max-w-5xl` shell
itself: items 1 through 9 (breadcrumb through `Related`) all render at the
shared `[1fr_20rem]` grid's `1fr` main column width, no `mx-auto`/`max-w-*`
wrapper anywhere in `ArticleLayout`. This matches the width `/learn`,
`/learn/browse`, and the other learn detail pages already give their
content; the two-tier shell/measure split kbx.7, D5, and kbx.15 each
iterated on is gone, not narrowed further.

Measured with a standalone Playwright script (own Chromium, explicit
viewport) on three representative articles, two `kind: article` migrated
posts (`/learn/articles/fusion-16-5`, `/learn/articles/directives-all-the-way-down`,
D5's own evidence page) and one comparison
(`/learn/articles/fusion-vs-apollo-router`; no `kind: explainer` content
exists in `content/learn/articles/` yet to check), at 1440/1920/2560:

| Viewport | Main column (`1fr`) | Body paragraph width | Indent (viewport edge → paragraph) | TOC rail |
| -------- | ------------------- | -------------------- | ---------------------------------- | -------- |
| 1440     | 1344px              | 1344px               | 48px                               | absent   |
| 1920     | 1280px              | 1280px               | 160px                              | present  |
| 2560     | 1280px              | 1280px               | 480px                              | present  |

Body paragraph width equals the main column width exactly at all three
viewports and across all three articles (no side padding is added inside
`article`; the only inset is the page shell's own `px-5 sm:px-12` plus its
`max-w-8xl` (100rem/1600px) container, matching the figures
learn-editorial.md section 12 gives for every other `/learn` page). Below
`2xl` (1440) there is no TOC rail, so the main column is the full container
width (1440 − 96px shell padding = 1344px); at `2xl` and above (1920, 2560)
the `20rem` (320px) TOC rail takes a fixed slice out of the `max-w-8xl`
container. At these two sampled viewports, both past the 1696px point
where the container hits its 1600px clamp, the main column holds steady
at 1280px, and the indent grows because the container centers inside the
wider viewport, not because the main column itself changes width; between
1536px and 1696px, where the clamp has not yet bound, the main column
instead scales with viewport width (see section 4.3 for the full range).
All three articles measured identically
at a given viewport, confirming the width comes from the shared grid, not
per-article content.

**Amendment (website-kbx.22, 2026-08-24): hero goes full column width,
superseding the `max-w-3xl` hero exception above.** User ruling: the hero
must span the same full content column width as the rest of the shell, not
a narrower boxed exception; this explicitly supersedes the same-day 'keep
768px cap' answer recorded on kbx.18. `Picture`'s `max-w-3xl` class is
removed; the hero is now a direct child of the `1fr` main column, matching
the widths above exactly (fusion-16-5, re-measured with the same standalone
Playwright script):

| Viewport | Hero width | Hero height (`max-h-[27rem]` cap) |
| -------- | ---------- | --------------------------------- |
| 1440     | 1344px     | 432px                             |
| 1920     | 1280px     | 432px                             |
| 2560     | 1280px     | 432px                             |

The height cap is `max-h-[27rem]` (432px), the exact rendered height of
the pre-change 768px-wide 16:9 hero (user ruling 2026-08-24, restoring the
literal "height cap unchanged" requirement over kbx.18's tried-and-reverted
26rem value): `aspect-video` alone would put a 1344px-wide hero at roughly
756px tall, so the cap plus `object-cover` keeps the page usable at the new
width. The cap binds at every column wider than 768px, so wide-column hero
images are center-cropped.

**Accepted consequence.** kbx.18 reverted this exact `max-h-[26rem]` +
`object-cover` full-width treatment because it cropped roughly 11 of the
27 article heroes: images with title text or a focal subject baked into
the art get their top or bottom edge cut by the center crop (verified
again on this pass against `hot-chocolate-16`, whose "HOT CHOCOLATE 16"
title arcs into the top and bottom edges of a 2560x1440 source and loses
part of both arcs to the crop at every sampled viewport). The kbx.22
ruling accepts this: full column width is the priority, and no per-image
re-crop or focal-point system is in scope for this change. A follow-up
ticket would be needed to re-crop or re-art the affected heroes if the
cropping proves unacceptable in practice.

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
- Measure: no width cap; breadcrumb through `Related` render at the full
  `1fr` main column width (below `2xl` the main column equals the
  container, viewport minus 96px shell padding, up to 1439px just under
  1536px; at `2xl` and above it is the container minus the 320px rail,
  1120px at a 1536px viewport rising to 1280px once the 1600px `max-w-8xl`
  clamp binds at viewports of 1696px and wider), per the section 4.1
  kbx.18 amendment, superseding D5's
  `max-w-[46rem]` (~80ch) figure and kbx.15's `max-w-2xl` (~73.5ch) figure
  both. Body paragraphs and list items scale up for the wider measure,
  `text-lg leading-8` (18px/32px) instead of the shared prose default's
  `text-base leading-7` (16px/28px), applied via `data-prose`-qualified
  selectors on the body wrapper so only MDX prose paragraphs and list items
  scale; components embedded in article MDX (`FeatureComparison`,
  `ComparisonVerdict`/`CheckList`) keep their own type sizes. Wide elements
  (comparison tables, code) scroll horizontally inside their own wrapper
  when they exceed the (now much wider) column, and no component
  implements a breakout today since none is needed.

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

---

# Part II: v2 editorial redesign (epic website-c6w, 2026-08-23)

v2 responds to the user's live review of the shipped v1 (epic website-c6w and
its comment): every item on the landing is the same card so nothing has
hierarchy, sections stack vertically instead of forming an editorial grid,
the topic nav is a pill row on the landing only, and the content column is
too narrow on widescreen. The binding layout reference is the user-supplied
ibm.com/think screenshot (2026-08-23): under the masthead a wide 3-column
band, LEFT a "Latest" column of compact vertical list items (small square
thumbnail, topic kicker in small text, 2-line title, author line, thin
divider between items), CENTER the featured story (large image, kicker,
display-size headline, dek, author), RIGHT a rail with a dark image promo
tile, a solid-color CTA banner tile with arrow, and a "Most popular" tag
cloud; columns separated by hairline rules; the band roughly 1600px wide on
desktop.

Component citations in Part II follow the Part I convention: unqualified
names exist in the repo (verified against `src/components/learn/` and
`app/(content)/learn/` on branch pse/adds-templates, 2026-08-23); new
components are marked **(new)**.

---

## 11. Disposition of every Part I section

Where a row says "superseded", the Part II section named in the row is the
spec and the Part I text is history. Where it says "stands", the shipped v1
surface is kept and Part II changes nothing about it beyond the container
and subnav that every /learn route inherits (sections 12 and 13).

| Part I section                    | Disposition under v2                                                                                                                                                                                                              |
| --------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1 Design direction                | **Amended.** The two-card-family rule (editorial `BlogTeaser` vs catalog `LearnCard`) is replaced by the five-treatment system (section 14). `BlogTeaser` leaves all /learn surfaces. The no-page-backdrop and token rules stand. |
| 2 Route map                       | **Stands.** Routes are as shipped: `/learn`, `/learn/browse`, `/learn/articles` (+ `/page/[page]`, `/tags/[tag]`), `/learn/articles/[slug]`, `/learn/templates/[slug]`.                                                           |
| 3.1 Masthead and topic subnav     | **Superseded** by sections 13 and 15.1. `LearnTopicNav` is removed outright; `LearnMasthead` leaves the landing and moves to `/learn/browse` (section 16.1).                                                                      |
| 3.2 Featured story hero           | **Superseded** by section 14.2. `LearnFeatureHero` is retired and replaced by `LearnFeaturedStory` inside the editorial band.                                                                                                     |
| 3.3 Latest                        | **Superseded** by sections 14.1 and 15.1. `LearnLatestSection` is retired; Latest becomes the compact list-row column of the band.                                                                                                |
| 3.4 Topic rails                   | **Amended** by section 15.2. `LearnTopicRail` stays but renders list rows only; the fixed teaser-plus-`LearnCard` mix is abolished.                                                                                               |
| 3.5 Collection band               | **Stands** (`LearnCollectionSection`). Plain `LearnCard`s remain the correct treatment for catalog items (section 14.5).                                                                                                          |
| 3.6 Explainers list               | **Stands** (`LearnExplainerList`), with the kind-filter data fix of cleanup item 17.1.                                                                                                                                            |
| 3.7 Videos                        | **Stands** (`LearnVideoSection`); videos keep plain `LearnCard`s (section 14.5).                                                                                                                                                  |
| 3.8 Subscribe band                | **Stands** as shipped (the single merged `LearnSubscribeBand`); it gains `id="subscribe"` as the subnav's Subscribe target (section 13.2).                                                                                        |
| 3.9 Responsive and motion summary | **Amended.** The uniform 1/2/3-column grid rule no longer describes the landing; the band's own collapse rules (section 15.1) and the motion rules of section 15.6 govern.                                                        |
| 4 Article reading page            | **Stands** (`ArticleLayout` as shipped). v2 adds only the subnav above it and the breadcrumb unification of cleanup item 17.5.                                                                                                    |
| 5 Browse entry points             | **Amended.** The "persistent" entry is now the subnav's Browse link on every /learn route; the pill-row entry is gone. Scoped and closing entries stand.                                                                          |
| 6 Comparison page layout          | **Stands.**                                                                                                                                                                                                                       |
| 7 Blog component dispositions     | **Amended.** `BlogTeaser`/`BlogTeaserGrid` are no longer used on any /learn surface (sections 14.5, 16.2). Their non-learn call sites are untouched.                                                                              |
| 8 Theme and token rules           | **Stands**, extended by section 14.6 for the new treatments.                                                                                                                                                                      |
| 9 Component inventory             | **Superseded** by section 18.                                                                                                                                                                                                     |
| 10 Non-goals                      | **Superseded** by section 19.                                                                                                                                                                                                     |

---

## 12. Wider learn container on widescreen

The rest of the site keeps its container. Only /learn routes widen.

- **Token**: add `--container-8xl: 100rem` to the `@theme` block in
  `app/globals.css`. 100rem = 1600px, matching the reference band's width.
- **Layout**: the learn routes move out of the shared `(content)` gutter
  into their own route group: `app/(content)/learn/*` becomes
  `app/(learn)/learn/*` with its own `app/(learn)/learn/layout.tsx`. That
  layout renders, in order: `LearnSubnav` **(new)** (full-bleed, section 13),
  then the gutter `<div className="px-5 py-8 sm:px-12"><div className="mx-auto
max-w-8xl">{children}</div></div>`. The existing `(content)` layout
  (`max-w-7xl`) is not touched; nothing outside /learn changes width.
- **Breakpoint behavior**: gutters stay identical to the rest of the site
  (`px-5`, `sm:px-12`). The container is fluid up to 100rem and centered
  beyond it, so /learn only differs from sibling pages once the viewport
  exceeds 1376px (1280px content + 96px gutters). Effective content widths:
  1280px viewport gives 1184px, 1440px gives 1344px, 1728px gives 1600px.
  Today the landing wastes 224px per side at 1728px (measured: 1280px
  container in a 1728px viewport).
- **Inner measures**: `ArticleLayout` no longer caps its own width (section
  4.1's kbx.18 amendment supersedes this line's original "unchanged, keeps
  `max-w-5xl`" claim); the article column fills this section's `max-w-8xl`
  gutter like every other `/learn` page, and reading pages simply gain
  rail/TOC breathing room. The articles index drops its double gutter
  instead of inheriting it (cleanup item 17.6).

---

## 13. Persistent subnav: `LearnSubnav` (new)

The Think-style second navigation bar, rendered by the learn layout on
**every** /learn route: landing, browse, articles index and its page/tag
variants, article pages, template pages.

### 13.1 Placement and sticky behavior

- Direct child of the layout, above the gutter, full viewport width.
- `sticky top-18 z-30`: the global header (`HeaderShell`) is `h-18 sticky
top-0 z-40`, so the subnav docks exactly under its 72px and stays below
  it in stacking order (header dropdowns and search overlay it).
- Height `h-12` (48px). Combined fixed chrome while scrolling: 120px.
- Surface echoes the header's sticky recipe without competing with it:
  `border-cc-card-border border-b bg-cc-card-bg backdrop-blur-[18px]
backdrop-saturate-150`. No inset highlight (that is the global header's
  signature line).
- The `/products/nitro` overlay special case in `HeaderShell` does not apply
  to /learn routes; no overlay variant exists for the subnav.

### 13.2 Anatomy

One row inside the learn gutter (`mx-auto max-w-8xl px-5 sm:px-12`), grid
`[auto_1fr_auto]`, items centered vertically:

1. **Learn wordmark**, pinned left: "Learn" in `font-heading text-cc-heading
font-semibold`, linking to `/learn`. This is the section identity; it
   replaces the landing masthead's job (section 15.1).
2. **Link row**, `text-sm`, in order: promoted topics **Hot Chocolate**
   (`/learn/browse?product=hot-chocolate`), **Fusion**
   (`/learn/browse?product=fusion`), **Nitro** (`/learn/browse?product=nitro`),
   then **Articles** (`/learn/articles`) and **Browse** (`/learn/browse`).
   Idle links `text-cc-ink-dim hover:text-cc-heading transition-colors`.
   Active state (route prefix match; for the three product links, `/learn/browse`
   with the matching `product` param): `text-cc-heading` plus a 2px
   `bg-cc-accent` underline bar sitting on the subnav's bottom border
   (tab-style, e.g. an absolutely positioned `bottom-0 h-0.5` span).
3. **Subscribe**, pinned right: a `text-cc-accent hover:text-cc-accent-hover
text-sm font-medium` link to `/learn#subscribe`. `LearnSubscribeBand`
   gains `id="subscribe"` with `scroll-mt-32` so the anchor lands below the
   120px of fixed chrome. From non-landing routes the link navigates to the
   landing anchor; no client state involved.

### 13.3 Mobile collapse

- Below `md`, the middle link row scrolls horizontally inside its own
  container: `overflow-x-auto whitespace-nowrap [scrollbar-width:none]
[&::-webkit-scrollbar]:hidden`. The wordmark and Subscribe stay pinned
  outside the scroller, so the two anchors of the bar (identity, primary
  action) are always visible. This fixes the shipped failure where the
  overflow row hid "Browse all" entirely (cleanup item 17.3).
- The subnav stays sticky on mobile; at 48px it is cheap, and it is the only
  learn navigation on small screens (the global `MobileNav` hamburger does
  not know about learn subsections).

### 13.3.1 kbx.12 amendment: gutter alignment and mobile edge-fade

User review (2026-08-24) found two shipped defects in the anatomy above:

1. **Desktop misalignment.** The subnav row is split into the same two
   nodes, in the same order, as the content gutter: an outer
   `px-5 sm:px-12` div with no width cap, wrapping an inner
   `max-w-8xl mx-auto` div carrying the row's grid. This makes the wordmark's
   left edge and the content's left edge coincide exactly at every
   viewport, verified at 2560/1920/1440/768/390.
2. **Mobile clipping.** The link row (`LearnSubnavScroller`) fades only the
   side that actually has more content to scroll toward: a left fade
   renders when `scrollLeft > 0`, a right fade renders when
   `scrollLeft + clientWidth < scrollWidth`, and neither renders once
   everything fits, so a fully visible first or last item is never dimmed.
   The fade state is recomputed on scroll and on resize via
   `ResizeObserver`. `scroll-padding-inline: 24px` on the scroller keeps a
   keyboard-focused link clear of an active fade. The wordmark and Subscribe
   stay pinned outside the scroller as in 13.3, since both are already
   reachable without scrolling, satisfying "Subscribe reachable at the row
   end" without folding it into the scroll track.

### 13.4 Relationship to `LearnTopicNav`

`LearnTopicNav` (`src/components/learn/LearnTopicNav.tsx`) is **removed**,
file deleted, not restyled: its browse-entry job moves to the subnav's
Browse link, its product topics move to the promoted links, and its two
non-product topics (GraphQL fundamentals, AI and agents) survive as landing
topic sections (15.2) and in the tag cloud, not as navigation. The five-topic
`TOPICS` table and mapping helpers in `editorial.ts` stay: topic sections
still consume them.

---

## 14. Treatment system: five treatments replace one-card-fits-all

The v1 rule "two card families" produced the rejected sameness: on the
shipped landing every one of the seven sections is a header row plus a
3-column grid of near-identical bordered cards. v2 assigns each content role
its own form. Importance reads through size and placement, not through
another border radius.

### 14.1 Compact list row: `LearnListRow` (new)

The workhorse for articles in the Latest column, topic sections, and the
articles index. One row, whole row a single link, **no card surface, no
border box, no CTA label**:

- Grid `[auto_1fr] gap-4 items-start`. Left: square thumbnail from the
  post's `featuredImage` via `Picture`, `size-20 rounded-lg border
border-cc-ink-faint object-cover`; posts without an image render a
  `bg-cc-white/4` square with the post's product `DrinkIcon` centered (the
  established fallback iconography, never an empty box).
- Right, stacked: kicker in the mono caption voice (`font-mono text-xs
uppercase tracking-wider text-cc-ink-dim`) carrying the post category
  (fallback: primary topic label); title `text-cc-heading font-medium
leading-snug line-clamp-2 transition-colors`, on row hover
  `text-cc-accent`; author line `text-cc-ink-dim text-sm` as
  "{author} · {MMM d, yyyy}" (single date, no avatar at this size).
- Rows separate with thin dividers: the list container uses `divide-y
divide-cc-ink-faint`, rows `py-4`. No hover translate; the title color
  shift is the whole affordance.

### 14.2 Featured story: `LearnFeaturedStory` (new), retires `LearnFeatureHero`

Exactly one per page (landing band center; articles index page 1 lead). An
open editorial composition, not a boxed panel: the v1 hero's card chrome
(`bg-cc-white/2.5` panel, border, hover translate) is dropped so the story
reads bigger than everything, not "same card, larger".

`LearnFeaturedStory` takes a `layout` prop (kbx.14 amendment): `stacked`
(default) is used by the landing and topic-hub editorial band center
column; `split` is used by the articles index page 1 lead so the title
stays above the fold at 1440 and up.

- Stacked: image (`aspect-video rounded-2xl border border-cc-ink-faint
object-cover`, `priority` on the landing), then kicker row (`Eyebrow`
  "Featured" in accent plus the category chip in the established mono chip
  style), then headline `font-heading text-cc-heading font-semibold
text-balance text-h4 sm:text-h3 xl:text-h2`, then dek `text-cc-ink-dim
text-lg line-clamp-3`, then one byline row (avatar `Picture` 30px, author,
  "·", full date). The v1 hero printed the date twice (meta row and byline);
  v2 prints it once, in the byline only.
- Split (kbx.14): below `lg` identical to stacked; from `lg` up the whole
  link becomes `lg:flex-row lg:items-center lg:gap-10`, image capped to
  `lg:w-[45%] lg:h-[22rem] lg:aspect-auto lg:shrink-0`, text column
  `lg:min-w-0 lg:flex-1` with the kicker row's `mt-6` dropped (`lg:mt-0`).
  Mirrors the kbx.7 detail-page hero-cap principle expressed as a
  side-by-side split.
- Whole composition one `Link`; hover shifts the headline to
  `text-cc-accent`. No translate, no border brightening (there is no
  border).
- Fallback (no post has `featuredImage`): render without the image block,
  headline first. Never empty.

### 14.3 Promo tile: `LearnPromoTile` (new)

The curated right-rail unit, two variants on one component:

- **Image variant** (the Think dark image tile): `relative overflow-hidden
rounded-2xl border border-cc-ink-faint aspect-[4/3]`, `Picture` filling
  with `object-cover`, a bottom-up scrim `bg-gradient-to-t
from-cc-surface/85 via-cc-surface/40 to-transparent`, and content pinned
  bottom-left over the scrim: mono kicker, title `text-cc-heading
font-heading font-semibold`, optional author line. Whole tile one link.
- **CTA banner variant** (the Think solid-color tile with arrow):
  `rounded-2xl bg-cc-accent p-6 text-cc-surface`, mono kicker at reduced
  opacity, one-line title in `font-heading font-semibold`, and
  `ArrowRightIcon` bottom-right with the standard `group-hover:translate-x-1`.
  This is the only solid-accent surface in the learn system; it is reserved
  for one curated action per page (initially Subscribe, see 15.1).

Tile content is editorial data passed as props (kicker, title, href, image);
components never pick their own content.

### 14.4 Tag cloud: `LearnTagCloud` (new)

The "Most popular" rail unit: a mono-caption heading ("Most popular") over a
`flex flex-wrap gap-2` of `Tag` components (the existing design-system pill,
already the facet-bar voice), each linking to `/learn/articles/tags/[tag]`.
Data: the most frequent tags across `listBlogPostSummaries()` (top 10 to 12),
computed by a small helper in `editorial.ts` at build time.

### 14.5 Plain cards: where `LearnCard` legitimately remains

`LearnCard` stays exactly as shipped for the five catalog types (template,
video, tutorial, example, workshop): the `/learn/browse` catalog, the
"Start building" collection band, the "Watch" section, and the template
detail's "More from Learn" grid. These are "use this" objects where a
uniform comparable card is the right form. `LearnCard` is never used for
articles (it never was; the article-side offender was the uniform
`BlogTeaser` grid), and `BlogTeaser`/`BlogTeaserGrid` now leave every /learn
surface: Latest and topic rails render `LearnListRow`s, the articles index
renders section 16.2. `BlogTeaser`'s non-learn call sites are untouched.

**CTA rule after commit 9857016d8f** (LearnCard CTA changed from per-type
accent to `text-cc-accent`, user ruling): that rule stands and stays
`LearnCard`-only. The new article treatments carry **no per-item CTA label
at all**: list rows and the featured story are title-led links whose hover
accent is the affordance, and the promo tile's arrow belongs to its CTA
banner variant. Nothing in v2 adopts a "Read" label, so the accent-CTA rule
neither spreads nor conflicts; it lives on wherever `LearnCard` renders.

### 14.6 Token rules for the new treatments

The site is single-theme dark (Part I section 8 stands; there is no light
mode to cover, only the `@media print` palette). Rules for the new pieces:

- Every color is a `cc-*` token or a token with an opacity modifier. The
  promo scrim uses `cc-surface` alphas (defined for exactly this purpose in
  `globals.css`); the CTA banner uses `bg-cc-accent` with `text-cc-surface`
  (the same pairing `Agent-ready`'s `bg-cc-warning text-cc-surface` chip
  already established for on-accent text). No new literals; the single
  sanctioned literal remains `bg-[#f5f0ea]` behind `STACK_ICONS`.
- Hairline rules between band columns: `border-cc-card-border`. Row
  dividers: `divide-cc-ink-faint`. Imagery keeps `border-cc-ink-faint`
  frames so bright thumbnails stay seated on the dark surface.

---

## 15. Landing v2: `/learn`

Top to bottom: subnav (section 13), editorial band, topic sections, "Start
building", "Explainers", "Watch", subscribe band. The page h1 becomes
visually hidden (`sr-only` "Learn ChilliCream"): the subnav wordmark carries
the visible identity, and the front page leads with content, which is the
Think gesture the masthead only imitated. `LearnMasthead` leaves this page
(it moves to `/learn/browse`, section 16.1).

### 15.1 Editorial band: `LearnEditorialBand` (new)

Replaces the shipped `LearnFeatureHero` + `LearnLatestSection` pair. One
component owning the three-column grid; content arrives as props from
`page.tsx`.

**Grid definition:**

- `xl` and up (1280px, where the container is at least 1184px):
  `xl:grid-cols-[minmax(14rem,19rem)_minmax(37.5rem,1fr)_minmax(14rem,19rem)]`.
  The 37.5rem (600px) center minimum is the featured story's floor: at a
  1280px viewport the side columns compress below their 19rem maximum to
  honor it, reaching full width from roughly a 1300px viewport.
  Column order: Latest, Featured, rail. Hairline rules between columns: the
  center and right columns take `xl:border-l xl:border-cc-card-border` with
  `xl:px-8`; the first column `xl:pr-8` (no gap property; the rules own the
  spacing, per the reference).
- `lg` to below `xl`: `lg:grid-cols-[minmax(0,1fr)_19rem]` with `gap-x-8`:
  Featured left, rail right; Latest drops below the pair as a full-width
  two-column list (`lg:columns` none, plain `sm:grid-cols-2 gap-x-10` of
  rows). Below `xl` even minimum-width side columns would squeeze the
  featured story under its 600px floor, thinner than a plain article card,
  so the band refuses three columns.
- Below `lg`: one column, order Featured, Latest, rail. The two promo tiles
  sit side by side at `sm` (`sm:grid-cols-2`) with the tag cloud full-width
  under them; below `sm` everything stacks.

**Column content:**

- **Latest (left)**: mono-caption column heading "Latest", then the 5
  newest posts (excluding the featured story) as `LearnListRow`s, then an
  `ArrowLink` "All articles" to `/learn/articles`.
- **Featured (center)**: `LearnFeaturedStory` with the newest post carrying
  a `featuredImage` (the shipped `getLatestBlogPost()` heuristic,
  editorially overridable later).
- **Rail (right)**, top to bottom: one `LearnPromoTile` image variant (a
  curated pick, initially the newest Release-category post not already in
  the band, chosen in `page.tsx`); one `LearnPromoTile` CTA banner
  ("Subscribe", "Never miss a release", `/learn#subscribe`); one
  `LearnTagCloud`.

**Dedupe rule:** a post appears at most once in the band, and posts shown in
the band are excluded from every topic section below. The shipped landing
shows Fusion 16.5 and Federated Event Streams up to four times each
(Latest, GraphQL fundamentals, Hot Chocolate, Federation and Fusion rails,
observed 2026-08-23); v2 makes that structurally impossible.

### 15.2 Topic sections: `LearnTopicRail` restyled

The rail keeps its name, header row (`text-h5 sm:text-h4` heading plus
`ArrowLink` "More {topic}"), and the `TOPICS` mapping, but its body becomes
4 `LearnListRow`s in `lg:grid-cols-2 gap-x-10` (single column below `lg`).
The `TopicRailSlot` union and the teaser-plus-`LearnCard` mix die: catalog
items no longer appear inside topic sections (that was the mis-filed
"Fusion 3-Service Federation template inside the Hot Chocolate rail"
problem; catalog items are reachable through the section's More link and the
collection band). A topic section renders only when it still has 3 or more
posts after the band dedupe; otherwise it is omitted, per the strategy's
no-empty-rails rule.

**Amended by website-kbx.28 (2026-08-24), shape A:** the lead-feature grid
(learn-harmonization.md D7/2.5.2) moves from `lg:grid-cols-2` to
`lg:grid-cols-3`. The lead occupies one column: its image is
a fixed `aspect-video` (16:9) box rather than a fixed-height crop, so it
never grows wider than that one column and never crops artwork that is
itself authored at 16:9 (the shipped `h-40` box let the image stretch to
half the container's width at wide viewports while holding height fixed,
over-cropping 16:9 source art). The remaining up to 3 `LearnListRow`s
(`density="compact"`) fill the other two columns as a single `lg:col-span-2`
stack, not a second row of cards, so the two visual columns (lead vs. rows)
stay balanced without needing to split an odd row count across two card
columns. Shape B (uniform N-up cards, lead distinction dropped) was
rejected: it would discard the kbx.20 hierarchy work (section h2 > rail
lead `text-h5` > row `text-h6`) for no stated benefit. Below `lg` the grid
collapses to one column, lead first. Applies to every `LearnTopicRail`
instance, including future topic hub page usage.

**Amended by website-446 (2026-08-29), direction A:** the kbx.28 shape (the
lead in one column of `lg:grid-cols-3`, up to 3 `LearnListRow`s
`density="compact"` filling the other two as an `lg:col-span-2` stack) is
replaced entirely. The rail now renders one `LearnArticleCard
layout="split"` lead across the full content width (16:9 image at 45% of
the row from `lg`, 40% from `xl`, `text-h5` headline, dek, `author ·
date`), followed by the remaining posts as `LearnArticleCard`s in a
`sm:grid-cols-2 lg:grid-cols-3` grid (16:9 image, kicker, `text-h6`
headline, `author · date`). No rows remain inside a topic rail;
`LearnListRow`'s `density="compact"` stays in use only by the editorial
band (15.1). See learn-harmonization.md section 2.5 item 2.

### 15.3 Collection, explainers, videos

"Start building" (`LearnCollectionSection`), "Explainers"
(`LearnExplainerList`), and "Watch" (`LearnVideoSection`) stand as shipped,
with one data fix: the landing passes `listArticlesByKind("explainer")` plus
`listArticlesByKind("comparison")` to `LearnExplainerList` instead of
`listArticleSummaries()` (cleanup item 17.1).

### 15.4 Subscribe band

`LearnSubscribeBand` stands as shipped and gains `id="subscribe"` and
`scroll-mt-32` (section 13.2).

### 15.5 Section rhythm

Sections keep the `border-cc-card-border border-t py-14 sm:py-20` rhythm,
except the editorial band, which sits directly under the subnav with `pt-8
sm:pt-10` and no top border (the subnav's bottom border is the rule above
it).

### 15.6 Motion

Card hovers stay as established where cards remain (`LearnCard`
`-translate-y-1`). The new treatments move nothing: list rows, the featured
story, and promo tiles animate color only (`transition-colors`), plus the
CTA banner's arrow `translate-x-1`. No carousels, no auto-advance.

---

## 16. Browse, articles index, and reading pages under v2

### 16.1 `/learn/browse`

- Inherits subnav and the wider container. The centered `PageHero` (which
  burns roughly 350px of viewport before content and speaks the display
  voice the landing no longer uses) is replaced by `LearnMasthead`,
  relocated from the landing: left-aligned eyebrow "Learn", title "Browse
  the catalog", one-sentence teaser. The separate breadcrumb row above it is
  dropped; the subnav marks the place (cleanup item 17.7).
- `LearnCatalog` and `LearnFacetBar` are unchanged in behavior. The result
  grid gains a fourth column at `xl` so cards keep their designed width in
  the 1600px container: extend `CardGrid` with a `cols: 4` option
  (`sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4` when `step="progressive"`),
  used by the catalog grid and its skeleton fallback only.
- Template detail pages (`TemplateDetail`) are unchanged apart from subnav,
  container, and breadcrumb style (17.5).

### 16.2 `/learn/articles` (and `/page/[page]`, `/tags/[tag]`)

The uniform `BlogTeaserGrid` page is replaced by the editorial treatments
(this is the ticket website-c6w.4 rollout):

- Page 1: h1 "Articles" (`ArticleBreadcrumb` superseded by website-xwu
  (2026-08-30): breadcrumbs removed from all /learn pages per user ruling),
  then `LearnFeaturedStory` with the newest post, then the remaining posts
  of the page as `LearnListRow`s in `lg:grid-cols-2 gap-x-10`, then the
  existing design-system `Pagination`.
- Pages 2+: same shell, rows only (the featured treatment appears once per
  surface, and only where the content is actually newest).
- Tag pages: heading plus rows, no featured story.
- `BlogIndexShell` is no longer used under /learn (it keeps its non-learn
  call sites). This also removes the shipped double gutter, cleanup 17.6.

### 16.3 `/learn/articles/[slug]`

**Amended by learn-harmonization.md D5 and website-kbx.18 (2026-08-24):**
`ArticleLayout` no longer stands as shipped; it has no article-column width
cap (section 4.1's kbx.18 amendment). v2 adds the subnav above it and
unifies the breadcrumb style (17.5). The wider container affects the
whitespace around the article column (now the full `max-w-8xl` gutter, not
a `max-w-5xl` inner cap) and the `2xl` TOC rail.

---

## 17. UI cleanup list

From walking the live pages on a dev server (branch pse/adds-templates,
2026-08-23) at 1728px, 834px, and 390px. Each item names the page, the
concrete defect, and the fix. Items 17.1 and 17.5 to 17.8 are standalone
fixes; the rest are absorbed by the v2 structures above.

1. **/learn, "Explainers" section lists all 28 articles.**
   `app/(content)/learn/page.tsx` passes `listArticleSummaries()` (every
   article, 27 of kind `article` plus 1 comparison) into
   `LearnExplainerList`, so the landing renders a 28-row two-column wall of
   every blog post with kind chips, swamping the sections around it. Part I
   section 3.6 specified explainer/comparison kinds only. Fix: pass
   `listArticlesByKind("explainer")` and `listArticlesByKind("comparison")`;
   with today's corpus the section renders 1 row.
2. **/learn, the same post appears up to 4 times.** Desktop walk: "Fusion
   16.5" and "Introducing Federated Event Streams" each appear in Latest,
   GraphQL fundamentals, Hot Chocolate, and Federation and Fusion;
   "Agents, Federation, and a Community" three times. Sections are
   independently "newest first" with no cross-section dedupe. Fix: the band
   dedupe rule and topic-section exclusion of section 15.1/15.2.
3. **/learn at 834px and 390px, primary browse entry invisible.** The topic
   pill row (`LearnTopicNav`, `overflow-x-auto`) overflows: "Browse all"
   (`ml-auto`) scrolls out of the viewport entirely, and the overflow
   container paints a permanent horizontal scrollbar track under the pills.
   Fix: `LearnTopicNav` removed; the subnav (13.3) keeps Browse and
   Subscribe pinned and hides scrollbars on its scroller.
4. **/learn, duplicate chrome in the head.** Eyebrow "LEARN" over the title
   "Learn ChilliCream" says the word twice; `LearnFeatureHero` prints the
   date twice (kicker row "AUG 2026" and byline "Aug 3, 2026"). Fix:
   masthead leaves the landing (15); `LearnFeaturedStory` prints the date
   once, in the byline (14.2).
5. **Two breadcrumb styles across sibling /learn pages.** Superseded by
   website-xwu (2026-08-30): `ArticleBreadcrumb` and all breadcrumbs were
   removed from /learn pages per user ruling.
6. **/learn/articles, double gutter and off-spec width.** The page nests
   `BlogIndexShell` (own `px-5 sm:px-12` + `max-w-6xl`) inside the
   `(content)` layout (own `px-5 sm:px-12` + `max-w-7xl`): measured 1152px
   grid in a 1280px container at 1728px viewport, and 40px of left padding
   on a 390px phone where every sibling page has 20px. Fix: 16.2 rebuilds
   the page without `BlogIndexShell` inside the learn layout's single
   gutter.
7. **/learn/browse, oversized centered hero.** The full `PageHero` centers
   an eyebrow, display title, and teaser across roughly 350px before any
   content, while the landing speaks left-aligned; the tiny breadcrumb
   "Learn / Browse" floats disconnected above it and repeats the eyebrow's
   "LEARN". Fix: compact left-aligned `LearnMasthead`, breadcrumb dropped
   (16.1).
8. **/learn Latest and articles index, misaligned teaser meta rows.**
   `BlogTeaser` renders the category chip (`py-1.5` pill) only when a
   category exists, so chip-less cards ("Open Your GraphQL API for the
   REST", "Introducing skillz") start their date line and title roughly 8px
   higher than neighbors with chips ("Newsletter May 2026"), breaking the
   row's baseline. Moot on /learn once teasers leave (14.5); `LearnListRow`
   always renders its kicker line (category with topic fallback), so rows
   align by construction.
9. **/learn topic rails, mixed families and mis-filed items.** `BlogTeaser`
   and `LearnCard` share rows with different surfaces, padding, and CTA
   voices ("READ" uppercase mono vs "View template" accent), and product
   mapping files the "Fusion 3-Service Federation" template into the Hot
   Chocolate rail. Fix: section 15.2 (rows only, no catalog items in topic
   sections).
10. **/learn, monotonous section rhythm.** Seven consecutive sections use
    the identical header-plus-arrow row over a 3-column card grid, the
    core "everything looks the same" complaint. Fix: the treatment system
    (14) and band (15.1); the remaining card grids ("Start building",
    "Watch") are exactly the surfaces where uniform cards are intended.

---

## 18. Component inventory v2

### Removed (files deleted)

| Component                | Replaced by                                      |
| ------------------------ | ------------------------------------------------ |
| `LearnTopicNav.tsx`      | `LearnSubnav` (section 13)                       |
| `LearnFeatureHero.tsx`   | `LearnFeaturedStory` (section 14.2)              |
| `LearnLatestSection.tsx` | Latest column inside `LearnEditorialBand` (15.1) |

### Changed

| Component / module       | Change                                                                           |
| ------------------------ | -------------------------------------------------------------------------------- |
| `LearnTopicRail.tsx`     | Body becomes `LearnListRow`s; `TopicRailSlot` union removed (15.2)               |
| `LearnMasthead.tsx`      | Leaves the landing; renders on `/learn/browse` replacing `PageHero` there (16.1) |
| `LearnExplainerList.tsx` | Unchanged visually; callers pass kind-filtered articles (17.1)                   |
| `LearnSubscribeBand.tsx` | Gains `id="subscribe"` and `scroll-mt-32` (13.2)                                 |
| `CardGrid.tsx`           | Gains `cols: 4` progressive step for the browse catalog at `xl` (16.1)           |
| `editorial.ts`           | Gains the popular-tags helper (14.4); topic mapping stays                        |
| `app/(content)/learn/*`  | Moves to `app/(learn)/learn/*` with the learn layout (12)                        |
| `app/globals.css`        | Gains `--container-8xl: 100rem` (12)                                             |

### New (all under `src/components/learn/`)

| Component                | Responsibility                                |
| ------------------------ | --------------------------------------------- |
| `LearnSubnav.tsx`        | Persistent second navigation bar (section 13) |
| `LearnEditorialBand.tsx` | Three-column Latest/Featured/rail grid (15.1) |
| `LearnListRow.tsx`       | Compact article list row (14.1)               |
| `LearnFeaturedStory.tsx` | Featured story treatment (14.2)               |
| `LearnPromoTile.tsx`     | Image promo and CTA banner tiles (14.3)       |
| `LearnTagCloud.tsx`      | "Most popular" tag cloud (14.4)               |

Unchanged and still load-bearing: `LearnCard`, `ContentTypeBadge`,
`contentTypeMeta`, `LearnCatalog`, `LearnFacetBar`, `LearnCardSkeleton`,
`LearnEmptyState`, `LearnClosing`, `LearnCollectionSection`,
`LearnVideoSection`, `ArticleLayout`, `ComparisonVerdict`, `TemplateDetail`,
`TemplateStackArt`, `learnItemHref`, `productArt`, `stackIcons`. All new
components are server components; the subnav's active-state needs the
current pathname and is the one new client component (`usePathname`, like
`HeaderShell`).

---

## 19. Ticket mapping and non-goals

- **website-c6w.2** implements section 13 (subnav) plus the route-group move
  of section 12 that hosts it.
- **website-c6w.3** implements sections 12 and 15.1 (container, editorial
  band) and the landing cleanups 17.1, 17.2, 17.4.
- **website-c6w.4** implements sections 15.2, 16.1, 16.2 (treatment rollout
  to topic sections, browse masthead/grid, articles index) and cleanups
  17.5 to 17.8. Cleanups 17.9 and 17.10 are not standalone items: 17.9 is
  absorbed by section 15.2 and 17.10 by sections 14 and 15.1.
- **website-hnm.1** implements section 20 (video detail pages under
  `/learn/videos/[slug]`, part of the TV-migration epic website-hnm), after
  its sibling data task lands the extended `VideoItem` shape (20.1).

Non-goals of v2: any change to the global header, footer, or non-learn
routes; a light theme (the site is single-theme dark); newsletter backend
mechanics; `/learn/topics/[topic]` surfaces; changes to article body
typography or the comparison genre structure (Part I sections 4 and 6
stand); content edits.

---

## 20. Video detail page: `/learn/videos/[slug]` (epic website-hnm, 2026-08-23)

The TV migration (epic website-hnm) folds tv.chillicream.com into /learn:
videos stop being external YouTube links and get native detail pages, so
the TV UI can be retired. This section designs that page; website-hnm.1
implements it. The route map (section 2) gains one row:
`/learn/videos/[slug]`, video detail, designed here. Everything else in
Part II stands unchanged; component citations follow the Part II
convention (unqualified names verified on branch pse/adds-templates,
2026-08-23).

### 20.1 Data contract and href retargeting

The page designs against the extended `VideoItem` in
`src/data/learn/types.ts`, seeded by website-hnm.1's sibling data task. On
top of the shipped shape (base fields plus `url`, `duration`, optional
`level`), the extension adds:

- `youtubeId`: the 11-character YouTube video id (same contract as
  `YouTubeVideo`'s `videoId` prop, validated by its `ID_RE`).
- `description`: long-form body as plain paragraphs (`readonly string[]`,
  the `TemplateSection.paragraphs` precedent). The TV descriptions'
  boilerplate social/footer block is stripped at data entry, never at
  render; bare URLs are linkified at data entry.
- `publishedAt`: ISO date of the YouTube publish, formatted at render via
  `formatDate` (`src/helpers/formatDate.ts`).
- `exampleUrl` (optional): direct URL of the free example-code asset.

`LearnItemSummary` keeps working unchanged: a video's summary is the full
item by definition (types.ts), so no summary type changes.

**Href retargeting**: the `video` case of `learnItemHref`
(`src/components/learn/learnItemHref.ts`) changes from `item.url` to
`/learn/videos/${item.slug}`. Every video `LearnCard` sitewide (the
"Watch" section, the browse catalog, related grids) turns internal;
`LearnCard` then renders the standard `ArrowRightIcon` instead of the
external arrow on its own, no card change. `url` stays the canonical
YouTube watch URL, used only by the "Watch on YouTube" link (20.4) and the
structured data (20.7).

### 20.2 Page anatomy and header

Route file `app/(learn)/learn/videos/[slug]/page.tsx`, inheriting
`LearnSubnav` and the `max-w-8xl` gutter from the learn layout (sections
12 and 13; no subnav link is active on this route, as on template detail).
`generateStaticParams` iterates `VIDEO_ITEMS`
(`src/data/learn/content.ts`). The page composes `LearnVideoDetail`
**(new)**, the `TemplateDetail` sibling: the page loads data and picks
related items, the component renders props.

Header (kbx.19 amendment, superseding the original kind-row layout): the
page opens at the shared 32px subnav-to-content rhythm (no extra header
`py`), top to bottom:

1. **Breadcrumb**: superseded by website-xwu (2026-08-30):
   `ArticleBreadcrumb` and all /learn breadcrumbs were removed per user
   ruling.
2. **Title**: `h1` in the `TemplateDetail` recipe (`font-heading
text-cc-heading text-h3 sm:text-h2 font-semibold tracking-[-0.02em]
text-balance`), at the top of the header: no eyebrow, no
   `ContentTypeBadge`, no topic kicker, no `level` tag above the title
   (kbx.19; consistent with the hub header treatment of kbx.10).
3. **Standfirst**: the `tagline`, `text-cc-prose mt-5 max-w-2xl text-lg
leading-relaxed` (template header recipe).

All metadata lives in the rail beside the player (section 20.4): published
date, duration and level where present, topic links to the
`/learn/topics/<slug>` hubs, products, and the example download button as
the rail's primary action. The date prints exactly once on the page (rules
14.2 and 17.4).

The header has no buttons: the play affordance is the facade itself and
the download lives in the example card (20.4). Repeating either here would
be the duplicate chrome cleanup 17.4 exists to prevent.

### 20.3 Player: `LearnVideoPlayer` (new)

Click-to-load only, never an eager iframe, per the shipped facade
convention:

- Reuses `VideoFacade` (`src/components/VideoFacade.tsx`) as-is: poster
  behind the play button, iframe mounted on click against
  `youtube-nocookie.com` with autoplay. The play button keeps its
  established treatment (`bg-cc-black/70`, hover `bg-cc-youtube`);
  `playlabel` is "Play {title}".
- `LearnVideoPlayer` **(new)** is a thin server wrapper that exists
  because `YouTubeVideo`'s frame is article chrome (`my-6 rounded-md
ring-1`) while learn imagery uses the `rounded-2xl border
border-cc-ink-faint overflow-hidden` frame (section 14.6). Poster
  resolution is identical to `YouTubeVideo` (self-hosted optimized
  `maxresdefault` via `getOptimizedImage`, external `hqdefault` fallback,
  `BrokenMedia` on a malformed id); website-hnm.1 extracts that poster
  block from `YouTubeVideo` into a shared helper rather than duplicating
  it. No visual change to `YouTubeVideo`'s call sites.

### 20.4 Body layout: embed area vs description column

The body reuses the `TemplateDetail` detail grid: `border-cc-card-border
grid gap-12 border-t py-12 lg:grid-cols-[minmax(0,1fr)_19rem] lg:gap-16`.

- **Desktop (`lg` and up)**: left column (`min-w-0`) is the player, then
  the description; right column is the 19rem aside (`sticky top-28`, the
  template aside recipe) carrying the example card then the facts list.
  The player fills the fluid left column: roughly 900px wide at a 1280px
  viewport up to about 1250px in the full 100rem container, so a 16:9
  embed stays comfortably inside the fold.
- **Tablet and mobile (below `lg`)**: one column in DOM order player, then
  the whole metadata rail (download button first), then the description
  (kbx.19 amendment: the rail stacks as one unit before the description so
  the free download never sinks below long prose; grid `order` utilities,
  no duplicated render slots).

**Description**: paragraphs as `text-cc-prose leading-7` with `space-y-4`,
in a `max-w-3xl` measure inside the left column so the prose line length
stays readable under the wide player. Links inside paragraphs render as
standard prose links. No MDX pipeline: the description is plain data on
`VideoItem`, matching the `TemplateSection` precedent.

**Example card** (rendered only when `exampleUrl` is set): the prominent
free-download affordance.

- Surface: the template aside recipe, `border-cc-card-border bg-cc-card-bg
rounded-2xl border p-5 backdrop-blur-sm`.
- Content: heading "Example code" (`text-cc-heading font-heading text-lg
font-semibold`), one sentence ("The complete project built in this
  video."), a full-width `SolidButton` "Download example" linking
  `exampleUrl` directly, and a `text-cc-ink-dim text-sm` caption "Free
  download, no signup". There is no gate of any kind: TV's paywall was
  dropped by user ruling, so the button is a plain link, no email capture,
  no interstitial.

**Facts list**: below the example card (or alone, 20.6), a `dl` in the
`TemplateDetail` `Detail` voice (mono uppercase `dt`, `text-cc-ink` `dd`):
Products, Duration, Level (when set), Published. Under it, "Watch on
YouTube" as an `ArrowLink` with `target="_blank"` (the `LearnVideoSection`
header's established external-link form) targeting `url`.

### 20.5 Related rail

Closing section in the `TemplateDetail` "More from Learn" recipe:
`border-cc-card-border border-t py-16 sm:py-24`, heading "More to watch"
(`font-heading text-h4 sm:text-h3 font-semibold`), `CardGrid cols={3}
step="progressive" itemsStretch` of plain `LearnCard`s (section 14.5:
catalog items keep the uniform card; the cards are now internal links per
20.1). Selection happens in `page.tsx`, never in the component: other
videos sharing a product, newest first; padded to 3 with the newest
remaining videos; if still short, with non-video learn items sharing a
product (templates first); the current video always excluded.

### 20.6 Empty states

- **No `exampleUrl`**: the example card is omitted entirely; the aside is
  the facts list alone, and below `lg` the facts render after the
  description (only the prominent download earns the above-description
  slot).
- **No related items** (possible only when the catalog is nearly empty):
  the whole related section is omitted, per the no-empty-rails rule. It
  never renders placeholder cards.
- **Empty description**: the left column is the player alone; the grid and
  aside are unchanged.
- **Poster failures** are handled inside the player: `hqdefault` external
  fallback when no optimized poster exists, `BrokenMedia` for a malformed
  id.

### 20.7 Structured data and metadata

- The page emits `VideoObject` JSON-LD (the sibling of the template page's
  existing JSON-LD block): `name` (title), `description` (tagline),
  `thumbnailUrl` (the resolved poster), `uploadDate` (`publishedAt`),
  `duration` converted to ISO 8601 (`PT51M49S` from `"51:49"`),
  `embedUrl` (`https://www.youtube-nocookie.com/embed/{youtubeId}`), and
  `url` (the canonical page URL). Plus a `BreadcrumbList` mirroring the
  breadcrumb, as the article pages do.
- Page metadata: title and description from `title`/`tagline`; the OG
  image is the poster.

### 20.8 Token rules and component inventory delta

Single-theme dark; section 14.6 stands with no additions. The player frame
uses `border-cc-ink-faint`, the cards use the established surfaces, and no
new color literals appear anywhere on the page.

| Component / module     | Change                                                                                  |
| ---------------------- | --------------------------------------------------------------------------------------- |
| `LearnVideoDetail.tsx` | **(new)** page composition under `src/components/learn/` (20.2, 20.4, 20.5)             |
| `LearnVideoPlayer.tsx` | **(new)** learn-framed click-to-load embed under `src/components/learn/` (20.3)         |
| `learnItemHref.ts`     | `video` case returns `/learn/videos/[slug]` (20.1)                                      |
| `YouTubeVideo.tsx`     | Poster-resolution block extracted for sharing with `LearnVideoPlayer`; no visual change |
| `editorial.ts`         | Gains `topicLabelForProduct` for the video kicker (20.2)                                |
| `src/data/learn/*`     | Extended `VideoItem` shape, owned by the sibling data task (20.1)                       |

Reused as-is: `VideoFacade`, `ContentTypeBadge`, `LearnCard`, `CardGrid`,
`ArrowLink`, `SolidButton`, `Tag`, `Picture`, `BrokenMedia`, `formatDate`,
`getOptimizedImage` (`ArticleBreadcrumb` superseded by website-xwu
(2026-08-30): breadcrumbs removed from all /learn pages per user ruling).
`LearnVideoDetail` and `LearnVideoPlayer` are server components;
`VideoFacade` remains the only client piece on the page.

Non-goals of section 20: data entry of the migrated videos (sibling task),
the TV redirect map (website-hnm.4), auth or gating of any kind, and any
dependency on tv.chillicream.com at build or runtime.
