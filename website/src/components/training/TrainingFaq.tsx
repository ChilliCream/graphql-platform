import { FaqSection } from "@/src/components/FaqSection";

const FAQS = [
  {
    q: "How long does a typical engagement take?",
    a: "Typically a focused few days up to a full week. Short engagements suit a team that already ships GraphQL and wants depth on one topic. Longer engagements suit foundations plus a small project at the end. The exact shape is set per engagement once we know the team.",
  },
  {
    q: "What team size works best?",
    a: "We are most comfortable with a single engineering team in one cohort. Larger groups are usually split into parallel tracks with the same trainer rotating between them, or run as two cohorts back to back. We will recommend the shape that fits once we know the headcount.",
  },
  {
    q: "What should the team know before day one?",
    a: "For the beginner track, working knowledge of one server-side language (typically C# or TypeScript) and any web framework is enough. For the advanced track we expect existing GraphQL exposure, ideally a schema in production. There is no certification gate.",
  },
  {
    q: "How much does it cost?",
    a: "Pricing is on request, because the right answer depends on team size, format (on site, remote, or hybrid), duration, and whether we are bundling a workshop project. Send us a short note and we will come back with a concrete proposal.",
  },
  {
    q: "How far ahead do we need to book?",
    a: "A few weeks of lead time is typical. We sometimes have shorter slots, and we will tell you honestly if your dates are tight. For on-site engagements travel logistics tend to be the long pole, not curriculum prep.",
  },
  {
    q: "Can the curriculum cover our actual codebase?",
    a: "Yes, and we encourage it. We can review a schema you share ahead of time, design exercises around shapes from your domain, and dedicate part of the week to bugs or design questions your team is wrestling with right now.",
  },
];

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
      items={FAQS.map((f) => ({ question: f.q, answer: f.a }))}
    />
  );
}
