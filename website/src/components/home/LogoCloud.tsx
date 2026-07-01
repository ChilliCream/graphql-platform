"use client";

import { useEffect, useRef, useState } from "react";
import {
  ALL_COMPANIES,
  FEATURED_COMPANIES,
  OTHER_COMPANIES,
  type Company,
} from "./companies";

const ROTATE_INTERVAL_MS = 2600;
// Let the featured three lead the rotation with a longer first hold, without
// leaving the band static for too long on load.
const INITIAL_HOLD_MS = 4500;

export function LogoCloud() {
  const [slots, setSlots] = useState<Company[]>(() => [...FEATURED_COMPANIES]);

  // Mirror the latest slots so the interval can read them without re-subscribing.
  const slotsRef = useRef(slots);
  useEffect(() => {
    slotsRef.current = slots;
  }, [slots]);

  useEffect(() => {
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      return;
    }

    // Shuffle bag: every customer is drawn once before any repeats. Seed it
    // with the companies not already on screen (the featured three).
    let deck = shuffle(OTHER_COMPANIES);

    // Age of the logo in each slot; the oldest is always evicted first. Seed the
    // featured three with a random order so which one leaves first is random.
    const placedAt = shuffle([0, 1, 2]);
    let sequence = placedAt.length - 1;

    const swap = () => {
      const current = slotsRef.current;
      const visible = new Set(current.map((company) => company.name));

      if (deck.length === 0) {
        // Whole roster shown; start a fresh cycle over everyone off screen.
        deck = shuffle(
          ALL_COMPANIES.filter((company) => !visible.has(company.name)),
        );
      }

      let next: Company | undefined;
      while (deck.length > 0) {
        const candidate = deck.shift();
        if (candidate && !visible.has(candidate.name)) {
          next = candidate;
          break;
        }
      }
      if (!next) {
        return;
      }

      // Evict the slot whose logo has been on screen the longest.
      let slot = 0;
      for (let index = 1; index < placedAt.length; index++) {
        if (placedAt[index] < placedAt[slot]) {
          slot = index;
        }
      }
      sequence += 1;
      placedAt[slot] = sequence;

      const updated = [...current];
      updated[slot] = next;
      setSlots(updated);
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
          <LogoSlot key={index} company={company} />
        ))}
      </div>
    </section>
  );
}

function LogoSlot({ company }: { company: Company }) {
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
        />
      )}
      <CompanyLink
        key={displayed.name}
        company={displayed}
        animationClassName={previous ? "animate-logo-in" : undefined}
      />
    </div>
  );
}

function CompanyLink({
  company,
  animationClassName,
  onAnimationEnd,
  hidden = false,
}: {
  company: Company;
  animationClassName?: string;
  onAnimationEnd?: () => void;
  hidden?: boolean;
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
