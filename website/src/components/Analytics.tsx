"use client";

import { usePathname } from "next/navigation";
import { useEffect } from "react";
import { canSendAnalytics, sendAnalyticsEvent } from "@/src/helpers/analytics";

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
 * Collection is gated by Cookiebot statistics consent and by GTM having
 * started. Nothing is queued for a visitor who has not opted in.
 */
export function Analytics() {
  const pathname = usePathname();

  useEffect(() => {
    function setContentGroup() {
      if (canSendAnalytics()) {
        window.gtag!("set", { content_group: getContentGroup(pathname) });
      }
    }

    setContentGroup();
    window.addEventListener("CookiebotOnConsentReady", setContentGroup);
    return () =>
      window.removeEventListener("CookiebotOnConsentReady", setContentGroup);
  }, [pathname]);

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (!(e.target instanceof Element)) {
        return;
      }

      const el = e.target.closest<HTMLElement>("[data-track], a[href]");
      if (!el) {
        return;
      }

      const href = el.getAttribute("href") ?? "";
      const inferredEvent = inferBusinessEvent(href);
      const eventName = el.dataset.track || inferredEvent;
      if (!eventName) {
        return;
      }

      sendAnalyticsEvent(eventName, {
        link_text: el.dataset.trackLabel || el.textContent?.trim(),
        link_url: href || undefined,
        page_path: pathname,
        content_group: getContentGroup(pathname),
      });
    }

    document.addEventListener("click", handleClick);
    return () => document.removeEventListener("click", handleClick);
  }, [pathname]);

  return null;
}

function inferBusinessEvent(href: string): string | null {
  try {
    const url = new URL(href, window.location.origin);
    if (url.hostname === "nitro.chillicream.com") {
      return "nitro_cta_click";
    }
    if (url.pathname === "/services/support/contact") {
      return "contact_cta_click";
    }
    if (
      url.protocol === "mailto:" &&
      url.pathname.toLowerCase() === "contact@chillicream.com"
    ) {
      return "contact_cta_click";
    }
  } catch {
    return null;
  }

  return null;
}
