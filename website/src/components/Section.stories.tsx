import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { IconFeatureCard } from "./IconFeatureCard";
import { CheckIcon } from "./CheckIcon";
import { Section } from "./Section";

const meta = {
  title: "Components/Section",
  component: Section,
  parameters: { layout: "fullscreen" },
  argTypes: {
    title: { control: "text" },
    className: { control: "text" },
    children: { control: false },
  },
  args: {
    title: "Built for teams that ship",
    children: (
      <p className="text-cc-ink/80 mx-auto max-w-2xl text-center text-lg">
        Bring your GraphQL skills up to speed with hands-on training, expert
        support, and a platform your whole team can rely on in production.
      </p>
    ),
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof Section>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const WithCards: Story = {
  args: {
    title: "What you get",
    children: (
      <div className="mx-auto grid max-w-3xl gap-4 sm:grid-cols-2">
        <IconFeatureCard
          icon={<CheckIcon size={16} />}
          title="Read a schema like a map"
          copy="Navigate a large GraphQL schema, recognise the common shapes, and explain why a type is modelled the way it is."
        />
        <IconFeatureCard
          icon={<CheckIcon size={16} />}
          title="Design for change"
          copy="Evolve a schema without breaking clients, using deprecations and additive changes with confidence."
        />
      </div>
    ),
  },
};

export const CustomClassName: Story = {
  args: {
    title: "Tighter spacing",
    className: "bg-cc-ink/5 rounded-2xl",
    children: (
      <p className="text-cc-ink/80 mx-auto max-w-2xl text-center text-lg">
        The optional className is appended to the section, so you can layer in a
        background, border, or spacing override per placement.
      </p>
    ),
  },
};
