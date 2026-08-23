import { Suspense } from "react";
import Link from "next/link";
import { LearnSubnavLinks } from "@/src/components/learn/LearnSubnavLinks";
import { LearnSubnavLinkList } from "@/src/components/learn/LearnSubnavLinkList";

/**
 * Persistent second navigation bar for every /learn route (learn-editorial.md
 * section 13): the Learn wordmark, promoted topic links, Articles, Browse,
 * and a right-aligned Subscribe link. Sticky under the global header;
 * rendered once by the learn segment layout.
 *
 * Server-rendered shell: only the active-link computation (`LearnSubnavLinks`)
 * needs `useSearchParams`, so it is the sole client boundary. Its Suspense
 * fallback renders the same five links with no active state, so the exported
 * static HTML always contains the wordmark, all links, and Subscribe.
 */
export function LearnSubnav() {
  return (
    <nav
      aria-label="Learn"
      className="border-cc-card-border bg-cc-card-bg sticky top-18 z-30 border-b backdrop-blur-[18px] backdrop-saturate-150"
    >
      <div className="max-w-8xl mx-auto grid h-12 grid-cols-[auto_1fr_auto] items-stretch gap-6 px-5 sm:px-12">
        <Link
          href="/learn"
          className="font-heading text-cc-heading flex shrink-0 items-center font-semibold no-underline"
        >
          Learn
        </Link>
        <div className="flex [scrollbar-width:none]! items-stretch gap-6 overflow-x-auto [&::-webkit-scrollbar]:hidden!">
          <Suspense fallback={<LearnSubnavLinkList />}>
            <LearnSubnavLinks />
          </Suspense>
        </div>
        <Link
          href="/learn#subscribe"
          className="text-cc-accent hover:text-cc-accent-hover flex shrink-0 items-center text-sm font-medium no-underline"
        >
          Subscribe
        </Link>
      </div>
    </nav>
  );
}
