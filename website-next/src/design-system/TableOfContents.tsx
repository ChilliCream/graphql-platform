import { TocActive } from "./TocActive";

export type HeadingItem = {
  id: string;
  text: string;
  depth: 2 | 3;
};

type TableOfContentsProps = {
  items: HeadingItem[];
};

export function TableOfContents({ items }: TableOfContentsProps) {
  if (items.length === 0) {
    return null;
  }

  return (
    <aside className="hidden 2xl:block sticky top-0 self-start max-h-screen overflow-y-auto px-5 py-8 max-w-[21rem]">
      <p className="text-xs font-semibold uppercase tracking-widest text-stone-500 mb-3">
        On this page
      </p>
      <nav>
        <ul className="space-y-1 border-l border-stone-200 text-sm">
          {items.map((item) => (
            <li key={item.id}>
              <a
                href={`#${item.id}`}
                data-toc-link={item.id}
                className="block border-l-2 border-transparent py-1 text-stone-600 hover:text-stone-900 transition-colors"
                style={{ paddingLeft: `${(item.depth - 2) * 0.75 + 0.75}rem` }}
              >
                {item.text}
              </a>
            </li>
          ))}
        </ul>
      </nav>
      <TocActive ids={items.map((i) => i.id)} />
    </aside>
  );
}
