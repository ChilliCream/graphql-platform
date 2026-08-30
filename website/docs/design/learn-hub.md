# Design spec: /learn hub

Design direction for the unified learning catalog that replaces `/templates`
(parent ticket website-5yo, this spec is ticket website-5yo.1). It governs the
hub page (`/learn`), the unified content card, and the template detail pages
(`/learn/templates/[slug]`). It is a design document only; implementation is
tickets website-5yo.3 and website-5yo.4.

Data model reference: `src/data/learn/types.ts` (`LearnItem` union over
`template | video | tutorial | example | workshop`) and
`src/data/learn/facets.ts` (`CONTENT_TYPE_OPTIONS`, `TEMPLATE_FILTER_AXES`,
`PRODUCT_OPTIONS`). The spec below is written against that model.

---

## 1. Design direction: what replaces the blueprint aesthetic

The current `/templates` page carries a bespoke look that no other page has:

- `src/components/templates/BlueprintBackdrop.tsx`: a full-page generative
  "drafting sheet" SVG layered behind the whole page.
- A hero, catalog, and closing band styled as one isolated composition on top
  of that sheet.

The rest of the site has a single, consistent surface: the global dark
starfield background (`--cc-dark-surface` via `.cc-content-dark` and the
`body` background in `app/globals.css`), content pages composed from
`PageHero` + `Section` + `CardGrid` + card primitives, and all color coming
from the `cc-*` token set.

**Replacement:** the /learn hub uses no page-level backdrop at all. The page
sits directly on the global site surface like `/resources`, `/platform`, and
`/products/*`. Visual interest comes from the card grid itself: the per-type
accent chips (section 4), the product drink iconography already used across
the site (`DrinkIcon`), and the standard card hover behavior. Concretely:

- `BlueprintBackdrop` is not used on /learn and is deleted with the old route.
- The `SHOW_BLUEPRINT_BACKDROP` flag and the `relative isolate` wrapper in the
  old `app/(content)/templates/page.tsx` go away.
- The drink and stack iconography (`DrinkIcon`, `STACK_ICONS`,
  `TemplateStackArt`) is kept. It is ChilliCream brand voice, not blueprint
  aesthetic, and it already appears elsewhere on the site.

---

## 2. Route and page structure

- Hub: `/learn`, under `app/(content)/learn/page.tsx`. The `(content)` layout
  already provides the outer gutter (`max-w-7xl`, `px-5 sm:px-12`), so the
  page does not wrap itself in `PageSection`.
- Template detail: `/learn/templates/[slug]`, under
  `app/(content)/learn/templates/[slug]/page.tsx`.
- The other four content types have no detail pages in this iteration. Their
  cards link straight to the target (`VideoItem.url`, `externalUrl`, or a docs
  path once content is seeded, see website-5yo.6).
- `/templates` and `/templates/[slug]` redirect permanently to `/learn?type=template`
  and `/learn/templates/[slug]` (routing detail owned by website-5yo.3/.4).

Vertical rhythm follows the content pages: hero block, catalog section,
closing band. No horizontal rules between hero and catalog; the catalog and
closing band separate with `border-cc-card-border` top borders as
`TemplateDetail` and `TemplatesClosing` already do.

---

## 3. Hub page anatomy

### 3.1 Hero

Reuse `PageHero` (`src/components/PageHero.tsx`), the hero every sibling
content page uses (`/resources`, `/platform`, `/products/*`). Not
`MarketingHero`: /learn is a content page, and dropping `MarketingHero` +
`ButtonRow` CTA pair is part of shedding the old bespoke composition.

- `eyebrow`: `"Learn"`
- `title`: `"Learn ChilliCream"` (final copy owned by website-5yo.3)
- `teaser`: one sentence covering all five content types, e.g. "Templates,
  videos, tutorials, examples, and workshops for building with Hot Chocolate,
  Fusion, and the rest of the platform."

No CTA buttons in the hero. The facet bar directly below is the call to
action.

### 3.2 Facet/filter bar

A horizontal bar directly under the hero, replacing the old left sidebar
of `TemplateCatalog`. The sidebar existed because templates have six filter
axes; on /learn the primary question is "what kind of thing am I looking
for", which is one axis with six states and belongs in a single visible row.

Row 1 (always visible):

- **Content-type pills**: `All` plus the five options from
  `CONTENT_TYPE_OPTIONS` (Templates, Videos, Tutorials, Examples, Workshops).
  Single-select. Each pill shows a live count. Styling follows the `Tag`
  pill shape (`rounded-full border border-cc-card-border`): inactive pills use
  `text-cc-ink-dim` with `hover:border-cc-accent`; the active pill uses that
  type's accent tint (section 4), e.g. `bg-cc-accent/15 text-cc-accent
