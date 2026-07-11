import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, userEvent, within } from "storybook/test";

import { FaqSection } from "./FaqSection";

const meta = {
  title: "Components/FaqSection",
  component: FaqSection,
  parameters: { layout: "fullscreen" },
  argTypes: {
    id: { control: "text" },
    className: { control: "text" },
    eyebrow: { control: "text" },
    heading: { control: "text" },
    items: { control: "object" },
  },
  args: {
    id: "faq",
    eyebrow: "Questions",
    heading: "Frequently asked questions",
    items: [
      {
        question: "How fast do you respond to support requests?",
        answer:
          "Most messages get a first response within minutes during business hours, and every request is acknowledged the same day.",
      },
      {
        question: "Do you offer on-site training?",
        answer:
          "Yes. A trainer can join your team in person for a focused workshop, or run the same session remotely if your team is distributed.",
      },
      {
        question: "Can we pay annually instead of monthly?",
        answer:
          "Annual billing is available on every plan and comes with a discount compared to paying month to month.",
      },
    ],
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof FaqSection>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const SingleItem: Story = {
  args: {
    id: "faq-single",
    eyebrow: "Support",
    heading: "One thing to know",
    items: [
      {
        question: "Is there a free trial?",
        answer:
          "Every plan starts with a 14-day free trial. No credit card is required to get going.",
      },
    ],
  },
};

export const ExpandsOnClick: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);

    const firstSummary = canvas.getAllByText(
      "How fast do you respond to support requests?",
    )[0];
    const details = firstSummary.closest("details");

    expect(details).not.toBeNull();
    expect(details).not.toHaveAttribute("open");

    await userEvent.click(firstSummary);

    expect(details).toHaveAttribute("open");
    expect(
      canvas.getByText(/Most messages get a first response within minutes/),
    ).toBeVisible();
  },
};
