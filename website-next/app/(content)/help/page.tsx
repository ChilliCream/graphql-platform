import type { Metadata } from "next";

import { type Plan, PlanGrid } from "@/src/components/PlanGrid";
import { Section } from "@/src/components/Section";

export const metadata: Metadata = {
  title: "Help",
  description:
    "Need urgent GraphQL help? Join the ChilliCream community Slack, book a session with an expert, or choose a support plan that fits your team.",
};

const PLANS: Plan[] = [
  {
    title: "Community",
    price: 0,
    period: "hour",
    description:
      "Be part of the Community, get help, and help others. Together we're strong.",
    features: ["Public Slack Channel", "7000+ Individuals"],
    ctaText: "Join Slack",
    ctaLink: "https://slack.chillicream.com/",
  },
  {
    title: "Consultancy",
    price: 300,
    period: "hour",
    description: "You need immediate help and want to talk to an Expert.",
    features: ["Dedicated Session", "Dedicated Expert"],
    ctaText: "Book a Session",
    ctaLink: "https://calendly.com/chillicream/60min",
  },
  {
    title: "Support",
    price: "custom",
    description: "You need a Support Plan. Here you go.",
    features: [
      "Dedicated Account Manager",
      "Private Slack Channel",
      "E-Mail Support",
      "And More...",
    ],
    ctaText: "Check out Plans",
    ctaLink: "/services/support",
  },
];

export default function HelpPage() {
  return (
    <Section title="In Need of Urgent Help">
      <PlanGrid plans={PLANS} />
    </Section>
  );
}
