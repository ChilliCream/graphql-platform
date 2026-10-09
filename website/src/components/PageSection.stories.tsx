import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PageSection } from "./PageSection";

const placeholderCard = (
  <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 text-center">
    <p className="text-cc-ink-dim font-mono text-xs tracking-[0.2em] uppercase">
      Section content
    </p>
  </div>
);

const meta = {
  title: "Components/PageSection",
  component: PageSection,
  parameters: { layout: "fullscreen" },
  argTypes: {
    maxWidth: { control: "select", options: ["7xl", "5xl"] },
    className: { control: "text" },
    children: { control: false },
  },
  args: {
    maxWidth: "7xl",
    className: "py-16 sm:py-24",
    children: placeholderCard,
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof PageSection>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const NarrowMaxWidth: Story = {
  args: {
    maxWidth: "5xl",
    className: "py-24 text-center sm:py-32",
  },
};

export const CenteredText: Story = {
  args: {
    className: "py-16 text-center sm:py-24",
  },
};

export const FlexColumnLayout: Story = {
  args: {
    className: "flex flex-col gap-8 py-16 sm:py-24",
    children: (
      <>
        {placeholderCard}
        {placeholderCard}
      </>
    ),
  },
};

export const MinHeightHero: Story = {
  args: {
    className:
      "flex min-h-[24rem] flex-col items-center justify-center py-20 text-center",
  },
};
