import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Offering } from "./Offering";

const meta = {
  title: "Components/Offering",
  component: Offering,
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-md">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof Offering>;

export default meta;
type Story = StoryObj<typeof meta>;

// Full offering: title, description, perks, and a call to action.
export const Default: Story = {
  args: {
    title: "Corporate Training",
    description:
      "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
    perks: [
      "Level up their proficiency",
      "Catered to different skills",
      "Overcome challenges they've been wrestling with",
      "Get everybody on the same technical page",
    ],
    callToAction: {
      title: "Talk to us",
      link: "mailto:contact@chillicream.com?subject=Corporate Training",
    },
  },
};

// Short perk list with a single feature.
export const ShortList: Story = {
  args: {
    title: "Contracting",
    description:
      "Options for teams who don't have the time, bandwidth, and/or expertise to implement their own GraphQL solutions.",
    perks: ["Proof of concept", "Implementation"],
    callToAction: {
      title: "Talk to an Expert",
      link: "mailto:contact@chillicream.com?subject=Contracting",
    },
  },
};

// Without a call to action button.
export const WithoutCallToAction: Story = {
  args: {
    title: "Consulting",
    description:
      "Hourly consulting services to get the help you need at any stage of your project.",
    perks: [
      "Mentoring and guidance",
      "Architecture",
      "Troubleshooting",
      "Code Review",
      "Best practices education",
    ],
  },
};
