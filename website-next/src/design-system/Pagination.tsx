import Link from "next/link";

type PaginationProps = {
  currentPage: number;
  totalPages: number;
  /**
   * Returns the URL for a given 1-based page number. Page 1 typically lives
   * at the section root (e.g. `/blog`); deeper pages at `/blog/page/N`.
   */
  hrefForPage: (page: number) => string;
};

export function Pagination({
  currentPage,
  totalPages,
  hrefForPage,
}: PaginationProps) {
  if (totalPages <= 1) {
    return null;
  }

  const pages = buildPageList(currentPage, totalPages);
  const prevHref = currentPage > 1 ? hrefForPage(currentPage - 1) : null;
  const nextHref =
    currentPage < totalPages ? hrefForPage(currentPage + 1) : null;

  return (
    <nav
      aria-label="Pagination"
      className="mt-10 flex items-center justify-center gap-1"
    >
      <PageButton href={prevHref} disabled={prevHref === null} ariaLabel="Previous page">
        ‹
      </PageButton>
      {pages.map((p, i) =>
        p === "ellipsis" ? (
          <span
            key={`gap-${i}`}
            aria-hidden="true"
            className="px-2 text-cc-ink-dim"
          >
            …
          </span>
        ) : (
          <PageButton
            key={p}
            href={hrefForPage(p)}
            active={p === currentPage}
            ariaLabel={`Page ${p}`}
          >
            {p}
          </PageButton>
        ),
      )}
      <PageButton href={nextHref} disabled={nextHref === null} ariaLabel="Next page">
        ›
      </PageButton>
    </nav>
  );
}

type PageButtonProps = {
  href: string | null;
  children: React.ReactNode;
  active?: boolean;
  disabled?: boolean;
  ariaLabel?: string;
};

function PageButton({
  href,
  children,
  active,
  disabled,
  ariaLabel,
}: PageButtonProps) {
  const baseClasses =
    "inline-flex h-9 min-w-9 items-center justify-center rounded-md px-3 text-sm no-underline transition-colors select-none";
  if (disabled || href === null) {
    return (
      <span
        aria-hidden="true"
        className={`${baseClasses} cursor-not-allowed border border-cc-card-border text-cc-ink-faint`}
      >
        {children}
      </span>
    );
  }
  if (active) {
    return (
      <span
        aria-current="page"
        aria-label={ariaLabel}
        className={`${baseClasses} border border-cc-accent bg-cc-accent/10 font-semibold text-cc-accent`}
      >
        {children}
      </span>
    );
  }
  return (
    <Link
      href={href}
      prefetch={false}
      aria-label={ariaLabel}
      className={`${baseClasses} border border-cc-card-border text-cc-ink-dim hover:border-cc-accent-hover hover:bg-cc-accent/10 hover:text-cc-accent-hover`}
    >
      {children}
    </Link>
  );
}

type PageListItem = number | "ellipsis";

function buildPageList(current: number, total: number): PageListItem[] {
  // Always show first, last, current, and one neighbour either side; collapse
  // the rest with single-step ellipses.
  const set = new Set<number>([1, total, current, current - 1, current + 1]);
  const sorted = [...set]
    .filter((n) => n >= 1 && n <= total)
    .sort((a, b) => a - b);

  const result: PageListItem[] = [];
  let last = 0;
  for (const n of sorted) {
    if (last && n - last > 1) {
      result.push("ellipsis");
    }
    result.push(n);
    last = n;
  }
  return result;
}
