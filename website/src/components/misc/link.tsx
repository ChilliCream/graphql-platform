import NextLink from "next/link";
import React, { FC } from "react";
import { sanitizeUrl } from "@/utils/url-helpers";

export const Link: FC<{
  className?: string;
  download?: any;
  to: string;
  onClick?: React.MouseEventHandler;
  prefetch?: false;
  children?: React.ReactNode;
}> = ({ to, prefetch, children, ...rest }) => {
  const safeUrl = sanitizeUrl(to);
  const isHash = safeUrl.startsWith("#");
  const internal = isHash || /^\/(?!\/)/.test(safeUrl);

  return isHash ? (
    <a href={safeUrl} {...rest}>
      {children}
    </a>
  ) : internal ? (
    prefetch === false ? (
      <a href={safeUrl} {...rest}>
        {children}
      </a>
    ) : (
      <NextLink href={safeUrl} {...rest}>
        {children}
      </NextLink>
    )
  ) : (
    <a href={safeUrl} target="_blank" rel="noopener noreferrer" {...rest}>
      {children}
    </a>
  );
};
