import { Suspense } from "react";
import type { ReactNode } from "react";
import { LearnSubnav } from "@/src/components/learn/LearnSubnav";

interface LearnLayoutProps {
  readonly children: ReactNode;
}

/**
 * Route-group layout for every /learn page (learn-editorial.md section 12).
 * Renders the persistent `LearnSubnav` full-bleed above the wider learn
 * gutter (`max-w-8xl`, replacing the site-wide `(content)` layout's
 * `max-w-7xl` for this section only). The route group is URL-invisible:
 * moving `app/(content)/learn/*` here does not change any /learn URL.
 */
export default function LearnLayout({ children }: LearnLayoutProps) {
  return (
    <>
      <Suspense fallback={<LearnSubnavFallback />}>
        <LearnSubnav />
      </Suspense>
      <div className="px-5 py-8 sm:px-12">
        <div className="max-w-8xl mx-auto">{children}</div>
      </div>
    </>
  );
}

/** Static shell matching `LearnSubnav`'s dimensions, shown until the client component resolves the active route from `useSearchParams` (required for this statically exported site). Avoids layout shift; carries no active state. */
function LearnSubnavFallback() {
  return (
    <div
      aria-hidden="true"
      className="border-cc-card-border bg-cc-card-bg sticky top-18 z-30 h-12 border-b backdrop-blur-[18px] backdrop-saturate-150"
    />
  );
}
