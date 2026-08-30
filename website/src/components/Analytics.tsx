"use client";

import { usePathname } from "next/navigation";
import { useEffect } from "react";

import { getTrackParams, isAnalyticsEventName } from "@/src/helpers/analyticsEvents";
import { getContentGroup } from "@/src/helpers/contentGroup";

/**
 * Reports a GA4 content group for the current route and turns a click on any
 * element carrying a `data-track` attribute into the key event named by that
 * attribute. Renders nothing.
 *
 * Every `data-track-*` attribute on the element becomes an event parameter
 * (`data-track-item-slug` becomes `item_slug`), plus `link_url` for links and
 * the current `page_path`. Unknown event names are ignored, so only the events
 * declared in `analyticsEvents` can reach GA4.
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

      const name = el.dataset.track;
      if (!isAnalyticsEventName(name)) {
        return;
      }

      window.gtag("event", name, {
        ...getTrackParams(Array.from(el.attributes)),
        link_url: el.getAttribute("href") || undefined,
        page_path: pathname,
      });
    }

    document.addEventListener("click", handleClick);
    return () => document.removeEventListener("click", handleClick);
  }, [pathname]);

  return null;
}
