# Mission Control hero approaches

`MissionControl.tsx` takes an optional `hero` prop and renders it full-bleed
behind the hero copy. It defaults to `../WallMap`, so concept v1 is unchanged;
versions v11..v15 branch v1 by passing a hero from this folder and nothing
else. Everything below the hero - the four console rows, the Nitro band, the
copy, the palette - stays exactly as v1 renders it.

## One hero per approach

One file per approach, `heroes/<Name>.tsx`, or a folder `heroes/<Name>/` with
an `index.tsx` when an approach needs helpers of its own. Nothing in this
folder is shared between approaches: they are developed concurrently and must
stay file-disjoint.

The module **default-exports** the hero component, it takes **no props**, and
it starts with `"use client"` - `MissionControl.tsx` is a server component, so
the hero element is created on the server and the hooks below only work in a
client module.

## The hero box contract

Same contract as `../WallMap.tsx`, which is the reference implementation:

- The root element is `<div className="absolute inset-0" aria-hidden="true">`.
  It fills the hero section (`relative ... min-h-[88svh] overflow-hidden`) and
  sits behind both the scrim and the `PageSection` copy, which come after it in
  the DOM. Do not position, size or scroll the hero section itself, and do not
  add interactive or focusable content - the hero is decorative, the copy above
  it carries the meaning.
- The visual is `aria-hidden`, so every word inside it is decoration. All page
  meaning lives in `../../../copy.ts`.
- Colour comes from `../palette.ts` roles (`MC`, `CLIENTS`, `STATIONS`,
  `SOURCES`, `specTag`) and `../../../brand.ts` (`CC`, `BRAND`, `FONTS`); SVG text
  sizes come from `TYPE`. No raw hex, no new npm dependencies.
- `../palette.ts` is shared by v1..v15 and by the console visuals: never edit
  it from a hero. Content an approach needs that it does not already hold (a
  fourth client, a per-subgraph language or spec badge the epic asks for) stays
  local to the hero module.
- Keyframes live in a `<style>` element inside the hero and are prefixed per
  hero (`mc-wall-*` for the wall map) so two heroes never collide.

## Motion gating (`../hooks.ts`)

A hero is not inside a `Scene`, so it gates on its own element:

```tsx
const ref = useRef<HTMLDivElement>(null);
const running = useElementMotion(ref); // in view + tab visible + motion allowed
const step = useCycle(running, STEPS, 1200, REST_STEP);
```

- `useElementMotion(ref)` is the gate; `useSceneMotion()` is for the bounded
  scene visuals further down the page, not for a hero.
- Every animation goes through `anim(running, "...")` so it collapses to `none`
  when the gate is closed, and every non-CSS-animated transform states its rest
  value explicitly (`transform: running ? undefined : "rotate(-34deg)"`).
- `useCycle` parks on its `rest` step when the gate is closed. The rest frame is
  what the server render, the reduced-motion render and the off-screen render
  all show, so it has to be a meaningful still picture of the architecture on
  its own - not an empty first frame.

## The 11px label floor at 375px

Every label must render at **11 rendered px or more on a 375px-wide viewport**
(`TYPE.label` is that floor). The hero is full-bleed, not a fixed-ratio scene
box, so an SVG that letterboxes to fit shrinks its lettering below the floor:
a 1200-unit-wide viewBox with `xMidYMid meet` lands at 375/1200 = 0.31x, and
`TYPE.caption` (14) would render at about 4px.

`WallMap` solves this with `preserveAspectRatio="xMidYMid slice"` on a
`1200 x 700` viewBox: `slice` scales by the _larger_ ratio, so on a 375px
viewport the ~590px-tall hero drives the scale to about 0.84x, `TYPE.caption`
renders at roughly 11.8px, and the narrow viewport simply shows less of the
map. Take the same approach - fill, then let the edges crop - and keep what
matters (gateway, clients, subgraph names, language and spec tags) inside the
central band that survives the crop. An approach that cannot fill may instead
switch its geometry at small widths, but the floor is not negotiable.

## Route

Each version task owns one route and nothing else:

```tsx
// app/(content)/products/fusion/v11/page.tsx
export const metadata: Metadata = {
  /* ... */ robots: { index: false, follow: false },
};

export default function PrototypeV11Page() {
  return (
    <PrototypeShell version={11}>
      <MissionControl hero={<LayeredDiagram />} />
    </PrototypeShell>
  );
}
```

`../../../versions.ts` already registers v11..v15 with the hero module path in
its `component` field (a plain string - do not turn it into an import), and the
switcher, the sitemap and the llms script already cover the new routes via
their `/products/fusion/v\d+` pattern.
