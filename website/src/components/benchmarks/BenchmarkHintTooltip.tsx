import type { ReactNode } from "react";

/** A question mark in a circle, as a mask whose question mark is transparent. */
const QUESTION_MARK = `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'%3E%3Cmask id='m'%3E%3Ccircle cx='12' cy='12' r='10' fill='white'/%3E%3Cpath d='M9.5 8.5a2.5 2.5 0 0 1 5 0c0 2-2.5 2-2.5 4' fill='none' stroke='black' stroke-width='2' stroke-linecap='round'/%3E%3Ccircle cx='12' cy='16.5' r='1.1'/%3E%3C/mask%3E%3Crect width='24' height='24' mask='url(%23m)'/%3E%3C/svg%3E")`;

/** The hovered hint button, unless Escape dismissed its tooltip. */
export const HOVERED_HINT_BUTTON =
  "[data-hint-button]:hover:not([data-hint-dismissed])";

/** The keyboard focused hint button while no hint button is hovered, unless Escape dismissed its tooltip. */
export const FOCUSED_HINT_BUTTON =
  ":root:not(:has([data-hint-button]:hover)) [data-hint-button]:focus-visible:not([data-hint-dismissed])";

// The button styles are in the components layer, so utilities on an enclosing element can adjust them.
const HINT_STYLES =
  "@layer components{" +
  "[data-hint-button]{display:inline-flex;width:1rem;height:1rem;margin-left:.25rem;translate:0 .125rem;align-items:center;justify-content:center;vertical-align:baseline;border-radius:9999px;color:var(--color-cc-ink-dim)}" +
  "[data-hint-button]:is(:hover,:focus-visible){color:var(--color-cc-prose)}" +
  "[data-hint-button]:focus-visible{outline:2px solid var(--color-cc-accent)}" +
  `[data-hint-button]::before{content:"";flex-shrink:0;width:.875rem;height:.875rem;background:currentColor;mask:${QUESTION_MARK} center/contain no-repeat}` +
  "}" +
  `:is(${HOVERED_HINT_BUTTON},${FOCUSED_HINT_BUTTON}){anchor-name:--benchmark-hint}` +
  "[data-hint]{display:none;position:fixed;position-anchor:--benchmark-hint;position-area:bottom;position-try-fallbacks:flip-block;margin:8px}" +
  `${HOVERED_HINT_BUTTON}+[data-hint],${FOCUSED_HINT_BUTTON}+[data-hint]{display:block}`;

interface BenchmarkHintTooltipProps {
  readonly id: string;
  /** The key that the hint buttons sharing this tooltip within the enclosing `data-hints` element reference, or undefined when the tooltip directly follows its button. */
  readonly hintKey: string | undefined;
  readonly children: ReactNode;
}

/** An explanation below the hovered or keyboard focused hint button, or above it when there is no room below. */
export function BenchmarkHintTooltip({
  id,
  hintKey,
  children,
}: BenchmarkHintTooltipProps) {
  return (
    <>
      <style href="benchmark-hints" precedence="default">
        {HINT_STYLES}
      </style>
      <span
        id={id}
        role="tooltip"
        data-hint
        data-hint-key={hintKey}
        data-llms-ignore
        className="border-cc-card-border bg-cc-surface text-cc-prose z-50 w-72 rounded-md border px-3 py-2 text-left text-xs leading-5 font-normal whitespace-normal shadow-lg shadow-black/40"
      >
        {children}
      </span>
    </>
  );
}
