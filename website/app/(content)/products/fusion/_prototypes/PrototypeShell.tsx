import type { ReactNode } from "react";

import { VersionSwitcher } from "./VersionSwitcher";

interface PrototypeShellProps {
  readonly version: number;
  readonly children: ReactNode;
}

/**
 * Shared chrome for every Fusion concept prototype page: the concept's own
 * markup rendered exactly as production would, then the version switcher.
 * The not-indexed notice lives in the switcher pill, not a top bar, so the
 * hero renders with no offset. There is no shared heading - each concept
 * renders its own hero.
 *
 * Every `v<n>/page.tsx` route wraps its concept in this shell and sets
 * `robots: { index: false, follow: false }` in its own metadata.
 */
export function PrototypeShell({ version, children }: PrototypeShellProps) {
  return (
    <>
      {children}

      <VersionSwitcher current={version} />
    </>
  );
}
