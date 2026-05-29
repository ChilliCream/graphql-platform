"use client";

import { useEffect } from "react";
import { usePathname } from "next/navigation";

/**
 * The desktop header dropdowns open on CSS `group-hover` and
 * `group-focus-within`. After a client-side navigation the Header persists, so
 * a panel can stay open two ways: focus remains inside the nav (focus-within),
 * or the cursor is still resting on the trigger/panel (hover). On every route
 * change we (1) drop focus out of the nav and (2) tag the nav so CSS forces the
 * panels closed until the pointer next moves, which dismisses the menu the
 * moment navigation completes without breaking normal hover afterwards.
 */
export function NavAutoClose() {
  const pathname = usePathname();

  useEffect(() => {
    const nav = document.querySelector("header nav");

    const active = document.activeElement;
    if (active instanceof HTMLElement && active.closest("header nav")) {
      active.blur();
    }

    if (!nav) {
      return;
    }

    nav.setAttribute("data-suppress-hover", "");
    const release = () => nav.removeAttribute("data-suppress-hover");
    window.addEventListener("pointermove", release, { once: true });
    window.addEventListener("pointerdown", release, { once: true });

    return () => {
      window.removeEventListener("pointermove", release);
      window.removeEventListener("pointerdown", release);
    };
  }, [pathname]);

  return null;
}
