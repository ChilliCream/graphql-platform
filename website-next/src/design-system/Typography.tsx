import type { ComponentPropsWithoutRef, ElementType, ReactNode } from "react";
import { getText, slugify } from "@/src/helpers/slugify";

export type TypographyVariant =
  | "h1"
  | "h2"
  | "h3"
  | "h4"
  | "h5"
  | "h6"
  | "body"
  | "strong"
  | "em"
  | "del";

const variantConfig: Record<
  TypographyVariant,
  { tag: ElementType; className: string }
> = {
  h1: {
    tag: "h1",
    className: "mt-10 mb-4 text-4xl font-bold tracking-tight text-cc-prose",
  },
  h2: {
    tag: "h2",
    className:
      "mt-12 mb-4 pb-2 text-3xl font-semibold tracking-tight text-cc-prose border-b border-cc-card-border",
  },
  h3: {
    tag: "h3",
    className: "mt-8 mb-3 text-2xl font-semibold tracking-tight text-cc-prose",
  },
  h4: {
    tag: "h4",
    className: "mt-6 mb-2 text-xl font-semibold text-cc-prose",
  },
  h5: {
    tag: "h5",
    className: "mt-5 mb-2 text-lg font-semibold text-cc-prose",
  },
  h6: {
    tag: "h6",
    className: "mt-4 mb-2 text-base font-semibold text-cc-prose",
  },
  body: { tag: "p", className: "my-4 text-base leading-7 text-cc-prose" },
  strong: {
    tag: "strong",
    className: "font-semibold text-cc-prose",
  },
  em: { tag: "em", className: "italic text-cc-prose" },
  del: {
    tag: "del",
    className: "line-through text-cc-prose",
  },
};

const HEADING_VARIANTS = new Set<TypographyVariant>([
  "h1",
  "h2",
  "h3",
  "h4",
  "h5",
  "h6",
]);

type TypographyProps = {
  variant: TypographyVariant;
  component?: ElementType;
  children?: ReactNode;
  /**
   * Render a `#` permalink anchor next to heading variants. Opt-in because
   * this only makes sense for prose-style headings inside MDX content, not
   * for static page titles.
   */
  anchor?: boolean;
} & Omit<ComponentPropsWithoutRef<"div">, "children">;

export function Typography({
  variant,
  component,
  id,
  className = "",
  children,
  anchor = false,
  ...rest
}: TypographyProps) {
  const config = variantConfig[variant];
  const Tag = component ?? config.tag;
  const isHeading = HEADING_VARIANTS.has(variant);
  const resolvedId =
    id ?? (isHeading && anchor ? slugify(children) : undefined);

  return (
    <Tag
      id={resolvedId}
      className={`${config.className} ${className}`.trim()}
      {...rest}
    >
      {children}
      {isHeading && anchor && resolvedId ? (
        <a
          href={`#${resolvedId}`}
          className="heading-anchor"
          aria-label={`Link to ${getText(children)}`}
        >
          #
        </a>
      ) : null}
    </Tag>
  );
}
