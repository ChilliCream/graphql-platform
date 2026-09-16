# Fusion hero prototypes

Each hero-graphic concept lives in its own folder, `heroes/<Name>/`, with an
`index.tsx` that **default-exports** the hero component. The component takes
**no props** and starts with `"use client"` - `HeroShell` (`../../HeroShell.tsx`)
is a server component, so the hero element is created on the server and the
hooks below only work in a client module.

`HeroShell` renders `{art}` full-bleed behind the hero copy and passes the
copy through unchanged; nothing in this folder touches the copy, `PageHero`,
the diagram panel, or anything below it (`../../FusionPage.tsx` composes
those). One folder per concept; nothing here is shared between concepts, so
they stay file-disjoint.

## 1. The bar

A Fusion hero has to sit next to the Mocha and Nitro heroes without reading
as a different site. It renders a scene, never a diagram: a viewer looks at
it and sees a lit, physical thing, not a labelled schematic. The first ten
concepts (v1..v10, removed) failed this bar: flat SVG strokes in five
saturated colours on plain navy, no light source, no depth, no fine detail, a
marker travelling along a path for motion.

Study the rendered result of both reference implementations before writing a
new concept, not just their code:

- **`src/components/mocha/HeroBoard.tsx` + `board.ts`**: the slate circuit
  structure is static, so it paints once into an offscreen `<canvas>`
  (`prerender`) and the visible canvas just `drawImage`s that layer every
  frame instead of rebuilding it. A live layer on top draws glow pulses with
  radial-gradient halos and additive blending
  (`globalCompositeOperation = "lighter"`). Mono designator labels (`U7`,
  `U12`, ...) set the technical tone. Three gradient scrims (left-to-right,
  bottom, top) keep the copy column dark.
- **`app/(content)/products/nitro/ClientPage.tsx`, `HeroSparks`**: a device
  pixel ratio cap combined with a total pixel-count cap
  (`Math.sqrt(4_000_000 / (width * height))`) keeps the canvas cheap at any
  viewport size. Motes glow through `ctx.shadowBlur` / `ctx.shadowColor`
  instead of flat fills. The draw loop only starts once `useInView` reports
  the canvas is on screen.
- **`app/(content)/products/nitro/ClientPage.tsx`, `HeroAurora`**: blurred
  conic-gradient light cones (`filter: blur(...)` over a `conic-gradient`
  background) layered with `mixBlendMode: "screen"` so light adds instead of
  covering. The only motion is one slow opacity breathe
  (`@keyframes v22-breathe`), proof that a mostly static light field, done
  with real depth and glow, carries a hero on its own.

## 2. Palette

Replaces any five-colour rule. The scene reads as one restrained light
source on a dark ground, not a rainbow:

- **Ground**: page navy (`BRAND.navy` / `CC.bg`). The scene sits on the page
  background; it never repaints the page a different colour.
- **Structure**: slate greys (`BRAND.slate`) at low alpha, the way
  `board.ts`'s `PAD_COLOR`, `HATCH_COLOR` and `TRACE_COLOR` constants do
  (`rgba(...)` strings in the 0.13-0.5 alpha range). Structure is felt as
  shape, not read as a colour.
- **Dominant light**: one accent carries the scene's light, `BRAND.cyan` or
  `BRAND.teal`. Every glow, halo and highlight keys off this one hue.
- **Warm accent**: one warm colour, `BRAND.coral` (or a `SERVICE_SPECTRUM`
  entry used as the warm note), placed only at the hottest point of the scene
  (a core, a fusion seam, a single hot edge). One warm accent, one place in
  the frame.
- **White**: `rgba(255,255,255,a)` is a real, visible colour for hot cores,
  filament centres and specular highlights, the way `HeroAurora`'s beam core
  and `HeroSparks`' brightest mote tints run near-white. White never fills a
  large area.
- **Service colours**: the five `SERVICE_SPECTRUM` colours appear only as
  small identifying accents (a thin strand, a label, a mote), never as a
  saturated, full-strength stroke or fill covering a large area. If a concept
  cannot point at the one small element a service colour identifies, drop
  that colour instead of forcing it in.
