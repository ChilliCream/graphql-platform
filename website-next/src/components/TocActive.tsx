"use client";

import { useEffect } from "react";

type SectionDescriptor = {
  id: string;
  childIds: string[];
  subtrees: { id: string; childIds: string[] }[];
};

export function TocActive({ sections }: { sections: SectionDescriptor[] }) {
  useEffect(() => {
    const allIds = sections.flatMap((s) => [s.id, ...s.childIds]);
    const sectionOf = new Map<string, string>();
    // Maps any heading id to the h3 subtree that owns it, so a subtree's
    // deeper (h4) links only reveal while that subtree is the active one.
    const subtreeOf = new Map<string, string>();
    for (const section of sections) {
      sectionOf.set(section.id, section.id);
      for (const childId of section.childIds) {
        sectionOf.set(childId, section.id);
      }
      for (const subtree of section.subtrees) {
        subtreeOf.set(subtree.id, subtree.id);
        for (const childId of subtree.childIds) {
          subtreeOf.set(childId, subtree.id);
        }
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

      const activeSubtree = activeId ? subtreeOf.get(activeId) ?? null : null;
      for (const subtreeEl of document.querySelectorAll<HTMLElement>(
        "[data-toc-subtree]"
      )) {
        if (subtreeEl.dataset.tocSubtree === activeSubtree) {
          subtreeEl.dataset.subtreeActive = "true";
        } else {
          delete subtreeEl.dataset.subtreeActive;
        }
      }

      scrollActiveIntoView(activeId);
    }

    // Pull the active entry up to near the top of the TOC's own scroll window
    // so the list tracks the reader's position as they scroll. The browser
    // clamps `scrollTop`, so the first entries settle the list at the very top
    // and the last entries settle it scrolled all the way to the bottom.
    function scrollActiveIntoView(activeId: string | null) {
      if (!activeId) {
        return;
      }
      const container = document.querySelector<HTMLElement>("[data-toc-scroll]");
      const link = container?.querySelector<HTMLElement>(
        `[data-toc-link="${CSS.escape(activeId)}"]`
      );
      if (!container || !link) {
        return;
      }
      const linkBox = link.getBoundingClientRect();
      const containerBox = container.getBoundingClientRect();
      container.scrollTop += linkBox.top - containerBox.top;
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
