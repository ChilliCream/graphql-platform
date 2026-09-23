import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { Card } from "./Card";

const meta = {
  title: "Design System/Card",
  component: Card,
  parameters: { layout: "fullscreen" },
  argTypes: {
    variant: {
      control: "select",
      options: ["plain", "tile", "panel"],
    },
    hoverBorder: { control: "boolean" },
    glow: { control: "boolean" },
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-md">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof Card>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Plain: Story = {
  args: {
    className: "p-6",
    children: (
      <p className="text-cc-ink text-sm">A plain card with p-6 padding.</p>
    ),
  },
};

export const Tile: Story = {
  args: {
    variant: "tile",
    hoverBorder: true,
    children: (
      <>
        <h2 className="text-cc-heading text-xl font-semibold">Analytics</h2>
        <p className="text-cc-ink-dim mt-3 text-sm">
          Instant Insights. Enhanced Performance.
        </p>
      </>
    ),
  },
};

export const PanelWithGlow: Story = {
  args: {
    variant: "panel",
    glow: true,
    children: (
      <p className="text-cc-ink text-sm">
        A panel card with the decorative glow overlay.
      </p>
    ),
  },
};

export const AsArticle: Story = {
  args: {
    as: "article",
    variant: "panel",
    children: (
      <>
        <h3 className="font-heading text-cc-heading text-lg font-semibold">
          On site
        </h3>
        <p className="text-cc-ink mt-2 text-sm leading-relaxed">
          A trainer joins your team in a room with a whiteboard and proper
          coffee.
        </p>
      </>
    ),
  },
};

export const AsLink: Story = {
  args: {
    as: "a",
    href: "/platform/analytics",
    variant: "tile",
    hoverBorder: true,
    children: (
      <>
        <h2 className="text-cc-heading text-xl font-semibold">Analytics</h2>
        <p className="text-cc-ink-dim mt-3 text-sm">
          Instant Insights. Enhanced Performance.
        </p>
        <span className="text-cc-accent mt-6 block text-sm font-medium">
          Learn more →
        </span>
      </>
    ),
  },
};
