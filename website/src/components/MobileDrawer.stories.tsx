import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { MobileDrawer } from "./MobileDrawer";

const NAV_LINKS = ["Getting Started", "Guides", "API Reference", "Changelog"];

const navList = (
  <ul className="m-0 flex list-none flex-col gap-1 p-4">
    {NAV_LINKS.map((label) => (
      <li key={label}>
        <a
          href="#"
          className="text-cc-ink hover:bg-cc-ink-faint block rounded-md px-3 py-2 no-underline"
        >
          {label}
        </a>
      </li>
    ))}
  </ul>
);

const tocList = (
  <nav className="px-5 py-4">
    <ol className="m-0 flex list-none flex-col gap-2 p-0 text-sm">
      {["Overview", "Installation", "Configuration", "Usage"].map((label) => (
        <li key={label}>
          <a href={`#${label}`} className="text-cc-ink-dim no-underline">
            {label}
          </a>
        </li>
      ))}
    </ol>
  </nav>
);

const fullScreenHeader = (
  <div className="border-cc-white/10 flex h-18 flex-none items-center justify-between border-b px-4">
    <span className="text-cc-heading text-lg font-semibold">ChilliCream</span>
    <button
      type="button"
      aria-label="Close navigation menu"
      className="text-cc-heading flex h-full items-center px-2"
    >
      Close
    </button>
  </div>
);

const fullScreenList = (
  <ol className="m-0 flex flex-1 list-none flex-col overflow-y-auto p-0">
    {NAV_LINKS.map((label) => (
      <li key={label} className="flex">
        <a
          href="#"
          className="border-cc-white/10 text-cc-heading flex h-18 w-full items-center border-b px-6 text-lg font-medium no-underline"
        >
          {label}
        </a>
      </li>
    ))}
  </ol>
);

const meta = {
  title: "Components/MobileDrawer",
  component: MobileDrawer,
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="cc-content-dark relative h-[32rem]">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof MobileDrawer>;

export default meta;
type Story = StoryObj<typeof meta>;

// Reproduces SidebarDrawer's mobile panel: left edge, lg breakpoint, default
// close header, closes when a nav link inside the content is clicked.
export const Left: Story = {
  args: {
    side: "left",
    breakpoint: "2xl",
    open: true,
    onOpenChange: () => {},
    closeOnContentClickSelector: "a",
    children: navList,
  },
};

// Reproduces TocDrawer's mobile panel: right edge, 2xl breakpoint, default
// close header, column panel layout via `panelClassName`.
export const Right: Story = {
  args: {
    side: "right",
    breakpoint: "2xl",
    open: true,
    onOpenChange: () => {},
    panelClassName: "flex flex-col",
    closeOnContentClickSelector: "a[href^='#']",
    children: tocList,
  },
};

// Reproduces MobileNav's full-screen slide-over: no backdrop, custom
// logo+close header, no pathname auto-close (links close explicitly instead).
export const Full: Story = {
  args: {
    side: "full",
    breakpoint: "min-[1060px]",
    open: true,
    onOpenChange: () => {},
    header: fullScreenHeader,
    children: fullScreenList,
  },
};
