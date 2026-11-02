import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { CheckIcon } from "./CheckIcon";
import { IconFeatureCard } from "./IconFeatureCard";

const meta = {
  title: "Components/IconFeatureCard",
  component: IconFeatureCard,
  parameters: { layout: "fullscreen" },
  argTypes: {
    layout: {
      control: "select",
      options: ["stacked", "inline"],
    },
    size: {
      control: "select",
      options: ["md", "lg"],
    },
    eyebrow: { control: "text" },
    subtitle: { control: "text" },
    footnote: { control: "text" },
    title: { control: "text" },
    copy: { control: "text" },
    icon: { control: false },
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto grid max-w-3xl gap-4 sm:grid-cols-2">
          <Story />
        </div>
      </div>
    ),
  ],
  args: { icon: <CheckIcon size={16} />, title: "", copy: "" },
} satisfies Meta<typeof IconFeatureCard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Stacked: Story = {
  args: {
    title: "Read a schema like a map",
    copy: "Navigate a large GraphQL schema, recognise the common shapes, and explain why a type is modelled the way it is.",
  },
};

export const Inline: Story = {
  args: {
    layout: "inline",
    title: "On site",
    subtitle: "We come to you",
    copy: "A trainer joins your team in a room with a whiteboard and proper coffee.",
    footnote: "Best for a single co-located team that can clear the calendar.",
  },
};

export const Hero: Story = {
  args: {
    eyebrow: "A quick question",
    size: "lg",
    title: "Message us, hear back in minutes",
    copy: "You hit an exception you have never seen before. You send us a message and get an answer in no time.",
  },
};

export const StackedWithEyebrow: Story = {
  args: {
    eyebrow: "Delivery format",
    title: "Remote workshop",
    copy: "Live sessions over video, recorded so the team can revisit the tricky parts.",
    footnote: "Best for distributed teams across several time zones.",
  },
};

export const InlineWithFootnote: Story = {
  args: {
    layout: "inline",
    title: "Code review",
    subtitle: "Async, on your PRs",
    copy: "We review real pull requests and leave comments your team can act on the same day.",
    footnote:
      "Best when the team is already shipping and wants targeted feedback.",
  },
};
