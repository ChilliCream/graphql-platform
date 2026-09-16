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

## The hero box contract

- The root element is `<div className="absolute inset-0" aria-hidden="true">`.
  `HeroShell`'s section is `relative ... min-h-[88svh] overflow-hidden`, and
  the hero fills it and sits behind the copy, which comes after it in the DOM.
  Do not position, size or scroll the hero section itself, and do not add
  interactive or focusable content - the hero is decorative, the copy above it
  carries the meaning.
- The hero may extend the full viewport width: `HeroShell`'s outer wrapper is
  already `w-screen`.
- Colour comes only from `../../spectrum` (`SERVICE_SPECTRUM`,
  `SPECTRUM_GRADIENT`) and `../../../tokens` (`CC`, `BRAND`, `FONTS`); no other
  raw hex. White and black are allowed solely inside `<mask>` luminance
  layers and gradient alpha stops (`stopOpacity`), never as a visible fill or
  stroke colour.
- Fonts come only from `FONTS.*` (SVG/canvas) or the Tailwind font utilities
  (`font-heading`, `font-body`).
- Keyframes live in a `<style>` element inside the hero and are prefixed
  `fx-<name>-` (e.g. `fx-prism-`) so two heroes never collide.
- Canvas is allowed (Mocha's `HeroBoard` precedent). No new npm dependencies.
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
