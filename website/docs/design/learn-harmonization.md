# Learn hub harmonization spec (task website-8s5.1)

Synthesis of the five-lens live design audit (spacing and density,
typographic hierarchy, color and chrome economy, component harmonization,
layout interest vs the IBM Think reference) run by a Fable 5 agent panel
against the dev server on branch `pse/adds-templates`, 2026-08-23, at 1440
and 1920 widths. Routes audited: `/learn`, `/learn/browse`,
`/learn/articles/directives-all-the-way-down`,
`/learn/templates/agent-ready-api`. All component and file citations were
re-verified against the repo before inclusion; all token citations exist in
`app/globals.css`.

Relationship to `docs/design/learn-editorial.md`: Part II of that document
(sections 11 to 20) remains the structural spec for the learn hub. This
document amends its visual system based on measurements of the shipped
result. Section 3 below lists every point where this spec supersedes or
amends Part II; anything not listed there stands as written in Part II.

The type-scale tokens referenced throughout (from `app/globals.css`
`@theme`): `text-h2` = 3.625rem (58px), `text-h3` = 2.75rem (44px),
`text-h4` = 2rem (32px), `text-h5` = 1.5rem (24px), `text-h6` = 1.125rem
(18px), `text-caption` = 0.875rem (14px). Several lens reports annotated
token names with incorrect px values; this document uses the real values.

---

## 1. Prioritized defect list

Findings are deduplicated across lenses. Where lenses disagreed on the fix,
the reconciled prescription is given here and the reconciliation is noted.
D1 to D10 are high severity, D11 to D18 medium, D19 to D23 low.

### D1 (high): ContentTypeBadge stretches to full column width in Explainers

- **Page**: `/learn`
- **Component**: `src/components/learn/LearnExplainerList.tsx` line 34
  (`Link` with `flex flex-col gap-2`) + `src/components/learn/ContentTypeBadge.tsx`
- **Evidence** (confirmed by four of five lenses): the badge's `inline-flex`
  box is a direct child of a `flex flex-col` link, so `align-items: stretch`
  blockifies it. Measured 647x19px at 1440 and 780x19px at 1920 for the text
  "Comparison" (~66 to 90px of content), a 9.4x oversize, rendering as the
  seed-reported full-width purple bar (`cc-tip` tint). The section renders
  exactly 1 item in a `lg:grid-cols-2` grid, leaving the right column empty:
  335px of section height for 81px of content (24% content).
- **Fix**: immediate: add `self-start` to the `ContentTypeBadge` (or
  `items-start` on the `Link`). Acceptance: badge width equals content width
  (~66 to 90px). Structural: see section 2.5 (the section does not render
  below 3 items; website-kbx.21 removed the fold into an adjacent band, so a
  sub-threshold explainer is simply omitted from /learn). This supersedes
  Part II sections 11 (row 3.6) and 15.3, which keep `LearnExplainerList`
  standing.

### D2 (high): section-shell rhythm, 5:1 outer-to-inner spacing ratio

- **Page**: `/learn` (also `/learn/templates/agent-ready-api` related band)
- **Component**: `LearnTopicRail.tsx` line 25, `LearnCollectionSection.tsx`
  line 27, `LearnExplainerList.tsx` line 27, `LearnVideoSection.tsx` line 20
  (all `border-cc-card-border border-t py-14 sm:py-20`);
  `TemplateDetail.tsx` line 111 (`py-16 sm:py-24`)
- **Evidence**: six consecutive sections compute 80px top and bottom
  padding; measured inter-section distance (last row to next h2) is 161px
  while heading-to-first-row is 32px and row-to-row is 0px (border-separated
  `py-4` rows). Page height 5,396px at 1440 with roughly 1,120px of bare
  section padding. TemplateDetail's "More from Learn" computes 96px both
  sides around 305px of cards.
- **Fix**: the spacing scale of section 2.1: section shells become
  `py-10 sm:py-12`, rows gain `py-5`, TemplateDetail's related band adopts
  the same token. Target: no vertical gap on the page larger than 3x the
  32px heading-to-content gap (96px). Supersedes Part II section 15.5.

### D3 (high): hairline divider overload

- **Page**: `/learn` (242 hairline segments at 1440, re-measured live
  inside `main`; the audit panel reported 220; 85 of them sit on elements
  starting in the first 960px of the page, where the panel reported 62),
  `/learn/browse` (196 segments at 1920, 57.1 per 1000px of page height,
  the highest density audited; the panel reported 158 and 45.7; the
  article page runs at 7.6 per 1000px). All four figures are re-measured
  live under the counting rule used for every segment figure and target
  in this document: segments are counted per rendered border side, so a
  4-side frame contributes 4 segments and a `border-b` contributes 1.
