import type { Metadata } from "next";

import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import { Section } from "@/src/components/Section";

export const metadata: Metadata = {
  title: "Help",
  description:
    "Need urgent GraphQL help? Join the ChilliCream community Slack, book a session with an expert, or choose a support plan that fits your team.",
};

const PLANS = [
  {
    title: "Community",
    price: "Free",
    description:
      "Be part of the Community, get help, and help others. Together we're strong.",
    perks: ["Public Slack Channel", "7000+ Individuals"],
    callToAction: {
      title: "Join Slack",
      link: "https://slack.chillicream.com/",
    },
  },
  {
    title: "Consultancy",
    price: "$300",
    priceNote: "per hour",
    description: "You need immediate help and want to talk to an Expert.",
    perks: ["Dedicated Session", "Dedicated Expert"],
    callToAction: {
      title: "Book a Session",
      link: "https://calendly.com/chillicream/60min",
    },
  },
  {
    title: "Support",
    price: "Custom",
    description: "You need a Support Plan. Here you go.",
    perks: [
      "Dedicated Account Manager",
      "Private Slack Channel",
      "E-Mail Support",
      "And More...",
    ],
    callToAction: { title: "Check out Plans", link: "/services/support" },
  },
];

export default function HelpPage() {
  return (
    <Section title="In Need of Urgent Help">
      <OfferingGrid columns="md:grid-cols-2 lg:grid-cols-3">
        {PLANS.map((plan) => (
          <Offering key={plan.title} {...plan} />
        ))}
      </OfferingGrid>
    </Section>
  );
}
