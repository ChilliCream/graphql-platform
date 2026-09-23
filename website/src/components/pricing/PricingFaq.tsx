import { FaqSection } from "@/src/components/FaqSection";

const FAQ = [
  {
    question: "Is the Free tier really free?",
    answer:
      "Yes. The Free tier runs on shared cloud and includes schemas and environments, 1M operations and 2 GB of ingest per month, and 3-day retention, at no cost. When you need more, move to Pay as you go.",
  },
  {
    question: "How does Pay as you go billing work?",
    answer:
      "Pay as you go is $20 per month and includes 5M operations and 2 GB of ingest per million operations, with 60-day retention. Beyond the included volume you pay $2 per additional million operations and $1.15 per additional gigabyte of ingest.",
  },
  {
    question: "How is the Dedicated instance priced?",
    answer:
      "A Dedicated instance is single-tenant and priced by volume, the compute, storage, and nodes it runs on, starting from $400 per month. Retention is configurable, and you can run it in your own cloud (BYOC).",
  },
  {
    question: "What support do I get, and how does it grow?",
    answer:
      "Free includes community support and Pay as you go adds email support. As your monthly consumption grows you unlock Business support at $2,000 a month and Enterprise support at $4,000 a month, with priority engineering and a dedicated solution architect. Talk to us about the right plan.",
  },
  {
    question: "Do you support SSO and audit logs?",
    answer:
      "SSO via OIDC, role-based access control, and an admin audit log are included on the Dedicated and Self-Hosted plans. The Free and Pay as you go tiers ship role-based access control.",
  },
  {
    question: "Can I move between tiers later?",
    answer:
      "You can move between Free and Pay as you go yourself at any time, and your schemas, environments, and telemetry come with you. Moving to a Dedicated instance or self-hosting is a conversation, talk to us and we'll help you migrate.",
  },
];

/**
 * The pricing FAQ, rendered with the shared `FaqSection` disclosure list.
 */
export function PricingFaq() {
  return (
    <FaqSection
      id="faq"
      className="mt-24 scroll-mt-24 sm:mt-28"
      eyebrow="FAQ"
      heading="Common questions"
      items={FAQ}
    />
  );
}
