import type { ReactNode } from "react";

export default function LicensingLayout({ children }: { children: ReactNode }) {
  return <div className="cc-prose-invert">{children}</div>;
}
