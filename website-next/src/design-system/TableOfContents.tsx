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
      {/* Empty grid cell reserves the 20rem column width; the TOC itself is
          position: fixed so it's anchored to the viewport instead of the
          inner grid (otherwise it slides up once the article ends, even
          though the footer is rendered separately below the grid). */}
      <aside className="hidden max-w-[21rem] 2xl:block" aria-hidden="true" />
      <div className="fixed bottom-0 right-0 top-[72px] z-10 hidden w-[20rem] overflow-y-auto px-5 py-8 2xl:block">
        <TocHeader />
        <TocNav sections={sections} />
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
              className="block border-l-2 border-transparent py-1 pl-3 text-cc-ink-dim transition-colors hover:text-cc-ink"
            >
              {section.h2.text}
            </a>
            {section.subtree.length > 0 && (
              <ul className="space-y-1">
                {section.subtree.map((node) => (
                  <li key={node.h3.id}>
                    <a
                      href={`#${node.h3.id}`}
                      data-toc-link={node.h3.id}
                      className="block border-l-2 border-transparent py-1 pl-6 text-cc-ink-dim transition-colors hover:text-cc-ink"
                    >
                      {node.h3.text}
                    </a>
                    {node.h4s.length > 0 && (
                      <ul className="hidden space-y-1 group-data-[section-active=true]/section:block">
                        {node.h4s.map((h4) => (
                          <li key={h4.id}>
                            <a
                              href={`#${h4.id}`}
                              data-toc-link={h4.id}
                              className="block border-l-2 border-transparent py-1 pl-9 text-cc-ink-dim transition-colors hover:text-cc-ink"
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
} {
  const childIds: string[] = [];
  for (const node of section.subtree) {
    childIds.push(node.h3.id);
    for (const h4 of node.h4s) {
      childIds.push(h4.id);
    }
  }
  return { id: section.h2.id, childIds };
}
