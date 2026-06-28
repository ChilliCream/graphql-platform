import type { ReactNode } from "react";
import { LinkedInIcon } from "@/src/icons/LinkedIn";
import { XIcon } from "@/src/icons/X";

type BlogShareBarProps = {
  /** Absolute URL of the blog post. */
  url: string;
  /** Title of the blog post. */
  title: string;
};

export function BlogShareBar({ url, title }: BlogShareBarProps) {
  const xParams = new URLSearchParams({ url, text: title });
  const linkedInParams = new URLSearchParams({ url });

  return (
    <div className="flex flex-row items-center gap-4">
      <ShareLink
        href={`https://x.com/intent/tweet?${xParams}`}
        label="Share this post on X"
      >
        <XIcon className="h-5 w-auto fill-current" />
      </ShareLink>
      <ShareLink
        href={`https://www.linkedin.com/sharing/share-offsite/?${linkedInParams}`}
        label="Share this post on LinkedIn"
      >
        <LinkedInIcon className="h-5 w-auto fill-current" />
      </ShareLink>
    </div>
  );
}

function ShareLink({
  href,
  label,
  children,
}: {
  href: string;
  label: string;
  children: ReactNode;
}) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={label}
      className="text-cc-ink-dim hover:text-cc-ink inline-flex items-center justify-center transition-colors"
    >
      {children}
      <span className="sr-only">{label}</span>
    </a>
  );
}
