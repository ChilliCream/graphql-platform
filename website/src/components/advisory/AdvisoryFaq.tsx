import { FaqSection } from "@/src/components/FaqSection";

export const ADVISORY_FAQ_ITEMS = [
  {
    question: "How is consulting priced?",
    answer:
      "Consulting is sold in agreed packages of hours. Contracting engagements are scoped separately with a statement of work that defines the deliverables, milestones, and commercial terms.",
  },
  {
    question: "How small is too small for an engagement?",
    answer:
      "Consulting starts in 20-hour increments, so you can use a focused package to unblock a specific decision, review, or troubleshooting problem. Contracting is the better fit when you want ChilliCream engineers to deliver an agreed result against a defined scope.",
  },
  {
    question: "Do you sign an NDA?",
    answer:
      "Tell us about your NDA and data-handling requirements before sharing code, schemas, or traces. We can discuss a mutual NDA and agree how project material will be handled as part of discovery.",
  },
  {
    question: "How quickly can you start?",
    answer:
      "Availability depends on the scope and current schedule. We will give you a realistic start date during discovery instead of promising a slot before we understand the work.",
  },
  {
    question: "What outcomes can I expect?",
    answer:
      "Concrete, written deliverables tied to your goal: an architecture decision record, a schema review with line-level comments, a working proof of concept, or a production implementation. The proposal defines the outcome before work starts. We do not bill for slideware.",
  },
  {
    question: "Who actually does the work?",
    answer:
      "The engineers who build Hot Chocolate, Fusion, and Nitro. The same people who write the framework code, review the pull requests, and answer the hard issues on GitHub are the people on your call.",
  },
] as const;

/**
 * The advisory FAQ, rendered with the shared `FaqSection` disclosure list.
 */
export function AdvisoryFaq() {
  return (
    <FaqSection
      id="faq"
      className="mt-20 sm:mt-28"
      eyebrow="Frequently asked"
      heading="Honest answers before you reach out."
      items={ADVISORY_FAQ_ITEMS}
    />
  );
}
