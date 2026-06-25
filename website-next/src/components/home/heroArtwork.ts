import type { ComponentType, CSSProperties } from "react";

import { CookieCrumble } from "@/src/icons/CookieCrumble";
import { Espresso } from "@/src/icons/Espresso";
import { Fusion } from "@/src/icons/Fusion";
import { GreenDonut } from "@/src/icons/GreenDonut";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { StrawberryShake } from "@/src/icons/StrawberryShake";

/**
 * Single source of truth for the landing hero artwork: the scattered product
 * drinks, the swirl marks, the headline copy, and the accent gradient. Shared
 * by the on-page hero ({@link "@/src/components/home/HomeHero"}) and the
 * Satori-rendered share card ({@link "@/src/og/ShareCard"}) so the composition
 * is defined once. Each consumer applies its own sizing strategy: the hero
 * scales responsively in the DOM, the card uses fixed px for the 1200x630
 * frame.
 */

export interface HeroDrinkComponentProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

export interface HeroDrink {
  readonly Drink: ComponentType<HeroDrinkComponentProps>;
  /** Center x as a percentage of the hero box. */
  readonly left: string;
  /** Center y as a percentage of the hero box. */
  readonly top: string;
  readonly rotate: string;
  /** Responsive `clamp()` width for the on-page hero. */
  readonly heroWidth: string;
  /** Fixed width in px for the share card. */
  readonly cardWidth: number;
  /** Intrinsic height / width, so the card can set an explicit height. */
  readonly aspect: number;
}

export const HERO_DRINKS: readonly HeroDrink[] = [
  {
    Drink: GreenDonut,
    left: "30%",
    top: "15%",
    rotate: "-8deg",
    heroWidth: "clamp(2.25rem,3.4vw,3.25rem)",
    cardWidth: 64,
    aspect: 1.847,
  },
  {
    Drink: Nitro,
    left: "61%",
    top: "12%",
    rotate: "9deg",
    heroWidth: "clamp(2rem,3vw,2.75rem)",
    cardWidth: 58,
    aspect: 1.787,
  },
  {
    Drink: Mocha,
    left: "9%",
    top: "33%",
    rotate: "-6deg",
    heroWidth: "clamp(2rem,3vw,2.75rem)",
    cardWidth: 58,
    aspect: 1.424,
  },
  {
    Drink: CookieCrumble,
    left: "90%",
    top: "24%",
    rotate: "8deg",
    heroWidth: "clamp(2.1rem,3.2vw,3rem)",
    cardWidth: 64,
    aspect: 1.847,
  },
  {
    Drink: HotChocolate,
    left: "9%",
    top: "74%",
    rotate: "-5deg",
    heroWidth: "clamp(2.1rem,3.2vw,3rem)",
    cardWidth: 62,
    aspect: 1.424,
  },
  {
    Drink: StrawberryShake,
    left: "33%",
    top: "85%",
    rotate: "6deg",
    heroWidth: "clamp(2.1rem,3.2vw,3rem)",
    cardWidth: 60,
    aspect: 1.847,
  },
  {
    Drink: Fusion,
    left: "55%",
    top: "85%",
    rotate: "-4deg",
    heroWidth: "clamp(2.25rem,3.4vw,3.25rem)",
    cardWidth: 64,
    aspect: 1.424,
  },
  {
    Drink: Espresso,
    left: "90%",
    top: "80%",
    rotate: "7deg",
    heroWidth: "clamp(1.8rem,2.6vw,2.5rem)",
    cardWidth: 54,
    aspect: 1.109,
  },
];

/**
 * Trimmed scatter for small screens. The full desktop composition is too dense
 * for a phone, so mobile gets four drinks pinned to the corners (two above the
 * headline, two below the copy) with their own sizes and positions, kept clear
 * of the centered text.
 */
export interface HeroMobileDrink {
  readonly Drink: ComponentType<HeroDrinkComponentProps>;
  /** Center x as a percentage of the hero box. */
  readonly left: string;
  /** Center y as a percentage of the hero box. */
  readonly top: string;
  readonly rotate: string;
  /** Fixed width for the on-page hero (clamp keeps it sensible across phones). */
  readonly width: string;
}

