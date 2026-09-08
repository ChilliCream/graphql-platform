"use client";

import { useEffect, useLayoutEffect, useRef, useState } from "react";

import { Eyebrow } from "@/src/design-system/Eyebrow";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

import { CANON, GatewayChip, INK_DIM, MicroLabel } from "../Primitives";
import { CHAPTERS } from "../story";

const MONO = "font-mono";

/** The five product-page fields, sourced from chapter 1's box so the field
 * names and owner colors are never re-typed. */
const FIELD_LINES = CHAPTERS[0].boxes[0].lines;

/** The five "GET /..." calls from chapter 2's box, in the same field order. */
const GET_LINES = CHAPTERS[1].boxes[0].lines;

/** Chapter 5's two source-schema boxes (Catalog, Billing). */
const SOURCE_SCHEMAS = CHAPTERS[4].boxes;

/** Chapter 6's composite-schema box. */
const COMPOSITE = CHAPTERS[5].boxes[0];

const RUNTIME_CALLS = [
  { label: "catalog", color: CANON[0].color },
  { label: "billing", color: CANON[1].color },
  { label: "ordering", color: CANON[2].color },
  { label: "shipping", color: CANON[3].color },
  { label: "user", color: CANON[4].color },
] as const;

const PANEL_HEADERS = [
  "Product page",
  "Product page",
  "Product page",
  "One query",
  null,
  "Composite schema",
  "Composite schema",
] as const;

