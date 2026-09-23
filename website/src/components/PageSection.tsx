import type { ReactNode } from "react";

type PageSectionMaxWidth = "7xl" | "5xl";

interface PageSectionProps {
  readonly maxWidth?: PageSectionMaxWidth;
  /** Vertical padding and any one-off utilities (text-center, flex/gap, min-h, ...). */
  readonly className?: string;
  readonly children: ReactNode;
}

const MAX_WIDTH: Record<PageSectionMaxWidth, string> = {
  "7xl": "max-w-7xl",
  "5xl": "max-w-5xl",
};

/**
 * The outer gutter shared by every top-level home/marketing section: a
 * centered, max-width container with the responsive side padding. Vertical
 * padding and any layout on top of it (centering text, flex rows, a minimum
 * height, ...) are the caller's concern via `className`.
 */
export function PageSection({
  maxWidth = "7xl",
  className,
  children,
}: PageSectionProps) {
  return (
    <section
      className={`mx-auto ${MAX_WIDTH[maxWidth]} px-5 sm:px-12 ${className ?? ""}`.trim()}
    >
      {children}
    </section>
  );
}
