import type { ComponentPropsWithoutRef, ElementType, ReactNode } from "react";

type EyebrowSize = "2xs" | "xs";
type EyebrowColor = "nav-label" | "ink-dim" | "accent";

interface EyebrowProps {
  readonly children: ReactNode;
  readonly size?: EyebrowSize;
  readonly color?: EyebrowColor;
  /** Element/component to render as, for contexts like a table header cell. Defaults to "p". */
  readonly as?: ElementType;
  readonly className?: string;
}

const SIZE_CLASSES: Record<EyebrowSize, string> = {
  "2xs": "text-[0.65rem]",
  xs: "text-xs",
};

const COLOR_CLASSES: Record<EyebrowColor, string> = {
  "nav-label": "text-cc-nav-label",
  "ink-dim": "text-cc-ink-dim",
  accent: "text-cc-accent",
};

/**
 * The small mono, uppercase kicker label used above section and card titles
 * across the marketing pages ("SKILLS", "REVIEW", "PACKAGES OF HOURS"). Owns
 * only its own text styling; the caller positions it and supplies spacing to
 * whatever follows.
 */
export function Eyebrow({
  children,
  size = "xs",
  color = "nav-label",
  as: Component = "p",
  className = "",
  ...rest
}: EyebrowProps & Omit<ComponentPropsWithoutRef<"p">, keyof EyebrowProps>) {
  const classes = [
    COLOR_CLASSES[color],
    "font-mono",
    SIZE_CLASSES[size],
    "tracking-[0.18em]",
    "uppercase",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <Component className={classes} {...rest}>
      {children}
    </Component>
  );
}
