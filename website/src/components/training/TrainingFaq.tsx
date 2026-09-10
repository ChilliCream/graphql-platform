import { FaqSection } from "@/src/components/FaqSection";

export const TRAINING_FAQ_ITEMS = [
  {
    question: "How long does a typical engagement take?",
    answer:
      "Typically a focused few days up to a full week. Short engagements suit a team that already ships GraphQL and wants depth on one topic. Longer engagements suit foundations plus a small project at the end. The exact shape is set per engagement once we know the team.",
  },
  {
    question: "What team size works best?",
    answer:
      "A single engineering team can usually work as one cohort. For a larger group, we will discuss whether separate cohorts or tracks make the material easier to apply. Share the headcount and experience mix when you contact us.",
  },
  {
    question: "What should the team know before day one?",
    answer:
      "For the beginner track, working knowledge of one server-side language (typically C# or TypeScript) and any web framework is enough. For the advanced track we expect existing GraphQL exposure, ideally a schema in production. There is no certification gate.",
  },
  {
    question: "How much does it cost?",
    answer:
      "Pricing is on request, because the right answer depends on team size, format (on site, remote, or hybrid), duration, and whether we are bundling a workshop project. Send us a short note and we will come back with a concrete proposal.",
  },
  {
    question: "How far ahead do we need to book?",
    answer:
      "Availability depends on the dates, delivery format, and curriculum. Send a few possible dates when you contact us. On-site engagements also need enough time to arrange travel.",
  },
  {
    question: "Can the curriculum cover our actual codebase?",
    answer:
      "It can. We can discuss reviewing a schema before the sessions, shaping exercises around your domain, or using part of a workshop for a current design question. Any access and data-handling requirements are agreed during planning.",
  },
] as const;

/**
 * The pre-booking FAQ, rendered with the shared `FaqSection` disclosure list.
 */
export function TrainingFaq() {
  return (
    <FaqSection
      id="faq"
      className="py-16 sm:py-20"
      eyebrow="Common questions"
      heading="Before you book."
      items={TRAINING_FAQ_ITEMS}
    />
  );
}
