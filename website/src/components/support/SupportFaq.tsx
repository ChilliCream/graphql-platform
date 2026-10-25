import { FaqSection } from "@/src/components/FaqSection";

export const SUPPORT_FAQ_ITEMS = [
  {
    question: "What counts as a critical incident?",
    answer:
      "An incident is critical when a production system you run on Hot Chocolate, Fusion, or Nitro is down, returning wrong data, or otherwise hard-blocked. Anything that degrades a live user experience qualifies. Local dev issues and questions are non-critical.",
  },
  {
    question: "How fast do you respond?",
    answer:
      "Startup and Business respond to critical incidents by the next business day. Enterprise responds to critical incidents within 24 hours. Business responds to non-critical incidents within 3 business days, while Enterprise responds by the next business day. Community Slack is best effort.",
  },
  {
    question: "How is an incident opened and tracked?",
    answer:
      "Startup, Business, and Enterprise include a private Slack channel. Business and Enterprise also include a private issue tracking board. Enterprise adds email and phone support to the listed channels.",
  },
  {
    question: "Which paid support plan should we choose?",
    answer:
      "Startup fits a team that needs a private channel and limited critical-incident coverage. Business adds non-critical incidents, email support, and private issue tracking. Enterprise adds tailored terms, more channels, status reviews, and a dedicated account manager.",
  },
  {
    question: "What products can a plan cover?",
    answer:
      "The support page covers Hot Chocolate, Fusion, and Nitro. Tell us which products and deployment models are on your critical path so the commercial agreement can reflect the systems your team operates.",
  },
  {
    question: "How is support different from an advisory engagement?",
    answer:
      "A support plan provides ongoing channels, incident allowances, and response terms. Advisory is scoped around a design decision, review, troubleshooting session, or implementation. Teams can use either service or combine them.",
  },
] as const;

/**
 * The support FAQ, rendered with the shared `FaqSection` disclosure list.
 */
export function SupportFaq() {
  return (
    <FaqSection
      id="faq"
      className="py-16"
      eyebrow="FAQ"
      heading="Common questions"
      items={SUPPORT_FAQ_ITEMS}
    />
  );
}
