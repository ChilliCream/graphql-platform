import Link from "next/link";

import { type Plan, PlanGrid } from "@/src/components/PlanCard";
import { PageHero, Section } from "@/src/components/SectionTitle";

const NITRO_PLANS: Plan[] = [
  {
    title: "Shared",
    price: 0,
    period: "month",
    fromPrice: true,
    description: "Hosted on shared infrastructure",
    features: [
      "Schema & Client Registry",
      "GraphQL IDE",
      "Fusion Management",
      "Document Sharing",
      "OpenTelemetry",
      "Github or Google Login",
      "Usage-based pricing",
    ],
    ctaText: "Start for Free",
    ctaLink: "https://nitro.chillicream.com",
  },
  {
    title: "Scale",
    price: 430,
    period: "month",
    fromPrice: true,
    description: "Dedicated Infrastructure with advanced features",
    features: [
      "Dedicated Infrastructure",
      "Dedicated DBs",
      "Choose your region",
      "Single Sign-On",
      "Scheduled Maintenance",
      "Volume Based Pricing",
      "Bring Your Own Domain*",
      "VNET Peering*",
      "BYOC*",
    ],
    ctaText: "Contact Sales",
    ctaLink: "/services/support/contact?subject=Pricing+%26+Plans",
  },
  {
    title: "Enterprise",
    price: "custom",
    description: "Everything in Scale plus enterprise features",
    features: [
      "Everything in Scale",
      "On-Premise Option",
      "Source Code Access",
      "Dedicated Account Manager",
      "24/7 Support",
    ],
    ctaText: "Contact Sales",
    ctaLink: "/services/support/contact?subject=Pricing+%26+Plans",
  },
];

const SUPPORT_PLANS: Plan[] = [
  {
    title: "Professional",
    price: 450,
    period: "month",
    description: "Essential support for growing teams ($5,000/year)",
    features: [
      "2 Critical Incidents",
      "24h Business Hour SLA",
      "Private Slack Channel",
      "Access to expert engineers",
    ],
    ctaText: "Contact Sales",
    ctaLink: "/services/support/contact?subject=Pricing+%26+Plans",
  },
  {
    title: "Business",
    price: 1300,
    period: "month",
    description: "Comprehensive support for business teams ($15,000/year)",
    features: [
      "Unlimited Critical Incidents",
      "24h Business Hour SLA",
      "Private Slack Channel",
      "Access to expert engineers",
      "4 Non-Critical Incidents",
      "Issue Tracking Board",
      "Email Support",
    ],
    ctaText: "Contact Sales",
    ctaLink: "/services/support/contact?subject=Pricing+%26+Plans",
  },
  {
    title: "Enterprise",
    price: "custom",
    description: "Premium support with dedicated resources",
    features: [
      "Unlimited Critical Incidents",
      "Custom SLA",
      "Private Slack Channel",
      "Access to expert engineers",
      "10 Non-Critical Incidents",
      "Issue Tracking Board",
      "Email Support",
      "Phone Support",
      "Dedicated Account Manager",
      "Quarterly Status Reviews",
      "Source Code Access",
      "Nitro License included",
    ],
    ctaText: "Contact Sales",
    ctaLink: "/services/support/contact?subject=Pricing+%26+Plans",
  },
];

const ADDON_PLANS: Plan[] = [
  {
    title: "Advisory Services",
    price: 300,
    period: "hour",
    fromPrice: true,
    description: "Expert guidance and consulting for your GraphQL implementation",
    features: [
      "GraphQL expertise",
      "Architecture reviews",
      "Performance optimization",
      "Best practices guidance",
      "Custom solution design",
    ],
    ctaText: "Contact Sales",
    ctaLink: "/services/support/contact?subject=Pricing+%26+Plans",
  },
  {
    title: "Private Workshops",
    price: 8000,
    period: "day",
    description: "Customized training sessions for your team",
    features: [
      "Full-day workshop",
      "Customized curriculum",
      "Hands-on exercises",
      "Expert instructors",
      "Post-workshop support",
      "Materials included",
    ],
    ctaText: "Contact Sales",
    ctaLink: "/services/support/contact?subject=Pricing+%26+Plans",
  },
  {
    title: "Monthly Sessions",
    price: 15000,
    period: "year",
    description: "Regular collaboration sessions with our expert team",
    features: [
      "Monthly 2-hour sessions",
      "Direct access to experts",
      "Progress tracking",
      "Strategic planning",
      "Problem-solving support",
      "Knowledge transfer",
    ],
    ctaText: "Contact Sales",
    ctaLink: "/services/support/contact?subject=Pricing+%26+Plans",
  },
];

export default function PricingPage() {
  return (
    <>
      <PageHero
        title="Nitro Plans"
        subtitle="For any Scale"
        teaser="Choose the right plan for your Team."
      />

      <Section title="Nitro Platform">
        <PlanGrid plans={NITRO_PLANS} />
        <p className="mt-8 text-center text-sm text-[var(--cc-ink-dim)]">
          *Available in Platinum and Enterprise plans
        </p>
      </Section>

      <Section title="Support Plans">
        <PlanGrid plans={SUPPORT_PLANS} />
      </Section>

      <Section title="Add-On Services">
        <PlanGrid plans={ADDON_PLANS} />
        <p className="mt-8 text-center text-sm italic text-[var(--cc-ink-dim)]">
          Add-on services are optional for all plans and can be combined with
          any Nitro or Support plan.
        </p>
      </Section>

      <Section title="Open Source Libraries">
        <div className="mx-auto max-w-2xl rounded-xl border border-[var(--cc-card-border)] bg-[var(--cc-card-bg)] backdrop-blur-sm p-8 text-center ">
          <h3 className="text-xl font-semibold text-[var(--cc-ink)]">
            All Our Core Libraries Are Free & Open Source
          </h3>
          <p className="mt-3 text-[var(--cc-ink-dim)]">
            <strong>HotChocolate</strong>, <strong>StrawberryShake</strong>, and{" "}
            <strong>GreenDonut</strong> are MIT licensed and completely free to
            use in any project, commercial or otherwise.
          </p>
          <div className="mt-6 flex flex-wrap justify-center gap-4">
            <Link
              href="https://github.com/ChilliCream/graphql-platform"
              target="_blank"
              rel="noopener noreferrer nofollow"
              className="inline-flex items-center rounded-md border border-[var(--cc-card-border)] px-5 py-2 text-sm font-medium text-[var(--cc-ink)] no-underline transition-colors hover:border-[var(--cc-card-border-hover)] hover:text-[var(--cc-ink)]"
            >
              View on GitHub
            </Link>
            <Link
              href="https://github.com/ChilliCream/graphql-platform/blob/main/LICENSE"
              target="_blank"
              rel="noopener noreferrer nofollow"
              className="inline-flex items-center rounded-md border border-[var(--cc-card-border)] px-5 py-2 text-sm font-medium text-[var(--cc-ink)] no-underline transition-colors hover:border-[var(--cc-card-border-hover)] hover:text-[var(--cc-ink)]"
            >
              MIT License
            </Link>
          </div>
        </div>
      </Section>
    </>
  );
}
