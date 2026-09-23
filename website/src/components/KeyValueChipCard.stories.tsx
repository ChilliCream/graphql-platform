import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { KeyValueChipCard } from "./KeyValueChipCard";

function CheckMark() {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className="text-cc-accent/70 size-3 shrink-0"
    >
      <path
        d="M3 8.5 6.5 12 13 4.5"
        stroke="currentColor"
        strokeWidth={1.6}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

const meta = {
  title: "Components/KeyValueChipCard",
  component: KeyValueChipCard,
  parameters: { layout: "fullscreen" },
  argTypes: {
    label: { control: "text" },
    value: { control: "text" },
    icon: { control: false },
    className: { control: "text" },
  },
  args: {
    label: "Query",
    value: "[Query]",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="grid max-w-xs grid-cols-2 gap-2.5">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof KeyValueChipCard>;

export default meta;
type Story = StoryObj<typeof meta>;

/** PatternsFacet tile: label + status icon above a tokenized code line. */
export const PatternTile: Story = {
  args: {
    label: "DataLoader",
    icon: <CheckMark />,
    value: (
      <>
        <span className="text-cc-ink">[</span>
        <span className="text-cc-accent">DataLoader</span>
        <span className="text-cc-ink">]</span>
      </>
    ),
  },
};

/** Without an icon, the label row collapses to just the label. */
export const NoIcon: Story = {
  args: {
    label: "Saga",
    value: (
      <>
        <span className="text-cc-accent">Saga</span>
        <span className="text-cc-ink">{"<TState>"}</span>
      </>
    ),
  },
};