border-cc-accent/40` for Templates. `All` active state uses
  `bg-cc-hover text-cc-heading border-cc-card-border-hover`.
- **Search input**: right-aligned on `lg`, full-width row on mobile. Same
  styling as the existing catalog search (leading `SearchIcon` from
  `src/icons/Search.tsx`, `border-cc-card-border bg-cc-surface/60
focus:border-cc-accent rounded-lg`). Searches across title, tagline, and
  facet labels of all items.

Row 2 (secondary filters):

- **Product filter**: `PRODUCT_OPTIONS` as small multi-select pills with the
  same checkbox affordance as today's facet options (`CheckGlyph` in a
  `size-4` box). Products are the one axis every `LearnItem` carries, so this
  row is always visible.
- **Template-only axes** (`TEMPLATE_FILTER_AXES`: topology, use case,
  language, client, agent-ready) appear only while the Templates pill is
  active, inside a collapsible "More filters" disclosure (the `details`
  pattern the current mobile catalog already uses, promoted to all
  viewports). This keeps the bar one line tall for the four content types
  that have no extra axes. Facet counts and the disabled-at-zero behavior
  carry over from `TemplateCatalog.FacetGroups`.
- **Clear all** appears at the end of row 2 whenever any filter or query is
  active, styled like today's clear control (mono uppercase, `text-cc-accent`).

All state is URL-synced exactly like today (`?type=`, `?product=`, `?q=`,
plus the template axis params `topology/use/language/client/agent` when the
Templates type is active), `router.replace` with `scroll: false`, typing
debounced 250ms. Any filtered view stays shareable.

### 3.3 Card grid

- `CardGrid` (`src/components/CardGrid.tsx`) with `cols={3}`
  `step="progressive"` `itemsStretch`, children are `LearnCard`s
  (section 4). This drops the bespoke `md:grid-cols-2 2xl:grid-cols-3` grid
  of `TemplateCatalog` in favor of the shared primitive used by every other
  content page.
- Default ordering: templates with `featured` first, then by content type in
  `CONTENT_TYPE_OPTIONS` order, then title. When a single type is selected the
  grid is just that type.
- A result count line above the grid in the caption voice
  (`text-caption text-cc-ink-dim`, e.g. "18 results") so filtering has
  visible feedback near the cards.

### 3.4 Closing band

Keep the pattern of `TemplatesClosing` (heading + line + `SolidButton` to
`/docs` inside a `border-y` band) with copy widened from templates to
learning. It becomes `src/components/learn/LearnClosing.tsx` (proposed, see
section 8); alternatively `NextStepsSection` can serve here if website-5yo.3
prefers the card-row form.

---

## 4. Unified content card: `LearnCard` (proposed)

One card component renders every `LearnItemSummary`. Base surface is the site
card language: `border-cc-card-border bg-cc-card-bg rounded-2xl border p-6
backdrop-blur-sm`, whole card is the link, `hover:-translate-y-1` plus a
hover border in the type accent (the existing `TemplateCard` interaction,
generalized). Equal height via `flex h-full flex-col` + grid `itemsStretch`.

Card anatomy, top to bottom:

1. **Header row**: `ContentTypeBadge` (proposed, section 4.1) on the left;
   on the right, per-type metadata in the mono caption voice
   (`font-mono text-[0.65rem] uppercase tracking-wider text-cc-ink-dim`):
   - template: the existing "Agent-ready" pill (`bg-cc-warning
