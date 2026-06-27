import type { ReactNode } from "react";

import { PreviewSwitcher } from "@/src/components/PreviewSwitcher";

/**
 * Full-width preview harness for the Mocha messaging-section takes. Uses the
 * real site chrome at the true landing width, with the floating v1-v5 switcher,
 * so each take is evaluated as it would sit on app/page.tsx after Different
 * Protocols and above pricing.
 */
export default function MochaSectionLayout({
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