export const HERO_DRINKS_MOBILE: readonly HeroMobileDrink[] = [
  {
    Drink: GreenDonut,
    left: "17%",
    top: "13%",
    rotate: "-8deg",
    width: "clamp(2.75rem,11vw,3.25rem)",
  },
  {
    Drink: Nitro,
    left: "83%",
    top: "13%",
    rotate: "9deg",
    width: "clamp(2.5rem,10vw,3rem)",
  },
  {
    Drink: StrawberryShake,
    left: "22%",
    top: "87%",
    rotate: "6deg",
    width: "clamp(2.6rem,10.5vw,3rem)",
  },
  {
    Drink: Fusion,
    left: "78%",
    top: "87%",
    rotate: "-4deg",
    width: "clamp(2.75rem,11vw,3.25rem)",
  },
];

export interface HeroSwirl {
  readonly left: string;
  readonly top: string;
  readonly rotate: string;
  /** Responsive size for the on-page hero. */
  readonly heroSize: string;
  /** Fixed size in px for the share card. */
  readonly cardSize: number;
}

export const HERO_SWIRLS: readonly HeroSwirl[] = [
  {
    left: "21%",
    top: "16%",
    rotate: "-12deg",
    heroSize: "1.5rem",
    cardSize: 24,
  },
  {
    left: "47%",
    top: "26%",
    rotate: "18deg",
    heroSize: "1.2rem",
    cardSize: 19,
  },
  {
    left: "78%",
    top: "30%",
    rotate: "-8deg",
    heroSize: "1.1rem",
    cardSize: 18,
  },
  {
    left: "94%",
    top: "14%",
    rotate: "10deg",
    heroSize: "1.4rem",
    cardSize: 22,
  },
  {
    left: "15%",
    top: "52%",
    rotate: "24deg",
    heroSize: "1.2rem",
    cardSize: 19,
  },
  {
    left: "4%",
    top: "62%",
    rotate: "-16deg",
    heroSize: "1.4rem",
    cardSize: 22,
  },
  {
    left: "95%",
    top: "52%",
    rotate: "14deg",
    heroSize: "1.1rem",
    cardSize: 18,
  },
  {
    left: "93%",
    top: "68%",
    rotate: "-10deg",
    heroSize: "1.4rem",
    cardSize: 22,
  },
  {
    left: "50%",
    top: "74%",
    rotate: "20deg",
    heroSize: "1.3rem",
    cardSize: 21,
  },
  {
    left: "22%",
    top: "92%",
    rotate: "-6deg",
    heroSize: "1.4rem",
    cardSize: 22,
  },
  {
    left: "73%",
    top: "92%",
    rotate: "12deg",
    heroSize: "1.5rem",
    cardSize: 24,
  },
];

/**
 * Sparse swirl set for small screens: a few marks tucked into the vertical gaps
 * around the centered headline and copy, none overlapping the text.
 */
export const HERO_SWIRLS_MOBILE: readonly HeroSwirl[] = [
  {
    left: "11%",
    top: "38%",
    rotate: "-12deg",
    heroSize: "1.3rem",
    cardSize: 21,
  },
  {
    left: "82%",
    top: "28%",
    rotate: "16deg",
    heroSize: "1.2rem",
    cardSize: 19,
  },
  {
    left: "13%",
    top: "73%",
    rotate: "20deg",
    heroSize: "1.2rem",
    cardSize: 19,
  },
  {
    left: "86%",
    top: "75%",
    rotate: "-8deg",
    heroSize: "1.3rem",
    cardSize: 21,
  },
  {
    left: "50%",
    top: "15%",
    rotate: "14deg",
    heroSize: "1.2rem",
    cardSize: 19,
  },
  {
    left: "50%",
    top: "89%",
    rotate: "-16deg",
    heroSize: "1.2rem",
    cardSize: 19,
  },
];

export const HERO_HEADLINE = {
  /** Top line, rendered in the ink color. */
  lead: "The API Platform",
  /** Bottom line, rendered with {@link HERO_ACCENT_GRADIENT}. */
  accent: "for Humans & Agents",
} as const;

/** Brand spectrum (cyan -> violet -> coral) used on the headline accent line. */
export const HERO_ACCENT_GRADIENT =
  "linear-gradient(100deg,#16b9e4 0%,#7c92c6 33%,#b681a9 63%,#f0786a 100%)";
