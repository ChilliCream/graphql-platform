import { GatsbyLinkProps, Link as GatsbyLink } from "gatsby";
import { OutboundLink } from "gatsby-plugin-google-analytics";
import React, { FC } from "react";

export const Link: FC<GatsbyLinkProps<unknown>> = ({
  activeClassName,
  children,
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
    >
      {children}
    </GatsbyLink>
  ) : (
    <OutboundLink href={to} target="_blank" rel="noopener noreferrer" {...rest}>
      {children}
    </OutboundLink>
  );
};
