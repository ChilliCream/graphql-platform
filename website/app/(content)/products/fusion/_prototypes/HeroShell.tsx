import type { ReactNode } from "react";

import { ButtonRow } from "@/src/components/ButtonRow";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";

import { HERO } from "../content";

interface HeroShellProps {
  readonly art: ReactNode;
  readonly align?: "start" | "center";
}

/**
 * Full-bleed hero shell for the Fusion prototype concepts, laid out like the
 * Mocha hero (`app/(content)/products/mocha/ClientPage.tsx`): `art` renders
 * full-bleed behind the hero copy, which is always the verbatim `HERO` copy
 * from `../content` so every concept shows the same words.
 */
export function HeroShell({ art, align = "start" }: HeroShellProps) {
  const centered = align === "center";

  return (
    <div className="relative left-1/2 isolate -mt-26 w-screen -translate-x-1/2 overflow-hidden">
      <section className="relative flex min-h-[88svh] items-center overflow-hidden">
        {art}
        <div className="relative z-10 mx-auto w-full max-w-6xl px-5 sm:px-12">
          <div
            className={`max-w-xl py-24 xl:max-w-2xl ${centered ? "mx-auto text-center" : ""}`}
          >
            <Eyebrow color="accent">{HERO.eyebrow}</Eyebrow>
            <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 text-balance">
              {HERO.title}
            </h1>
            <p className="text-cc-ink mt-6 max-w-xl text-base sm:text-lg">
              {HERO.teaser}
            </p>
            <ButtonRow align={align} className="mt-9">
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
