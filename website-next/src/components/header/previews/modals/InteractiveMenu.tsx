"use client";

import { motion } from "motion/react";
import Link from "next/link";
import { type ComponentType, useEffect, useRef, useState } from "react";

import { NAV_ITEMS, type NavItem } from "@/src/components/header/navData";
import { SolidButton } from "@/src/design-system/Button";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";
import { ChilliCreamWinking } from "@/src/icons/ChilliCreamWinking";
import { GitHubIcon } from "@/src/icons/GitHub";
import { SearchIcon } from "@/src/icons/Search";

interface InteractiveMenuProps {
  /** The candidate dropdown design, mounted for whichever section is active. */
  readonly Panel: ComponentType<{ readonly item: NavItem }>;
}

// Open the first section with a dropdown by default so each menu shows content
// to compare; hovering any section swaps it, leaving returns to the default.
const DEFAULT_INDEX = NAV_ITEMS.findIndex((item) => item.groups);

/**
 * A working copy of the real site header: hovering or focusing any section with
 * a dropdown (Platform, Services, Developers, Company) opens the given `Panel`
 * design populated with that section's links; Pricing and Help stay plain
 * links. Used to preview a dropdown design across every menu section at once.
 */
export function InteractiveMenu({ Panel }: InteractiveMenuProps) {
  const [active, setActive] = useState(DEFAULT_INDEX);
  const activeItem = active >= 0 ? NAV_ITEMS[active] : null;

  // Hover intent: keep the panel from snapping shut as the pointer travels
  // diagonally toward it, the close delay NN/g recommends for mega menus.
  const closeTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const cancelClose = () => {
    if (closeTimer.current) {
      clearTimeout(closeTimer.current);
      closeTimer.current = null;
    }
  };
  const open = (index: number) => {
    cancelClose();
    setActive(index);
  };
  const scheduleClose = () => {
    cancelClose();
    closeTimer.current = setTimeout(() => setActive(DEFAULT_INDEX), 350);
  };
  useEffect(() => cancelClose, []);

  return (
    <div
      className="border-cc-card-border bg-cc-bg overflow-x-auto rounded-xl border"
      onMouseEnter={cancelClose}
      onMouseLeave={scheduleClose}
    >
      <div className="min-w-[1100px]">
        <div className="border-cc-white/10 bg-cc-card-bg flex h-18 w-full items-center justify-between gap-8 border-b px-4 backdrop-blur-[18px] backdrop-saturate-150">
          <span className="text-cc-ink flex flex-none items-center">
            <ChilliCreamWinking className="h-8 w-8 fill-current" />
          </span>

          <nav className="flex h-full flex-1 items-stretch">
            <ol className="m-0 flex h-full list-none items-stretch gap-1 p-0">
              {NAV_ITEMS.map((item, index) =>
                item.groups ? (
                  <li key={item.label} className="relative flex items-stretch">
                    <button
                      type="button"
                      onMouseEnter={() => open(index)}
                      onFocus={() => open(index)}
                      onClick={() => open(index)}
                      aria-expanded={active === index}
                      className={[
                        "flex cursor-pointer items-center gap-1.5 px-4 text-sm font-medium transition-colors",
                        active === index
                          ? "text-cc-ink"
                          : "text-cc-ink-dim hover:text-cc-ink",
                      ].join(" ")}
                    >
                      {item.label}
                      <ChevronDownIcon
                        className={[
                          "h-3 w-3 fill-current transition-transform",
                          active === index ? "rotate-180" : "",
                        ].join(" ")}
                      />
                    </button>
                    {active === index ? (
                      <motion.span
                        layoutId="menu-underline"
                        aria-hidden="true"
                        transition={{
                          type: "spring",
                          stiffness: 500,
                          damping: 40,
                        }}
                        className="bg-cc-accent absolute right-3 bottom-0 left-3 h-0.5 rounded-full"
                      />
                    ) : null}
                  </li>
                ) : (
                  <li key={item.label} className="flex items-stretch">
                    <Link
                      href={item.href}
                      className="text-cc-ink-dim hover:text-cc-ink flex items-center px-4 text-sm font-medium no-underline transition-colors"
                    >
                      {item.label}
                    </Link>
                  </li>
                ),
              )}
            </ol>
          </nav>

          <div className="flex flex-none items-center gap-5">
            <span className="border-cc-card-border bg-cc-hover text-cc-ink-dim inline-flex items-stretch overflow-hidden rounded-md border text-xs font-medium">
              <span className="inline-flex items-center gap-1.5 px-2 py-1">
                <GitHubIcon className="text-cc-ink h-3.5 w-3.5 fill-current" />
                Star
              </span>
              <span className="border-cc-card-border inline-flex items-center border-l px-2 py-1 tabular-nums">
                5,723
              </span>
            </span>
            <Link
              href="/services/support/contact"
              className="text-cc-ink-dim hover:text-cc-ink text-sm font-medium no-underline transition-colors"
            >
              Contact Us
            </Link>
            <SolidButton
              href="https://nitro.chillicream.com"
              className="h-10 py-0"
            >
              Launch
            </SolidButton>
            <span className="text-cc-ink-dim flex items-center">
              <SearchIcon className="h-4 w-4 fill-current" />
            </span>
          </div>
        </div>

        {/* The dropdown opens in flow under the bar, inset to sit under the nav,
            with the small gap the live header uses. */}
        <div className="min-h-[340px] px-4 pt-2 pb-12 pl-12">
          {activeItem ? <Panel key={active} item={activeItem} /> : null}
        </div>
      </div>
    </div>
  );
}
