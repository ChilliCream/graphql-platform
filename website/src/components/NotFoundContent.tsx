import Link from "next/link";

type SecondaryLink = {
  href: string;
  label: string;
};

type NotFoundContentProps = {
  /**
   * Optional context-aware link rendered next to "Take me home" (e.g. the docs
   * index or a specific product). Rendered on the server so it is present in the
   * static HTML with no layout shift.
   */
  secondary?: SecondaryLink | null;
};

/**
 * Shared 404 body. Used by the global `not-found` page and by the per-section
 * static 404 pages that the docs and blog routes prerender, so every dead end
 * looks identical and the secondary call-to-action never pops in after hydration.
 */
export function NotFoundContent({ secondary = null }: NotFoundContentProps) {
  return (
    <div className="flex min-h-[calc(100vh-72px)] flex-col items-center justify-start px-6 pt-16 pb-16 text-center sm:pt-24">
      <div className="border-cc-card-border text-cc-ink-dim flex aspect-3/2 w-75 max-w-full items-center justify-center rounded-xl border border-dashed text-sm font-medium sm:w-90">
        [illustration here]
      </div>

      <p className="text-cc-ink-dim mt-8 font-mono text-sm font-semibold tracking-[0.3em] uppercase">
        Error 404
      </p>
      <h1 className="text-cc-ink mt-3 text-4xl font-bold tracking-tight sm:text-5xl">
        Well, that&apos;s a spill.
      </h1>
      <p className="text-cc-ink-dim mt-4 max-w-md text-base leading-7">
        We knocked this page over and the drink went everywhere. Whatever you
        were looking for isn&apos;t here anymore.
      </p>

      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <Link
          href="/"
          className="border-cc-cta bg-cc-cta text-cc-ink hover:bg-cc-cta-hover inline-flex h-11 items-center rounded-md border-2 px-7 text-sm font-medium no-underline transition-colors"
        >
          Take me home
        </Link>
        {secondary ? (
          <Link
            href={secondary.href}
            className="border-cc-card-border text-cc-ink-dim hover:border-cc-card-border-hover hover:text-cc-ink inline-flex h-11 items-center rounded-md border px-7 text-sm font-medium no-underline transition-colors"
          >
            {secondary.label}
          </Link>
        ) : null}
      </div>
    </div>
  );
}
