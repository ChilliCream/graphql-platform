import { FaqSection } from "@/src/components/FaqSection";

export const HELP_FAQ_ITEMS = [
  {
    question: "Where should I start?",
    answer:
      "Start with the docs, GitHub, or community Slack when the question can be handled in public and does not need a response guarantee. Choose advisory for a defined technical problem or review. Choose a support plan when your production team needs ongoing channels, incident allowances, and response terms.",
  },
  {
    question: "What response time can I expect on Slack?",
    answer:
      "Community Slack is best effort and does not include a response guarantee. If response terms matter to your team, compare the paid support plans and choose the incident coverage that matches your production needs.",
  },
  {
    question: "When should we choose advisory instead of a support plan?",
    answer:
      "Advisory fits a scoped question, architecture review, troubleshooting need, or implementation. A support plan fits ongoing production coverage with plan-specific channels, incident allowances, and response times. Teams can also use both.",
  },
  {
    question: "How do I escalate something urgent in production?",
    answer:
      "Use the channels and escalation process defined in your support agreement. Without a paid support plan, community channels remain best effort and should not be treated as an incident-response commitment.",
  },
  {
    question: "Can ChilliCream help us design a schema or migration?",
    answer:
      "Schema design, reviews, Fusion rollout planning, and migration guidance can be scoped as advisory work. For an implementation, discuss a contracting engagement with defined deliverables and milestones.",
  },
  {
    question: "Is the community Slack the right place for bug reports?",
    answer:
      "Slack is good for triage and reproductions. Once a bug is confirmed, please file it on GitHub so it gets a tracking issue, a label, and a place to land the fix.",
  },
] as const;

/**
 * The help FAQ, rendered with the shared `FaqSection` disclosure list.
 */
export function HelpFaq() {
  return (
    <FaqSection
      id="faq"
      className="py-16"
      eyebrow="FAQ"
      heading="Answers to common questions."
      items={HELP_FAQ_ITEMS}
    />
  );
}
