import type { ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { SectionHeading } from "@/src/components/SectionHeading";

import { SECTION_TITLE } from "./story";
import { PROTOTYPE_VERSIONS } from "./versions";
import { VersionSwitcher } from "./VersionSwitcher";

interface PrototypeShellProps {
  readonly version: number;
  readonly children: ReactNode;
}

/**
 * Shared chrome for every "What problem does GraphQL Federation solve?"
 * prototype page: a slim not-indexed notice, the real section heading, the
 * concept's own markup, then the version switcher.
 */
export function PrototypeShell({ version, children }: PrototypeShellProps) {
  const meta = PROTOTYPE_VERSIONS.find((v) => v.n === version);
  const label = meta ? `v${meta.n} · ${meta.name}` : `v${version}`;
  const round = meta?.round ?? "?";

  return (
    <section className="border-cc-card-border scroll-mt-24 border-t">
      <div className="border-cc-card-border bg-cc-surface border-b">
        <PageSection maxWidth="6xl" className="py-2">
          <p className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
            {`PROTOTYPE ${label} · round ${round} - not linked, not indexed`}
          </p>
        </PageSection>
      </div>

      <PageSection maxWidth="6xl" className="pt-16 sm:pt-24">
        <SectionHeading align="center" title={SECTION_TITLE} />
      </PageSection>

      {children}

      <VersionSwitcher current={version} />
    </section>
  );
}
