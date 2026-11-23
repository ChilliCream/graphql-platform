import { ArrowLink } from "@/src/components/ArrowLink";

interface ArticleJumpLink {
  /** An in-page anchor (`#id`) or a root-relative path. */
  readonly href: string;
  readonly label: string;
}

interface ArticleJumpLinksProps {
  readonly links: readonly ArticleJumpLink[];
  /** Outer spacing utilities, e.g. "mb-6". */
  readonly className?: string;
}

/**
 * A compact "Jump to" row at the top of a long article that links to the
 * sections readers most often look for. Renders nothing without links.
 */
export function ArticleJumpLinks({ links, className }: ArticleJumpLinksProps) {
  if (links.length === 0) {
    return null;
  }

  return (
    <nav
      aria-label="Jump to"
      data-comparison-jump
      className={`flex flex-wrap items-center gap-x-5 gap-y-2 ${className ?? ""}`.trim()}
    >
      <span className="text-cc-ink-dim text-sm">Jump to</span>
      {links.map((link) => (
        <ArrowLink key={link.href} href={link.href}>
          {link.label}
        </ArrowLink>
      ))}
    </nav>
  );
}
