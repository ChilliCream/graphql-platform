import { GatsbyLinkProps, Link as GatsbyLink } from "gatsby";
import { OutboundLink } from "gatsby-plugin-google-analytics";
import React, { FC } from "react";

export const Link: FC<GatsbyLinkProps<unknown>> = ({
  activeClassName,
  partiallyActive,
  to,
  ref,
  ...rest
}) => {
  const internal = /^\/(?!\/)/.test(to);

  return internal ? (
    <GatsbyLink
      to={to}
      activeClassName={activeClassName}
      partiallyActive={partiallyActive}
      {...rest}
    />
  ) : (
    <OutboundLink
      href={to}
      target="_blank"
      rel="noopener noreferrer"
      {...rest}
    />
  );
};
