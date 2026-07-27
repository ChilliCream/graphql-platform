"use client";

import { usePathname } from "next/navigation";
import { useEffect } from "react";

declare global {
  interface Window {
    gtag?: (...args: unknown[]) => void;
  }
}

function getContentGroup(pathname: string): string {
  if (pathname.startsWith("/docs")) {
    return "Documentation";
  }
  if (pathname.startsWith("/blog")) {
    return "Blog";
  }
  if (pathname.startsWith("/products")) {
    return "Products";
  }
  if (pathname.startsWith("/platform")) {
    return "Platform";
  }
  if (pathname.startsWith("/services")) {
    return "Services";
  }
  if (pathname.startsWith("/pricing")) {
    return "Pricing";
  }
  if (pathname.startsWith("/resources")) {
    return "Resources";
  }
  if (pathname.startsWith("/help")) {
    return "Help";
  }
  if (pathname.startsWith("/legal") || pathname.startsWith("/licensing")) {
    return "Legal";
  }
  if (pathname === "/") {
    return "Home";
  }
  return "Other";
}

/**
 * Reports a GA4 content group for the current route and tracks clicks on any
 * element carrying a `data-track` attribute. Renders nothing.
 *
 * Both effects no-op until `window.gtag` exists, so they are inert until the
 * user grants consent and Google Tag Manager loads.
 */
export function Analytics() {
  const pathname = usePathname();

  useEffect(() => {
    window.gtag?.("set", { content_group: getContentGroup(pathname) });
  }, [pathname]);

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (!(e.target instanceof Element)) {
        return;
      }

      const el = e.target.closest<HTMLElement>("[data-track]");
      if (!el || !window.gtag) {
        return;
      }

      window.gtag("event", el.dataset.track, {
        event_label: el.dataset.trackLabel || el.textContent?.trim(),
        link_url: el.getAttribute("href") || undefined,
        page_path: pathname,
      });
    }

    document.addEventListener("click", handleClick);
    return () => document.removeEventListener("click", handleClick);
  }, [pathname]);

  return null;
}
