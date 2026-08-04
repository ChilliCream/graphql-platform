import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { NextStepsSection } from "./NextStepsSection";

const meta = {
  title: "Components/NextStepsSection",
  component: NextStepsSection,
  parameters: { layout: "fullscreen" },
  argTypes: {
    title: { control: "text" },
    text: { control: false },
    primaryLink: { control: "text" },
    primaryLinkText: { control: "text" },
    secondaryLink: { control: "text" },
    secondaryLinkText: { control: "text" },
  },
  args: {
    title: "Ready to get started?",
    text: "Pick the path that fits your team and ship your first schema today.",
    primaryLink: "/get-started",
    primaryLinkText: "Get started",
    secondaryLink: "/docs",
    secondaryLinkText: "Read the docs",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof NextStepsSection>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Support: Story = {
  args: {
    title: "Talk to an engineer",
    text: "Get production support from the people who build HotChocolate, with response times measured in minutes.",
    primaryLink: "/contact",
    primaryLinkText: "Contact sales",
    secondaryLink: "/services/support",
    secondaryLinkText: "Compare plans",
  },
};

export const RichText: Story = {
  args: {
    title: "Two ways forward",
    text: (
      <>
        Start free with the open-source SDK, or{" "}
        <strong>book a guided onboarding</strong> for your team.
      </>
    ),
    primaryLink: "/get-started",
    primaryLinkText: "Start free",
    secondaryLink: "/contact",
    secondaryLinkText: "Book onboarding",
  },
};
