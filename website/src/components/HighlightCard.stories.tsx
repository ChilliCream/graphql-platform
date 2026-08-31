import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { CheckIcon } from "./CheckIcon";
import { HighlightCard } from "./HighlightCard";

const meta = {
  title: "Components/HighlightCard",
  component: HighlightCard,
  parameters: { layout: "fullscreen" },
  argTypes: {
    as: { control: "select", options: ["div", "article"] },
    highlight: { control: "boolean" },
    badgeLabel: { control: "text" },
    rowCount: { control: "number" },
    subgrid: { control: "boolean" },
    gap: { control: "text" },
    padding: { control: "text" },
    children: { control: false },
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-sm">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof HighlightCard>;

export default meta;
type Story = StoryObj<typeof meta>;

const perks = ["Mentoring and guidance", "Architecture", "Code Review"];

function PerkBody() {
  return (
    <div className="flex h-full flex-col gap-5">
      <h3 className="font-heading text-cc-heading text-h4 font-semibold">
        Working team
      </h3>
      <ul className="flex flex-1 flex-col gap-2">
        {perks.map((item) => (
          <li key={item} className="flex items-start gap-3">
            <span className="text-cc-accent mt-1 flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{item}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

// TierGrid's TierCard shape: plain flex column, no subgrid, no rowCount.
export const Plain: Story = {
  args: {
    children: <PerkBody />,
  },
};

export const Highlighted: Story = {
  args: {
    highlight: true,
    children: <PerkBody />,
  },
};

export const HighlightedCustomBadge: Story = {
  args: {
    highlight: true,
    badgeLabel: "Start here",
    children: <PerkBody />,
  },
};

// PerkCard's shape: an <article>, subgrid rows, gap-5, p-6 sm:p-7.
export const PerkCardShape: Story = {
  args: {
    as: "article",
    subgrid: true,
    rowCount: 3,
    gap: "gap-5",
    padding: "p-6 sm:p-7",
    highlight: true,
    children: <PerkBody />,
  },
};
