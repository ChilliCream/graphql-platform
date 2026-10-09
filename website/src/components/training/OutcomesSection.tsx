import { CardGrid } from "@/src/components/CardGrid";
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
    copy: "Navigate a large GraphQL schema, recognize the common shapes, and explain why a type is modeled the way it is.",
    Icon: MapIcon,
  },
  {
    title: "Write resolvers without surprises",
    copy: "Move from simple fields to data loaders and pagination with patterns that scale instead of snippets that bite later.",
    Icon: WrenchIcon,
  },
  {
    title: "Plan a client they can live with",
    copy: "Use fragments, variables, and error handling to structure a Relay or Apollo client the next person on the team can maintain.",
    Icon: PlugIcon,
  },
  {
    title: "Diagnose the slow query",
    copy: "Open a trace, read the plan, find the N+1, and know which Hot Chocolate patterns to reach for before turning to hacks.",
    Icon: GraphIcon,
  },
  {
    title: "Have an opinion on federation",
    copy: "Know when to split a schema, when not to, and how Hot Chocolate Fusion fits with the platform you already run.",
    Icon: BranchIcon,
  },
  {
    title: "Speak the same language",
    copy: "Give backend, frontend, and platform engineers one shared vocabulary, so the next design review is faster and friendlier.",
    Icon: ChatIcon,
  },
];

/**
 * The outcomes grid: practical skills the curriculum can develop for a team.
 */
export function OutcomesSection() {
  return (
    <section className="py-16 sm:py-20">
      <div className="mb-10">
        <SectionHeading
          align="center"
          eyebrow="By the end of the training"
          title="What your team will actually know."
          description="No certificate-printer outcomes. These are the things your team walks away able to do, wherever they started."
        />
      </div>
      <CardGrid cols={3} step="progressive" gap={4}>
        {OUTCOMES.map((outcome) => (
          <IconFeatureCard
            key={outcome.title}
            icon={<outcome.Icon />}
            title={outcome.title}
            copy={outcome.copy}
          />
        ))}
      </CardGrid>
    </section>
  );
}
