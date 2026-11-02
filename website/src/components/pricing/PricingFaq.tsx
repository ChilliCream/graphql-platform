import { FaqSection } from "@/src/components/FaqSection";

export const PRICING_FAQ_ITEMS = [
  {
    question: "What is included in the Nitro Free plan?",
    answer:
      "The Free plan runs on the shared cloud and includes schemas and environments, 1 million operations, 2 GB of ingest per month, and 3-day log and trace retention for $0 per month.",
  },
  {
    question: "How does Pay as you go pricing work?",
    answer:
      "Pay as you go is $20 per month and includes 5 million operations, 2 GB of ingest per million operations, and 60-day retention. Additional usage is $2 per million operations and $1.15 per GB of ingest.",
  },
  {
    question: "How is a Dedicated Nitro instance priced?",
    answer:
      "Dedicated starts at $400 per month and is priced by instance size and volume. It supports a single-tenant ChilliCream cloud deployment or BYOC, with configurable retention and private networking.",
  },
  {
    question: "When should I choose Self-Hosted Nitro?",
    answer:
      "Choose Self-Hosted when Nitro must run on your infrastructure, including on-premises or air-gapped environments. The plan has custom pricing, configurable retention, a long-term release channel, and priority engineering support.",
  },
  {
    question: "Which plans include SSO and audit logs?",
    answer:
      "Dedicated and Self-Hosted include SSO, an audit log, and roles with stage-scoped publish permissions. Free and Pay as you go do not include those access-control features in the current comparison.",
  },
  {
    question: "How do I choose between Dedicated, BYOC, and Self-Hosted?",
    answer:
      "Choose Dedicated for a single-tenant deployment managed in the ChilliCream cloud, BYOC to run a dedicated instance in your cloud account, or Self-Hosted to run Nitro on your own infrastructure. Contact us to review isolation, networking, retention, and data-location requirements.",
  },
] as const;

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
      items={PRICING_FAQ_ITEMS}
    />
  );
}
