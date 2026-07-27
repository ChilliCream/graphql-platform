import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { SectionHeading } from "./SectionHeading";

const meta = {
  title: "Components/SectionHeading",
  component: SectionHeading,
  parameters: { layout: "fullscreen" },
  argTypes: {
    eyebrow: { control: "text" },
    title: { control: false },
    description: { control: false },
    titleId: { control: "text" },
    align: { control: "select", options: ["left", "center"] },
    size: { control: "select", options: ["md", "lg"] },
  },
  args: {
    eyebrow: "Why HotChocolate",
    title: "Ship a GraphQL API your team can reason about",
    description:
      "Read a large schema like a map, recognise the common shapes, and explain why a type is modelled the way it is.",
    align: "left",
    size: "md",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof SectionHeading>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Centered: Story = {
  args: {
    align: "center",
  },
};

export const LargeWithEyebrow: Story = {
  args: {
    eyebrow: "Training",
    size: "lg",
    title: "Hands-on workshops led by the people who build the framework",
    description:
      "Two days of practical exercises that take your team from reading a schema to shipping a federated graph in production.",
  },
};

export const TitleOnly: Story = {
  args: {
    eyebrow: undefined,
    description: undefined,
    title: "Frequently asked questions",
  },
};
