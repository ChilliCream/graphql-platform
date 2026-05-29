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
  "inline-block rounded-md border border-cc-card-border bg-cc-ink-faint px-3 py-1 text-sm text-cc-ink-dim no-underline transition-colors";
const INTERACTIVE_CLASSES = "hover:border-cc-card-border-hover hover:bg-cc-ink-faint";

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
