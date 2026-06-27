import type { ReactNode } from "react";

import { PreviewSwitcher } from "@/src/components/PreviewSwitcher";

/**
 * Full-width preview harness for the combined sections. Each version is one
 * topic's three best takes merged into a single compacted section. Uses the
 * real site chrome at the true landing width, with the floating v1-v6 switcher.
 */
export default function CombinedSectionLayout({
  children,
}: {
  readonly children: ReactNode;
}) {
  return (
    <>
      {children}
      <PreviewSwitcher />
    </>
  );
}
