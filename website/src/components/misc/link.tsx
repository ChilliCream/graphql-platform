import { GatsbyLinkProps, Link as GatsbyLink } from "gatsby";
import { OutboundLink } from "gatsby-plugin-google-analytics";
import React, { FC } from "react";

export const Link: FC<GatsbyLinkProps<unknown> & { prefetch?: false }> = ({
  activeClassName,
  partiallyActive,
  to,
  ref,
  prefetch = true,
  ...rest
}) => {
  const internal = /^\/(?!\/)/.test(to);

  return internal ? (
    prefetch ? (
      <GatsbyLink
        to={to}
        activeClassName={activeClassName}
        partiallyActive={partiallyActive}
        {...rest}
      />
    ) : (
      <a href={to} {...rest} />
    )
  ) : (
    <OutboundLink
      href={to}
      target="_blank"
      rel="noopener noreferrer"
      {...rest}
    />
  );
};
