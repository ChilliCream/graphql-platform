import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { DotGridSurface } from "./DotGridSurface";
import { SectionHeading } from "./SectionHeading";

const cardContent = (
  <div className="p-10">
    <SectionHeading
      eyebrow="Client registry"
      title="Know who is still on the old schema"
      description="Every operation your clients send is fingerprinted and tracked, so a breaking change shows exactly who it would affect."
    />
  </div>
);

const heroContent = (
  <div className="mx-auto max-w-3xl px-5 py-16 text-center sm:px-12 sm:py-24">
    <SectionHeading
      align="center"
      size="lg"
      eyebrow="Release safety"
      title="Ship GraphQL schema changes with confidence"
      description="Nitro's schema checks test a proposed change against the operations your clients rely on in production."
    />
  </div>
);

const meta = {
  title: "Components/DotGridSurface",
  component: DotGridSurface,
  parameters: { layout: "fullscreen" },
  argTypes: {
    className: { control: "text" },
    id: { control: "text" },
    children: { control: false },
  },
  args: {
    className: "border-cc-card-border bg-cc-card-bg rounded-3xl border",
    children: cardContent,
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-3xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof DotGridSurface>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const FullBleedHero: Story = {
  args: {
    className: "border-cc-card-border/50 bg-cc-surface/25 border-b",
    children: heroContent,
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark">
        <Story />
      </div>
    ),
  ],
};
