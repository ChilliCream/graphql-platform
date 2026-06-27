import type { ReactNode } from "react";

import { PreviewSwitcher } from "@/src/components/PreviewSwitcher";

/**
 * Preview harness for the three Mocha messaging-flow graphic candidates. Each
 * version renders the same Messaging heading above one graphic, with the
 * floating v1-v3 switcher.
 */
export default function MessagingGraphicLayout({
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
