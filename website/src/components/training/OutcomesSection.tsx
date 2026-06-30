import { IconFeatureCard } from "@/src/components/IconFeatureCard";
import { SectionHeading } from "@/src/components/SectionHeading";
import { BranchIcon } from "@/src/icons/BranchIcon";
import { ChatIcon } from "@/src/icons/ChatIcon";
import { GraphIcon } from "@/src/icons/GraphIcon";
import { MapIcon } from "@/src/icons/MapIcon";
import { PlugIcon } from "@/src/icons/PlugIcon";
import { WrenchIcon } from "@/src/icons/WrenchIcon";

const OUTCOMES = [
  {
    title: "Read a schema like a map",
    copy: "Your team can navigate a large GraphQL schema, recognise the common shapes, and explain why a type is modelled the way it is.",
    Icon: MapIcon,
  },
  {
    title: "Write resolvers without surprises",
    copy: "From simple fields to data loaders and pagination, with the patterns that scale instead of the snippets that bite later.",
    Icon: WrenchIcon,
  },
  {
    title: "Plan a client they can live with",
    copy: "Fragments, variables, error handling, and a Relay or Apollo setup that the next person on the team can actually maintain.",
    Icon: PlugIcon,
  },
  {
    title: "Diagnose the slow query",
    copy: "Open a trace, read the plan, find the N+1, and know which knobs to turn in Hot Chocolate before reaching for hacks.",
    Icon: GraphIcon,
  },
  {
    title: "Have an opinion on federation",
    copy: "When to split a schema, when not to, and how Hot Chocolate Fusion fits with the platform they already run.",
    Icon: BranchIcon,
  },
  {
    title: "Speak the same language",
    copy: "Backend, frontend, and platform engineers leave with one shared vocabulary, so the next design review is faster and friendlier.",
    Icon: ChatIcon,
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
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {OUTCOMES.map((outcome) => (
          <IconFeatureCard
            key={outcome.title}
            icon={<outcome.Icon />}
            title={outcome.title}
            copy={outcome.copy}
          />
        ))}
      </div>
    </section>
  );
}
