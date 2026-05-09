# Illustration registers

Per `uplift-plan.md` § CC4 and `vercel-research.md` § "13 design devices", every
redesigned page picks a dominant illustration register and a secondary. There
are four:

- **R1 abstract spatial** (`Orbit`, `Hemisphere`, `PerspectiveGrid`, `RayBurst`,
  `LightCone`) used on `/enterprise` (federation orbit), `/products/nitro/agents`
  (the Loop's ambient dome), `/integrations` (Spotlight orbit-dial), and every
  `/solutions/[slug]` (per-solution micro-diagram).
- **R2 product UI screenshot** built per page from real chrome, not from this
  library.
- **R3 synthetic data viz** (`DashboardComposite` with its panel kinds: trace,
  chart, log-stream, kpi-tile, schema-diff) used on `/products/nitro/observability`
  (full-bleed dashboard hero), `/enterprise` (ROI panels), `/customers` (per-
  customer stat panes), and `/pricing` (calculator output).
- **R4 typographic moment** (`TypographicMoment`) used on `/customers` (customer
  wordmarks), `/enterprise` (ROI numerals), `/pricing` (mini-calculator
  output), and `/solutions/[slug]` (proof numbers).

## Compose, don't author

Pages combine these primitives, they don't ship one-off SVGs. If a page needs a
new visual, the first move is "which composition of existing primitives gets us
80% of the way?" — bespoke art is the last resort, not the first. This is the
same trick Vercel uses (`vercel-research.md` § D13): the same orbit-dial,
hemisphere, and Gantt strip recur across pages with small recolors, but each
_recipe_ is page-specific.

## Examples

The observability hero composes a dashboard with an ambient hemisphere:

```tsx
<DashboardComposite
  panels={["trace", "chart", "log-stream"]}
  bleedDirection="right"
/>
<Hemisphere side="right" />
```

The enterprise ROI band layers an oversized numeral on a ray burst:

```tsx
<RayBurst rayCount={20} />
<TypographicMoment text="47" unit="BFFs → 1 graph" size="huge" variant="gradient" />
```

The integrations Spotlight composes the orbit-dial inside a perspective grid:

```tsx
<PerspectiveGrid density="sparse" />
<Orbit rings={3} rotate={-12} />
```

Each page should commit to two registers max. Mixing all four reads as
inventory display, not visual identity.
