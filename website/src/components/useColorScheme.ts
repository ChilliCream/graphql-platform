"use client";

import { useSyncExternalStore } from "react";

export type ColorScheme = "light" | "dark";

function subscribe(listener: () => void) {
  const observer = new MutationObserver(listener);
  observer.observe(document.documentElement, {
    attributes: true,
    attributeFilter: ["data-theme", "style"],
  });
  const system = window.matchMedia("(prefers-color-scheme: light)");
  system.addEventListener("change", listener);
  return () => {
    observer.disconnect();
    system.removeEventListener("change", listener);
  };
}

function getSnapshot(): ColorScheme {
  return getComputedStyle(document.documentElement).colorScheme.includes(
    "light",
  )
    ? "light"
    : "dark";
}

function getServerSnapshot(): ColorScheme {
  return "dark";
}

/**
 * The color scheme the page currently resolves to. Reads the document's
 * computed scheme, so it also works without the app's theme provider. Use it
 * only for JS-drawn output (canvas); server-rendered appearance comes from CSS.
 */
export function useColorScheme(): ColorScheme {
  return useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);
}
