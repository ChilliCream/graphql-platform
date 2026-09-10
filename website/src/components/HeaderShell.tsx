"use client";

import type { ReactNode } from "react";
import { usePathname } from "next/navigation";

const BASE_CLASS =
  "flex h-18 w-full justify-center backdrop-blur-[18px] backdrop-saturate-150";

const STICKY_CLASS =
  "border-cc-white/10 bg-cc-card-bg sticky top-0 z-40 border-b shadow-[inset_0_1px_0_var(--cc-highlight)]";

const OVERLAY_CLASS = "fixed inset-x-0 top-0 z-40 bg-transparent";

interface HeaderShellProps {
  readonly children: ReactNode;
}

export function HeaderShell({ children }: HeaderShellProps) {
  const pathname = usePathname();
  const overlay = pathname === "/products/nitro";

  return (
    <header
      className={`${BASE_CLASS} ${overlay ? OVERLAY_CLASS : STICKY_CLASS}`}
    >
      {children}
    </header>
  );
}
