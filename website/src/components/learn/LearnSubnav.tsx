import Link from "next/link";
import { LearnSubnavLinks } from "@/src/components/learn/LearnSubnavLinks";
import { LearnSubnavScroller } from "@/src/components/learn/LearnSubnavScroller";

/**
 * Persistent second navigation bar for every /learn route (learn-editorial.md
 * section 13): the Learn wordmark, promoted topic links, Articles, Browse,
 * and a right-aligned Subscribe link. Sticky under the global header;
 * rendered once by the learn segment layout.
 *
 * Server-rendered shell; `LearnSubnavLinks` (active link) and
 * `LearnSubnavScroller` (overflow fade) are the only client pieces. The
 * gutter is an outer `px-5 sm:px-12` layer wrapping an inner
 * `max-w-8xl mx-auto` box, matching the learn content gutter's node
 * structure.
 */
export function LearnSubnav() {
  return (
    <nav
      aria-label="Learn"
      className="border-cc-card-border bg-cc-card-bg sticky top-18 z-30 border-b backdrop-blur-[18px] backdrop-saturate-150"
    >
      <div className="px-5 sm:px-12">
        <div className="max-w-8xl mx-auto grid h-12 grid-cols-[auto_1fr_auto] items-stretch gap-6">
          <Link
            href="/learn"
            className="font-heading text-cc-heading flex shrink-0 items-center font-semibold no-underline"
          >
            Learn
          </Link>
          <LearnSubnavScroller>
            <LearnSubnavLinks />
          </LearnSubnavScroller>
          <Link
            href="/learn#subscribe"
            className="text-cc-accent hover:text-cc-accent-hover flex shrink-0 items-center text-sm font-medium no-underline"
          >
            Subscribe
          </Link>
        </div>
      </div>
    </nav>
  );
}
