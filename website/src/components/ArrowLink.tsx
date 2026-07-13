import NextLink from "next/link";
import type { ComponentPropsWithoutRef, ReactNode } from "react";

import { IconElement } from "../icons/IconElement";

interface ArrowLinkProps extends Omit<ComponentPropsWithoutRef<"a">, "href"> {
  readonly href: string;
  readonly children: ReactNode;
  readonly className?: string;
}

/**
 * The recurring "label + trailing arrow" link: an accent-colored,
 * medium-weight link with a trailing `ArrowRightIcon`.
 */
export function ArrowLink({
  href,
  children,
  className = "",
  ...props
}: ArrowLinkProps) {
  const linkClassName =
    `text-cc-accent hover:text-cc-accent-hover text-sm font-medium no-underline transition-colors inline-flex items-center gap-1.5 ${className}`.trim();
  const content = (
    <>
      {children}
      <IconElement icon="arrow-right" size="sm" />
    </>
  );

  return href.startsWith("/") ? (
    <NextLink href={href} className={linkClassName} {...props}>
      {content}
    </NextLink>
  ) : (
    <a href={href} className={linkClassName} {...props}>
      {content}
    </a>
  );
}
