import type { ComponentPropsWithoutRef, ElementType, ReactNode } from "react";
import { slugify } from "@/src/helpers/slugify";

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
      "mt-8 mb-4 text-5xl font-extrabold text-indigo-700 underline decoration-pink-500 decoration-4 underline-offset-8",
  },
  h2: {
    tag: "h2",
    className:
      "mt-10 mb-3 text-4xl font-bold italic text-emerald-700 border-b-2 border-emerald-200 pb-1",
  },
  h3: {
    tag: "h3",
    className: "mt-8 mb-2 text-3xl font-semibold text-amber-700",
  },
  h4: {
    tag: "h4",
    className: "mt-6 mb-2 text-2xl font-semibold text-rose-700",
  },
  h5: {
    tag: "h5",
    className: "mt-5 mb-1 text-xl font-medium text-sky-700",
  },
  h6: {
    tag: "h6",
    className:
      "mt-4 mb-1 text-lg font-medium uppercase tracking-widest text-purple-700",
  },
  body: { tag: "p", className: "my-4 text-base leading-7 text-stone-800" },
  lead: { tag: "p", className: "my-4 text-xl leading-8 text-stone-900" },
  caption: { tag: "span", className: "text-sm text-stone-500" },
  strong: {
    tag: "strong",
    className: "font-bold text-yellow-700 bg-yellow-100 px-0.5 rounded",
  },
  em: { tag: "em", className: "italic text-emerald-700" },
  del: {
    tag: "del",
    className: "line-through decoration-red-500 decoration-2 text-stone-500",
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
  const resolvedId =
    id ?? (HEADING_VARIANTS.has(variant) ? slugify(children) : undefined);

  return (
    <Tag
      id={resolvedId}
      className={`${config.className} ${className}`.trim()}
      {...rest}
    >
      {children}
    </Tag>
  );
}