text-cc-surface`) when `agentReady`
   - video: `duration` (e.g. "12 MIN")
   - tutorial / example / workshop: `level` when present ("BEGINNER" etc.)
2. **Title**: `font-heading text-cc-heading text-h6 font-semibold`
   (unchanged from `TemplateCard`).
3. **Tagline**: `text-cc-ink-dim text-sm leading-relaxed`, clamped to 3
   lines.
4. **Footer** (pinned with `mt-auto`, above a `border-cc-card-border`
   top border): left side shows the product mix as `DrinkIcon`s (all types
   carry `products`); templates additionally show their `STACK_ICONS` chips
   as today. Right side is the trailing affordance in `text-cc-accent`:
   per-type label + `ArrowRightIcon` with the `group-hover:translate-x-1`
   slide. Labels: "View template", "Watch video", "Start tutorial",
   "View example", "View workshop". Items resolving to an external URL
   (`externalUrl`, `VideoItem.url`) open in a new tab and swap the arrow for
   the established external affordance.

The accent stays confined to the badge and the hover border. Body text,
borders, and surfaces remain neutral so a mixed grid reads as one family, not
five different cards.

### 4.1 Per-type accents

Follow the `STATUS_META` pattern from `src/components/StatusChip.tsx`: a
static record of Tailwind classes per type (static strings so Tailwind sees
them), living in `src/components/learn/contentTypeMeta.ts` (proposed).

| Type     | Token        | Chip classes                                          | Hover border                 |
| -------- | ------------ | ----------------------------------------------------- | ---------------------------- |
| template | `cc-accent`  | `text-cc-accent bg-cc-accent/10 ring-cc-accent/30`    | `hover:border-cc-accent/60`  |
| video    | `cc-danger`  | `text-cc-danger bg-cc-danger/10 ring-cc-danger/30`    | `hover:border-cc-danger/60`  |
| tutorial | `cc-success` | `text-cc-success bg-cc-success/10 ring-cc-success/30` | `hover:border-cc-success/60` |
| example  | `cc-info`    | `text-cc-info bg-cc-info/10 ring-cc-info/30`          | `hover:border-cc-info/60`    |
| workshop | `cc-warning` | `text-cc-warning bg-cc-warning/10 ring-cc-warning/30` | `hover:border-cc-warning/60` |

All five are existing tokens in `app/globals.css`. `cc-note` and `cc-tip`
stay reserved for admonitions. `ContentTypeBadge` renders the
`contentTypeLabel` singular form (Template, Video, ...) in the `StatusChip`
shape: mono, uppercase, tinted background, `ring-1 ring-inset`, no dot.

The same meta record supplies the active-pill classes for the facet bar
(section 3.2) and the badge on the detail page, so type color is defined in
exactly one place.

---

## 5. Template detail page: `/learn/templates/[slug]`

The existing `TemplateDetail` layout is structurally sound and already speaks
the site language (token colors, `text-h*` scale, `Tag`, `CopyCommand`,
`CodeBlock`, buttons). It moves to `src/components/learn/TemplateDetail.tsx`
and is restyled only where it referenced the old world:

1. **Breadcrumb**: superseded by website-xwu (2026-08-30): all /learn
   breadcrumbs, including this one, were removed per user ruling. The
   header now opens directly with item 2 (tags row).
2. **Header**: unchanged two-column grid (`lg:grid-cols-[1fr_0.9fr]`): tags
   row (`Tag` for topology, the warning-tinted Agent-ready tag), `text-h3
sm:text-h2` title, `text-cc-prose` tagline, `SolidButton` "View source"
   with `GitHubIcon` + optional `OutlineButton` "Live demo". A
   `ContentTypeBadge` for "Template" joins the tags row so the detail page
   visibly belongs to the hub taxonomy.
3. **Art panel**: keep `TemplateStackArt` in the bordered `aspect-[16/10]`
   panel. It is drink iconography, not blueprint.
4. **Body + sidebar**: unchanged: `lg:grid-cols-[minmax(0,1fr)_19rem]`,
   article sections at `text-h5 sm:text-h4` with `CodeBlock`, sticky
   "Get started" card with `CopyCommand` rows and the `dl` metadata list
   (language, use cases, clients, products, stack, license, updated).
5. **Related**: heading becomes "More from Learn". The grid renders
   `LearnCard`s (same type first, then other types sharing a product) via
   `CardGrid cols={3} step="progressive"`, replacing the old
   `TemplateCard`-only row.

---

## 6. Empty and loading states

### 6.1 Empty (no results for filters/search)

Reuse the pattern already in `TemplateCatalog`, extracted to
`LearnEmptyState` (proposed): a `rounded-2xl border border-dashed
border-cc-card-border` panel, centered, `px-8 py-20`, containing a
`font-heading text-cc-heading` one-liner, a `text-cc-ink-dim text-sm`
explanation, and a pill button "Clear search & filters" that resets to
`/learn`. Copy adapts to scope:

- Filters/search yield nothing: "Nothing matches" / "Loosen a filter or clear
  the search."
- A content-type pill with zero seeded items (possible until website-5yo.6
  lands): "No {videos} yet" / "New content lands here as it ships. Browse
  templates in the meantime." with the reset button labeled "Show everything".

The grid area keeps a minimum height (`min-h-[24rem]`) so toggling between
results and empty does not collapse the page.

### 6.2 Loading

The catalog is a client component behind `Suspense` (it reads
`useSearchParams`, same constraint as today). The fallback must hold layout,
not be a blank spacer like the current `CatalogFallback`:

- Facet bar skeleton: pill-shaped `bg-cc-hover animate-pulse` blocks in the
  bar's exact geometry.
- Grid skeleton: 6 `LearnCardSkeleton`s (proposed) inside the same
  `CardGrid cols={3} step="progressive"`: a `border-cc-card-border
