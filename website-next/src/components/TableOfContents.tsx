import { TocActive } from "./TocActive";
import { TocDrawer } from "./TocDrawer";

export type HeadingItem = {
  id: string;
  text: string;
  depth: 2 | 3 | 4;
};

type SubtreeNode = {
  h3: HeadingItem;
  h4s: HeadingItem[];
};

export type TocSection = {
  h2: HeadingItem;
  subtree: SubtreeNode[];
};

type TableOfContentsProps = {
  items: HeadingItem[];
};

export function TableOfContents({ items }: TableOfContentsProps) {
  if (items.length === 0) {
    return null;
  }

  const sections = buildSections(items);

  return (
    <>
      <aside className="hidden max-w-84 2xl:block" aria-hidden="true" />
      <div className="cc-content-dark fixed right-0 top-18 z-30 -mt-px hidden max-h-[calc(100vh-72px-var(--docs-rail-bottom,0px))] min-h-[var(--docs-rail-min,0px)] w-80 flex-col px-5 pt-8 2xl:flex">
        <TocHeader />
        <div
          data-toc-scroll
          className="min-h-0 flex-1 overflow-y-auto overflow-x-hidden pb-8"
        >
          <TocNav sections={sections} />
        </div>
      </div>
      <TocDrawer>
        <TocHeader />
        <TocNav sections={sections} />
      </TocDrawer>
      <TocActive sections={sections.map(toSectionMap)} />
    </>
  );
}

function TocHeader() {
  return (
    <p className="mb-3 text-xs font-semibold uppercase tracking-widest text-cc-ink-dim">
      On this page
    </p>
  );
}

export function TocNav({ sections }: { sections: TocSection[] }) {
  return (
    <nav>
      <ul className="space-y-1 border-l border-cc-card-border text-sm">
        {sections.map((section) => (
          <li
            key={section.h2.id}
            data-toc-section={section.h2.id}
            className="group/section"
          >
            <a
              href={`#${section.h2.id}`}
              data-toc-link={section.h2.id}
              className="block border-l-2 border-transparent py-1 pl-3 text-cc-nav-text transition-colors hover:text-cc-white"
            >
              {section.h2.text}
            </a>
            {section.subtree.length > 0 && (
              <ul className="space-y-1">
                {section.subtree.map((node) => (
                  <li
                    key={node.h3.id}
                    data-toc-subtree={node.h3.id}
                    className="group/subtree"
                  >
                    <a
                      href={`#${node.h3.id}`}
                      data-toc-link={node.h3.id}
                      className="block border-l-2 border-transparent py-1 pl-6 text-cc-nav-text transition-colors hover:text-cc-white"
                    >
                      {node.h3.text}
                    </a>
                    {node.h4s.length > 0 && (
                      <ul className="hidden space-y-1 group-data-[subtree-active=true]/subtree:block">
                        {node.h4s.map((h4) => (
                          <li key={h4.id}>
                            <a
                              href={`#${h4.id}`}
                              data-toc-link={h4.id}
                              className="block border-l-2 border-transparent py-1 pl-9 text-cc-nav-text transition-colors hover:text-cc-white"
                            >
                              {h4.text}
                            </a>
                          </li>
                        ))}
                      </ul>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </li>
        ))}
      </ul>
    </nav>
  );
}

function buildSections(items: HeadingItem[]): TocSection[] {
  const sections: TocSection[] = [];
  let orphans: HeadingItem[] | null = null;

  for (const item of items) {
    if (item.depth === 2) {
      sections.push({ h2: item, subtree: [] });
      continue;
    }
    if (sections.length === 0) {
      // Sub-heading appears before any h2; surface it as a synthetic section
      // so it stays visible (rare, but possible when authors skip h2).
      orphans ??= [];
      orphans.push(item);
      continue;
    }
    const section = sections[sections.length - 1];
    if (item.depth === 3) {
      section.subtree.push({ h3: item, h4s: [] });
      continue;
    }
    // depth === 4
    if (section.subtree.length === 0) {
      section.subtree.push({ h3: item, h4s: [] });
      continue;
    }
    section.subtree[section.subtree.length - 1].h4s.push(item);
  }

  if (orphans) {
    return [
      ...orphans.map<TocSection>((h) => ({ h2: h, subtree: [] })),
      ...sections,
    ];
  }
  return sections;
}

function toSectionMap(section: TocSection): {
  id: string;
  childIds: string[];
  subtrees: { id: string; childIds: string[] }[];
} {
  const childIds: string[] = [];
  const subtrees: { id: string; childIds: string[] }[] = [];
  for (const node of section.subtree) {
    childIds.push(node.h3.id);
    const h4Ids = node.h4s.map((h4) => h4.id);
    childIds.push(...h4Ids);
    subtrees.push({ id: node.h3.id, childIds: h4Ids });
  }
  return { id: section.h2.id, childIds, subtrees };
}
