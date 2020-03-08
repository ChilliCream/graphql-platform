import { GatsbyLinkProps, Link as GatsbyLink } from "gatsby";
import React, { FunctionComponent } from "react";

export const Link: FunctionComponent<GatsbyLinkProps<unknown>> = ({
  children,
  className,
  to,
}) => {
  const internal = /^\/(?!\/)/.test(to);

  return internal ? (
    <GatsbyLink to={to} className={className}>
      {children}
    </GatsbyLink>
  ) : (
    <a href={to} target="_blank" className={className}>
      {children}
    </a>
  );
};
