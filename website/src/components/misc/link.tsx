import { GatsbyLinkProps, Link as GatsbyLink } from "gatsby";
import { OutboundLink } from "gatsby-plugin-google-analytics";
import React, { FunctionComponent } from "react";

export const Link: FunctionComponent<GatsbyLinkProps<unknown>> = ({
  activeClassName,
  children,
  className,
  partiallyActive,
  to,
}) => {
  const internal = /^\/(?!\/)/.test(to);

  return internal ? (
    <GatsbyLink
      to={to}
      className={className}
      activeClassName={activeClassName}
      partiallyActive={partiallyActive}
    >
      {children}
    </GatsbyLink>
  ) : (
    <OutboundLink
      href={to}
      target="_blank"
      rel="noopener noreferrer"
      className={className}
    >
      {children}
    </OutboundLink>
  );
};
