"use client";

import { useEffect, useState, type MouseEvent, type ReactNode } from "react";
import { usePathname } from "next/navigation";
import { createPortal } from "react-dom";
import { IconButton } from "@/src/design-system/IconButton";

const DEFAULT_CLOSE_ICON = (
  <svg
    xmlns="http://www.w3.org/2000/svg"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    className="h-5 w-5"
    aria-hidden="true"
  >
    <line x1="6" y1="6" x2="18" y2="18" />
    <line x1="6" y1="18" x2="18" y2="6" />
  </svg>
);

interface MobileDrawerProps {
  readonly side: "left" | "right" | "full";
  /** Tailwind responsive prefix at which the drawer is hidden and the underlying page chrome takes over, e.g. `"lg"` or `"2xl"`. */
  readonly breakpoint: string;
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  readonly children: ReactNode;
  /**
   * Header row content. Defaults to a trailing close button (`IconButton` +
   * X icon), matching the docs sidebar/toc drawers. Pass a custom header
   * (e.g. logo + plain close button) for the full-screen nav variant.
   */
  readonly header?: ReactNode;
  /** Accessible label for the default close button. Ignored when `header` is provided. */
  readonly closeLabel?: string;
  /**
   * Close the drawer automatically when the route changes (the docs
   * sidebar/toc pattern). When `false`, callers are responsible for closing
   * the drawer themselves, e.g. via `onClick` on each link.
   */
  readonly closeOnPathnameChange?: boolean;
  /** Content clicks that should close the drawer, matched via `element.closest(selector)`. */
  readonly closeOnContentClickSelector?: string;
  /** Extra classes appended to the sliding panel, e.g. `"flex flex-col"` to lay out the panel as a column (as the table-of-contents drawer does). */
  readonly panelClassName?: string;
}

export function MobileDrawer({
  side,
  breakpoint,
  open,
  onOpenChange,
  children,
  header,
  closeLabel = "Close menu",
  closeOnPathnameChange = false,
  closeOnContentClickSelector,
  panelClassName,
}: MobileDrawerProps) {
  const pathname = usePathname();
  const [prevPathname, setPrevPathname] = useState(pathname);
  if (closeOnPathnameChange && prevPathname !== pathname) {
    setPrevPathname(pathname);
    if (open) {
      onOpenChange(false);
    }
  }

  useEffect(() => {
    if (!open) {
      return;
    }
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previous;
    };
  }, [open]);

  function handleContentClick(event: MouseEvent<HTMLDivElement>) {
    if (!closeOnContentClickSelector) {
      return;
    }
    const target = event.target as HTMLElement;
    if (target.closest(closeOnContentClickSelector)) {
      onOpenChange(false);
    }
  }

  const closeHeader = header ?? (
    <div className="border-cc-card-border flex items-center justify-end border-b px-3 py-2">
      <IconButton aria-label={closeLabel} onClick={() => onOpenChange(false)}>
        {DEFAULT_CLOSE_ICON}
      </IconButton>
    </div>
  );

  if (side === "full") {
    if (!open) {
      return null;
    }
    // Portal to <body> so the overlay escapes any ancestor `backdrop-filter`,
    // which would otherwise create a containing block that clips `position: fixed`.
    return createPortal(
      <div
        className={`bg-cc-surface fixed inset-0 z-50 flex flex-col ${breakpoint}:hidden ${panelClassName ?? ""}`}
      >
        {closeHeader}
        <div onClick={handleContentClick} className="contents">
          {children}
        </div>
      </div>,
      document.body,
    );
  }

  const sideClasses =
    side === "left"
      ? {
          position: "left-0",
          hiddenTransform: "-translate-x-full",
        }
      : {
          position: "right-0",
          hiddenTransform: "translate-x-full",
        };

  return (
    <div
      className={`fixed inset-0 z-50 ${breakpoint}:hidden ${open ? "" : "pointer-events-none"}`}
      aria-hidden={!open}
    >
      <div
        className={`bg-cc-black/40 absolute inset-0 transition-opacity ${
          open ? "opacity-100" : "opacity-0"
        }`}
        onClick={() => onOpenChange(false)}
      />
      <div
        className={`bg-cc-bg absolute inset-y-0 ${sideClasses.position} w-80 max-w-[85vw] overflow-y-auto shadow-xl transition-transform duration-200 ${
          open ? "translate-x-0" : sideClasses.hiddenTransform
        } ${panelClassName ?? ""}`}
      >
        {closeHeader}
        <div onClick={handleContentClick}>{children}</div>
      </div>
    </div>
  );
}