- **Source**: every colour still comes from `../../spectrum`
  (`SERVICE_SPECTRUM`, `SPECTRUM_GRADIENT`), `../../../tokens` (`CC`,
  `BRAND`, `FONTS`) or white; no other raw hex literal. Canvas code that
  needs a token as `rgba(...)` (`shadowColor`, a gradient stop, an alpha
  fade) converts it through one local `hexToRgba(hex, alpha)` helper defined
  in the concept's own module: parse the hex once, reuse the result, the same
  idea as `board.ts`'s `GLOW_RGB` constant, not a re-parse at every call
  site and not a new npm colour library.

## 3. Lighting and depth

- Every luminous element carries a glow: `shadowBlur` with `shadowColor`, a
  blurred duplicate layer (`filter: blur(...)`), or a radial-gradient
  falloff. A shape with a hard, unglowing edge is not a light source.
- Light adds instead of covering: `globalCompositeOperation = "lighter"` on
  canvas, `mixBlendMode: "screen"` in DOM, the way `HeroBoard`'s pulse layer
  and `HeroAurora`'s cones both do.
- A vignette darkens the scene's outer edge so the light source reads as the
  brightest point in the frame.
- Directional scrims keep the copy column dark at every width. `HeroBoard`'s
  three overlays are the template: left-to-right (dark at the copy side,
  fading out toward the art), bottom (dark at the bottom edge), top (dark at
  the top edge). Nothing bright renders under the copy column at any
  viewport width from 375px to 1440px.

## 4. Detail at three scales

A hero reads as cheap when it only has one scale of shape. Every concept
needs all three:

- **Macro**: the overall silhouette of the scene (a ring, two merging
  spheres, a filament field). Large shapes never carry a hard vector edge; a
  glow, a gradient falloff or a blur softens every large boundary.
- **Mid**: the structure inside the macro shape (lanes, tiles, ribs, bands).
- **Fine**: texture at close range (filaments, tile seams, motes, grain,
  noise). Fine detail is what a viewer notices on a second look, not the
  first glance.

## 5. Motion

Motion is slow, continuous and physical: flicker, crawl, drift, breathe,
orbit. A shape travelling along a fixed path from point A to point B, like a
marker or a progress indicator, is not physical motion and fails review.

- **Budget**: measured with `performance.now()` around 300 consecutive
  animation frames at a 1440x900 canvas size, the average frame costs 4ms or
  less. A concept that cannot hit this budget needs a cheaper draw, not a
  smaller canvas.
- **Gating**: the animation loop (`requestAnimationFrame`) runs only while
  `useElementMotion` (a hero) or `useSceneMotion` (a visual inside a `Scene`)
  reports `true`; see **Motion gating** below.
- **Reduced motion**: when motion is not allowed, the hero draws one rich
  still frame that carries the whole picture on its own (full palette, full
  detail scales, full scrims), not an empty or half-built first frame.

## 6. Technique

- Canvas 2D is the house technique for a hero's light and structure, the way
  `HeroBoard` and `HeroSparks` use it. Static structure (anything that does
  not move) renders once into an offscreen `<canvas>`, cached, and
  re-rendered only on resize; the visible canvas `drawImage`s that cached
  layer every frame instead of rebuilding the structure from scratch, the
  way `HeroBoard`'s `prerender` / `render` split works. The animated layer
  (pulses, motes, breathing highlights) draws fresh on top of it every
  frame.
- DOM blur layers are allowed for atmosphere, the way `HeroAurora` builds
  light cones from blurred, screen-blended `<div>`s instead of canvas.
  Reach for canvas for anything with per-frame, per-particle state (pulses,
  motes, filaments); DOM/CSS is fine for a broad, mostly static light field.
- No new npm dependencies. Everything above is built from `<canvas>`,
  gradients, filters and `requestAnimationFrame`.
- The root, `aria-hidden`, keyframe-prefix and font rules in the hero box
  contract below stay unchanged for a canvas hero: the canvas still sits
  inside the same `aria-hidden` root, any DOM-layer `@keyframes` still
  carries the `fx-<name>-` prefix, and any on-canvas text still comes from
  `FONTS.mono`.

## 7. Self-critique loop (implementer)

Before handing a concept over for review, run at least three rounds of:

1. Capture the served hero at 1440x900 and at 375x900 (a screenshot of the
   route, not a read of the source).
2. Read both screenshots and write down, in one line, what still looks
   cheap, flat or off-model against sections 1-5 above.
3. Fix it.

