import { CardGrid } from "@/src/components/CardGrid";
import { PerkCard } from "@/src/components/PerkCard";
import { SectionHeading } from "@/src/components/SectionHeading";

const ENGAGEMENT_STEPS = [
  {
    index: "01",
    title: "Introductory call",
    description:
      "Walk us through the system, the decision or outcome you need, and the constraints that shape the work.",
    bullets: [
      "Current architecture and stack",
      "Goal, timeline, and constraints",
      "NDA requirements discussed up front",
    ],
  },
  {
    index: "02",
    title: "Proposal",
    description:
      "If there is a fit, the proposal matches the engagement to your need: a package of consulting hours or a scoped statement of work.",
    bullets: [
      "Hour package or fixed scope",
      "Clear deliverables and milestones",
      "Commercial terms in writing",
    ],
  },
  {
    index: "03",
    title: "Kickoff",
    description:
      "Once the scope and terms are agreed, we establish the working channels, backlog, and delivery checkpoints for the engagement.",
    bullets: [
      "Shared working channel",
      "Visible backlog and checkpoints",
      "Direct access to the engineers doing the work",
    ],
  },
];

/**
 * How an engagement starts: a bordered panel with the three steps from first
 * call to first commit, each a perk card with a short checklist.
 */
export function EngagementStrip() {
  return (
    <section
      aria-labelledby="engagement-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
    >
      <SectionHeading
        align="center"
        eyebrow="How an engagement starts"
        title="From first call to first commit in three steps."
        titleId="engagement-heading"
        description="Speak directly with an engineer, get a written proposal, and kick off with the scope, deliverables, and working model agreed in advance."
      />

      <div className="mt-10">
        <CardGrid cols={3} gap={6}>
          {ENGAGEMENT_STEPS.map((step) => (
            <PerkCard
              key={step.index}
              tag={`Step ${step.index}`}
              title={step.title}
              intro={step.description}
              items={step.bullets}
            />
          ))}
        </CardGrid>
      </div>
    </section>
  );
}
