"use client";

import { useEffect } from "react";

export function TocActive({ ids }: { ids: string[] }) {
  useEffect(() => {
    const headings = ids
      .map((id) => document.getElementById(id))
      .filter((el): el is HTMLElement => el !== null);
    if (headings.length === 0) {
      return;
    }

    const visible = new Set<string>();

    function setActive(id: string | null) {
      for (const link of document.querySelectorAll<HTMLElement>("[data-toc-link]")) {
        if (link.dataset.tocLink === id) {
          link.dataset.active = "true";
        } else {
          delete link.dataset.active;
        }
      }
    }

    function recompute() {
      // Pick the topmost visible heading; otherwise the last heading scrolled past.
      const ordered = ids.filter((id) => visible.has(id));
      if (ordered.length > 0) {
        setActive(ordered[0]);
        return;
      }
      const scrollY = window.scrollY;
      let last: string | null = null;
      for (const heading of headings) {
        if (heading.getBoundingClientRect().top + scrollY <= scrollY + 100) {
          last = heading.id;
        }
      }
      setActive(last);
    }

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          const id = entry.target.id;
          if (entry.isIntersecting) {
            visible.add(id);
          } else {
            visible.delete(id);
          }
        }
        recompute();
      },
      { rootMargin: "-80px 0px -70% 0px", threshold: 0 }
    );

    for (const h of headings) {
      observer.observe(h);
    }
    recompute();

    return () => observer.disconnect();
  }, [ids]);

  return null;
}
