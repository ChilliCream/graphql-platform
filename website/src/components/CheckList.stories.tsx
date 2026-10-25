import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { CheckList } from "./CheckList";

const meta = {
  title: "Components/CheckList",
  component: CheckList,
  parameters: { layout: "fullscreen" },
  argTypes: {
    items: { control: "object" },
    variant: { control: "select", options: ["plain", "pill"] },
    columns: { control: "select", options: [1, 2] },
    className: { control: "text" },
  },
  args: {
    items: [
      "Pair with our engineers on your real schema",
      "Migrate from Apollo without a rewrite",
      "Profile and fix slow resolvers",
      "Lock down auth before you ship",
    ],
    variant: "plain",
    columns: 1,
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-2xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof CheckList>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Plain: Story = {};

export const PlainTwoColumns: Story = {
  args: {
    columns: 2,
  },
};

export const Pill: Story = {
  args: {
    variant: "pill",
  },
};
