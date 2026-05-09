"use client";

import {
  Children,
  isValidElement,
  useState,
  type ReactElement,
  type ReactNode,
} from "react";

type TabProps = {
  label: string;
  children: ReactNode;
};

/**
 * Marker component. Renders nothing on its own — only meaningful inside
 * a {@link Tabs} parent which inspects its children to build the tab strip.
 */
export function Tab(_: TabProps): null {
  return null;
}

type TabsProps = {
  children: ReactNode;
  /** Zero-based index of the tab to open initially. Defaults to 0. */
  defaultIndex?: number;
};

function hasLabelProp(child: unknown): child is ReactElement<TabProps> {
  if (!isValidElement(child)) {
    return false;
  }
  const props = child.props as { label?: unknown };
  return typeof props.label === "string";
}

export function Tabs({ children, defaultIndex = 0 }: TabsProps) {
  // Identify tabs structurally (any child with a string `label` prop) rather
  // than by component identity. Component references can diverge across the
  // server/client boundary (RSC bundling), so an identity check fails for
  // <Tab> elements that were created by the MDX runtime.
  const tabs = Children.toArray(children).filter(hasLabelProp);

  const safeDefault = Math.min(Math.max(defaultIndex, 0), Math.max(tabs.length - 1, 0));
  const [active, setActive] = useState(safeDefault);

  if (tabs.length === 0) {
    return null;
  }

  const current = tabs[active] ?? tabs[0];

  return (
    <div className="my-6 overflow-hidden rounded-lg ring-1 ring-stone-200">
      <div
        role="tablist"
        className="flex flex-wrap border-b border-stone-200 bg-stone-50"
      >
        {tabs.map((tab, i) => {
          const selected = i === active;
          return (
            <button
              key={`${tab.props.label}-${i}`}
              type="button"
              role="tab"
              aria-selected={selected}
              tabIndex={selected ? 0 : -1}
              onClick={() => setActive(i)}
              className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors -mb-px ${
                selected
                  ? "border-fuchsia-600 text-fuchsia-700"
                  : "border-transparent text-stone-600 hover:text-stone-900"
              }`}
            >
              {tab.props.label}
            </button>
          );
        })}
      </div>
      <div
        role="tabpanel"
        className="p-5 [&>*:first-child]:mt-0 [&>*:last-child]:mb-0"
      >
        {current.props.children}
      </div>
    </div>
  );
}
