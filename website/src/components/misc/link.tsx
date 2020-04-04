import { GatsbyLinkProps, Link as GatsbyLink } from "gatsby";
import { OutboundLink } from "gatsby-plugin-google-analytics";
import React from "react";

export const Link = React.forwardRef<
  HTMLAnchorElement,
  GatsbyLinkProps<unknown>
>(({ activeClassName, children, className, partiallyActive, to }, ref) => {
  const internal = /^\/(?!\/)/.test(to);

  return internal ? (
    <GatsbyLink
      to={to}
      className={className}
      activeClassName={activeClassName}
      partiallyActive={partiallyActive}
      ref={ref as string & React.RefObject<HTMLAnchorElement>}
    >
      {children}
    </GatsbyLink>
  ) : (
    <OutboundLink
      href={to}
      target="_blank"
      className={className}
      ref={ref as string & React.RefObject<HTMLAnchorElement>}
    >
      {children}
    </OutboundLink>
  );
});
