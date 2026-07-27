"use client";

import { ListBulletsIcon } from "@/src/icons/ListBullets";
import { MenuLinesIcon } from "@/src/icons/MenuLines";
import { SIDEBAR_OPEN_EVENT } from "./SidebarDrawer";
import { TOC_OPEN_EVENT } from "./TocDrawer";

export function DocsToolbar({
  menuLabel = "Open documentation menu",
  menuPillLabel = "Menu",
}: {
  menuLabel?: string;
  menuPillLabel?: string;
} = {}) {
  return (
    <div className="pointer-events-none fixed inset-x-0 top-18 z-20 flex items-start justify-between px-4 py-3 2xl:hidden print:hidden">
      <PillButton
        ariaLabel={menuLabel}
        event={SIDEBAR_OPEN_EVENT}
        label={menuPillLabel}
        className="lg:invisible"
      >
        <MenuLinesIcon className="h-3.5 w-3.5" aria-hidden="true" />
      </PillButton>
      <PillButton
        ariaLabel="Open table of contents"
        event={TOC_OPEN_EVENT}
        label="On this page"
        iconPosition="right"
      >
        <ListBulletsIcon className="h-3.5 w-3.5" aria-hidden="true" />
      </PillButton>
    </div>
  );
}

function PillButton({
  ariaLabel,
  event,
  label,
  children,
  iconPosition = "left",
  className = "",
}: {
  ariaLabel: string;
  event: string;
  label: string;
  children: React.ReactNode;
  iconPosition?: "left" | "right";
  className?: string;
}) {
  return (
    <button
      type="button"
      aria-label={ariaLabel}
      onClick={() => window.dispatchEvent(new CustomEvent(event))}
      className={`border-cc-card-border bg-cc-bg/95 text-cc-ink-dim hover:bg-cc-bg pointer-events-auto inline-flex cursor-pointer items-center gap-2 rounded-md border px-3 py-1.5 text-xs font-medium shadow-sm backdrop-blur transition-colors ${className}`}
    >
      {iconPosition === "left" && children}
      {label}
      {iconPosition === "right" && children}
    </button>
  );
}
