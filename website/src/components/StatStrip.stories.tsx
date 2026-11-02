import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { StatStrip } from "./StatStrip";

const meta = {
  title: "Components/StatStrip",
  component: StatStrip,
  parameters: { layout: "fullscreen" },
  argTypes: {
    items: { control: "object" },
    className: { control: "text" },
  },
  args: {
    className: "max-w-2xl",
    items: [
      { label: "Hourly rate", value: "$300" },
      { label: "Intro call", value: "60 min" },
      { label: "Engagements", value: "2 tiers" },
    ],
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof StatStrip>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const TwoStats: Story = {
  args: {
    className: "max-w-md",
    items: [
      { label: "Response time", value: "< 1 day" },
      { label: "Satisfaction", value: "98%" },
    ],
  },
};

export const FourStats: Story = {
  args: {
    className: "max-w-3xl",
    items: [
      { label: "Projects", value: "120+" },
      { label: "Contributors", value: "40" },
      { label: "Releases", value: "300" },
      { label: "Uptime", value: "99.9%" },
    ],
  },
};
