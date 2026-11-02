import type { CSSProperties } from "react";

import {
  HERO_ACCENT_GRADIENT,
  HERO_DRINKS,
  HERO_DRINKS_MOBILE,
  HERO_HEADLINE,
  HERO_SWIRLS,
  HERO_SWIRLS_MOBILE,
} from "@/src/components/home/heroArtwork";
import { PageSection } from "@/src/components/PageSection";
import { Swirl } from "@/src/icons/Swirl";

function position(
  left: string,
  top: string,
  extra: CSSProperties,
): CSSProperties {
  return { left, top, transform: "translate(-50%, -50%)", ...extra };
}

/**
 * Landing hero: the scattered product drinks and swirl marks around the
 * headline. The artwork composition lives in
 * {@link "@/src/components/home/heroArtwork"} and is shared with the share card;
 * here it renders responsively in the DOM. The scatter is purely decorative, so
 * the layer is hidden from assistive tech and ignores pointer events.
 */
export function HomeHero() {
  return (
    <PageSection className="relative isolate flex min-h-[34rem] flex-col items-center justify-center py-20 text-center sm:min-h-[40rem] lg:min-h-[46rem]">
      {/* Decorative scatter, behind the headline. Trimmed corner composition on
          phones, full scatter from the `sm` breakpoint up. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 -z-10 select-none sm:hidden"
      >
        {HERO_DRINKS_MOBILE.map(({ Drink, left, top, width, rotate }) => (
          <Drink
            key={left + top}
            className="absolute h-auto drop-shadow-[0_8px_20px_rgba(0,0,0,0.45)]"
            style={position(left, top, { width, rotate })}
          />
        ))}
        {HERO_SWIRLS_MOBILE.map(({ left, top, heroSize, rotate }) => (
          <Swirl
            key={left + top}
            className="text-cc-ink-dim/60 absolute"
            style={position(left, top, {
              width: heroSize,
              height: heroSize,
              rotate,
            })}
          />
        ))}
      </div>
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 -z-10 hidden select-none sm:block"
      >
        {HERO_DRINKS.map(({ Drink, left, top, heroWidth, rotate }) => (
          <Drink
            key={left + top}
            className="absolute h-auto drop-shadow-[0_8px_20px_rgba(0,0,0,0.45)]"
            style={position(left, top, { width: heroWidth, rotate })}
          />
        ))}
        {HERO_SWIRLS.map(({ left, top, heroSize, rotate }) => (
          <Swirl
            key={left + top}
            className="text-cc-ink-dim/60 absolute"
            style={position(left, top, {
              width: heroSize,
              height: heroSize,
              rotate,
            })}
          />
        ))}
      </div>

      <h1 className="font-heading text-cc-heading text-h4 sm:text-h3 lg:text-h2 xl:text-h1 font-semibold tracking-[-0.02em] text-balance">
        {HERO_HEADLINE.lead}
        <span
          className="block bg-clip-text pb-[0.12em] leading-[1.12] text-transparent"
          style={{ backgroundImage: HERO_ACCENT_GRADIENT }}
        >
          {HERO_HEADLINE.accent}
        </span>
      </h1>

      <p className="text-cc-ink mt-6 max-w-2xl text-base text-pretty sm:text-xl">
        Unify all your APIs into a comprehensive company graph, streamlining
        data accessibility and enhancing integration. Transform the way you
        manage and interact with your data.
      </p>
    </PageSection>
  );
}
