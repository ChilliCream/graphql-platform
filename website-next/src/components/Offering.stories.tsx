import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { DripBrewer } from "@/src/icons/DripBrewer";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

import { Offering } from "./Offering";
import { OfferingGrid } from "./OfferingGrid";

const meta = {
  title: "Components/Offering",
  component: Offering,
  // Stories compose multiple `Offering`s via `render`, so per-card props are set
  // inline. These satisfy the component's required args at the meta level.
  args: { title: "", perks: [] },
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-6xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof Offering>;

export default meta;
type Story = StoryObj<typeof meta>;

// Pricing layout (as on the home page): three plans with icons and prices,
// the middle one highlighted as "Most Popular".
export const PricingPlans: Story = {
  render: () => (
    <OfferingGrid columns="md:grid-cols-3">
      <Offering
        Icon={FrenchPress}
        title="Shared Instance"
        description="Shared resources, fully managed"
        price="Start free"
        priceNote="pay-as-you-go"
        perks={[
          "Multi-tenant cloud region",
          "1 Schema · 3 Environments",
          "Up to 5M ops / month included",
          "Community Slack support",
        ]}
        callToAction={{ title: "Start for Free", link: "/get-started" }}
      />
      <Offering
        popular
        Icon={DripBrewer}
        title="Dedicated Instance"
        description="Dedicated resources, fully managed"
        price="$400"
        priceNote="per month"
        perks={[
          "Single-tenant cloud region",
          "Unlimited schemas",
          "BYOC region · private networking",
          "99.95% SLA · email + private chat",
          "SSO, audit log, role-based access",
        ]}
        callToAction={{ title: "Start for Free", link: "/get-started" }}
      />
      <Offering
        Icon={PourOver}
        title="Self-Hosted"
        description="Self managed"
        price="Custom"
        priceNote="talk to us"
        perks={[
          "Run on your own infrastructure",
          "Air-gapped & on-prem supported",
          "Priority engineering support",
          "Long-term release channel",
        ]}
        callToAction={{
          title: "Talk to Us",
          link: "/services/support/contact",
        }}
      />
    </OfferingGrid>
  ),
};

// Services layout: no icons, no prices, and descriptions of different lengths
// to show the subgrid keeping the divider and perks aligned across the row.
export const Services: Story = {
  render: () => (
    <OfferingGrid columns="md:grid-cols-3">
      <Offering
        title="Consulting"
        description="Hourly help at any stage of your project."
        perks={["Mentoring and guidance", "Architecture", "Code Review"]}
        callToAction={{
          title: "Talk to us",
          link: "mailto:contact@chillicream.com?subject=Consulting",
        }}
      />
      <Offering
        title="Corporate Training"
        description="Get your team trained in GraphQL, any of our products, and even React/Relay, with a curriculum tailored to beginner, advanced, or mixed teams."
        perks={[
          "Level up their proficiency",
          "Catered to different skills",
          "Get everybody on the same technical page",
        ]}
        callToAction={{
          title: "Talk to us",
          link: "mailto:contact@chillicream.com?subject=Corporate Training",
        }}
      />
      <Offering
        title="Contracting"
        description="For teams without the time or in-house expertise to ship their own GraphQL solution."
        perks={["Proof of concept", "Implementation"]}
        callToAction={{
          title: "Talk to an Expert",
          link: "mailto:contact@chillicream.com?subject=Contracting",
        }}
      />
    </OfferingGrid>
  ),
};
