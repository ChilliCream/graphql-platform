import { FaqSection } from "@/src/components/FaqSection";

const FAQ = [
  {
    question: "How is consulting priced?",
    answer:
      "Consulting is sold in packages of hours agreed up front, on a retainer or a small purchase order, so you never get a surprise invoice. Contracting engagements are scoped separately as a statement of work.",
  },
  {
    question: "How small is too small for an engagement?",
    answer:
      "A small package is fine. Many teams start with a small block of hours to unblock a specific decision (schema shape, Fusion composition, auth model) and only return when the next question lands. There is no minimum retainer to talk to us.",
  },
  {
    question: "Do you sign an NDA?",
    answer:
      "Yes. We will sign a mutual NDA before the introductory call when you ask, and we are comfortable with most standard agreements. Customer code, schemas, and traces never leave the engagement.",
  },
  {
    question: "How quickly can you start?",
    answer:
      "Consulting usually starts within the same week, sometimes the same day. Contracting engagements depend on scope and current bandwidth, and we will tell you the realistic start date on the introductory call rather than promise a slot we cannot honor.",
  },
  {
    question: "What outcomes can I expect?",
    answer:
      "Concrete, written deliverables tied to your goal: an architecture decision record, a schema review with line-level comments, a working proof of concept, or a production-ready implementation. We do not bill for slideware.",
  },
  {
    question: "Who actually does the work?",
    answer:
      "The engineers who build Hot Chocolate, Fusion, and Nitro. The same people who write the framework code, review the pull requests, and answer the hard issues on GitHub are the people on your call.",
  },
];

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
      items={FAQ.map((f) => ({ question: f.question, answer: f.answer }))}
    />
  );
}
