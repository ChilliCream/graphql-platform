"use client";

import { useId, useRef, useState, type ReactNode } from "react";
import { CircleQuestionIcon } from "@/src/icons/CircleQuestionIcon";

interface BenchmarkColumnHintProps {
  readonly label: string;
  readonly children: ReactNode;
}

const WIDTH = 288;
const GAP = 8;

/** A question mark button after a column label that shows the column's explanation on hover and focus. */
export function BenchmarkColumnHint({
  label,
  children,
}: BenchmarkColumnHintProps) {
  const id = useId();
  const button = useRef<HTMLButtonElement>(null);
  const [position, setPosition] = useState<{ left: number; top: number }>();

  const open = () => {
    const rect = button.current?.getBoundingClientRect();
    if (!rect) {
      return;
    }
    const left = Math.min(
      Math.max(GAP, rect.left + rect.width / 2 - WIDTH / 2),
      window.innerWidth - WIDTH - GAP,
    );
    setPosition({ left, top: rect.bottom + GAP });
  };
  const close = () => setPosition(undefined);

  return (
    <>
      <button
        ref={button}
        type="button"
        className="text-cc-ink-dim hover:text-cc-prose focus-visible:text-cc-prose focus-visible:outline-cc-accent ml-1 inline-flex size-4 translate-y-0.5 items-center justify-center rounded-full align-baseline focus-visible:outline-2"
        aria-label={`About ${label}`}
        aria-describedby={id}
        onMouseEnter={open}
        onMouseLeave={close}
        onFocus={open}
        onBlur={close}
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            close();
          }
        }}
      >
        <CircleQuestionIcon className="size-3.5 shrink-0" />
      </button>
      <span
        id={id}
        role="tooltip"
        hidden={position === undefined}
        className="border-cc-card-border bg-cc-surface text-cc-prose fixed z-50 w-72 rounded-md border px-3 py-2 text-left text-xs leading-5 font-normal whitespace-normal shadow-lg shadow-black/40"
        style={position}
      >
        {children}
      </span>
    </>
  );
}
