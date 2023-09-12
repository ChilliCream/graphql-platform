import { GatsbyLinkProps, Link as GatsbyLink } from "gatsby";
import React, { FC } from "react";

export const Link: FC<
  Pick<GatsbyLinkProps<unknown>, "download" | "to" | "onClick"> & {
    prefetch?: false;
  }
> = ({ to, prefetch = true, ...rest }) => {
  const internal = /^\/(?!\/)/.test(to);

  return internal ? (
    prefetch ? (
      <GatsbyLink to={to} {...rest} />
    ) : (
      <a href={to} {...rest} />
    )
  ) : (
    <a href={to} target="_blank" rel="noopener noreferrer" {...rest} />
  );
};
