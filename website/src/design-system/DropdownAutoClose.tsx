"use client";

import { useEffect, useRef } from "react";

/**
 * Closes the nearest ancestor <details> on an outside pointer-down, on Escape,
 * or when a menu item (anything in the panel other than the summary) is
 * clicked. Isolated into its own client component so the <Dropdown> shell can
 * stay a server component. (The header menus don't need this — they're
 * hover-based, so CSS closes them when the pointer leaves.)
 */
export function DropdownAutoClose() {
  const anchor = useRef<HTMLSpanElement>(null);

  useEffect(() => {
    const details = anchor.current?.closest("details");
    if (!details) {
      return;
    }
    const summary = details.querySelector("summary");

    function onPointerDown(event: PointerEvent) {
      if (details!.open && !details!.contains(event.target as Node)) {
        details!.open = false;
      }
    }
    function onClick(event: Event) {
      // A click inside the panel (i.e. on a menu item, not the summary) closes
      // it. Runs after the item's own click handler, so links still navigate.
      const target = event.target as Node;
      if (details!.open && summary && !summary.contains(target)) {
        details!.open = false;
      }
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape" && details!.open) {
        details!.open = false;
      }
    }

    document.addEventListener("pointerdown", onPointerDown);
    details.addEventListener("click", onClick);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      details.removeEventListener("click", onClick);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, []);

  return <span ref={anchor} hidden />;
}
