"use client";

import { useEffect } from "react";

type SectionDescriptor = {
  id: string;
  childIds: string[];
};

export function TocActive({ sections }: { sections: SectionDescriptor[] }) {
  useEffect(() => {
    const allIds = sections.flatMap((s) => [s.id, ...s.childIds]);
    const sectionOf = new Map<string, string>();
    for (const section of sections) {
      sectionOf.set(section.id, section.id);
      for (const childId of section.childIds) {
        sectionOf.set(childId, section.id);
      }
    }

    const headings = allIds
      .map((id) => document.getElementById(id))
      .filter((el): el is HTMLElement => el !== null);
    if (headings.length === 0) {
      return;
    }

    const visible = new Set<string>();

    function applyActive(activeId: string | null) {
      const activeSection = activeId ? sectionOf.get(activeId) ?? null : null;

      for (const link of document.querySelectorAll<HTMLElement>(
        "[data-toc-link]"
      )) {
        if (link.dataset.tocLink === activeId) {
          link.dataset.active = "true";
        } else {
          delete link.dataset.active;
        }
      }

      for (const sectionEl of document.querySelectorAll<HTMLElement>(
        "[data-toc-section]"
      )) {
        if (sectionEl.dataset.tocSection === activeSection) {
          sectionEl.dataset.sectionActive = "true";
        } else {
          delete sectionEl.dataset.sectionActive;
        }
      }
    }

    function recompute() {
      // Prefer the topmost in-viewport heading; otherwise the last heading
      // we've scrolled past (covers long sections without a sub-heading yet).
      const ordered = allIds.filter((id) => visible.has(id));
      if (ordered.length > 0) {
        applyActive(ordered[0]);
        return;
      }
      const scrollY = window.scrollY;
      let last: string | null = null;
      for (const heading of headings) {
        if (heading.getBoundingClientRect().top + scrollY <= scrollY + 100) {
          last = heading.id;
        }
      }
      applyActive(last);
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
  }, [sections]);

  return null;
}
