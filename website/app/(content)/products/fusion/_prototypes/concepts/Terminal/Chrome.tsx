"use client";

import type { ReactNode } from "react";

import { MONO, SPINNER, TERM } from "./palette";

/**
 * Shared chrome for the Terminal concept: the pane every scene is printed in,
 * plus the two glyphs the scenes animate on their own clock (a braille spinner
 * and a block caret).
 *
 * The pane sizes its whole session with container query units, so a scene is
 * the same terminal at 1200px and at 320px - the type shrinks with the pane
 * instead of the line wrapping or spilling out of the scene. Everything inside
 * a pane is laid out in `ch` and `em`, which scale with that one font size.
 *
 * Motion is switched by `data-run`: when it is `false` (off-screen, hidden
 * tab, or reduced motion) every animation in the pane is dropped and the
 * static markup - which is written as the finished session - is what shows.
 */

export const CHROME_CSS = `
.tt-pane[data-run="false"], .tt-pane[data-run="false"] * { animation: none !important; }
.tt-spin-frame { animation: tt-spin-frame 0.72s steps(1, end) infinite; }
@keyframes tt-spin-frame {
  0%, 24.99% { opacity: 1; }
  25%, 100% { opacity: 0; }
}
.tt-caret { animation: tt-caret 1.06s steps(1, end) infinite; }
@keyframes tt-caret {
  0%, 54% { opacity: 1; }
  55%, 100% { opacity: 0; }
}
`;

interface PaneProps {
  /** Left of the title bar: the file or command the session is about. */
  readonly title: string;
  /** Right of the title bar: a short status, e.g. "exit 1". */
  readonly meta: string;
  /** `false` off-screen, on a hidden tab, or under reduced motion. */
  readonly run: boolean;
  readonly children: ReactNode;
}

/** One terminal pane, filling its `Scene`. */
export function Pane({ title, meta, run, children }: PaneProps) {
  return (
    <div
      style={{ containerType: "inline-size" }}
      className="absolute inset-0"
      aria-hidden="true"
    >
      <style>{CHROME_CSS}</style>
      <div
        className="tt-pane absolute inset-0 flex flex-col overflow-hidden"
        data-run={run ? "true" : "false"}
        style={{
          background: TERM.pane,
          color: TERM.text,
          fontFamily: MONO,
          fontSize: "clamp(6px, 2.4cqw, 15px)",
          lineHeight: 1.5,
        }}
      >
        <div
          className="flex items-baseline justify-between gap-[1ch]"
          style={{
            borderBottom: `1px solid ${TERM.rule}`,
            color: TERM.dim,
            fontSize: "0.86em",
            letterSpacing: "0.08em",
            padding: "0.9em 1.2em",
            whiteSpace: "pre",
          }}
        >
          <span style={{ overflow: "hidden" }}>{title}</span>
          <span style={{ color: TERM.faint }}>{meta}</span>
        </div>
        <div
          className="flex-1 overflow-hidden"
          style={{ padding: "1em 1.2em", whiteSpace: "pre" }}
        >
          {children}
        </div>
      </div>
    </div>
  );
}

interface SpinnerProps {
  readonly className?: string;
  readonly color?: string;
}

/**
 * A braille spinner: the four frames are stacked and cross-faded, because CSS
 * cannot rewrite text. At rest only the first frame is painted.
 */
export function Spinner({ className, color = TERM.warn }: SpinnerProps) {
  return (
    <span
      className={`relative inline-block ${className ?? ""}`.trim()}
      style={{ color, width: "1ch", height: "1em", verticalAlign: "baseline" }}
    >
      {SPINNER.map((frame, i) => (
        <span
          key={frame}
          className="tt-spin-frame absolute inset-0"
          style={{ animationDelay: `${i * 0.18}s`, opacity: i === 0 ? 1 : 0 }}
        >
          {frame}
        </span>
      ))}
    </span>
  );
}

interface CaretProps {
  readonly className?: string;
}

/** The block caret; solid at rest, blinking while the scene runs. */
export function Caret({ className }: CaretProps) {
  return (
    <span
      className={`tt-caret inline-block ${className ?? ""}`.trim()}
      style={{
        background: TERM.caret,
        height: "1.05em",
        marginLeft: "0.2ch",
        verticalAlign: "text-bottom",
        width: "0.62ch",
      }}
    />
  );
}
