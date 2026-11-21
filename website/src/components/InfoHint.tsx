"use client";

import {
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type PointerEvent,
  type ReactNode,
} from "react";
import { CircleQuestionIcon } from "@/src/icons/CircleQuestionIcon";

interface InfoHintProps {
  readonly label: string;
  readonly children: ReactNode;
  /**
   * `baseline` sits the icon on the text line it follows; `center` centers it
   * in a flex row, e.g. next to a badge.
   */
  readonly align?: "baseline" | "center";
}

const ALIGN = {
  baseline: "translate-y-0.5 align-baseline",
  center: "",
} as const;

/** Space between the button and the tooltip, and between the tooltip and the viewport edge. */
const GAP = 8;

/** How long a hover preview outlives the pointer, so the pointer can cross the gap onto the tooltip. */
const LEAVE_DELAY = 100;

/**
 * A question mark button that explains the preceding label. A mouse hover
 * previews the explanation. A click, tap, Enter or Space toggles it, and it
 * then stays open until the button loses focus, Escape is pressed, or the
 * reader clicks or taps somewhere else.
 */
export function InfoHint({
  label,
  children,
  align = "baseline",
}: InfoHintProps) {
  const id = useId();
  const button = useRef<HTMLButtonElement>(null);
  const tooltip = useRef<HTMLSpanElement>(null);
  const leaveTimer = useRef<number | undefined>(undefined);
  const [pinned, setPinned] = useState(false);
  const [hovered, setHovered] = useState(false);
  const open = pinned || hovered;

  const close = () => {
    window.clearTimeout(leaveTimer.current);
    setPinned(false);
    setHovered(false);
  };

  // Touch and pen input fire pointer enter and leave too; only a mouse hovers.
  const enter = (event: PointerEvent) => {
    if (event.pointerType === "mouse") {
      window.clearTimeout(leaveTimer.current);
      setHovered(true);
    }
  };
  const leave = (event: PointerEvent) => {
    if (event.pointerType === "mouse") {
      window.clearTimeout(leaveTimer.current);
      leaveTimer.current = window.setTimeout(
        () => setHovered(false),
        LEAVE_DELAY,
      );
    }
  };

  useEffect(() => () => window.clearTimeout(leaveTimer.current), []);

  // Place the fixed tooltip below the button, or above it when it does not fit
  // below, and keep it on the button while the page or a table scrolls.
  useLayoutEffect(() => {
    if (!open) {
      return;
    }
    const place = () => {
      const anchor = button.current?.getBoundingClientRect();
      const tip = tooltip.current;
      if (!anchor || !tip) {
        return;
      }
      const left = Math.min(
        Math.max(GAP, anchor.left + anchor.width / 2 - tip.offsetWidth / 2),
        window.innerWidth - tip.offsetWidth - GAP,
      );
      const below = anchor.bottom + GAP;
      const above = anchor.top - GAP - tip.offsetHeight;
      const fitsBelow = below + tip.offsetHeight <= window.innerHeight - GAP;
      tip.style.left = `${left}px`;
      tip.style.top = `${fitsBelow || above < GAP ? below : above}px`;
    };
    place();
    window.addEventListener("scroll", place, { capture: true, passive: true });
    window.addEventListener("resize", place);
    return () => {
      window.removeEventListener("scroll", place, { capture: true });
      window.removeEventListener("resize", place);
    };
  }, [open]);

  // Close on a press outside the button, or on Escape. Safari does not focus a
  // button on click, so the button's own blur and keydown do not cover it.
  useEffect(() => {
    if (!open) {
      return;
    }
    const dismiss = () => {
      window.clearTimeout(leaveTimer.current);
      setPinned(false);
      setHovered(false);
    };
    const closeOutside = (event: globalThis.PointerEvent) => {
      if (!button.current?.contains(event.target as Node)) {
        dismiss();
      }
    };
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        dismiss();
      }
    };
    document.addEventListener("pointerdown", closeOutside);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("pointerdown", closeOutside);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [open]);

  return (
    <>
      {/* The `after` box grows the hit area to 24px without moving the 16px button. */}
      <button
        ref={button}
        type="button"
        className={`text-cc-ink-dim hover:text-cc-prose focus-visible:text-cc-prose focus-visible:outline-cc-accent relative ml-1 inline-flex size-4 items-center justify-center rounded-full after:absolute after:-inset-1 focus-visible:outline-2 ${ALIGN[align]}`.trim()}
        aria-label={`About ${label}`}
        aria-describedby={id}
        onPointerEnter={enter}
        onPointerLeave={leave}
        onClick={() => {
          if (pinned) {
            close();
          } else {
            setPinned(true);
          }
        }}
        onBlur={() => setPinned(false)}
        onKeyDown={(event) => {
          if (event.key === "Escape" && open) {
            // Keep Escape from also closing a dialog or menu around the hint.
            event.preventDefault();
            event.stopPropagation();
            close();
          }
        }}
      >
        <CircleQuestionIcon className="size-3.5 shrink-0" />
      </button>
      <span
        ref={tooltip}
        id={id}
        role="tooltip"
        hidden={!open}
        onPointerEnter={enter}
        onPointerLeave={leave}
        className="border-cc-card-border bg-cc-surface text-cc-prose fixed z-50 w-72 max-w-[calc(100vw-16px)] rounded-md border px-3 py-2 text-left text-xs leading-5 font-normal whitespace-normal shadow-lg shadow-black/40"
      >
        {children}
      </span>
    </>
  );
}
