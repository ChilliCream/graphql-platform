import type { ReactNode } from "react";

type Align = "left" | "center";
type Size = "md" | "lg";

interface SectionHeadingProps {
  readonly eyebrow?: string;
  readonly title: ReactNode;
  readonly description?: ReactNode;
  /** Heading element id, e.g. for an `aria-labelledby` on the enclosing section. */
  readonly titleId?: string;
  readonly align?: Align;
  readonly size?: Size;
}

const TITLE_SIZE: Record<Size, string> = {
  md: "text-h4 sm:text-h3",
  lg: "text-h3 sm:text-h2",
};

const DESCRIPTION_SIZE: Record<Size, string> = {
  md: "text-base",
  lg: "text-base sm:text-lg",
};

/**
 * The lead block shared by section headers and bands: an optional mono eyebrow,
 * a heading, and an optional lead paragraph. Owns only its internal spacing; the
 * caller positions it (in a section, panel, or grid cell).
 */
export function SectionHeading({
  eyebrow,
  title,
  description,
  titleId,
  align = "left",
  size = "md",
}: SectionHeadingProps) {
  const centered = align === "center";

  return (
    <div className={centered ? "text-center" : undefined}>
      {eyebrow && (
        <p className="text-cc-ink-dim font-mono text-xs tracking-[0.18em] uppercase">
          {eyebrow}
        </p>
      )}
      <h2
        id={titleId}
        className={`font-heading text-cc-heading font-semibold text-balance ${TITLE_SIZE[size]} ${
          eyebrow ? "mt-3" : ""
        }`}
      >
        {title}
      </h2>
      {description && (
        <p
          className={`text-cc-ink mt-4 max-w-xl text-pretty ${DESCRIPTION_SIZE[size]} ${
            centered ? "mx-auto" : ""
          }`}
        >
          {description}
        </p>
      )}
    </div>
  );
}
