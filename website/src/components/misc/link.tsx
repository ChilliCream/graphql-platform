import NextLink from "next/link";
import { AnchorHTMLAttributes, FC } from "react";

type LinkProps = Omit<AnchorHTMLAttributes<HTMLAnchorElement>, "href"> & {
  href: string;
  prefetch?: boolean;
};

export const Link: FC<LinkProps> = ({ href, prefetch, children, ...rest }) => {
  const external = /^https?:\/\/|^mailto:/.test(href);

  return external ? (
    <a href={href} target="_blank" rel="noopener noreferrer" {...rest}>
      {children}
    </a>
  ) : (
    <NextLink href={href} prefetch={prefetch} {...rest}>
      {children}
    </NextLink>
  );
};
