import type { ReactNode } from "react";

import { PreviewSwitcher } from "@/src/components/PreviewSwitcher";

/**
 * Full-width preview harness for the landing-page platform section takes. Uses
 * the real site chrome (header/footer from the root layout) at the true landing
 * width, so each take is evaluated exactly as it would sit on app/page.tsx, with
 * the floating v1-v5 switcher to compare them.
 */
export default function PlatformSectionLayout({
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
