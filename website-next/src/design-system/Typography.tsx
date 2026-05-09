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
  | "lead"
  | "caption"
  | "strong"
  | "em"
  | "del";

const variantConfig: Record<
  TypographyVariant,
  { tag: ElementType; className: string }
> = {
  h1: {
    tag: "h1",
    className:
      "mt-10 mb-4 text-4xl font-bold tracking-tight text-slate-900",
  },
  h2: {
    tag: "h2",
    className:
      "mt-12 mb-4 pb-2 text-3xl font-semibold tracking-tight text-slate-900 border-b border-slate-200",
  },
  h3: {
    tag: "h3",
    className: "mt-8 mb-3 text-2xl font-semibold tracking-tight text-slate-900",
  },
  h4: {
    tag: "h4",
    className: "mt-6 mb-2 text-xl font-semibold text-slate-900",
  },
  h5: {
    tag: "h5",
    className: "mt-5 mb-2 text-lg font-semibold text-slate-900",
  },
  h6: {
    tag: "h6",
    className:
      "mt-4 mb-2 text-sm font-semibold uppercase tracking-wider text-slate-700",
  },
  body: { tag: "p", className: "my-4 text-base leading-7 text-slate-700" },
  lead: { tag: "p", className: "my-4 text-lg leading-8 text-slate-800" },
  caption: { tag: "span", className: "text-sm text-slate-500" },
  strong: {
    tag: "strong",
    className: "font-semibold text-slate-900",
  },
  em: { tag: "em", className: "italic" },
  del: {
    tag: "del",
    className: "line-through text-slate-500",
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
} & Omit<ComponentPropsWithoutRef<"div">, "children">;

export function Typography({
  variant,
  component,
  id,
  className = "",
  children,
  ...rest
}: TypographyProps) {
  const config = variantConfig[variant];
  const Tag = component ?? config.tag;
  const isHeading = HEADING_VARIANTS.has(variant);
  const resolvedId = id ?? (isHeading ? slugify(children) : undefined);

  return (
    <Tag
      id={resolvedId}
      className={`${config.className} ${className}`.trim()}
      {...rest}
    >
      {children}
      {isHeading && resolvedId ? (
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
