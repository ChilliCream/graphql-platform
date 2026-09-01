"use client";

import { useEffect, useRef, useState, useSyncExternalStore } from "react";
import type { CSSProperties, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";

import { CANON, GatewayChip, HorizonRule, MicroLabel } from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";

/**
 * The five lamp homes, as a percentage of the ambient layer's own box
 * (which spans the section's max-w-5xl stage, not the raw browser
 * viewport): Catalog top-left, Billing left-low, Ordering top-right,
 * Shipping right-low, User top-center - matching the five ownership dots on
 * the product-page card in chapter 1.
 */
const LAMP_HOMES = [
  { name: "Catalog", left: 12, top: 18, color: CANON[0].color },
  { name: "Billing", left: 6, top: 64, color: CANON[1].color },
  { name: "Ordering", left: 85, top: 12, color: CANON[2].color },
  { name: "Shipping", left: 92, top: 58, color: CANON[3].color },
  { name: "User", left: 55, top: 6, color: CANON[4].color },
] as const;

type LampAlpha = readonly [number, number, number, number, number];

interface BeatRecipe {
  /** Opacity of each of the five lamps, in LAMP_HOMES order. */
  readonly alpha: LampAlpha;
  /** Whether the lamps drift ~6% toward center this beat (B2 only). */
  readonly drift: boolean;
  /** The B3 harsh white flood's opacity, 0 when hidden. */
  readonly flood: number;
  /** The B7 gateway teal glow's opacity, 0 when hidden. */
  readonly gatewayGlow: number;
  /** Whether the one-time lamp name labels are visible (B1 only). */
  readonly labels: boolean;
}

/**
 * One recipe per chapter/beat (0-indexed, matching CHAPTERS). Alpha values
 * stay within the placement guard's ambient cap (<=16% away from copy,
 * <=8% directly behind it) - the lamps live at the stage's periphery, well
 * clear of the centered reading column.
 */
const BEATS: readonly BeatRecipe[] = [
  // B1: five lamps at home, dim, named once.
  {
    alpha: [0.08, 0.08, 0.08, 0.08, 0.08],
    drift: false,
    flood: 0,
    gatewayGlow: 0,
    labels: true,
  },
  // B2: every app gathers all five lights onto itself.
  {
    alpha: [0.1, 0.1, 0.1, 0.1, 0.1],
    drift: true,
    flood: 0,
    gatewayGlow: 0,
    labels: false,
  },
  // B3: one team's harsh white flood washes the others out. Flood capped at
  // 0.06 to keep the ambient layer's behind-copy delta within the placement
  // guard's <=8% cap.
  {
    alpha: [0.04, 0.04, 0.04, 0.04, 0.04],
    drift: false,
    flood: 0.06,
    gatewayGlow: 0,
    labels: false,
  },
  // B4: flood gone, colors restored, nothing moved.
  {
    alpha: [0.12, 0.12, 0.12, 0.12, 0.12],
    drift: false,
    flood: 0,
    gatewayGlow: 0,
    labels: false,
  },
  // B5: only Catalog and Billing brighten - each document lit by its own lamp.
  {
    alpha: [0.14, 0.14, 0.05, 0.05, 0.05],
    drift: false,
    flood: 0,
    gatewayGlow: 0,
    labels: false,
  },
  // B6: all five equal - composition is lit by all five at once.
  {
    alpha: [0.14, 0.14, 0.14, 0.14, 0.14],
    drift: false,
    flood: 0,
    gatewayGlow: 0,
    labels: false,
  },
  // B7: runtime - the services are still there, answering the executor.
  {
    alpha: [0.1, 0.1, 0.1, 0.1, 0.1],
    drift: false,
    flood: 0,
    gatewayGlow: 0.1,
    labels: false,
  },
];

function subscribeToReducedMotion(callback: () => void): () => void {
  if (typeof window === "undefined" || !window.matchMedia) return () => {};
  const query = window.matchMedia("(prefers-reduced-motion: reduce)");
  query.addEventListener("change", callback);
  return () => query.removeEventListener("change", callback);
}

function getReducedMotionSnapshot(): boolean {
  if (typeof window === "undefined" || !window.matchMedia) return false;
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

function getReducedMotionServerSnapshot(): boolean {
  return false;
}

/**
 * `useSyncExternalStore` (rather than a `useState` + `useEffect` pair) so
 * the real value is picked up on the client's very first paint after
 * hydration, not only after the OS setting later changes mid-session.
 */
function usePrefersReducedMotion(): boolean {
  return useSyncExternalStore(
    subscribeToReducedMotion,
    getReducedMotionSnapshot,
    getReducedMotionServerSnapshot,
  );
}

interface AmbientLayerProps {
  readonly beat: number;
  readonly reducedMotion: boolean;
}

/**
 * The five peripheral lamp-glows, plus the B3 flood and B7 gateway glow.
 * position:sticky within the section's own stage so it stays framed for the
 * whole scroll; the lamps never move except for the B2 center-drift. Hidden
 * below `sm` per the mobile note - the concept's payload lives on the cards.
 */
function AmbientLayer({ beat, reducedMotion }: AmbientLayerProps) {
  const recipe = BEATS[beat] ?? BEATS[0];
  const transition = reducedMotion
    ? "none"
    : "opacity 700ms ease, transform 900ms ease";

  return (
    <div
      aria-hidden="true"
      className="pointer-events-none sticky top-0 z-0 hidden h-screen w-full overflow-hidden sm:block"
    >
      {LAMP_HOMES.map((lamp, i) => {
        const drift = recipe.drift && !reducedMotion;
        const dxVw = drift ? (50 - lamp.left) * 0.06 : 0;
        const dyVh = drift ? (50 - lamp.top) * 0.06 : 0;
        return (
          <div
            key={lamp.name}
            className="absolute aspect-square w-[44vmin] rounded-full"
            style={{
              left: `${lamp.left}%`,
              top: `${lamp.top}%`,
              transform: `translate(-50%, -50%) translate(${dxVw}vw, ${dyVh}vh)`,
              background: `radial-gradient(closest-side, ${lamp.color} 0%, transparent 70%)`,
              mixBlendMode: "screen",
              opacity: recipe.alpha[i],
              transition,
            }}
          />
        );
      })}

      {LAMP_HOMES.map((lamp) => (
        <div
          key={`${lamp.name}-label`}
          className="absolute whitespace-nowrap"
          style={{
            left: `${lamp.left}%`,
            top: `${lamp.top}%`,
            transform: "translate(-50%, -50%) translateY(30px)",
            opacity: recipe.labels ? 1 : 0,
            transition,
          }}
        >
          <MicroLabel>{lamp.name}</MicroLabel>
        </div>
      ))}

      <div
        className="absolute top-1/2 left-1/2 h-[60vmax] w-[60vmax] -translate-x-1/2 -translate-y-1/2 rounded-full"
        style={{
          background: "#ffffff",
          opacity: recipe.flood,
          mixBlendMode: "screen",
          transition,
        }}
      />

      <div
        className="absolute bottom-0 left-1/2 h-[50vmax] w-[70vmax] translate-x-[-50%] translate-y-[50%] rounded-full"
        style={{
          background:
            "radial-gradient(closest-side, #5eead4 0%, transparent 70%)",
          mixBlendMode: "screen",
          opacity: recipe.gatewayGlow,
          transition,
        }}
      />
    </div>
  );
}

function hexWithAlpha(hex: string, alpha: number): string {
  const a = Math.round(alpha * 255)
    .toString(16)
    .padStart(2, "0");
  return `${hex}${a}`;
}

const FIVE_RIM_GRADIENT =
  "conic-gradient(from 210deg, #f27765 0 17%, #eabd21 23% 37%, #66be77 43% 57%, #00bce5 63% 77%, #a983ba 83% 100%)";

interface RimBoxProps {
  readonly rim: "five" | string;
  readonly active: boolean;
  readonly reducedMotion: boolean;
  readonly children: ReactNode;
}

/**
 * Wraps a ProtoCodeBox in a 1px gradient ring: the five-color conic rim for
 * a schema lit by all five lamps (B2's client card, B6's composite), or a
 * single service color at 55% alpha for a schema lit only by its own team
 * (B5). The ring is drawn as a `-inset-px` overlay just outside the box's own
 * border rather than masked into a padding ring, and its visibility is
 * gated by `active` (only the current beat's card is lit) with a fade
 * driven by `reducedMotion`.
 */
function RimBox({ rim, active, reducedMotion, children }: RimBoxProps) {
  const isFive = rim === "five";
  const background = isFive ? FIVE_RIM_GRADIENT : hexWithAlpha(rim, 0.55);
  const shadowColor = isFive ? CANON[0].color : rim;

  const ringStyle: CSSProperties = {
    background,
    boxShadow: `0 0 28px 0 ${hexWithAlpha(shadowColor, 0.12)}`,
    opacity: active ? 1 : 0,
    transition: reducedMotion ? "none" : "opacity 700ms ease",
  };

  return (
    <div className="relative rounded-xl">
      <div
        aria-hidden="true"
        className="absolute -inset-px rounded-[13px]"
        style={ringStyle}
      />
      <div className="relative">{children}</div>
    </div>
  );
}

/** Which chapter/box pairs carry a rim, and which color. */
function rimFor(chapterIndex: number, boxIndex: number): string | undefined {
  if (chapterIndex === 1) return "five"; // one screen, five calls
  if (chapterIndex === 4)
    return boxIndex === 0 ? CANON[0].color : CANON[1].color; // Catalog / Billing schemas
  if (chapterIndex === 5) return "five"; // composite schema
  return undefined;
}

/**
 * v20 "Five Lamps" (round 2): the backbone is viewport-anchored light, not
 * geometry. Five soft radial glows, one per CANON service, hold fixed
 * peripheral positions for the whole scroll; the story is told entirely by
 * choreographing what each beat's card is lit by, never by anything that
 * moves, converges, or is drawn in the content field. A tiny
 * IntersectionObserver state machine tracks which chapter is centered and
 * drives the lamp recipe; the cards themselves carry the same signal as
 * pure CSS rims, so the concept survives fully on mobile even though the
 * ambient layer itself is desktop-only.
 *
 * The tall aspect-ratio map and absolute copy/box placement math used by
 * the geometry-drawing concepts is deliberately not reused here: nothing is
 * drawn in the content field, so there is no diagram to align copy against,
 * and a plain stacked column avoids the copy/scrim-over-geometry failure
 * class the other prototypes had to retune around.
 */
export function FiveLamps() {
  const [beat, setBeat] = useState(0);
  const reducedMotion = usePrefersReducedMotion();
  const chapterRefs = useRef<(HTMLDivElement | null)[]>([]);

  useEffect(() => {
    if (typeof IntersectionObserver === "undefined") return;
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (!entry.isIntersecting) continue;
          const index = chapterRefs.current.indexOf(
            entry.target as HTMLDivElement,
          );
          if (index !== -1) setBeat(index);
        }
      },
      { rootMargin: "-45% 0px -45% 0px", threshold: 0 },
    );
    for (const node of chapterRefs.current) {
      if (node) observer.observe(node);
    }
    return () => observer.disconnect();
  }, []);

  return (
    <PageSection maxWidth="6xl">
      <div className="relative mx-auto max-w-5xl">
        <AmbientLayer beat={beat} reducedMotion={reducedMotion} />

        <div className="relative z-10 mx-auto flex max-w-2xl flex-col gap-16 py-16">
          {CHAPTERS.map((chapter, i) => (
            <div
              key={i}
              ref={(node) => {
                chapterRefs.current[i] = node;
              }}
              className="flex flex-col gap-6"
            >
              <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
                {chapter.title}
              </h3>
              <div className="text-cc-ink space-y-3 text-sm sm:text-base">
                {chapter.body}
              </div>
              {chapter.boxes.length > 0 && (
                <div
                  className={
                    chapter.boxes.length > 1
                      ? "grid items-start gap-4 sm:grid-cols-2"
                      : "grid gap-4"
                  }
                >
                  {chapter.boxes.map((box, j) => {
                    const rim = rimFor(i, j);
                    const codeBox = (
                      <ProtoCodeBox
                        key={j}
                        label={box.label}
                        color={box.color}
                        lines={box.lines}
                      />
                    );
                    return rim ? (
                      <RimBox
                        key={j}
                        rim={rim}
                        active={beat === i}
                        reducedMotion={reducedMotion}
                      >
                        {codeBox}
                      </RimBox>
                    ) : (
                      codeBox
                    );
                  })}
                </div>
              )}
              {i === 6 && (
                <svg
                  viewBox="0 0 140 30"
                  width={140}
                  height={30}
                  aria-hidden="true"
                  className="mx-auto"
                >
                  <GatewayChip x={70} y={15} />
                </svg>
              )}
              {i === 5 && <HorizonRule />}
            </div>
          ))}
        </div>
      </div>
    </PageSection>
  );
}
