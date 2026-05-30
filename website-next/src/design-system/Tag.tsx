import Link from "next/link";
import type { ReactNode } from "react";

type CommonProps = {
  children: ReactNode;
  className?: string;
};

type LinkTagProps = CommonProps & {
  href: string;
};

type StaticTagProps = CommonProps & {
  href?: undefined;
};

export type TagProps = LinkTagProps | StaticTagProps;

const BASE_CLASSES =
  "inline-flex items-center rounded-full border border-cc-card-border bg-cc-hover px-3 py-1 text-xs font-medium text-cc-ink-dim no-underline transition-colors";
const INTERACTIVE_CLASSES =
  "hover:border-cc-accent-hover hover:bg-cc-accent/10 hover:text-cc-accent-hover";

export function Tag(props: TagProps) {
  const { children, className } = props;
  const cls = [
    BASE_CLASSES,
    props.href ? INTERACTIVE_CLASSES : "",
    className ?? "",
  ]
    .filter(Boolean)
    .join(" ");

  if (props.href) {
    return (
      <Link href={props.href} className={cls}>
        {children}
      </Link>
    );
  }
  return <span className={cls}>{children}</span>;
}
