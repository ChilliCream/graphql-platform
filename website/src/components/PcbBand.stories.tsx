import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PcbBand } from "./PcbBand";
import { SectionHeading } from "./SectionHeading";

const messagingContent = (
  <div className="mx-auto max-w-3xl px-5 py-16 text-center sm:px-12 sm:py-24">
    <SectionHeading
      align="center"
      size="lg"
      eyebrow="Messaging"
      title="Events flow through the whole graph"
      description="Subscriptions, live queries, and fan-out delivery, all riding the same execution engine as your queries and mutations."
    />
  </div>
);

const meta = {
  title: "Components/PcbBand",
  component: PcbBand,
  parameters: { layout: "fullscreen" },
  argTypes: {
    className: { control: "text" },
    id: { control: "text" },
    children: { control: false },
  },
  args: {
    className: "pb-16 sm:pb-24",
    children: messagingContent,
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof PcbBand>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const CompactPadding: Story = {
  args: {
    className: "pb-8 sm:pb-12",
  },
};
