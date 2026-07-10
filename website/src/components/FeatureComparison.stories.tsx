import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { FeatureComparison } from "./FeatureComparison";

const meta = {
  title: "Components/FeatureComparison",
  component: FeatureComparison,
  parameters: { layout: "fullscreen" },
  argTypes: {
    id: { control: "text" },
    className: { control: "text" },
    eyebrow: { control: "text" },
    heading: { control: "text" },
    columns: { control: "object" },
    groups: { control: "object" },
  },
  args: {
    id: "feature-comparison",
    eyebrow: "Compare plans",
    heading: "Every capability, side by side",
    columns: ["Community", "Professional", "Enterprise"],
    groups: [
      {
        title: "Support",
        rows: [
          {
            label: "Response time",
            cells: ["Best effort", "Next business day", "4 hours"],
          },
          { label: "Private Slack channel", cells: [false, true, true] },
          { label: "Dedicated engineer", cells: [false, false, true] },
        ],
      },
      {
        title: "Platform",
        rows: [
          { label: "Schema registry", cells: [true, true, true] },
          { label: "Distributed tracing", cells: [false, true, true] },
          {
            label: "Seats",
            cells: ["Unlimited", "Up to 25", "Unlimited"],
          },
        ],
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
} satisfies Meta<typeof FeatureComparison>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const TwoPlans: Story = {
  args: {
    id: "support-comparison",
    eyebrow: "Support tiers",
    heading: "Standard vs. Priority support",
    columns: ["Standard", "Priority"],
    groups: [
      {
        title: "Channels",
        rows: [
          { label: "Email support", cells: [true, true] },
          { label: "Live chat", cells: [false, true] },
          { label: "Phone escalation", cells: [false, true] },
        ],
      },
      {
        title: "Service levels",
        rows: [
          {
            label: "First response",
            cells: ["2 business days", "1 hour"],
          },
          { label: "Uptime SLA", cells: ["99.5%", "99.95%"] },
        ],
      },
    ],
  },
};

export const StringHeavy: Story = {
  args: {
    id: "limits-comparison",
    eyebrow: "Limits",
    heading: "What each tier includes",
    columns: ["Starter", "Growth", "Scale"],
    groups: [
      {
        title: "Quotas",
        rows: [
          {
            label: "Monthly requests",
            cells: ["100K", "5M", "Unlimited"],
          },
          { label: "Environments", cells: ["1", "5", "Unlimited"] },
          { label: "Data retention", cells: ["7 days", "30 days", "1 year"] },
        ],
      },
    ],
  },
};