bg-cc-card-bg rounded-2xl border p-6` shell with pulsing `bg-cc-hover`
  bars for badge, title, two tagline lines, and footer. `aria-hidden="true"`
  on the whole fallback.

No spinners. Content is statically known, so the skeleton shows for one paint
at most and must not flash a different layout.

---

## 7. Light/dark behavior

The site is single-theme dark: the full `cc-*` palette is defined once on
`:root` in `app/globals.css` `@theme`, and the only palette swap in the app
is `@media print`, which remaps the same tokens to a light set. There is no
`prefers-color-scheme` or `data-theme` handling on the website (the only
theme module, `src/nitro/lib/theme.tsx`, belongs to the embedded Nitro app).

Rules for /learn:

- Every color comes from a `cc-*` token or a token with an opacity modifier
  (`bg-cc-accent/10`, `border-cc-accent/60`). No raw hex, no `oklch` literals
  in components. This makes the pages correct under `@media print` today and
  under any future light theme for free.
- Accent tints use the `/10`-`/15` background plus `/30`-`/60` border recipe
  (the `StatusChip` recipe) rather than hand-picked dark-only colors, so they
  survive a palette swap.
- The one sanctioned literal is the `bg-[#f5f0ea]` light chip behind
  `STACK_ICONS` brand logos on template cards (brand marks need a light
  ground in both themes). It carries over as-is; do not add new literals.
- The grid and detail pages must not paint their own page background
  (that was the blueprint mistake): the global surface owns the backdrop.

---

## 8. Component inventory

### Reused as-is (all verified to exist)

| Primitive                                                  | Path                              | Role on /learn                                                                       |
| ---------------------------------------------------------- | --------------------------------- | ------------------------------------------------------------------------------------ |
| `PageHero`                                                 | `src/components/PageHero.tsx`     | Hub hero                                                                             |
| `CardGrid`                                                 | `src/components/CardGrid.tsx`     | Hub grid, related grid                                                               |
| `Section`                                                  | `src/components/Section.tsx`      | Not used on the hub (catalog owns its heading), available for future editorial bands |
| `Card`                                                     | `src/design-system/Card.tsx`      | Surface reference; `LearnCard` may compose it or match its classes                   |
| `Tag`                                                      | `src/design-system/Tag.tsx`       | Detail-page tag row                                                                  |
| `Eyebrow`                                                  | `src/design-system/Eyebrow.tsx`   | Facet group labels                                                                   |
| `SolidButton` / `OutlineButton`                            | `src/design-system/Button.tsx`    | Detail CTAs, closing band                                                            |
| `CopyCommand`                                              | `src/components/CopyCommand.tsx`  | Detail sidebar CLI rows                                                              |
| `CodeBlock`                                                | `src/design-system/CodeBlock.tsx` | Detail body code                                                                     |
| `DrinkIcon`                                                | `src/components/DrinkIcon.tsx`    | Product mix on cards                                                                 |
| `TemplateStackArt`, `STACK_ICONS`, `PRODUCT_ART`           | `src/components/templates/*`      | Move under `src/components/learn/`, unchanged rendering                              |
| `SearchIcon`, `CheckGlyph`, `ArrowRightIcon`, `GitHubIcon` | `src/icons/*`                     | Search, facet checks, card affordance, source button                                 |

Type and voice utilities: `text-h2`..`text-h6`, `text-caption` from the
`@theme` scale; `font-heading` for titles, `font-body`/`font-mono` per the
existing usage; `text-balance`/`text-pretty` as on sibling pages.

### Proposed new components (all under `src/components/learn/`)

| Proposed name           | Kind             | Responsibility                                                                         |
| ----------------------- | ---------------- | -------------------------------------------------------------------------------------- |
| `LearnCatalog.tsx`      | client component | URL-synced filter state + search + grid orchestration (successor of `TemplateCatalog`) |
| `LearnFacetBar.tsx`     | client component | Content-type pills, search, product filter, template-axis disclosure, clear-all        |
| `LearnCard.tsx`         | component        | Unified summary card for all five types (section 4)                                    |
| `ContentTypeBadge.tsx`  | component        | Per-type tinted chip (section 4.1)                                                     |
| `contentTypeMeta.ts`    | module           | `CONTENT_TYPE_META` record: label form, accent classes, CTA label per type             |
| `LearnEmptyState.tsx`   | component        | Dashed empty panel with scoped copy (section 6.1)                                      |
| `LearnCardSkeleton.tsx` | component        | Loading placeholder card (section 6.2)                                                 |
| `LearnClosing.tsx`      | component        | Closing band (successor of `TemplatesClosing`)                                         |
| `TemplateDetail.tsx`    | component        | Detail layout, moved from `src/components/templates/` and adjusted per section 5       |

### Retired with the old route

`BlueprintBackdrop.tsx`, `TemplatesHero.tsx`, `TemplateCatalog.tsx`,
`TemplateCard.tsx`, `TemplatesClosing.tsx` (superseded by the learn
equivalents above). `TemplateStackArt.tsx`, `productArt.ts`, and
`stackIcons.ts` survive by moving into `src/components/learn/`.
