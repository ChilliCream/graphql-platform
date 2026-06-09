import { PageHero } from "@/src/components/PageHero";
import { SolidButton } from "@/src/design-system/Button";

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
    ctaLink: "mailto:contact@chillicream.com?subject=Consulting",
  },
  {
    title: "Contracting",
    description:
      "Options for teams who don't have the time, bandwidth, and/or expertise to implement their own GraphQL solutions.",
    features: ["Proof of concept", "Implementation"],
    ctaText: "Talk to an Expert",
    ctaLink: "mailto:contact@chillicream.com?subject=Contracting",
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
        <div className="grid gap-6 md:grid-cols-2">
          {PLANS.map((plan) => (
            <div
              key={plan.title}
              className="flex flex-col rounded-xl border border-cc-card-border bg-cc-card-bg backdrop-blur-sm p-8 "
            >
              <h2 className="text-2xl font-semibold text-cc-ink">
                {plan.title}
              </h2>
              <p className="mt-3 text-sm text-cc-ink-dim">{plan.description}</p>
              <ul className="mt-6 flex flex-1 flex-col gap-2 text-sm text-cc-ink">
                {plan.features.map((feature) => (
                  <li key={feature} className="flex items-start gap-2">
                    <span aria-hidden className="mt-1 text-cc-accent">
                      ✓
                    </span>
                    <span>{feature}</span>
                  </li>
                ))}
              </ul>
              <SolidButton href={plan.ctaLink} className="mt-8">
                {plan.ctaText}
              </SolidButton>
            </div>
          ))}
        </div>
      </section>
    </>
  );
}
