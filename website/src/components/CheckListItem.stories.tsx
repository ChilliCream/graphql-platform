import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { CheckListItem } from "./CheckListItem";

const meta = {
  title: "Components/CheckListItem",
  component: CheckListItem,
  parameters: { layout: "fullscreen" },
  argTypes: {
    children: { control: "text" },
    iconClassName: { control: "text" },
    className: { control: "text" },
  },
  args: {
    children: "Pair with our engineers on your real schema",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <ul className="mx-auto flex max-w-md flex-col gap-3">
          <Story />
        </ul>
      </div>
    ),
  ],
} satisfies Meta<typeof CheckListItem>;

export default meta;
type Story = StoryObj<typeof meta>;

// The default accent check, as used by `Offering` and `CheckList`'s plain variant.
export const Default: Story = {};

// A dynamic accent tint, as used by `PerkCard` for its violet level cards.
export const VioletAccent: Story = {
  args: {
    children: "DataLoaders and the N+1 problem",
    iconClassName: "text-[#7c92c6]",
  },
};

// A dynamic accent tint, as used by `PerkCard` for its coral level cards.
export const CoralAccent: Story = {
  args: {
    children: "Fusion and distributed schemas",
    iconClassName: "text-[#f0786a]",
  },
};

// A label + value pair nested in the row, as used by `ContactBand`.
export const LabelAndValue: Story = {
  args: {
    children: (
      <>
        <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          Consulting
        </span>
        <br />
        Packages of hours
      </>
    ),
  },
};
