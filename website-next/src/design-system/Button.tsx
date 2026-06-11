import NextLink from "next/link";
import type { ReactNode } from "react";

export type ButtonProps = {
  children: ReactNode;
  /**
   * Optional destination. Internal paths (`/...`) render a Next.js `Link`,
   * `#`/`mailto:`/`tel:` links stay in the same tab, and any other URL opens in
   * a new tab. When omitted the button renders as a `<button>`.
   */
  href?: string;
  className?: string;
  /** Button type when rendered as a `<button>` (ignored for links). */
  type?: "button" | "submit";
  /** Disabled state when rendered as a `<button>` (ignored for links). */
  disabled?: boolean;
};

const BASE_CLASSES =
  "inline-flex cursor-pointer items-center justify-center rounded-full px-7 py-3 text-sm font-medium no-underline transition-colors disabled:cursor-not-allowed disabled:opacity-60";

// Filled pill: ink surface with the dark page color as the label.
const SOLID_CLASSES = "bg-cc-ink text-cc-surface hover:bg-cc-white";

// Outlined pill: hairline border that brightens on hover.
const OUTLINE_CLASSES =
  "border border-cc-card-border text-cc-ink hover:border-cc-card-border-hover";

function renderButton(variantClasses: string, props: ButtonProps) {
  const { children, href, className, type = "button", disabled } = props;
  const cls = [BASE_CLASSES, variantClasses, className ?? ""]
    .filter(Boolean)
    .join(" ");

  if (href === undefined) {
    return (
      <button type={type} disabled={disabled} className={cls}>
        {children}
      </button>
    );
  }

  if (href.startsWith("/")) {
    return (
      <NextLink href={href} className={cls}>
        {children}
      </NextLink>
    );
  }

  if (
    href.startsWith("#") ||
    href.startsWith("mailto:") ||
    href.startsWith("tel:")
  ) {
    return (
      <a href={href} className={cls}>
        {children}
      </a>
    );
  }

  return (
    <a href={href} target="_blank" rel="noopener noreferrer" className={cls}>
      {children}
    </a>
  );
}

export function SolidButton(props: ButtonProps) {
  return renderButton(SOLID_CLASSES, props);
}

export function OutlineButton(props: ButtonProps) {
  return renderButton(OUTLINE_CLASSES, props);
}
