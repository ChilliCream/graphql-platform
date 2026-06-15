import type { CSSProperties } from "react";

import { STARTPAGE_SPRITE_MARKUP } from "./startpageSpriteMarkup";

/**
 * Each ChilliCream product is drawn as one drink in a single shared artwork
 * (`public/images/startpage-sprite.svg`, 800x200). Rather than slice the file,
 * the whole sprite is inlined once as a hidden master (see {@link DrinkSpriteMaster})
 * and every visible drink is cropped out of it with `<use>` plus a per-drink
 * `viewBox` window. The gradients live in the master's `<defs>` and resolve
 * document-wide, so the cropped instances render identically to the source.
 *
 * The `id`s are the group ids from the source file; the `viewBox` values are the
 * measured group bounding boxes (padded a little) in the sprite's coordinate
 * space, left to right.
 */
export type DrinkId =
  | "greenDonut"
  | "strawberryShake"
  | "cookieCrumble"
  | "fusion"
  | "mocha"
  | "hotChocolate"
  | "nitro"
  | "espresso";

type DrinkSpec = {
  /** Product name, surfaced as the accessible label. */
  readonly name: string;
  /** Source group id inside the sprite master. */
  readonly ref: string;
  /** Crop window into the sprite coordinate space. */
  readonly viewBox: string;
};

export const DRINKS: Record<DrinkId, DrinkSpec> = {
  greenDonut: { name: "Green Donut", ref: "uuid-af369a32-3769-43e7-afe2-9e12713569c0", viewBox: "7 9 59 109" },
  strawberryShake: { name: "Strawberry Shake", ref: "uuid-469c28bd-2881-4283-96b0-8eccde386705", viewBox: "107 7 59 109" },
  cookieCrumble: { name: "Cookie Crumble", ref: "uuid-deab248e-2b91-4faa-b7d2-a517f9d2168a", viewBox: "207 7 59 109" },
  fusion: { name: "Fusion", ref: "uuid-f31d5226-8a00-4c71-a6dc-79bebb400524", viewBox: "276 5 122 122" },
  mocha: { name: "Mocha", ref: "uuid-ef30f771-ceb9-4d15-8744-3868cb8db0a1", viewBox: "407 32 59 84" },
  hotChocolate: { name: "Hot Chocolate", ref: "uuid-be6db7e2-4dca-4a2a-8d97-dcfec0ce7ab3", viewBox: "507 32 59 84" },
  nitro: { name: "Nitro", ref: "uuid-52f1d3f9-8ddf-4664-a015-ce7260c04625", viewBox: "607 32 47 84" },
  espresso: { name: "Espresso", ref: "uuid-cced5aef-3e2b-45f3-967e-d0a5b10474e0", viewBox: "696 55 55 61" },
};

/**
 * Inlines the full drink artwork once, hidden, so the cropped {@link Drink}
 * instances have a source to reference. Render this exactly once per page.
 */
export function DrinkSpriteMaster() {
  return (
    <svg
      aria-hidden="true"
      width={0}
      height={0}
      style={{ position: "absolute", width: 0, height: 0, overflow: "hidden" }}
      dangerouslySetInnerHTML={{ __html: STARTPAGE_SPRITE_MARKUP }}
    />
  );
}

type DrinkProps = {
  drink: DrinkId;
  className?: string;
  style?: CSSProperties;
};

/** A single product drink, cropped from the shared sprite master. */
export function Drink({ drink, className, style }: DrinkProps) {
  const spec = DRINKS[drink];

  return (
    <svg
      viewBox={spec.viewBox}
      role="img"
      aria-label={spec.name}
      className={className}
      style={style}
    >
      <use href={`#${spec.ref}`} />
    </svg>
  );
}