- **Component**: `LearnListRow.tsx` (5 hairlines per row: `border-b` plus a
  4-side `border-cc-ink-faint` border on the `size-20` thumbnail, lines 35
  to 45); `LearnCard.tsx` (5 per card: 4-side border plus the internal
  `border-cc-card-border mt-auto border-t` footer divider, line 72; 125 of
  browse's 196 segments, 64%); section `border-t` x6; editorial band
  `border-l` rules x2
- **Evidence**: two near-identical border colors in play,
  `--color-cc-card-border` rgba(245,241,234,0.12) and
  `--color-cc-ink-faint` rgba(245,241,234,0.16), a 0.04-alpha difference
  that is imperceptible yet inconsistently assigned (LearnCard all 0.12,
  LearnListRow all 0.16). At each section seam a reader crosses a row
  border-b, a section border-t, and the next heading within 161px.
- **Fix**: the divider policy of section 2.2. Targets: under 150 per-side
  segments on `/learn`; at most 2 border rules per item, where a 4-side
  outline counts as one rule. Supersedes Part II sections 14.1
  and 14.6 on thumbnail frames and the two-token border split.

### D4 (high): badge, chip, and accent color sprawl

- **Page**: all four routes
- **Component**: `src/components/learn/contentTypeMeta.ts` (7 accent
  recipes x 5 class slots: `text`, `bg`, `ring`, `hoverBorder`,
  `activePill`), `ContentTypeBadge.tsx`, `LearnCard.tsx` footer (product
  drink icons via `productArt.ts`, stack icon tiles on hardcoded
  `bg-[#f5f0ea]`, lines 73 to 95), `src/design-system/languages.ts` chips
- **Evidence**: 6 distinct small-chip colors on `/learn/browse` at once
  (slate x11, cc-accent x8, cc-danger x9, cc-success x3, cc-info x3,
  cc-warning x2; 25 tinted badges total). Card footers add 11 distinct
  chromatic SVG fills plus 11 solid-cream `#f5f0ea` 28px tiles, the
  brightest surfaces on the dark page; a single viewport carries ~16
  chromatic families. Type is triple-encoded per card (badge hue plus CTA
  text plus per-type hover border). The article page layers a second chip
  system: `LANGUAGES`-tinted language chips (16 more potential hues) vs the
  neutral `Tag` pills on `/learn`.
- **Fix**: the palette reduction of section 2.3. Supersedes the per-type
  color system implicit in Part II's retention of `contentTypeMeta` and the
  `bg-[#f5f0ea]` literal sanctioned in Part II section 14.6.

### D5 (high): unbounded prose measure on reading pages

- **Page**: `/learn/articles/directives-all-the-way-down`,
  `/learn/templates/agent-ready-api`
- **Component**: `ArticleLayout.tsx` line 67 (`mx-auto max-w-5xl`);
  `TemplateDetail.tsx` line 60 (grid
  `lg:grid-cols-[minmax(0,1fr)_19rem]`, body paragraphs with no max-width)
- **Evidence**: article paragraphs render 1024px wide at 16px/28px,
  measured 123 to 127 characters per line at both 1440 and 1920 (readable
  band is 45 to 90ch; IBM Think reading pages hold ~70 to 80ch). Template
  body paragraphs grow unbounded: 966px at ~92 to 122ch at 1440, 1232px at
  139ch at 1920.
- **Fix**: cap running text at `max-w-[46rem]` (736px, roughly 80ch at
  16px). ArticleLayout: keep the `max-w-5xl` article shell, wrap running
  prose in a `max-w-[46rem] mx-auto` measure and give code blocks, figures,
  and the hero image a breakout to the full `max-w-5xl` (this also creates
  the wide/narrow contrast the page lacks below 2xl). TemplateDetail: change
  the body grid to `lg:grid-cols-[minmax(0,46rem)_19rem]` with
  `justify-between`, letting the gap absorb extra width so the aside keeps
  its 19rem track. Amends Part II sections 11 (row 4) and 16.3, which state
  ArticleLayout "stands as shipped": the shell stands, the inner measure is
  corrected.

  **ArticleLayout number superseded by website-kbx.15 (2026-08-24):** the
  shipped `max-w-[46rem]` measure (this D5 fix) was never `mx-auto`
  centered as prescribed here; it rendered flush against the shell's left
  edge with a 288px dead gutter on the right, which user review flagged as
  stale/wrong-padding on the wide v2 container. kbx.15 re-measured the
  rendered body font (16px `system-ui`, `1ch` = 9.140625px in-browser) and
  lowered the cap to `max-w-2xl` (672px, ~73.5ch, down from 736px/~80.5ch)
  with `mx-auto` centering applied for real, and folded the breadcrumb,
  kind chip, hero, title, standfirst, meta row, and tags into that same
  centered `max-w-2xl` wrapper rather than leaving them at the full shell
  width. `Related` is the one piece that keeps the full `max-w-5xl` shell
  width (it is a card grid, not running text). See
  learn-editorial.md section 4.1's kbx.15 amendment for the full
  before/after table. TemplateDetail's `max-w-[…]` figure is untouched by
  kbx.15 (out of that ticket's file scope) and still needs the D5 number
  reconciled separately if TemplateDetail is revisited.

  **ArticleLayout measure removed entirely by website-kbx.18 (2026-08-24),
  superseding the kbx.15 paragraph above:** user ruling rejected the
  reading-measure direction outright, articles must use the same width
  every other learn page gives its content. `ArticleLayout` no longer caps
  running prose at any `max-w-*` value; breadcrumb through `Related`
  (title and body included) render at the full `1fr` main column of the
  shared `[1fr_20rem]` grid (1120px-1280px at `2xl` and above depending on
  viewport, 1280px binding once the `max-w-8xl` clamp is reached at 1696px
  and wider; see learn-editorial.md section 4.3 for the full range). D5's
  original premise, that unbounded prose is a defect to fix with a measure
  cap, no longer holds for `ArticleLayout`: the fix here is the opposite
  direction, remove the cap kbx.15 installed. The hero is the one deliberate
  exception (kbx.7's regression risk) and does not follow the full-width
  ruling: it keeps kbx.7's original `max-w-3xl` width cap rather than
  spanning the main column, since a `max-h-[26rem]` + `object-cover`
  full-width treatment tried during kbx.18 review cropped roughly 11 of 27
  article heroes and was reverted pending a design call. See
  learn-editorial.md section 4.1's kbx.18 amendment for the measured
  before/after. TemplateDetail is still untouched (out of kbx.18's file
  scope too) and still carries D5's `max-w-[46rem]` /
  `lg:grid-cols-[minmax(0,46rem)_19rem]` figures.

### D6 (high): heading hierarchy inversion across the hub

- **Page**: all four routes
- **Component**: `LearnFeaturedStory.tsx` line 53 (h2
  `text-h4 sm:text-h3 xl:text-h2`), `TemplateDetail.tsx` line 112 ("More
  from Learn" h2 `text-h4 sm:text-h3`) and line 40 (h1 `text-h3 sm:text-h2`),
  `LearnMasthead.tsx` line 18 (h1 `text-h3 sm:text-h2`),
  `LearnSubscribeBand.tsx` line 24 via `src/components/SectionHeading.tsx`
  (default `md` size `text-h4 sm:text-h3`), `ArticleLayout.tsx` line 88 via
  `src/design-system/Typography.tsx` line 22 (h1 variant
  `text-4xl font-bold`, body font)
- **Evidence**: same-rank headings span three sizes and invert importance.
  The featured card's h2 measures 58px, equal to the browse and template
  page h1s; the actual reading page's h1 measures 36px in the system body
  font at weight 700 (`text-4xl font-bold`, which also violates the
  AGENTS.md rule against ad-hoc `text-4xl` display headings), so every hub
  surface renders a story's title bigger than its destination page does.
  "More from Learn" (44px) and the subscribe CTA heading (44px) outrank the
  32px content-section h2s they sit beside. Within the article, h1 36px vs
  prose h2 30px is a 1.2x ratio and the h1 reads as bolded body text.
- **Fix**: the heading ladder of section 2.4. Amends Part II section 14.2
  (featured headline capped at `sm:text-h3`, dropping the `xl:text-h2`
  step) and the "ArticleLayout stands" rows.

### D7 (high): flat rows and monotone rails (the anti-Think landing)

- **Page**: `/learn`
- **Component**: `LearnTopicRail.tsx` rendering `LearnListRow.tsx`
  (line 51 title: no size class, 16px/500 body font);
  `LearnEditorialBand.tsx` line 37 (grid
  `xl:grid-cols-[minmax(14rem,19rem)_minmax(37.5rem,1fr)_minmax(14rem,19rem)]`)
- **Evidence**: three consecutive topic sections are pixel-identical 2x2
  grids of 647x113px rows (780x113 at 1920); twelve interchangeable rows in
  a 1377px run, title text occupying 21 to 35% of the text column. Row
  titles are 16px/500, only 2px and one weight step above their own 14px
  meta; card titles elsewhere are 18px/600 `font-heading` (`LearnCard.tsx`
  line 70), so identical-importance content renders in two voices. The band
  rails are frozen at 304px at both 1440 and 1920 (all +266px of widescreen
  growth goes to the center, 1 : 3.26 : 1), so the reused 80px-thumb row
  leaves a ~176px text column and 3-line wrapped titles, while the rail h2s
  ("Latest", "Most popular") render at 12px mono, smaller than every row
  they label.
- **Fix**: section 2.5 (landing prescriptions): row title moves to the card
  voice (`font-heading text-h6 font-semibold`, 18px), each topic rail gains
  a lead-story slot alternating left/right (A-B-A instead of A-A-A, the
  Think per-collection feature pattern), `LearnListRow` gains a compact
  density variant for the 19rem rails, and the band rails participate in
  2xl growth. Amends Part II sections 14.1, 15.1, and 15.2.

### D8 (high): browse catalog has no entry point and buries its content

- **Page**: `/learn/browse`
- **Component**: `LearnCatalog.tsx` line 280 (`CardGrid cols={4}`),
  `app/(learn)/learn/browse/page.tsx` (`FEATURED_TEMPLATE_SLUGS` ordering,
  line 28), `LearnMasthead.tsx` line 16 (`py-10 sm:py-14`, h1
  `text-h3 sm:text-h2`), `LearnFacetBar.tsx`
- **Evidence**: 25 cards in 4 equal tracks, heights 221 to 269px, 0 of 25
  containing any image or art; featured templates are sorted first but
  rendered identically to every other card. At 1440x900 the 58px h1 sits in
  a 271px hero, followed by a measured 140px empty gap to the facet bar;
  first cards appear around y=640, so roughly 70% of the first viewport is
  chrome on a page whose job is scanning results. The "25 results" count
  occupies its own line.
- **Fix**: section 2.6. Amends Part II section 16.1 (which placed
  `LearnMasthead` here; the masthead stays but shrinks).

### D9 (high): micro-type below the legibility floor, five label voices

- **Page**: all four routes
- **Component**: `ContentTypeBadge.tsx` line 14 (`text-[0.6rem]`, 9.6px,
  `tracking-[0.14em]`), `LearnCard.tsx` lines 41 and 50 (`text-[0.65rem]`,
  10.4px), `TemplateDetail.tsx` lines 88 and 128 (`text-[0.65rem]` dt
  labels), `LearnFacetBar.tsx` lines 112, 125, 160, 175 (`text-[0.65rem]`)
- **Evidence**: uppercase micro-label census on `/learn` at 1440: 58
  instances in five distinct size/weight/font combos (12px/600 sans,
  12px/400 mono, 9.6px/600 mono, 10.4px/600 mono, 10.4px/400 mono) across
  six text colors. 9.6px uppercase mono on a dark ground is below the ~11px
  legibility floor. The same screens run up to 58px display type, a 6x span
  with nothing between 14px body and 32px headings doing hierarchy work.
- **Fix**: two label tokens only (section 2.4): the existing 12px mono
  kicker (`font-mono text-xs uppercase tracking-wider`) and one 11px badge
  token (`text-[0.6875rem]`) replacing every `text-[0.6rem]` and
  `text-[0.65rem]` occurrence in learn components.

### D10 (high): "Agent-ready" flag has two competing skins

- **Page**: `/learn/browse` vs `/learn/templates/agent-ready-api`
- **Component**: `LearnCard.tsx` line 41 (solid pill
  `bg-cc-warning text-cc-surface rounded-full px-3 py-1 font-mono text-[0.65rem]`,
  the only solid-warning surface in the system) vs `TemplateDetail.tsx`
  line 38 (`<Tag className="border-cc-warning/40 text-cc-warning">`)
- **Evidence**: same semantic flag, measured as a solid amber 10.4px
  full-round pill on cards and a transparent 12px outline Tag on the detail
  page.
- **Fix**: the outline `Tag` variant is canonical (it stays inside the Tag
  primitive); `LearnCard`'s bespoke solid pill markup is deleted and
  replaced with the same outline Tag. Amends the Part II section 14.6
  remark that endorsed the solid `bg-cc-warning text-cc-surface` pairing.

### D11 (medium): every card states its type twice

- **Page**: `/learn/browse`, `/learn`
- **Component**: `contentTypeMeta.ts` (`ctaLabel` field), `LearnCard.tsx`
  footer CTA
- **Evidence**: badge top-left ("Template") plus footer CTA bottom-right
  ("View template"): 18 cards x 2 type mentions in one 1440 screen, in 5
  CTA text variants, inside cards already carrying level meta, drink icons,
  and stack tiles (up to 6 metadata systems per card).
- **Fix**: badge is the single type statement; `ctaLabel` is deleted from
  `ContentTypeMeta` and the footer renders one uniform affordance (the
  arrow icon alone, or a constant "Open") in `text-cc-accent`. This keeps
  the user ruling recorded in Part II section 14.5 (CTA color is uniform
  accent, commit 9857016d8f) and extends it: uniform text as well as color.

### D12 (medium): Template badge is byte-identical to the interactive accent

- **Component**: `contentTypeMeta.ts` line 27 (`template.text:
"text-cc-accent"`)
- **Evidence**: `#16b9e4` occurs 98 times on browse and 78 on `/learn`
  across text, backgrounds, and SVG strokes; a non-interactive TEMPLATE
  chip in the exact interactive color reads as an active control and
  dilutes cyan as the click affordance.
- **Fix**: absorbed by D4/section 2.3 (badges go neutral); `cc-accent` is
  reserved exclusively for interactive elements.

### D13 (medium): cc-note is indistinguishable from cc-accent at badge size

- **Component**: `app/globals.css` line 68 (`--color-cc-note`),
  `contentTypeMeta.ts` explainer/article entries
- **Evidence**: Delta-E76 between `cc-note` and `cc-accent` is 13.8 with
  matched lightness; at 9.6px the Article/Explainer chip cannot be told
  from a Template chip or link cyan.
- **Fix**: absorbed by section 2.3; `cc-note` leaves the badge system (the
  token itself stays in `globals.css` for admonitions).

### D14 (medium): two chip systems for tags and languages

- **Component**: `src/design-system/languages.ts` tinted chips (article and
  template headers) vs the neutral `Tag` pills in `LearnTagCloud.tsx`
- **Evidence**: article page totals 19 distinct chromatic colors, template
  page 26; `LANGUAGES` defines 16 more potential chip hues for the same
  "tag" job the neutral pill already does.
- **Fix**: one chip recipe for tags and languages on learn surfaces: the
  neutral `Tag` pill. Syntax highlighting inside code blocks already
  supplies the language color story.

### D15 (medium): LearnCard internal rhythm is inverted

- **Component**: `LearnCard.tsx` (card `rounded-2xl border p-6`, footer
  divider line 72)
- **Evidence**: 316x244px card at 1440; measured 20px between badge row and
  title but only 8px between title and description, with 23px of `mt-auto`
  slack above the footer; the largest fixed gap sits under the least
  important element.
- **Fix**: descending rhythm: 12px badge-to-title, 8px title-to-description,
  `mt-auto` absorbs the rest; footer divider removal comes from D3.

### D16 (medium): related-content surfaces are the weakest on their pages

- **Page**: `/learn/articles/[slug]` ("You might also like" via
  `ArticleLayout` `related` slot), `/learn/templates/[slug]` ("More from
  Learn", `TemplateDetail.tsx` lines 111 to 119)
- **Evidence**: the article's closing recommendation surface is three
  492x113px `LearnListRow`s in a 2-column grid producing a 2+1 orphan cell,
  323px tall on a 6822px page. The template page ends on three uniform
  517x221px text cards, its most monotonous band.
- **Fix**: promote related items to card weight on articles (3 `LearnCard`s
  in a 3-col grid, or supply an even count if rows are kept); on templates,
  lead with one horizontal feature card (related template with
  `TemplateStackArt`) plus two compact items. Heading and padding fixes come
  from D2 and D6.

### D17 (medium): TemplateDetail header art outweighs the content

- **Component**: `TemplateDetail.tsx` line 23 (header `py-10 sm:py-16`),
  line 33 (grid `lg:grid-cols-[1fr_0.9fr]` with `TemplateStackArt`)
- **Evidence**: at 1920 the header is 639px tall and the decorative art tile
  occupies 737x461px, larger than the entire sticky get-started sidebar;
  body text starts below the first viewport at 1440x900.
- **Fix**: cap the art column (`lg:grid-cols-[1fr_minmax(0,28rem)]`) and
  trim header padding to `sm:py-12` so the CLI commands and first body
  section enter the initial 1440x900 viewport.

### D18 (medium): subscribe band heading outranks content sections

- **Component**: `LearnSubscribeBand.tsx` line 24 via `SectionHeading`
- **Evidence**: the CTA heading measures 44px, second-largest type on
  `/learn`, larger than every 32px content-section heading; the page's two
  loudest elements are the first story and the newsletter pitch.
- **Fix**: `SectionHeading` gains a `sm` size (`text-h5 sm:text-h4`) in its
  `TITLE_SIZE` map (`src/components/SectionHeading.tsx` line 18) and the
  band uses it; the button row carries the CTA emphasis.

### D19 (low): row kicker duplicates the section heading

- **Component**: `LearnListRow.tsx` line 50 kicker inside
  `LearnCollectionSection`/`LearnTopicRail` sections
- **Evidence**: rows under "GraphQL fundamentals" each repeat "GRAPHQL
  FUNDAMENTALS" at 12px mono under the same string at 32px; 26 of the 58
  uppercase labels on the page are these kickers.
- **Fix**: inside a headed section the kicker carries content type or date,
  or is omitted; the collection-name kicker is reserved for contexts without
  a section heading (the Latest rail). Amends Part II section 14.1's kicker
  content rule.

### D20 (low): three co-existing chip geometries and six corner radii

- **Component**: `ContentTypeBadge.tsx` (`rounded-[5px]`),
  `LearnFeaturedStory.tsx` line 48 (one-off `rounded-md` bordered category
  chip), `LearnFacetBar.tsx` lines 95 to 201 (rounded-full pills plus
  `rounded-[4px]` checkboxes), `Tag` (full-round), code-language chips (4px)
- **Evidence**: six radii in the learn system: 4, 5, 6, 8, 16px, full-round;
  four chip font sizes (9.6, 10.4, 12, 14px) for the same small-label job.
- **Fix**: the radius scale of section 2.3: full-round for all chips and
  pills, `rounded-lg` (8px) for inputs and thumbnails, `rounded-2xl` (16px)
  for cards and tiles. The featured story's category chip renders through
  `Tag`.

### D21 (low): stack icon tiles use a raw hex literal

- **Component**: `LearnCard.tsx` line 88 (`bg-[#f5f0ea]` size-7 tiles)
- **Evidence**: the only light-background, non-tokenized fill inside the
  dark card system.
- **Fix**: absorbed by D4/section 2.3: icon wells drop to a tokenized
  neutral (`bg-cc-white/8`) or are removed in favor of `currentColor`
  icons. Supersedes the "single sanctioned literal" clause of Part II
  section 14.6.

### D22 (low): browse facet chrome above the results

- **Component**: `LearnFacetBar.tsx` (results count line, legend labels)
- **Evidence**: the "25 results" count sits on its own line below a 34px
  pill row and 42px search input, part of D8's 70% first-viewport chrome.
- **Fix**: results count renders inline at the right end of the type-pill
  row; legend and count micro-type rises with D9.

### D23 (low): masthead teaser jump

- **Component**: `LearnMasthead.tsx` teaser under the h1
- **Evidence**: 58px h1 drops straight to a 16px/400 teaser, a 3.6:1 jump
  with no intermediate level; browse has exactly three sizes doing all
  hierarchy work above the label layer (58, 18, 16/14).
- **Fix**: teaser at `text-lg` (18px), matching TemplateDetail's tagline
  treatment; combined with the h1 cap from section 2.4 the jump becomes
  44 to 18.

---

## 2. The harmonized system

### 2.1 Spacing scale

One rhythm scale for all learn surfaces. The governing rule: **no vertical
gap on a page may exceed 3x the heading-to-content gap** (32px), i.e. 96px.

| Token               | Value                         | Used for                                                                                                                  |
| ------------------- | ----------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| Section shell       | `py-10 sm:py-12` (40/48px)    | Every landing section, `LearnClosing`, TemplateDetail "More from Learn" (replacing `py-14 sm:py-20` and `py-16 sm:py-24`) |
| Section heading gap | 32px (current, keep)          | h2 row to first content row                                                                                               |
| List row            | `py-5` (20px, up from `py-4`) | `LearnListRow`; the 16px removed from the shell moves into the rows                                                       |
| Page hero (browse)  | `py-6 sm:py-8`                | `LearnMasthead` (down from `py-10 sm:py-14`); hero-to-facet gap collapses to 40 to 48px                                   |
| Detail header       | `py-10 sm:py-12`              | `TemplateDetail` header (down from `py-10 sm:py-16`)                                                                      |
| Card padding        | `p-6` (keep)                  | `LearnCard`, with the internal rhythm fix of D15                                                                          |

Resulting inter-section distance: 96px (48 + 48), down from 161px, giving a
3:1 outer-to-inner ratio instead of 5:1. This supersedes Part II section
15.5 ("sections keep the border-t py-14 sm:py-20 rhythm"). The editorial
band keeps its Part II `pt-8 sm:pt-10` opening.

### 2.2 Divider policy

Segments are counted per rendered border side (the D3 counting rule); the
live per-side baseline on `/learn` at 1440 is 242 segments inside `main`.

Budget: **at most 2 border rules per item, where a 4-side outline counts
as one rule; under 150 per-side segments on `/learn`.** The prescriptions
below remove 96 of the 242 baseline segments (68 thumbnail frames, 15 card
footer dividers, 5 section `border-t`, 8 featured and promo image
frames), landing at roughly 146. The kept segments are dominated by the
card outlines and the chip pills (60 sides each), which item 5 below
retains. Routing the featured category chip through `Tag` (section 2.3)
is segment-neutral on `/learn`: the chip it replaces is already bordered.

1. **One hairline token.** All learn borders and dividers use
   `--color-cc-card-border` (rgba(245,241,234,0.12), exists in
   `app/globals.css` line 57). Learn components stop using
   `--color-cc-ink-faint` for borders (`divide-y divide-cc-ink-faint`
   becomes `divide-cc-card-border`; the token itself stays in
   `globals.css` for its non-border text uses elsewhere). Supersedes the
   Part II section 14.6 two-token split (card-border for columns,
   ink-faint for rows and image frames).
2. **No borders on imagery.** `LearnListRow` thumbnails and fallback
   squares drop their 4-side border (the `rounded-lg` crop plus
   `bg-cc-white/4` is enough separation on the dark ground); the featured
   story and promo tile image frames drop theirs likewise. Supersedes Part
   II sections 14.1 to 14.3 image `border border-cc-ink-faint`.
3. **No internal card dividers.** `LearnCard`'s footer
   `border-cc-card-border mt-auto border-t` becomes plain `pt` spacing.
4. **Section seams: border or padding, never both at full strength.**
   Sections whose bodies are divider-separated row lists drop their
   `border-t` (the 96px rhythm plus the tinted band variant carry the
   seam); the `border-t` survives only where the section body is a card
   grid with no row dividers ("Watch"). The editorial band's inter-column
   `border-l` rules stay: they are the Think signature and are 2 segments
   total.
5. **Kept hairlines**: row separators inside lists, card outlines,
   band column rules, the facet-pill borders.

### 2.3 Badge, tag, and accent palette reduction

**Chip primitives: exactly two.**

- `Tag` (`src/design-system`, existing): 12px sans, full-round, neutral
  recipe (`text-cc-ink-dim`, `bg-cc-hover`, `border-cc-card-border`, as
  shipped in `Tag.tsx`). Used
  for: tag cloud, facet pills (with the active state), the featured story's
  category chip (replacing the one-off `rounded-md` chip in
  `LearnFeaturedStory.tsx` line 48), language chips (replacing the
  `languages.ts` tinted recipe on learn surfaces), and the Agent-ready flag
  (outline variant `border-cc-warning/40 text-cc-warning`, canonical per
  D10).
- `ContentTypeBadge` (existing): moves to 11px (`text-[0.6875rem]`,
  raising 9.6px), full-round (from `rounded-[5px]`), and **one neutral
  recipe** for every content type: `text-cc-ink-dim`, `bg-cc-hover`,
  `ring-cc-card-border`. The label text differentiates types.

Both primitives share the `bg-cc-hover` ground (rgba(245,241,234,0.05),
`app/globals.css` line 54). `cc-hover` drops to transparent under the
`@media print` palette, so both chips lose their fill together in print;
a `bg-cc-white/5` ground would not (`cc-white` stays `#fff` there).

**`CONTENT_TYPE_META` slimming** (`contentTypeMeta.ts`): the per-type
`text`, `bg`, `ring`, `hoverBorder`, `activePill`, and `ctaLabel` fields
are deleted. `LearnCard` gets one shared hover border
(`hover:border-cc-card-border-hover`, token exists at `globals.css` line 58) and one uniform footer affordance (D11). Facet active state is one
recipe (the existing cyan-checkbox/heavy-border treatment in
`LearnFacetBar`), not per-type.

**Token disposition in `app/globals.css`** (no token is deleted from the
file; this is about who may use them on learn surfaces):

| Token                                                                                             | Learn-surface role after harmonization                                                              |
| ------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `--color-cc-accent` / `--color-cc-accent-hover`                                                   | **Interactive only**: links, CTAs, hover accents, facet active. Never a badge or label color (D12). |
| `--color-cc-warning`                                                                              | Agent-ready outline Tag only.                                                                       |
| `--color-cc-danger`, `--color-cc-success`, `--color-cc-info`, `--color-cc-tip`, `--color-cc-note` | Leave the learn chip system entirely (remain for admonitions and non-learn uses).                   |
| `--color-cc-card-border`                                                                          | The single learn hairline (section 2.2).                                                            |
| `--color-cc-card-border-hover`                                                                    | The single card hover border.                                                                       |
| `--color-cc-ink-dim`                                                                              | The single label/meta/badge text color.                                                             |
| `--color-cc-ink-faint`                                                                            | No longer a learn border color.                                                                     |
| `bg-[#f5f0ea]` literal (LearnCard)                                                                | Dropped; stack icon wells become `bg-cc-white/8` or disappear (D21).                                |

**Card footer iconography**: product drink icons and stack icons render
monochrome at `text-cc-ink-dim` (via `currentColor`), gaining full color
only on card hover. A resting card's only colored element is nothing at
all; hover brings the accent. This takes the browse grid from ~16
chromatic families per viewport to 2 (ink plus accent-on-hover).

**Radius scale, three steps**: full-round (chips, pills, buttons),
`rounded-lg` 8px (inputs, thumbnails, icon wells), `rounded-2xl` 16px
(cards, tiles, panels). The 4px, 5px, and 6px one-offs are deleted (D20).

### 2.4 Typographic ladder

One rank scale across the hub. All headings `font-heading font-semibold`
unless noted.

| Rank                    | Recipe                                  | Applies to                                                                                                                                                             |
| ----------------------- | --------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Page h1                 | `text-h3` (44px), no `sm:text-h2` step  | `LearnMasthead` h1, `TemplateDetail` h1, article h1                                                                                                                    |
| Featured story headline | `text-h4 sm:text-h3` (cap 44px)         | `LearnFeaturedStory` h2; the `xl:text-h2` step is dropped so no index headline exceeds a page h1                                                                       |
| Section h2              | `text-h5 sm:text-h4` (cap 32px)         | All landing sections (current), TemplateDetail content sections (current), **and** "More from Learn" and the subscribe band (both currently one step too big, D6/D18)  |
| Row/card title          | `text-h6` (18px)                        | `LearnCard` h3 (current) **and** `LearnListRow` titles (currently unsized 16px body): one voice for one content class                                                  |
| Body                    | 18px / `leading-8`, full column width   | Prose spans the `1fr` main column, no measure cap; size bumped from the shared 16px/`leading-7` prose default for the wider measure (D5, superseded by website-kbx.18) |
| Meta                    | `text-caption` (14px) `text-cc-ink-dim` | Author lines, descriptions in rows                                                                                                                                     |
| Kicker                  | `font-mono text-xs uppercase` (12px)    | The only mono label voice besides the badge                                                                                                                            |
| Badge                   | `text-[0.6875rem]` (11px)               | `ContentTypeBadge`, facet legends/counts, TemplateDetail `dt` labels, `LearnCard` meta: every current `text-[0.6rem]` and `text-[0.65rem]`                             |

Article pages: the title renders in the heading voice at the page-h1 rank
(`font-heading text-h3 font-semibold`), via a display variant in
`Typography` or a dedicated class in `ArticleLayout`, replacing the
`text-4xl font-bold` body-font h1 (D6). Prose h2 stays 30px, giving a
44 to 30 to 24 ladder, and the reading surface finally outranks its own
index. The masthead teaser rises to `text-lg` (D23).

### 2.5 Per-section layout prescriptions: `/learn` landing

The IBM Think reference gesture is variation with a spine: a mixed-width
editorial band, then collection bands that each lead with a feature, not
six identical slabs. Scroll rhythm becomes open / tinted / open instead of
six equal hairline-topped bands.

1. **Editorial band** (`LearnEditorialBand.tsx`): the Part II three-column
   structure stands. Amendments: (a) rails participate in widescreen growth,
   `2xl:grid-cols-[minmax(16rem,24rem)_minmax(37.5rem,1fr)_minmax(16rem,24rem)]`,
   so 1920 stops giving all +266px to the center; (b) the featured headline
   caps at `sm:text-h3` (section 2.4); (c) `LearnListRow` gains a `density`
   prop: `compact` (48px thumb or none, 2-line clamp, `text-h6` title) for
   columns narrower than 20rem, `default` elsewhere, ending the reuse of an
   80px-thumb row in a 176px text column; (d) the first Latest item may
   render with thumbnail and the rest without, giving the column a lead.
2. **Topic rails** (`LearnTopicRail.tsx`): each rail gains a lead-story
   slot: the first (or editorially flagged) post renders as a
   feature (16:9 image, `text-h4` headline, one-line standfirst) filling
   one column of the `lg:grid-cols-2` grid; the remaining 3 posts are
   compact rows in the other column. Rails alternate lead-left and
   lead-right (A-B-A). This amends Part II section 15.2 ("rows only"): rows
   remain the only secondary treatment, but each rail leads with a feature.
3. **"Start building"** (`LearnCollectionSection.tsx`): becomes the tinted
   band (full-bleed `bg-cc-card-bg`, no `border-t`, section 2.1 padding).
   The first item (featured template) spans 2 columns as a horizontal
   feature card using the existing, currently unused-in-band
   `TemplateStackArt.tsx` / `productArt.ts` assets; the remaining cards
   stay 1x1.
4. **Explainers** (`LearnExplainerList.tsx`): does not render as its own
   section below 3 items (supersedes Part II 11/15.3 "stands"). website-kbx.21
   dropped the fold into an adjacent band; a sub-threshold item is simply
   omitted. Whenever the section returns, it is single-column below 3 items
   and its badge carries `self-start` (D1).
5. **"Watch"** (`LearnVideoSection.tsx`): keeps its card grid and its
   `border-t` (the one card-grid section without row dividers), on the
   section 2.1 padding token.
6. **Subscribe band** (`LearnSubscribeBand.tsx`): stands (Part II 15.4),
   with the `SectionHeading` size drop of D18.
7. **Row kickers** inside headed sections carry type or date, not the
   section name (D19).

### 2.6 Per-section layout prescriptions: `/learn/browse`

1. **Masthead**: `LearnMasthead` stays (Part II 16.1 stands) but compresses:
   `py-6 sm:py-8`, h1 `text-h3`, teaser `text-lg`. Target: facet bar within
   ~300px of the top of the content area, first cards inside the first
   1440x900 viewport.
2. **Feature row**: in the default unfiltered view, the featured templates
   (already sorted first by `FEATURED_TEMPLATE_SLUGS`) render as
   `col-span-2` tiles carrying `TemplateStackArt`/product art above the
   uniform grid; `CardGrid` gains a span escape hatch (a `featureFirst`
   prop or the caller wraps the first N children), used by `LearnCatalog`.
   The grid collapses to uniform whenever a filter or search query is
   active. Amends Part II 16.1, which left the catalog grid unchanged.
3. **Facet bar**: results count inline at the right end of the type-pill
   row (D22); pill geometry per section 2.3 (facet pills are `Tag` with an
   active state, checkbox squares keep the single cyan active recipe).
4. **Cards**: neutral badges, monochrome footers, single hover border, no
   internal divider, `text-h6` titles (all per 2.2 to 2.4).

### 2.7 Per-section layout prescriptions: reading pages

1. **`/learn/articles/[slug]`** (`ArticleLayout.tsx`): no article shell or
   reading-column cap; breadcrumb through `Related` (hero excepted) render
   at the full `1fr` main column of the shared `[1fr_20rem]` grid
   (website-kbx.18, superseding kbx.15's `max-w-2xl` reading column, this
   D5 line's `max-w-[46rem]` figure, and both their breakout claims; the
   kbx.7 cross-reference below still governs the hero, which keeps its
   original `max-w-3xl` width cap rather than joining the full-width
   ruling, pending a design call on a `max-h-[26rem]` + `object-cover`
   full-width treatment that was found to crop article art); the title
   takes the page-h1 recipe (D6); related items render at card weight or in
   even counts (D16). Language chips render through `Tag` (D14).
2. **`/learn/templates/[slug]`** (`TemplateDetail.tsx`): header
   `py-10 sm:py-12` with the art column capped at
   `lg:grid-cols-[1fr_minmax(0,28rem)]` (D17); body grid
   `lg:grid-cols-[minmax(0,46rem)_19rem]` with `justify-between` (D5);
   "More from Learn" on the section token with a `text-h5 sm:text-h4`
   heading (D2, D6) and a feature-plus-compact composition (D16); sidebar
   `dt` labels at the 11px badge token (D9).

---

## 3. Where this spec supersedes or amends learn-editorial.md Part II

| Part II section                                                                                                         | Disposition here                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| ----------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 11 (row 3.6) and 15.3, Explainers stands                                                                                | **Superseded**: section does not render below 3 items; the fold into an adjacent band was removed (website-kbx.21), sub-threshold explainers are omitted.                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| 11 (row 4) and 16.3, ArticleLayout stands                                                                               | **Amended**: no shell or prose measure cap (website-kbx.18 removed it entirely); title moves to the heading voice at page-h1 rank (D5, D6).                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| 14.1 `LearnListRow` recipe                                                                                              | **Amended**: title gains `font-heading text-h6 font-semibold`; thumbnail loses its border; rows `py-5`; compact density variant added; kicker rule per D19.                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| 14.2 `LearnFeaturedStory` headline                                                                                      | **Amended**: `xl:text-h2` step dropped; image frame border dropped; category chip renders through `Tag`.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| 14.3 promo tile image frame                                                                                             | **Amended**: image border dropped (divider policy).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| 14.5 CTA rule                                                                                                           | **Amended, ruling kept**: CTA stays uniform `text-cc-accent`; additionally the per-type `ctaLabel` text is deleted for one uniform affordance (D11).                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| 14.6 token rules                                                                                                        | **Superseded in part**: single hairline token (2.2); `bg-[#f5f0ea]` literal no longer sanctioned (D21); solid `bg-cc-warning` pill retired (D10).                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| 15.1 band grid                                                                                                          | **Amended**: rails grow at `2xl`; Latest column may lead with one thumbed row (2.5.1).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 15.2 topic rails "rows only"                                                                                            | **Amended**: rails lead with one feature slot, alternating sides; secondary items remain rows (2.5.2).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 15.5 section rhythm                                                                                                     | **Superseded**: `py-10 sm:py-12` scale, border-or-padding seam rule, tinted collection band (2.1, 2.2, 2.5.3).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| 16.1 browse                                                                                                             | **Amended**: masthead compresses; unfiltered catalog opens with a featured `col-span-2` row (2.6).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| 11 (rows 6 and 8), which keep Part I 6.1 "Kind chips for editorial types" and Part I 8 "Theme and token rules" standing | **Amended**: Part I 6.1's editorial accent table (the `cc-note`/`cc-tip` tints for article, comparison, explainer) is retired, and so is its sentence keeping the five catalog accents of learn-hub.md section 4.1 unchanged: section 2.3 deletes the per-type `text`/`bg`/`ring`/`hoverBorder`/`activePill` fields from `CONTENT_TYPE_META`, so all eight content types render the neutral recipe that 6.1 names as its fallback, now canonical as the `ContentTypeBadge` form with the section 2.3 tokens. Part I 8's StatusChip accent-tint extension to editorial chips is retired with it (2.3, D13). |
| Everything else in Part II                                                                                              | **Stands** (subnav, container, route structure, treatment system, dedupe rules, section 20 video pages).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |

---

## 4. Verification targets for the implementing task

- `/learn` total per-side hairline segments under 150 (was 242, counted
  per the D3 rule); no gap over 96px
  between a section's last row and the next h2 (was 161px).
- ContentTypeBadge bounding width equals content width everywhere
  (Explainers badge ~66 to 90px, was 647px).
- Smallest rendered text on any learn route is 11px (was 9.6px); largest
  index headline is 44px and no index headline exceeds its destination
  page's h1.
- A resting browse viewport shows at most 2 chromatic families outside
  imagery (was ~16).
- Article and template prose measures at 75 to 85ch at 1920 (was 123 to
  139ch).
- The three topic rails are not pixel-identical: lead slots alternate
  A-B-A.
