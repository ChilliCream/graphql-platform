import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { CardGrid } from "./CardGrid";

const PLACEHOLDER_CARDS = [
  "Alpha",
  "Bravo",
  "Charlie",
  "Delta",
  "Echo",
  "Foxtrot",
];

function PlaceholderCard({
  label,
  tall = false,
}: {
  readonly label: string;
  readonly tall?: boolean;
}) {
  return (
    <div
      className={`border-cc-card-border bg-cc-card-bg rounded-xl border p-6 backdrop-blur-sm ${
        tall ? "h-full" : ""
      }`}
    >
      <h3 className="text-cc-ink text-lg font-semibold">{label}</h3>
      <p className="text-cc-ink-dim mt-2 text-sm">
        Placeholder card body copy for the grid story.
      </p>
    </div>
  );
}

const meta = {
  title: "Components/CardGrid",
  component: CardGrid,
  parameters: { layout: "padded" },
  argTypes: {
    cols: { control: "select", options: [2, 3] },
    step: { control: "select", options: ["single", "progressive"] },
    breakpoint: { control: "select", options: ["sm", "md", "lg"] },
    gap: { control: "select", options: [4, 6] },
    itemsStretch: { control: "boolean" },
    children: { control: false },
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
  args: { children: null },
} satisfies Meta<typeof CardGrid>;

export default meta;
type Story = StoryObj<typeof meta>;

// sm:grid-cols-2 lg:grid-cols-3, gap-6 (ecosystem, hotchocolate, resources)
export const ProgressiveThreeColGap6: Story = {
  args: { cols: 3, step: "progressive", gap: 6 },
  render: (args) => (
    <CardGrid {...args}>
      {PLACEHOLDER_CARDS.map((label) => (
        <PlaceholderCard key={label} label={label} />
      ))}
    </CardGrid>
  ),
};

// sm:grid-cols-2 lg:grid-cols-3, gap-4 (OutcomesSection, SelfServeGrid)
export const ProgressiveThreeColGap4: Story = {
  args: { cols: 3, step: "progressive", gap: 4 },
  render: (args) => (
    <CardGrid {...args}>
      {PLACEHOLDER_CARDS.map((label) => (
        <PlaceholderCard key={label} label={label} />
      ))}
    </CardGrid>
  ),
};

// md:grid-cols-3, gap-6 (platform, services, EngagementStrip, TeamSection)
export const SingleThreeColGap6: Story = {
  args: { cols: 3, step: "single", breakpoint: "md", gap: 6 },
  render: (args) => (
    <CardGrid {...args}>
      {PLACEHOLDER_CARDS.slice(0, 3).map((label) => (
        <PlaceholderCard key={label} label={label} />
      ))}
    </CardGrid>
  ),
};

// md:grid-cols-3, gap-4 (DeliveryFormatsSection, LevelsSection, SupportHero)
export const SingleThreeColGap4: Story = {
  args: { cols: 3, step: "single", breakpoint: "md", gap: 4 },
  render: (args) => (
    <CardGrid {...args}>
      {PLACEHOLDER_CARDS.slice(0, 3).map((label) => (
        <PlaceholderCard key={label} label={label} />
      ))}
    </CardGrid>
  ),
};

// sm:grid-cols-2, gap-6 (strawberryshake)
export const SingleTwoColSmGap6: Story = {
  args: { cols: 2, step: "single", breakpoint: "sm", gap: 6 },
  render: (args) => (
    <CardGrid {...args}>
      {PLACEHOLDER_CARDS.slice(0, 4).map((label) => (
        <PlaceholderCard key={label} label={label} />
      ))}
    </CardGrid>
  ),
};

// md:grid-cols-2, gap-4 (OffersSection)
export const SingleTwoColMdGap4: Story = {
  args: { cols: 2, step: "single", breakpoint: "md", gap: 4 },
  render: (args) => (
    <CardGrid {...args}>
      {PLACEHOLDER_CARDS.slice(0, 2).map((label) => (
        <PlaceholderCard key={label} label={label} />
      ))}
    </CardGrid>
  ),
};

// lg:grid-cols-2 lg:items-stretch, gap-6 (TierGrid)
export const SingleTwoColLgItemsStretch: Story = {
  args: {
    cols: 2,
    step: "single",
    breakpoint: "lg",
    gap: 6,
    itemsStretch: true,
  },
  render: (args) => (
    <CardGrid {...args}>
      <PlaceholderCard label="Consulting" tall />
      <div className="border-cc-card-border bg-cc-card-bg h-full rounded-xl border p-6 backdrop-blur-sm">
        <h3 className="text-cc-ink text-lg font-semibold">Contracting</h3>
        <p className="text-cc-ink-dim mt-2 text-sm">
          A shorter card to demonstrate items-stretch equalizing row height.
        </p>
      </div>
    </CardGrid>
  ),
};
