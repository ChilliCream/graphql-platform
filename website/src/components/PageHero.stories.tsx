import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PageHero } from "./PageHero";

const meta = {
  title: "Components/PageHero",
  component: PageHero,
  parameters: { layout: "fullscreen" },
  argTypes: {
    eyebrow: { control: "text" },
    title: { control: "text" },
    subtitle: { control: "text" },
    teaser: { control: "text" },
  },
  args: {
    eyebrow: "Support",
    title: "Help when you",
    subtitle: "need it most",
    teaser:
      "Reach the engineers who build HotChocolate and get unblocked fast, with response times you can plan around.",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof PageHero>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Full: Story = {};

export const TitleOnly: Story = {
  args: {
    eyebrow: undefined,
    title: "Pricing",
    subtitle: undefined,
    teaser: undefined,
  },
};

export const WithSubtitle: Story = {
  args: {
    eyebrow: undefined,
    title: "Built for teams",
    subtitle: "shipping at scale",
    teaser: undefined,
  },
};
