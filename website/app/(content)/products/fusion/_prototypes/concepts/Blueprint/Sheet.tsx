"use client";

import type { ReactNode } from "react";

import { BP, DRAFT } from "./palette";

/**
 * Shared chrome for the Blueprint concept: the drafting sheet every plate is
 * drawn on, plus the CSS helpers its scenes animate with.
 *
 * A sheet is a gridded blueprint field inside a drawn frame, closed by a title
 * block that carries the drawing title, its number and its revision. It sizes
 * its lettering in container query units, so the same plate is legible at
 * 1200px and at 320px instead of wrapping or spilling out of the scene.
 *
 * Motion is switched by `data-run`: when it is `false` (off-screen, hidden tab
 * or reduced motion) every animation inside the sheet is dropped, and the base
 * styles - written as the finished, fully drafted drawing - are what shows.
 */

/** Every plate on the page runs on the same loop, so the set reads as one set. */
export const DUR = 16;

/**
 * Draws a stroke on: the element is a `<path pathLength={1}>`, hidden until
 * `at` percent of the loop and drafted in over `span` percent. At rest the
 * path is fully drawn.
 */
export function draw(name: string, at: number, span = 6): string {
  return `.${name}{stroke-dasharray:1;stroke-dashoffset:0;animation:${name} ${DUR}s linear infinite}
@keyframes ${name}{0%,${at}%{stroke-dashoffset:1}${at + span}%,100%{stroke-dashoffset:0}}`;
}

/** Fades a label, balloon or stamp in at `at` percent; at rest it is there. */
export function fade(name: string, at: number, span = 2): string {
  return `.${name}{opacity:1;animation:${name} ${DUR}s linear infinite}
@keyframes ${name}{0%,${at}%{opacity:0}${at + span}%,100%{opacity:1}}`;
}

/**
 * Fades an element in at `at` and back out at `until`, for a step of a
 * sequence that is superseded later on. At rest it is hidden, because the rest
 * state of a plate is the state its sequence ends in.
 */
export function flash(name: string, at: number, until: number): string {
  return `.${name}{opacity:0;animation:${name} ${DUR}s linear infinite}
@keyframes ${name}{0%,${at}%{opacity:0}${at + 1.5}%,${until}%{opacity:1}${until + 1.5}%,100%{opacity:0}}`;
}

/** Stamps a mark on: it lands slightly oversized and settles, like a rubber stamp. */
export function stamp(name: string, at: number): string {
  return `.${name}{opacity:1;transform:none;animation:${name} ${DUR}s linear infinite}
@keyframes ${name}{
0%,${at}%{opacity:0;transform:scale(1.5) rotate(-6deg)}
${at + 1}%{opacity:1;transform:scale(0.94) rotate(1.5deg)}
${at + 2}%,100%{opacity:1;transform:none}}`;
}

/**
 * The gating rule, the lettering defaults every plate inherits, and the rule
 * that folds the title block down to its title and its number once the sheet
 * is too narrow to letter every field at the 11px floor.
 */
export const SHEET_CSS = `
.bp-sheet[data-run="false"] *{animation:none!important}
.bp-sheet text{fill:${BP.ink};font-family:${DRAFT};letter-spacing:0.08em}
@container (max-width: 28rem){.bp-sheet .bp-tb-wide{display:none}}
.bp-sheet .bp-t-dim{fill:${BP.inkDim}}
.bp-sheet .bp-t-cyan{fill:${BP.dim}}
.bp-sheet .bp-t-ok{fill:${BP.ok}}
.bp-sheet .bp-t-query{fill:${BP.query}}
.bp-sheet .bp-t-red{fill:${BP.redline}}
`;

interface SheetProps {
  /** Drawing title, printed in the title block. */
  readonly title: string;
  /** Drawing number, e.g. "DWG-100". */
  readonly no: string;
  /** Revision letter. */
  readonly rev: string;
  /** Trailing title block field, e.g. "1:1". */
  readonly field?: string;
  /** `false` off-screen, on a hidden tab, or under reduced motion. */
  readonly run: boolean;
  readonly children: ReactNode;
}

/** One drafting sheet, filling its `Scene`. */
export function Sheet({
  title,
  no,
  rev,
  field = "1:1",
  run,
  children,
}: SheetProps) {
  return (
    <div
      aria-hidden="true"
      className="absolute inset-0"
      style={{ containerType: "inline-size" }}
    >
      <style>{SHEET_CSS}</style>
      <div
        className="bp-sheet absolute inset-0 flex flex-col overflow-hidden"
        data-run={run ? "true" : "false"}
        style={{
          background: `
            repeating-linear-gradient(0deg, ${BP.gridMajor} 0 1px, transparent 1px 100%) 0 0 / 100% 12.5%,
            repeating-linear-gradient(90deg, ${BP.gridMajor} 0 1px, transparent 1px 100%) 0 0 / 12.5% 100%,
            repeating-linear-gradient(0deg, ${BP.grid} 0 1px, transparent 1px 100%) 0 0 / 100% 2.5%,
            repeating-linear-gradient(90deg, ${BP.grid} 0 1px, transparent 1px 100%) 0 0 / 2.5% 100%,
            ${BP.plate}`,
          color: BP.ink,
          fontFamily: DRAFT,
          fontSize: "clamp(11px, 3.2cqw, 14px)",
        }}
      >
        <div
          className="m-[1.4em] flex min-h-0 flex-1 flex-col"
          style={{ border: `1px solid ${BP.inkFaint}` }}
        >
          <div className="relative min-h-0 flex-1">{children}</div>

          <div
            className="flex items-stretch"
            style={{
              borderTop: `1px solid ${BP.inkFaint}`,
              letterSpacing: "0.12em",
              whiteSpace: "nowrap",
            }}
          >
            <div
              className="bp-tb-wide shrink-0 px-[1em] py-[0.6em]"
              style={{ borderRight: `1px solid ${BP.inkFaint}` }}
            >
              <span style={{ color: BP.inkDim }}>CHILLICREAM</span>
            </div>
            <div
              className="min-w-0 flex-1 overflow-hidden px-[1em] py-[0.6em]"
              style={{ textOverflow: "ellipsis" }}
            >
              {title.toUpperCase()}
            </div>
            <div
              className="shrink-0 px-[1em] py-[0.6em]"
              style={{
                borderLeft: `1px solid ${BP.inkFaint}`,
                color: BP.inkDim,
              }}
            >
              {`${no} · REV ${rev}`}
              <span className="bp-tb-wide">{` · ${field}`}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
