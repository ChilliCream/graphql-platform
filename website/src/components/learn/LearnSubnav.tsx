import Link from "next/link";
import { LearnSubnavLinks } from "@/src/components/learn/LearnSubnavLinks";

/**
 * Persistent second navigation bar for every /learn route (learn-editorial.md
 * section 13): the Learn wordmark, promoted topic links, Articles, Browse,
 * and a right-aligned Subscribe link. Sticky under the global header;
 * rendered once by the learn segment layout.
 *
 * Server-rendered shell: only the active-link computation (`LearnSubnavLinks`)
 * is a client component, reading the pathname via `usePathname`, which needs
 * no Suspense boundary.
 *
 * The gutter is split into an outer padding layer (`px-5 sm:px-12`) and an
 * inner `max-w-8xl mx-auto` box, matching the learn layout's content gutter
 * node-for-node (13.2 amendment, kbx.12) so the wordmark's left edge and the
 * content's left edge sit at the same x-position once the viewport is wide
 * enough for `max-w-8xl` to cap the box. Putting the padding and the max
 * width on the same node (the previous shape) offsets the two by the padding
 * value on any viewport wider than that cap.
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
          <div className="flex [scrollbar-width:none]! items-stretch gap-6 overflow-x-auto [mask-image:linear-gradient(to_right,transparent,black_24px,black_calc(100%_-_24px),transparent)] lg:[mask-image:none] [&::-webkit-scrollbar]:hidden!">
            <LearnSubnavLinks />
          </div>
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
