"use client";

import { useEffect } from "react";

/**
 * Enables smooth scrolling only after the first paint, so opening a `#hash`
 * deep-link lands instantly on the target section while later in-page anchor
 * clicks animate. Pairs with the `html[data-smooth-scroll="true"]` rule in
 * globals.css. Renders nothing.
 */
export function EnableSmoothScroll() {
  useEffect(() => {
    const id = requestAnimationFrame(() => {
      document.documentElement.dataset.smoothScroll = "true";
    });
    return () => cancelAnimationFrame(id);
  }, []);

  return null;
}
