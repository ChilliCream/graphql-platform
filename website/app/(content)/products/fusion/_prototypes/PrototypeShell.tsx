import type { ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";

import { PROTOTYPE_VERSIONS } from "./versions";
import { VersionSwitcher } from "./VersionSwitcher";

interface PrototypeShellProps {
  readonly version: number;
  readonly children: ReactNode;
}

/**
 * Shared chrome for every Fusion concept prototype page: a slim not-indexed
 * notice, the concept's own markup, then the version switcher. There is no
 * shared heading - each concept renders its own hero.
 *
 * Every `v<n>/page.tsx` route wraps its concept in this shell and sets
 * `robots: { index: false, follow: false }` in its own metadata.
 */
export function PrototypeShell({ version, children }: PrototypeShellProps) {
  const meta = PROTOTYPE_VERSIONS.find((v) => v.n === version);
  const label = meta ? `v${meta.n} · ${meta.name}` : `v${version}`;

  return (
    <>
      <div className="border-cc-card-border bg-cc-surface border-b">
        <PageSection maxWidth="6xl" className="py-2">
          <p className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
            {`PROTOTYPE ${label} - not linked, not indexed`}
          </p>
        </PageSection>
      </div>

      {children}

      <VersionSwitcher current={version} />
    </>
  );
}
