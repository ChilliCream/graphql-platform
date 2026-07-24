import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { OutlineButton, SolidButton } from "../design-system/Button";
import { Icon } from "../icons/Icon";
import { ButtonRow } from "./ButtonRow";
import { IconFeatureCard } from "./IconFeatureCard";
import { MarketingHero } from "./MarketingHero";

const actions = (
  <ButtonRow align="center">
    <SolidButton href="#">Start for Free</SolidButton>
    <OutlineButton href="#">Talk to Sales</OutlineButton>
  </ButtonRow>
);

const meta = {
  title: "Components/MarketingHero",
  component: MarketingHero,
  parameters: { layout: "fullscreen" },
  argTypes: {
    eyebrow: { control: "text" },
    title: { control: "text" },
    lead: { control: "text" },
    footnote: { control: "text" },
    actions: { control: false },
    children: { control: false },
  },
  args: {
    eyebrow: "Nitro pricing",
    title: "Pricing that scales with your platform.",
    lead: "Start free on the shared cloud. Pay as you go as traffic grows, run a dedicated single-tenant instance, or self-host on your own infrastructure.",
    actions,
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof MarketingHero>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const WithFootnote: Story = {
  args: {
    eyebrow: "ChilliCream Training",
    title: "Beginner team. Advanced team. Mixed team.",
    lead: "The curriculum shapes to the team in the room.",
    footnote: "Same trainers, different starting line.",
  },
};

export const WithCardGrid: Story = {
  args: {
    eyebrow: "ChilliCream Support",
    title: "Support from the people who build the platform.",
    lead: "You work with the core engineers, not a first-line queue.",
    children: (
      <div className="mt-14 grid gap-4 md:grid-cols-3">
        {["A quick question", "Production is down", "A second opinion"].map(
          (label) => (
            <IconFeatureCard
              key={label}
              eyebrow={label}
              size="lg"
              icon={<Icon icon="check" />}
              title="We respond fast"
              copy="A core team member jumps in and stays with you until it is resolved."
            />
          ),
        )}
      </div>
    ),
  },
};
