import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PopularBadge } from "./PopularBadge";

const meta = {
  title: "Components/PopularBadge",
  component: PopularBadge,
  parameters: { layout: "fullscreen" },
  argTypes: {
    label: { control: "text" },
  },
  args: {
    label: "Most Popular",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="border-cc-card-border bg-cc-surface relative mx-auto h-40 w-72 rounded-3xl border">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof PopularBadge>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const CustomLabel: Story = {
  args: {
    label: "Best Value",
  },
};
