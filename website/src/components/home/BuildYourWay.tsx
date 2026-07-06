import Link from "next/link";

import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { Fusion } from "@/src/icons/Fusion";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { StrawberryShake } from "@/src/icons/StrawberryShake";

import { DrinkIcon } from "./DrinkIcon";

/** Brand spectrum (cyan -> violet -> coral), the card's pitch background. */
const CARD_GRADIENT =
  "linear-gradient(100deg,#16b9e4 0%,#7c92c6 36%,#b681a9 64%,#f0786a 100%)";

const PRODUCTS = [
  {
    label: "Hot Chocolate",
    role: "GraphQL Server",
    href: "/products/hotchocolate",
    Icon: HotChocolate,
  },
  {
    label: "Fusion",
    role: "Federation Gateway",
    href: "/docs/fusion",
    Icon: Fusion,
  },
  {
    label: "Nitro",
    role: "Control Plane",
    href: "/products/nitro",
    Icon: Nitro,
  },
  {
    label: "Strawberry Shake",
    role: "GraphQL Client",
    href: "/products/strawberryshake",
    Icon: StrawberryShake,
  },
  {
    label: "Mocha",
    role: "Messaging & Mediator",
    href: "/docs/mocha",
    Icon: Mocha,
  },
] as const;

/**
 * "Stack your way" feature card: one rounded, clipped container holding a gradient
 * pitch region on top (the coffee-tray illustration + headline) and a dark product
 * drawer directly beneath it. The two share the card's corners with a seam where
 * they meet, so the product menu reads as the bottom of the card rather than a
 * panel laid over it. Drinks are normalized by {@link DrinkIcon} so they share a
 * baseline. Stacks to a single column on small screens.
 */
export function BuildYourWay() {
  return (
    <section className="mx-auto max-w-7xl px-5 py-8 sm:px-12">
      <div className="mx-auto max-w-7xl overflow-hidden rounded-3xl shadow-[0_24px_50px_rgba(0,0,0,0.4)]">
        {/* Pitch region (top of the card). */}
        <div
          className="px-8 pt-12 pb-12 text-[#0b1018] sm:px-12 sm:pt-14 lg:px-16"
          style={{ backgroundImage: CARD_GRADIENT }}
        >
          <div className="flex flex-col items-center gap-10 md:flex-row md:gap-16">
            <CoffeeTray className="h-44 w-auto flex-none drop-shadow-[0_18px_30px_rgba(0,0,0,0.25)] sm:h-52 lg:h-60" />
            <div className="max-w-xl text-center lg:text-left">
              <h2 className="font-heading text-h4 sm:text-h3 leading-[1.1] font-semibold">
                Start where you are.
                <br />
                Add what you need.
              </h2>
              <p className="mt-5 text-base/relaxed text-[#0b1018]/75 sm:text-lg/relaxed">
                Pick the pieces that fit what you are building: a monolith, a
                federated API, a message-heavy service, or a client application.
                We give you tools that work on their own and fit together when
                your platform grows.
              </p>
            </div>
          </div>
        </div>

        {/* Product drawer (bottom of the SAME card). */}
        <div className="bg-cc-surface px-5 pt-5 pb-5 sm:px-8 sm:pt-6 sm:pb-6">
          <div className="flex items-center gap-3 px-1">
            <span className="bg-cc-ink-faint h-px flex-1" />
            <span className="text-cc-ink-dim font-mono text-[0.65rem] tracking-[0.15em] uppercase">
              Products
            </span>
            <span className="bg-cc-ink-faint h-px flex-1" />
          </div>

          {/* Five products across three layouts. Mobile is a 2-up grid with the
              lone last item centered. The medium tier uses a 6-column grid where
              each tile spans 2 columns: the top three fill columns 1-2/3-4/5-6
              and the 4th tile starts at column 2, so the bottom two settle into
              the gaps between the top three (an "Olympic rings" arrangement).
              Large collapses everything onto a single five-up row. */}
          <nav
            aria-label="ChilliCream products"
            className="mt-3 grid grid-cols-2 gap-1 sm:grid-cols-6 lg:grid-cols-5"
          >
            {PRODUCTS.map(({ label, role, href, Icon }, index) => {
              const isLast = index === PRODUCTS.length - 1;
              const isFirstOnSecondRow = index === 3;
              return (
                <Link
                  key={label}
                  href={href}
                  className={`group focus-visible:ring-cc-accent hover:bg-cc-card-bg/60 flex flex-col items-center gap-2 rounded-2xl border border-transparent px-2 py-3 text-center transition-colors duration-200 focus-visible:ring-2 focus-visible:outline-none sm:col-span-2 lg:col-span-1 ${
                    isLast ? "max-sm:col-span-2" : ""
                  } ${isFirstOnSecondRow ? "sm:col-start-2 lg:col-start-auto" : ""}`}
                >
                  <DrinkIcon
                    Icon={Icon}
                    name={label}
                    base={56}
                    className="transition-transform duration-200 group-hover:-translate-y-1"
                  />
                  <span className="text-cc-heading font-heading text-sm leading-tight font-semibold">
                    {label}
                  </span>
                  <span className="text-cc-ink-dim group-hover:text-cc-ink text-[0.7rem] leading-tight transition-colors duration-200">
                    {role}
                  </span>
                </Link>
              );
            })}
          </nav>
        </div>
      </div>
    </section>
  );
}
