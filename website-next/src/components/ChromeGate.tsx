"use client";

import { usePathname } from "next/navigation";

interface ChromeGateProps {
  readonly children: React.ReactNode;
}

/**
 * Hides the site chrome (header/footer) on standalone routes such as the
 * `/design-preview` reference page, so they render without the surrounding
 * layout.
 */
export function ChromeGate({ children }: ChromeGateProps) {
  const pathname = usePathname();

  if (pathname?.startsWith("/design-preview")) {
    return null;
  }

  return <>{children}</>;
}
