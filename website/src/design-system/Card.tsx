import Link from "next/link";
import type { CSSProperties, ReactNode } from "react";

export type CardVariant = "plain" | "tile" | "panel";

const VARIANT_CLASSES: Record<CardVariant, string> = {
  plain: "rounded-2xl bg-cc-card-bg",
  tile: "rounded-xl p-6 bg-cc-card-bg backdrop-blur-sm",
  panel: "rounded-3xl p-6 sm:p-8 bg-cc-card-bg/60",
};

interface CardProps {
  readonly children: ReactNode;
  readonly className?: string;
  /**
   * Root element. `"a"` renders a Next.js `Link` and requires `href`; the
   * other values render the plain structural element.
   */
  readonly as?: "div" | "article" | "li" | "a";
  /** Destination for the whole card. Required when `as="a"`. */
  readonly href?: string;
  readonly target?: string;
  readonly rel?: string;
  /**
   * Styling preset. `"plain"` (default) is an unpadded `rounded-2xl` surface.
   * `"tile"` is a padded, blurred `rounded-xl` surface. `"panel"` is a padded
   * `rounded-3xl` surface with a translucent background.
   */
  readonly variant?: CardVariant;
  /** Brightens the border on hover plus `transition-colors`. */
  readonly hoverBorder?: boolean;
  /** Decorative radial-gradient glow overlay in the top-right corner, clipped to the card. */
  readonly glow?: boolean;
  /** Inline styles for values Tailwind cannot express statically, such as a dynamic `gridRow` span. */
  readonly style?: CSSProperties;
}

/**
 * The bordered surface shell used across marketing and product pages: a
 * `cc-card-border` outline over a `cc-card-bg` fill. `variant` selects the
 * radius, padding, and background preset; `hoverBorder` and the decorative
 * `glow` layer on top. Renders as a `div` by default; pass `as` for
 * `article`/`li`, or `as="a"` with `href` to make the whole card a Next.js
 * link.
 */
export function Card({
  children,
  className,
  as = "div",
  href,
  target,
  rel,
  variant = "plain",
  hoverBorder = false,
  glow = false,
  style,
}: CardProps) {
  const cls = [
    "border-cc-card-border relative overflow-hidden border",
    VARIANT_CLASSES[variant],
    hoverBorder ? "hover:border-cc-card-border-hover transition-colors" : "",
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  const content = (
    <>
      {glow && <CardGlow />}
      {glow ? <span className="relative z-10">{children}</span> : children}
    </>
  );

  if (as === "a") {
    if (!href) {
      throw new Error('Card: `href` is required when `as="a"`.');
    }
    return (
      <Link href={href} target={target} rel={rel} className={cls} style={style}>
        {content}
      </Link>
    );
  }

  const Tag = as;
  return (
    <Tag className={cls} style={style}>
      {content}
    </Tag>
  );
}

function CardGlow() {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute -top-24 right-0 -z-0 h-56 w-56 opacity-40 blur-3xl"
      style={{
        background:
          "radial-gradient(50% 50% at 60% 40%, rgba(22,185,228,0.18), transparent 70%)",
      }}
    />
  );
}
