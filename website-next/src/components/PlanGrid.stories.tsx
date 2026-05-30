import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { type Plan, PlanGrid } from "./PlanGrid";

const meta = {
  title: "Components/PlanGrid",
  component: PlanGrid,
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-6xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof PlanGrid>;

export default meta;
type Story = StoryObj<typeof meta>;

// A free tier: zero price with a period, short feature list, external CTA.
const community: Plan = {
  title: "Community",
  price: 0,
  period: "month",
  description: "Get help from the community and help others along the way.",
  features: ["Public Slack channel", "7000+ developers"],
  ctaText: "Join Slack",
  ctaLink: "https://slack.chillicream.com/",
};

// A paid tier: "from" pricing with a period and a longer feature list.
const pro: Plan = {
  title: "Pro",
  price: 49,
  period: "month",
  fromPrice: true,
  description: "Everything a growing team needs to ship with confidence.",
  features: [
    "Private Slack channel",
    "Priority email support",
    "Schema registry",
    "CI/CD integration",
    "Usage analytics",
  ],
  ctaText: "Start Free Trial",
  ctaLink: "/pricing",
};

// A custom tier: no numeric price, internal CTA.
const enterprise: Plan = {
  title: "Enterprise",
  price: "custom",
  description: "Tailored support and onboarding for organizations at scale.",
  features: [
    "Dedicated account manager",
    "SLA-backed support",
    "On-site training",
    "Security review",
  ],
  ctaText: "Contact Sales",
  ctaLink: "mailto:contact@chillicream.com?subject=Enterprise",
};

// A flat one-time price: no period, single feature, mailto CTA.
const oneTime: Plan = {
  title: "Audit",
  price: 2500,
  description: "A one-off architecture and performance review of your API.",
  features: ["Full written report"],
  ctaText: "Request Audit",
  ctaLink: "mailto:contact@chillicream.com?subject=Audit",
};

export const Default: Story = {
  args: { plans: [community, pro, enterprise] },
};

export const TwoTiers: Story = {
  args: { plans: [community, enterprise] },
};

export const FourTiers: Story = {
  args: { plans: [community, oneTime, pro, enterprise] },
};

export const SingleTier: Story = {
  args: { plans: [pro] },
};
