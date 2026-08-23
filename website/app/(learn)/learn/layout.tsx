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
      <LearnSubnav />
      <div className="px-5 py-8 sm:px-12">
        <div className="max-w-8xl mx-auto">{children}</div>
      </div>
    </>
  );
}
