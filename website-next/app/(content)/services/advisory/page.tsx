import type { Metadata } from "next";

import { Offering } from "@/src/components/Offering";
import { OfferingGrid } from "@/src/components/OfferingGrid";
import { PageHero } from "@/src/components/PageHero";

export const metadata: Metadata = {
  title: "Advisory",
  description:
    "Get quick access to ChilliCream's GraphQL experts: hourly consulting, architecture guidance, code reviews, and full contracting engagements.",
};

interface InquiryPlan {
  title: string;
  description: string;
  features: string[];
  ctaText: string;
  ctaLink: string;
}

const PLANS: InquiryPlan[] = [
  {
    title: "Consulting",
    description:
      "Hourly consulting services to get the help you need at any stage of your project. This is the best way to get started.",
    features: [
      "Mentoring and guidance",
      "Architecture",
      "Troubleshooting",
      "Code Review",
      "Best practices education",
    ],
    ctaText: "Talk to an Expert",
    ctaLink: "/services/support/contact?subject=Technical+Support",
  },
  {
    title: "Contracting",
    description:
      "Options for teams who don't have the time, bandwidth, and/or expertise to implement their own GraphQL solutions.",
    features: ["Proof of concept", "Implementation"],
    ctaText: "Talk to an Expert",
    ctaLink: "/services/support/contact?subject=Technical+Support",
  },
];

export default function AdvisoryPage() {
  return (
    <>
      <PageHero
        title="Get Quick Access to Experts"
        teaser="At ChilliCream, we want you to be successful. From guidance to embedded experts, find the right level for your business."
      />
      <section className="py-8">
        <OfferingGrid columns="md:grid-cols-2">
          {PLANS.map((plan) => (
            <Offering
              key={plan.title}
              headingLevel="h2"
              title={plan.title}
              description={plan.description}
              perks={plan.features}
              callToAction={{ title: plan.ctaText, link: plan.ctaLink }}
            />
          ))}
        </OfferingGrid>
      </section>
    </>
  );
}
