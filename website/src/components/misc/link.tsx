import NextLink from "next/link";
import React, { FC } from "react";

export const Link: FC<{
  className?: string;
  download?: any;
  to: string;
  onClick?: React.MouseEventHandler;
  prefetch?: false;
  children?: React.ReactNode;
}> = ({ to, prefetch, children, ...rest }) => {
  const isHash = to.startsWith("#");
  const internal = isHash || /^\/(?!\/)/.test(to);

  return isHash ? (
    <a href={to} {...rest}>
      {children}
    </a>
  ) : internal ? (
    prefetch === false ? (
      <a href={to} {...rest}>
        {children}
      </a>
    ) : (
      <NextLink href={to} {...rest}>
        {children}
      </NextLink>
    )
  ) : (
    <a href={to} target="_blank" rel="noopener noreferrer" {...rest}>
      {children}
    </a>
  );
};
