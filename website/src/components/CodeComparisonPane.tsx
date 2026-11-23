"use client";

import {
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type ReactNode,
} from "react";

interface CodeComparisonPaneProps {
  /** Tints the pane with the site accent to mark the code as ours. */
  readonly accent: boolean;
  /** One fenced code block, rendered by `CodeBlock`. */
  readonly children: ReactNode;
}

/** Widest right-edge fade in pixels; it narrows over the last pixels of scroll. */
const FADE_WIDTH = 48;

// The pane styles the `CodeBlock` inside it, because the block's own styles
// are shared with every docs page.
const PANE = [
  // A named container, so the type below follows this pane's width. The
  // block fills the pane, so both panes in a row are the same height.
  "@container/pane min-w-0 [&>figure]:my-0 [&>figure]:h-full",
  // The tool name is the pane's title: it leads, larger and in the heading
  // color, and the language badge moves to the right.
  "[&_figcaption>span+span]:order-first [&_figcaption>span+span]:font-heading [&_figcaption>span+span]:text-body [&_figcaption>span+span]:font-semibold [&_figcaption>span+span]:text-cc-heading [&_figcaption>span:first-child]:ml-auto",
  // 14px code fits a 56-character line from 31.5rem; below that the code
  // shrinks to 13px, and to 12px below 29.5rem, so it scrolls less.
  "[&_.shiki-wrapper]:@max-[31.5rem]/pane:text-[0.8125rem] [&_.shiki-wrapper]:@max-[31.5rem]/pane:leading-[1.375rem]",
  "[&_.shiki-wrapper]:@max-[29.5rem]/pane:text-xs [&_.shiki-wrapper]:@max-[29.5rem]/pane:leading-5",
  // A right-edge fade while the code can scroll further right.
  "data-scroll-fade:[&_.shiki-wrapper]:[mask-image:linear-gradient(to_left,transparent,#000_var(--scroll-fade))]",
].join(" ");

const ACCENT =
  "[&>figure]:ring-cc-accent/40 [&_figcaption]:border-cc-accent/25 [&_figcaption]:bg-linear-to-r [&_figcaption]:from-cc-accent/15 [&_figcaption]:to-cc-accent/5";

/**
 * One side of a `CodeComparison`. Shows a fade at the right edge of the code
 * while it can still scroll right, so readers on narrow screens see that the
 * lines go on.
 */
export function CodeComparisonPane({
  accent,
  children,
}: CodeComparisonPaneProps) {
  const paneRef = useRef<HTMLDivElement>(null);
  const [fade, setFade] = useState(0);

  useEffect(() => {
    const scroller =
      paneRef.current?.querySelector<HTMLElement>(".shiki-wrapper");
    if (!scroller) {
      return;
    }

    let frame = 0;
    const measure = () => {
      frame = 0;
      const remaining =
        scroller.scrollWidth - scroller.clientWidth - scroller.scrollLeft;
      // Sub-pixel layout can leave a fraction of a pixel at the scroll end.
      setFade(remaining > 1 ? Math.min(Math.round(remaining), FADE_WIDTH) : 0);
    };
    const schedule = () => {
      if (frame === 0) {
        frame = requestAnimationFrame(measure);
      }
    };

    scroller.addEventListener("scroll", schedule, { passive: true });
    // Watch the code as well as the scroller: a font-size change from the
    // container queries changes how far the code reaches, not the box.
    const observer = new ResizeObserver(schedule);
    observer.observe(scroller);
    const code = scroller.querySelector("code");
    if (code) {
      observer.observe(code);
    }
    schedule();

    return () => {
      cancelAnimationFrame(frame);
      scroller.removeEventListener("scroll", schedule);
      observer.disconnect();
    };
  }, []);

  return (
    <div
      ref={paneRef}
      data-scroll-fade={fade > 0 ? "" : undefined}
      style={
        fade > 0
          ? ({ "--scroll-fade": `${fade}px` } as CSSProperties)
          : undefined
      }
      className={accent ? `${PANE} ${ACCENT}` : PANE}
    >
      {children}
    </div>
  );
}
