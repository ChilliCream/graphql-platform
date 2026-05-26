"use client";

import { SIDEBAR_OPEN_EVENT } from "./SidebarDrawer";
import { TOC_OPEN_EVENT } from "./TocDrawer";

/**
 * Two compact pill buttons that float over the docs content below the header.
 * Fixed-positioned so they don't take flow space; pinned to the article
 * gutters. Hidden at 2xl+ where both drawers dock inline.
 */
export function DocsToolbar() {
  return (
    <div className="pointer-events-none fixed inset-x-0 top-[72px] z-20 flex items-start justify-between px-4 py-3 2xl:hidden">
      <PillButton
        ariaLabel="Open documentation menu"
        event={SIDEBAR_OPEN_EVENT}
        label="Menu"
        className="lg:invisible"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="h-3.5 w-3.5"
          aria-hidden="true"
        >
          <line x1="4" y1="6" x2="20" y2="6" />
          <line x1="4" y1="12" x2="20" y2="12" />
          <line x1="4" y1="18" x2="20" y2="18" />
        </svg>
      </PillButton>
      <PillButton
        ariaLabel="Open table of contents"
        event={TOC_OPEN_EVENT}
        label="On this page"
        iconPosition="right"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          className="h-3.5 w-3.5"
          aria-hidden="true"
        >
          <line x1="8" y1="6" x2="21" y2="6" />
          <line x1="8" y1="12" x2="21" y2="12" />
          <line x1="8" y1="18" x2="21" y2="18" />
          <line x1="3" y1="6" x2="3.01" y2="6" />
          <line x1="3" y1="12" x2="3.01" y2="12" />
          <line x1="3" y1="18" x2="3.01" y2="18" />
        </svg>
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
      className={`pointer-events-auto inline-flex items-center gap-2 rounded-md border border-slate-200 bg-white/95 px-3 py-1.5 text-xs font-medium text-slate-700 shadow-sm backdrop-blur transition-colors hover:bg-white ${className}`}
    >
      {iconPosition === "left" && children}
      {label}
      {iconPosition === "right" && children}
    </button>
  );
}
