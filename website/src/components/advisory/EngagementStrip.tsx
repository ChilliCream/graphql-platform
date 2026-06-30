import { PerkCard } from "@/src/components/PerkCard";
import { SectionHeading } from "@/src/components/SectionHeading";

const ENGAGEMENT_STEPS = [
  {
    index: "01",
    title: "Introductory call",
    description:
      "A working call. You walk us through the system, the goal, and the constraints. We ask the hard questions and tell you whether we are the right fit.",
    bullets: [
      "Senior engineer, no sales handoff",
      "Mutual NDA available on request",
      "Written recap with next steps",
    ],
  },
  {
    index: "02",
    title: "Proposal",
    description:
      "A written proposal that matches your need: a package of hours for consulting, or a scoped statement of work for contracting with deliverables, milestones, and a target timeline.",
    bullets: [
      "Hour package or fixed scope",
      "Clear deliverables and milestones",
      "Written, scoped, and signable",
    ],
  },
  {
    index: "03",
    title: "Kickoff",
    description:
      "Contract signed, channel opened, work starts. You get a direct line to the engineers doing the work, a shared backlog, and a weekly checkpoint.",
    bullets: [
      "Shared Slack or Teams channel",
      "Weekly checkpoint and written status",
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
        description="No long sales cycle. You speak to an engineer, you get a written proposal, you kick off."
      />

      <div className="mt-10 grid gap-6 md:grid-cols-3">
        {ENGAGEMENT_STEPS.map((step) => (
          <PerkCard
            key={step.index}
            tag={`Step ${step.index}`}
            title={step.title}
            intro={step.description}
            items={step.bullets}
          />
        ))}
      </div>
    </section>
  );
}
