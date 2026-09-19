import { ButtonRow } from "@/src/components/ButtonRow";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";

import { HERO } from "../content";
import Tokamak from "./Tokamak";

/**
 * The Fusion product page's hero: the Tokamak plasma-torus scene full-bleed
 * behind the hero copy, laid out like the Mocha hero
 * (`app/(content)/products/mocha/ClientPage.tsx`).
 */
export function FusionHero() {
  return (
    <div className="relative left-1/2 isolate -mt-26 w-screen -translate-x-1/2 overflow-hidden">
      <section className="relative flex min-h-[88svh] items-center overflow-hidden">
        <Tokamak />
        <div className="relative z-10 mx-auto w-full max-w-6xl px-5 sm:px-12">
          <div className="max-w-xl py-24 xl:max-w-2xl">
            <Eyebrow color="accent">{HERO.eyebrow}</Eyebrow>
            <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 text-balance">
              {HERO.title}
            </h1>
            <p className="text-cc-ink mt-6 max-w-xl text-base sm:text-lg">
              {HERO.teaser}
            </p>
            <ButtonRow align="start" className="mt-9">
              <SolidButton href={HERO.buttons[0].href}>
                {HERO.buttons[0].label}
              </SolidButton>
              <OutlineButton href={HERO.buttons[1].href}>
                {HERO.buttons[1].label}
              </OutlineButton>
            </ButtonRow>
          </div>
        </div>
      </section>
    </div>
  );
}
