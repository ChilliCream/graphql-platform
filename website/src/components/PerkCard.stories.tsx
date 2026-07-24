import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PerkCard } from "./PerkCard";

const meta = {
  title: "Components/PerkCard",
  component: PerkCard,
  parameters: { layout: "fullscreen" },
  argTypes: {
    title: { control: "text" },
    items: { control: "object" },
    tag: { control: "text" },
    subtitle: { control: "text" },
    intro: { control: "text" },
    listLabel: { control: "text" },
    accent: {
      control: "select",
      options: ["accent", "violet", "coral"],
    },
    highlight: { control: "boolean" },
    highlightLabel: { control: "text" },
    cta: { control: "object" },
  },
  args: {
    title: "Beginner team",
    items: [
      "Schema-first thinking and the type system",
      "Queries, mutations, variables, and fragments",
      "Hot Chocolate basics on ASP.NET Core",
    ],
    accent: "accent",
    highlight: false,
    highlightLabel: "Most popular",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto grid max-w-3xl gap-4 sm:grid-cols-2 sm:items-stretch">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof PerkCard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

// A level card: mono tag, per-card accent, no CTA.
export const Level: Story = {
  args: {
    tag: "Level 1",
    title: "Beginner team",
    subtitle: "Heard of GraphQL. Maybe shipped a toy server.",
    intro:
      "We start from REST instincts and rebuild them, until the team can read a schema and write resolvers with confidence.",
    listLabel: "What we cover",
    items: [
      "Schema-first thinking and the type system",
      "Queries, mutations, variables, and fragments",
      "Hot Chocolate basics on ASP.NET Core",
    ],
    accent: "accent",
  },
};

// The violet accent tints the tag, icon, and perk checks.
export const VioletAccent: Story = {
  args: {
    tag: "Level 2",
    title: "Working team",
    subtitle: "Shipping GraphQL in production already.",
    listLabel: "What we cover",
    items: [
      "DataLoaders and the N+1 problem",
      "Schema stitching and federation",
      "Performance, caching, and persisted queries",
    ],
    accent: "violet",
  },
};

// The coral accent paired with a top-right illustration.
export const CoralWithIcon: Story = {
  args: {
    tag: "Level 3",
    title: "Platform team",
    subtitle: "Owning the gateway for many teams.",
    listLabel: "What we cover",
    items: [
      "Fusion and distributed schemas",
      "Subscriptions at scale",
      "Operational guardrails and observability",
    ],
    accent: "coral",
    icon: "check",
  },
};

// An offer card with an outline CTA, not highlighted.
export const WithOutlineCta: Story = {
  args: {
    title: "Team Workshop",
    subtitle: "A focused two-day intensive",
    intro:
      "Build a GraphQL server with ASP.NET Core and Hot Chocolate over two days, tailored to where your team is today.",
    listLabel: "What is in the box",
    items: [
      "Core concepts and advanced",
      "Work on a real project",
      "Level up your entire team at once",
    ],
    cta: {
      label: "Enquire about a workshop",
      href: "mailto:contact@chillicream.com",
    },
  },
};

// An offer card: highlighted (solid accent border + badge) with a CTA.
export const HighlightedOffer: Story = {
  args: {
    title: "Corporate Workshop",
    subtitle: "Hands on, with a real project at the end",
    intro:
      "Build a GraphQL server with ASP.NET Core and Hot Chocolate, then explore React and Relay on a real project.",
    listLabel: "What is in the box",
    items: [
      "Core concepts and advanced",
      "Work on a real project",
      "Level up your entire team at once",
    ],
    cta: {
      label: "Book Corporate Workshop",
      href: "mailto:contact@chillicream.com",
      solid: true,
    },
    highlight: true,
    highlightLabel: "Most popular",
  },
};