function prefersReducedMotionNow(): boolean {
  if (typeof window === "undefined" || !window.matchMedia) return false;
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

function usePrefersReducedMotion(): boolean {
  const [reduced, setReduced] = useState(prefersReducedMotionNow);
  useEffect(() => {
    if (typeof window === "undefined" || !window.matchMedia) return;
    const query = window.matchMedia("(prefers-reduced-motion: reduce)");
    const onChange = (event: MediaQueryListEvent) => setReduced(event.matches);
    query.addEventListener("change", onChange);
    return () => query.removeEventListener("change", onChange);
  }, []);
  return reduced;
}

/**
 * Scrollspy: the chapter whose vertical center sits closest to the viewport
 * center. Driven by scroll position rather than a thin IntersectionObserver
 * band, so it can't be skipped by a fast scroll or a hash-link jump landing
 * mid-chapter.
 */
function useActiveChapter(count: number) {
  const [active, setActive] = useState(0);
  const refs = useRef<(HTMLDivElement | null)[]>([]);

  useEffect(() => {
    let frame = 0;
    const compute = () => {
      frame = 0;
      const mid = window.innerHeight / 2;
      let best = 0;
      let bestDistance = Infinity;
      for (const [i, el] of refs.current.entries()) {
        if (!el) continue;
        const rect = el.getBoundingClientRect();
        const distance = Math.abs(rect.top + rect.height / 2 - mid);
        if (distance < bestDistance) {
          bestDistance = distance;
          best = i;
        }
      }
      setActive(best);
    };
    const onScroll = () => {
      if (!frame) frame = requestAnimationFrame(compute);
    };
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", onScroll);
    return () => {
      window.removeEventListener("scroll", onScroll);
      window.removeEventListener("resize", onScroll);
      if (frame) cancelAnimationFrame(frame);
    };
  }, [count]);

  return { active, refs };
}

function FooterRow() {
  return (
    <div className="border-cc-card-border mt-2 flex items-center gap-2 border-t pt-2">
      {CANON.map((service) => (
        <span
          key={service.name}
          aria-hidden="true"
          className="inline-block h-2.5 w-2.5 rounded-[3px]"
          style={{ background: service.color }}
        />
      ))}
      <span
        className={`${MONO} ml-auto text-[10px] tracking-[0.1em]`}
        style={{ color: INK_DIM }}
      >
        TEAMS 5
      </span>
    </div>
  );
}

interface FieldRowProps {
  readonly beat: number;
  readonly index: number;
  readonly innerRef: (el: HTMLDivElement | null) => void;
}

/** One of the five stable field rows; only its inner content differs by beat. */
function FieldRow({ beat, index, innerRef }: FieldRowProps) {
  const line = FIELD_LINES[index];
  const color = line.dots?.[0];
  const dimmed = beat === 3 && (index === 2 || index === 4);

  let inner;
  if (beat === 1) {
    const [prefix, ...rest] = GET_LINES[index].text.split(" ");
    inner = (
      <>
        <span style={{ color: INK_DIM }}>{prefix} </span>
        <span style={{ color }}>{rest.join(" ")}</span>
      </>
    );
  } else {
    inner = <span className="text-[#c9d4e8]">{line.text}</span>;
  }

  return (
    <div
      ref={innerRef}
      className={`flex items-center gap-2 ${beat === 3 ? "pl-4" : ""}`}
      style={{ opacity: dimmed ? 0.4 : 1 }}
    >
      <span className="whitespace-pre">{inner}</span>
      {beat === 0 && color && (
        <span
          aria-hidden="true"
          className="ml-auto inline-block h-2 w-2 rounded-full"
          style={{ background: color }}
        />
      )}
      {beat === 2 && (
        <span
          aria-hidden="true"
          className="ml-auto inline-block h-[6px] w-[6px] rounded-full border"
          style={{ borderColor: INK_DIM }}
        />
      )}
    </div>
  );
}

interface DocumentPanelProps {
  readonly beat: number;
  /** Desktop sticky instance animates with FLIP; mobile inline copies don't. */
  readonly animated: boolean;
}

/**
 * The sticky mono panel that IS the code box: five field rows persist as the
 * same elements through beats 0-3 (product page -> five calls -> queued ->
 * one query), then the content folds into the shared source-schema boxes,
 * the composite-schema box, and a runtime fan-out. The invariant footer -
 * five service squares, "TEAMS 5" - is one unconditional block that never
 * remounts, so the DOM node backing it truly never changes across beats.
 */
function DocumentPanel({ beat, animated }: DocumentPanelProps) {
  const reducedMotion = usePrefersReducedMotion();
  const rowRefs = useRef<(HTMLDivElement | null)[]>([]);
  const prevRects = useRef<Record<number, DOMRect>>({});
  const prevBeat = useRef(beat);

  const inFieldRowRange = (b: number) => b >= 0 && b <= 3;

  useLayoutEffect(() => {
    if (!animated) return;
    const from = prevBeat.current;
    prevBeat.current = beat;

    if (reducedMotion || !inFieldRowRange(from) || !inFieldRowRange(beat)) {
      for (const [i, el] of rowRefs.current.entries()) {
        if (el) prevRects.current[i] = el.getBoundingClientRect();
      }
      return;
    }

    for (const [i, el] of rowRefs.current.entries()) {
      if (!el) continue;
      const previous = prevRects.current[i];
      const next = el.getBoundingClientRect();
      if (previous && from !== beat) {
        const dy = previous.top - next.top;
        if (dy !== 0) {
          el.style.transition = "none";
          el.style.transform = `translateY(${dy}px)`;
          // Force reflow so the transform above applies before it eases out.
          void el.offsetHeight;
          el.style.transition = "transform 320ms ease";
          el.style.transform = "";
        }
      }
      prevRects.current[i] = next;
    }
  }, [beat, animated, reducedMotion]);

  const header = PANEL_HEADERS[beat];

  return (
    <div
      aria-hidden="true"
      className="border-cc-card-border relative w-full max-w-[21rem] rounded-xl border bg-[#0d1424] p-4"
    >
      {beat === 5 && (
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -inset-6 -z-10 rounded-[2rem]"
          style={{
            background:
              "radial-gradient(ellipse at center, rgba(255,255,255,0.08) 0%, rgba(255,255,255,0) 70%)",
          }}
        />
      )}

      {header && (
        <>
          <MicroLabel>{header}</MicroLabel>
          <div className="border-cc-card-border mt-2 border-t pt-2" />
        </>
      )}

      {beat === 2 && (
        <div
          className={`${MONO} mb-2 flex items-center justify-between rounded px-2 py-1 text-[10px]`}
          style={{ background: "rgba(255,255,255,0.06)", color: INK_DIM }}
        >
          <span>api-team/</span>
          <span>owners: 1</span>
        </div>
      )}

      {beat === 3 && (
        <div className={`${MONO} text-[12px] leading-6 text-[#c9d4e8]`}>
          <div>{"{"}</div>
          <div>{'  productById(id: "P-42") {'}</div>
        </div>
      )}

      {beat <= 3 && (
        <div className={`${MONO} text-[12px] leading-6`}>
          {FIELD_LINES.map((_, i) => (
            <FieldRow
              key={i}
              beat={beat}
              index={i}
              innerRef={(el) => (rowRefs.current[i] = el)}
            />
          ))}
        </div>
      )}

      {beat === 3 && (
        <div className={`${MONO} text-[12px] leading-6 text-[#c9d4e8]`}>
          <div className="pl-4">{"}"}</div>
          <div>{"}"}</div>
        </div>
      )}

      {beat === 4 && (
        <div className="space-y-2">
          {SOURCE_SCHEMAS.map((box) => (
            <div
              key={box.label}
              className="rounded-lg p-2"
              style={{ border: `1.5px solid ${box.color}` }}
            >
              <MicroLabel className="opacity-80">{box.label}</MicroLabel>
              <div className={`${MONO} mt-1 text-[12px] leading-6`}>
                {box.lines.map((line, i) => {
                  if (line.accent && line.text.includes(line.accent)) {
                    const [before, after] = line.text.split(line.accent);
                    return (
                      <div key={i} className="whitespace-pre text-[#c9d4e8]">
                        {before}
                        <span className="text-[#5eead4]">{line.accent}</span>
                        {after}
                      </div>
                    );
                  }
                  return (
                    <div key={i} className="whitespace-pre text-[#c9d4e8]">
                      {line.text}
                    </div>
                  );
                })}
              </div>
            </div>
          ))}
          <div
            className="rounded-lg p-2 text-center opacity-40"
            style={{ border: "1.5px solid rgba(245,241,234,0.3)" }}
          >
            <span className={`${MONO} text-[12px]`}>&hellip;</span>
          </div>
        </div>
      )}

      {(beat === 5 || beat === 6) && (
        <div className={`${MONO} text-[12px] leading-6`}>
          {COMPOSITE.lines.map((line, i) => (
            <div key={i} className="flex items-center gap-2">
              <span className="whitespace-pre text-[#c9d4e8]">{line.text}</span>
              {line.dots && line.dots.length > 0 && (
                <span className="ml-auto flex items-center gap-1">
                  {line.dots.map((d, k) => (
                    <span
                      key={k}
                      aria-hidden="true"
                      className="inline-block h-1.5 w-1.5 rounded-full"
                      style={{ background: d }}
                    />
                  ))}
                </span>
              )}
            </div>
          ))}
        </div>
      )}

      {beat === 6 && (
        <div className="mt-2">
          <div className="relative py-1">
            <div
              className="border-t"
              style={{
                borderColor: "rgba(245,241,234,0.3)",
                borderStyle: "dashed",
                borderWidth: "1px",
              }}
            />
            <span
              className={`${MONO} absolute top-1/2 right-0 -translate-y-1/2 bg-[#0d1424] pl-2 text-[8px] tracking-[0.15em]`}
              style={{ color: INK_DIM }}
            >
              RUNTIME
            </span>
          </div>
          <div className="my-1.5">
            <svg width="90" height="26" viewBox="0 0 90 26" aria-hidden="true">
              <GatewayChip x={45} y={13} w={86} />
            </svg>
          </div>
          <div className={`${MONO} space-y-1.5 text-[11px]`}>
            {RUNTIME_CALLS.map((call) => (
              <div
                key={call.label}
                className="border-b border-dashed pb-1"
                style={{
                  color: call.color,
                  borderColor: call.color,
                  opacity: 0.8,
                }}
              >
                {`→ ${call.label}`}
              </div>
            ))}
          </div>
        </div>
      )}

      <FooterRow />
    </div>
  );
}

/**
 * Prototype v7 — "The Document Is the Protagonist": the map is replaced by a
 * sticky mono code panel. Its five field rows are the same DOM elements
 * through beats 0-3 (product page fields -> five REST calls -> queued behind
 * one team -> one GraphQL query), FLIP-animating position only; beyond that
 * the panel folds into the shared source-schema boxes, the composite schema,
 * and the runtime fan-out. A footer of five service squares reads "TEAMS 5"
 * under every single state, unconditionally rendered so the DOM node behind
 * it never changes: one document above, five squares below, count unchanged.
 */
export function DocumentProtagonist() {
  const { active, refs } = useActiveChapter(CHAPTERS.length);

  return (
    <div className="relative mx-auto w-full max-w-5xl px-5 py-16">
      <div className="sm:grid sm:grid-cols-[minmax(0,28rem)_1fr] sm:gap-x-12">
        <div>
          {CHAPTERS.map((chapter, i) => (
            <div
              key={i}
              ref={(el) => {
                refs.current[i] = el;
              }}
              className="flex min-h-[75vh] flex-col justify-center py-10 first:pt-0 sm:py-0"
            >
              <RevealOnScroll>
                <Eyebrow color="ink-dim" size="2xs">
                  {`Chapter ${i + 1} / ${CHAPTERS.length}`}
                </Eyebrow>
                <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-2 text-balance">
                  {chapter.title}
                </h3>
                <div className="text-cc-ink mt-4 space-y-3 text-sm sm:text-base">
                  {chapter.body}
                </div>
              </RevealOnScroll>

              {/* Mobile: each beat's panel state renders inline, in the slot
                  its chapter's code box occupies today - no sticky column. */}
              <div className="mt-6 sm:hidden">
                <DocumentPanel beat={i} animated={false} />
              </div>
            </div>
          ))}
        </div>

        <div className="hidden sm:block">
          <div className="sticky top-[calc(50vh-14rem)]">
            <DocumentPanel beat={active} animated />
          </div>
        </div>
      </div>
    </div>
  );
}
