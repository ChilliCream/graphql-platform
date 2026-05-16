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
  if (pathname.startsWith("/legal") || pathname.startsWith("/licensing")) {
    return "Legal";
  }
  if (pathname === "/") {
    return "Home";
  }
  return "Other";
}

export function AnalyticsContentGroup() {
  const pathname = usePathname();

  useEffect(() => {
    if (window.gtag) {
      window.gtag("set", { content_group: getContentGroup(pathname) });
    }
  }, [pathname]);

  return null;
}

export function AnalyticsClickTracker() {
  const pathname = usePathname();

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