Record each round on the ticket as two lines: what the screenshots showed,
and what the fix was. Three rounds is a floor; keep going until a round
finds nothing left to fix.

## 8. Review rubric (reviewer)

Capture `/products/mocha` at 1440x900 to `test-results/mocha-1440.png` in the
same container the hero screenshot came from, view it next to the hero
screenshot, and answer these in writing on the ticket:

- (a) Does the hero sit next to the Mocha screenshot as the same site?
- (b) Does the scene show lighting and depth, or does it read flat?
- (c) Is any large area filled with a single saturated brand or service
  colour?
- (d) Would a design lead ship this?

A negative answer to any of the four is a major finding. The mechanical
gates (`tsc`, `eslint`, `format`, the route serving 200, the 11px label
floor, no horizontal `scrollWidth` overflow, a valid reduced-motion frame)
still apply, but passing them is not acceptance on its own.

## The hero box contract

- The root element is `<div className="absolute inset-0" aria-hidden="true">`.
  `HeroShell`'s section is `relative ... min-h-[88svh] overflow-hidden`, and
  the hero fills it and sits behind the copy, which comes after it in the DOM.
  Do not position, size or scroll the hero section itself, and do not add
  interactive or focusable content - the hero is decorative, the copy above it
  carries the meaning.
- The hero may extend the full viewport width: `HeroShell`'s outer wrapper is
  already `w-screen`.
- Colour follows the **Palette** section above: navy ground, slate structure,
  one dominant cyan/teal light, one warm coral accent at the hottest point,
  white for hot cores and highlights, the five `SERVICE_SPECTRUM` colours
  only as small accents. `../../spectrum` and `../../../tokens` remain the
  only sources of hex values, plus white, per that section. Black remains
  allowed inside `<mask>` luminance layers and gradient alpha stops
  (`stopOpacity`), where it encodes transparency, never a visible colour.
- Fonts come only from `FONTS.*` (SVG/canvas) or the Tailwind font utilities
  (`font-heading`, `font-body`).
- Keyframes live in a `<style>` element inside the hero and are prefixed
  `fx-<name>-` (e.g. `fx-prism-`) so two heroes never collide.
- Canvas 2D is the house technique (see **Technique** above). No new npm
  dependencies.
- The concept owns making its copy readable over the art - any scrim lives
  inside the hero's own root, behind the copy's `z-10` stacking.
- Every label renders at 11px or more on a 375px-wide viewport.
- Decorative only: `aria-hidden`, no focusable content, no page meaning. All
  page meaning lives in `../../../content.ts`.

## Motion gating (`../../../visuals/hooks`)

A hero is not inside a `Scene`, so it gates on its own element:

```tsx
const ref = useRef<HTMLDivElement>(null);
const running = useElementMotion(ref); // in view + tab visible + motion allowed
```

A canvas hero ties its `requestAnimationFrame` loop to that gate directly
instead of animating with CSS, the way `HeroBoard`'s `start` / `stop` / `sync`
functions do:

```tsx
useEffect(() => {
  if (!running) {
    return; // the rest frame already drawn outside this effect stays on screen
  }
  let raf = 0;
  const loop = (time: number) => {
    step(time); // advance physical state (pulses, motes, breathe phase)
    draw(time); // paint the cached static layer + the animated layer on top
    raf = requestAnimationFrame(loop);
  };
  raf = requestAnimationFrame(loop);
  return () => cancelAnimationFrame(raf);
}, [running]);
```

Every animation collapses to a meaningful rest frame when the gate is closed.
That rest frame is what the server render, the reduced-motion render and the
off-screen render all show, so it has to read as a still picture of the
concept on its own, not an empty first frame.

## Route

Each version task owns one route and nothing else:

```tsx
// app/(content)/products/fusion/v1/page.tsx
import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import Prism from "../_prototypes/heroes/Prism";

export const metadata: Metadata = {
  robots: { index: false, follow: false },
};

export default function PrototypeV1Page() {
  return (
    <PrototypeShell version={1}>
      <FusionPage hero={<HeroShell art={<Prism />} />} />
    </PrototypeShell>
  );
}
```

`../versions.ts` already registers v1..v10 with the hero module path in its
`component` field (a plain string - do not turn it into an import), and the
switcher, the sitemap and the llms script already cover the `/products/fusion/vN`
routes via their `/products/fusion/v\d+` pattern.
