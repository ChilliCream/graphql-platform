import type { CSSProperties } from "react";

import { Swirl } from "@/src/icons/Swirl";
import { Drink, DrinkSpriteMaster, type DrinkId } from "./HeroDrinks";

/**
 * Scatter of product drinks around the headline, mirroring the startpage
 * artwork. Positions are percentages of the hero box so the whole arrangement
 * scales with the viewport; widths use `clamp()` for the same reason. Purely
 * decorative, so the layer is hidden from assistive tech and ignores pointer
 * events.
 */
const DRINK_SCATTER: ReadonlyArray<{
  drink: DrinkId;
  left: string;
  top: string;
  width: string;
  rotate: string;
}> = [
  { drink: "greenDonut", left: "30%", top: "15%", width: "clamp(2.25rem,3.4vw,3.25rem)", rotate: "-8deg" },
  { drink: "nitro", left: "61%", top: "12%", width: "clamp(2rem,3vw,2.75rem)", rotate: "9deg" },
  { drink: "mocha", left: "9%", top: "33%", width: "clamp(2rem,3vw,2.75rem)", rotate: "-6deg" },
  { drink: "cookieCrumble", left: "90%", top: "24%", width: "clamp(2.1rem,3.2vw,3rem)", rotate: "8deg" },
  { drink: "hotChocolate", left: "9%", top: "74%", width: "clamp(2.1rem,3.2vw,3rem)", rotate: "-5deg" },
  { drink: "strawberryShake", left: "33%", top: "85%", width: "clamp(2.1rem,3.2vw,3rem)", rotate: "6deg" },
  { drink: "fusion", left: "55%", top: "85%", width: "clamp(2.25rem,3.4vw,3.25rem)", rotate: "-4deg" },
  { drink: "espresso", left: "90%", top: "80%", width: "clamp(1.8rem,2.6vw,2.5rem)", rotate: "7deg" },
];

const SWIRL_SCATTER: ReadonlyArray<{
  left: string;
  top: string;
  size: string;
  rotate: string;
}> = [
  { left: "21%", top: "16%", size: "1.5rem", rotate: "-12deg" },
  { left: "47%", top: "26%", size: "1.2rem", rotate: "18deg" },
  { left: "78%", top: "30%", size: "1.1rem", rotate: "-8deg" },
  { left: "94%", top: "14%", size: "1.4rem", rotate: "10deg" },
  { left: "15%", top: "52%", size: "1.2rem", rotate: "24deg" },
  { left: "4%", top: "62%", size: "1.4rem", rotate: "-16deg" },
  { left: "95%", top: "52%", size: "1.1rem", rotate: "14deg" },
  { left: "93%", top: "68%", size: "1.4rem", rotate: "-10deg" },
  { left: "50%", top: "74%", size: "1.3rem", rotate: "20deg" },
  { left: "22%", top: "92%", size: "1.4rem", rotate: "-6deg" },
  { left: "73%", top: "92%", size: "1.5rem", rotate: "12deg" },
];

// Brand spectrum (cyan -> violet -> pink) used on the headline accent line.
const ACCENT_GRADIENT =
  "linear-gradient(100deg,#29c5e6 0%,#6e8fe0 30%,#ab86c9 62%,#e87bb4 100%)";

function position(left: string, top: string, extra: CSSProperties): CSSProperties {
  return { left, top, transform: "translate(-50%, -50%)", ...extra };
}

export function HomeHero() {
  return (
    <section className="relative isolate mx-auto flex min-h-[34rem] max-w-6xl flex-col items-center justify-center px-5 py-20 text-center sm:min-h-[40rem] sm:px-12 lg:min-h-[46rem]">
      <DrinkSpriteMaster />

      {/* Decorative scatter, behind the headline. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 -z-10 hidden select-none sm:block"
      >
        {DRINK_SCATTER.map((item) => (
          <Drink
            key={item.drink}
            drink={item.drink}
            className="absolute h-auto drop-shadow-[0_8px_20px_rgba(0,0,0,0.45)]"
            style={position(item.left, item.top, {
              width: item.width,
              rotate: item.rotate,
            })}
          />
        ))}
        {SWIRL_SCATTER.map((item, i) => (
          <Swirl
            key={i}
            className="absolute text-cc-nav-label/60"
            style={position(item.left, item.top, {
              width: item.size,
              height: item.size,
              rotate: item.rotate,
            })}
          />
        ))}
      </div>

      <h1 className="font-heading font-bold tracking-[-0.02em] text-balance text-cc-ink text-h4 sm:text-h3 lg:text-h2 xl:text-h1">
        The API Platform
        <span
          className="block bg-clip-text pb-[0.12em] leading-[1.12] text-transparent"
          style={{ backgroundImage: ACCENT_GRADIENT }}
        >
          for Humans &amp; Agents
        </span>
      </h1>

      <p className="mt-6 max-w-2xl text-pretty text-base text-cc-ink-dim sm:text-xl">
        Unify all your APIs into a comprehensive company graph, streamlining data
        accessibility and enhancing integration. Transform the way you manage and
        interact with your data.
      </p>
    </section>
  );
}
