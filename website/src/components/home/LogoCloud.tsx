"use client";

import { useEffect, useRef, useState } from "react";
import { FEATURED_COMPANIES, OTHER_COMPANIES, type Company } from "./companies";

const ROTATE_INTERVAL_MS = 3500;
// Let the featured three lead the rotation with a longer first hold, without
// leaving the band static for too long on load.
const INITIAL_HOLD_MS = 4500;
/** The animation delay between each item in the rotation */
const ITEM_ANIMATION_OFFSET_MS = 250;

function wrapIndex(index: number, length: number) {
  return ((index % length) + length) % length;
}

function getItem<T>(index: number, array: T[]) {
  return array[wrapIndex(index, array.length)];
}

const NUM_SLOTS = FEATURED_COMPANIES.length;

export function LogoCloud() {
  const companyQueue = useRef([
    ...shuffle(OTHER_COMPANIES),
    ...FEATURED_COMPANIES,
  ]);
  const currentCompanyIndex = useRef(0);
  const [slots, setSlots] = useState<Company[]>(() => [...FEATURED_COMPANIES]);

  useEffect(() => {
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      return;
    }

    const swap = () => {
      const index = currentCompanyIndex.current;
      const updatedSlots = new Array(NUM_SLOTS)
        .fill(0)
        .map((_, i) => getItem(index + i, companyQueue.current));
      setSlots(updatedSlots);

      currentCompanyIndex.current = wrapIndex(
        currentCompanyIndex.current + NUM_SLOTS,
        companyQueue.current.length,
      );
    };

    // Hold the featured three, then rotate at a steady cadence.
    let intervalId = 0;
    const startId = window.setTimeout(() => {
      swap();
      intervalId = window.setInterval(swap, ROTATE_INTERVAL_MS);
    }, INITIAL_HOLD_MS);

    return () => {
      window.clearTimeout(startId);
      window.clearInterval(intervalId);
    };
  }, []);

  return (
    <section className="mx-auto max-w-7xl px-5 py-12 text-center sm:px-12 sm:py-16">
      <p className="text-cc-ink-dim font-mono text-xs tracking-[0.2em] uppercase">
        Trusted by Enterprises
      </p>
      <div className="text-cc-heading mt-10 grid grid-cols-1 place-items-center gap-y-10 sm:mt-14 sm:grid-cols-3 sm:gap-x-8">
        {slots.map((company, index) => (
          <LogoSlot
            key={index}
            company={company}
            delay={index * ITEM_ANIMATION_OFFSET_MS}
          />
        ))}
      </div>
    </section>
  );
}

function LogoSlot({ company, delay }: { company: Company; delay: number }) {
  const [displayed, setDisplayed] = useState(company);
  const [previous, setPrevious] = useState<Company | null>(null);

  // Adjust state during render when the slot's company changes: promote the
  // outgoing logo to `previous` (it plays the fade-out) and show the new one.
  // The in-animation only runs when there was a previous logo, so the initial
  // server-rendered three appear without a flash.
  if (company.name !== displayed.name) {
    setPrevious(displayed);
    setDisplayed(company);
  }

  return (
    <div className="relative flex h-20 w-full items-center justify-center overflow-hidden">
      {previous && (
        <CompanyLink
          key={previous.name}
          company={previous}
          animationClassName="animate-logo-out"
          onAnimationEnd={() => setPrevious(null)}
          hidden
          delay={delay}
        />
      )}
      <CompanyLink
        key={displayed.name}
        company={displayed}
        animationClassName={previous ? "animate-logo-in" : undefined}
        delay={delay}
      />
    </div>
  );
}

function CompanyLink({
  company,
  animationClassName,
  onAnimationEnd,
  hidden = false,
  delay,
}: {
  company: Company;
  animationClassName?: string;
  onAnimationEnd?: () => void;
  hidden?: boolean;
  delay: number;
}) {
  const { Logo } = company;
  return (
    <a
      href={company.href}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={company.name}
      aria-hidden={hidden}
      tabIndex={hidden ? -1 : undefined}
      onAnimationEnd={onAnimationEnd}
      className={`absolute inset-0 flex items-center justify-center transition-opacity ${
        animationClassName ?? ""
      }`}
      style={{ animationDelay: delay + "ms" }}
    >
      <Logo
        className={`max-w-full ${company.maxHeightClassName ?? "max-h-11"}`}
        style={{ width: `${company.width}px` }}
      />
    </a>
  );
}

function shuffle<T>(items: readonly T[]): T[] {
  const result = [...items];
  for (let i = result.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [result[i], result[j]] = [result[j], result[i]];
  }
  return result;
}
