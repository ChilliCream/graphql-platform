import { CardGrid } from "@/src/components/CardGrid";
import { IconFeatureCard } from "@/src/components/IconFeatureCard";
import { SectionHeading } from "@/src/components/SectionHeading";
import { Icon, IconName } from "@/src/icons/Icon";

const OUTCOMES: { title: string; copy: string; icon: IconName }[] = [
  {
    title: "Read a schema like a map",
    copy: "Your team can navigate a large GraphQL schema, recognise the common shapes, and explain why a type is modelled the way it is.",
    icon: "map",
  },
  {
    title: "Write resolvers without surprises",
    copy: "From simple fields to data loaders and pagination, with the patterns that scale instead of the snippets that bite later.",
    icon: "wrench",
  },
  {
    title: "Plan a client they can live with",
    copy: "Fragments, variables, error handling, and a Relay or Apollo setup that the next person on the team can actually maintain.",
    icon: "plug",
  },
  {
    title: "Diagnose the slow query",
    copy: "Open a trace, read the plan, find the N+1, and know which knobs to turn in Hot Chocolate before reaching for hacks.",
    icon: "chart-line",
  },
  {
    title: "Have an opinion on federation",
    copy: "When to split a schema, when not to, and how Hot Chocolate Fusion fits with the platform they already run.",
    icon: "code-branch",
  },
  {
    title: "Speak the same language",
    copy: "Backend, frontend, and platform engineers leave with one shared vocabulary, so the next design review is faster and friendlier.",
    icon: "message",
  },
];

/**
 * The outcomes grid: the concrete things every team should walk away able to
 * do by the end of the week, regardless of where they started.
 */
export function OutcomesSection() {
  return (
    <section className="py-16 sm:py-20">
      <div className="mb-10">
        <SectionHeading
          align="center"
          eyebrow="By the end of the week"
          title="What your team will actually know."
          description="No certificate-printer outcomes. These are the things we expect every team to walk away able to do, regardless of where they started."
        />
      </div>
      <CardGrid cols={3} step="progressive" gap={4}>
        {OUTCOMES.map((outcome) => (
          <IconFeatureCard
            key={outcome.title}
            icon={<Icon icon={outcome.icon} size="lg" />}
            title={outcome.title}
            copy={outcome.copy}
          />
        ))}
      </CardGrid>
    </section>
  );
}
