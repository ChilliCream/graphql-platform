import { PreviewSwitcher } from "@/src/components/PreviewSwitcher";
import type { ReactNode } from "react";

export default function ContentLayout({ children }: { children: ReactNode }) {
  return (
    <div className="px-5 py-8 sm:px-12">
      <div className="mx-auto max-w-7xl">{children}</div>
      <PreviewSwitcher />
    </div>
  );
}
