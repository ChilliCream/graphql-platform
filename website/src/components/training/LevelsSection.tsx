import { CardGrid } from "@/src/components/CardGrid";
import { PerkCard } from "@/src/components/PerkCard";
import { SectionHeading } from "@/src/components/SectionHeading";

const LEVELS = [
  {
    tag: "Level 1",
    title: "Beginner team",
    hint: "Heard of GraphQL. Maybe shipped a toy server.",
    intro:
      "We start from REST instincts and rebuild them. By the end of week one your team can read a schema, write resolvers with confidence, and stop confusing fields with arguments.",
    covers: [
      "Schema-first thinking and the type system",
      "Queries, mutations, variables, and fragments",
      "Hot Chocolate basics on ASP.NET Core",
      "Wiring up a real Relay or Apollo client",
      "Pagination, errors, and the everyday traps",
    ],
    icon: "seedling",
    accent: "accent",
  },
  {
    tag: "Level 2",
    title: "Mixed team",
    hint: "Half the team has shipped. Half the team is bluffing.",
    intro:
      "The most common shape we see. We split sessions into shared foundations plus parallel tracks, so nobody is bored and nobody is lost. Everyone leaves on the same page.",
    covers: [
      "Shared foundations to align vocabulary",
      "Parallel tracks for newcomers and veterans",
      "Pair exercises that mix the two groups",
      "A real schema review on your codebase",
      "Working sessions on the bugs you brought with you",
    ],
    icon: "circle-half-stroke",
    accent: "violet",
  },
  {
    tag: "Level 3",
    title: "Advanced team",
    hint: "Schemas in production. Now the corners get sharp.",
    intro:
      "For teams already shipping GraphQL who want to go deeper. We focus on the parts that hurt at scale: schema design, performance, federation with Fusion, and operating Hot Chocolate in anger.",
    covers: [
      "Schema design at scale and review patterns",
      "Data loaders, batching, and query plans",
      "Federation with Hot Chocolate Fusion",
      "Observability and Nitro in production",
      "Versioning and breaking-change workflows",
    ],
    icon: "mountain",
    accent: "coral",
  },
] as const;

/**
 * The "where is your team today?" tri-column: the same curriculum framed for a
 * beginner, mixed, or advanced team, each as an accented perk card.
 */
export function LevelsSection() {
  return (
    <section id="levels" className="py-16 sm:py-20">
      <div className="mb-10">
        <SectionHeading
          align="center"
          eyebrow="Where is your team today?"
          title="Pick the row that sounds like your standup."
          description="The curriculum is the same set of building blocks. The order, the depth, and the exercises change for the room."
        />
      </div>
      <CardGrid cols={3} gap={4}>
        {LEVELS.map((level) => (
          <PerkCard
            key={level.title}
            tag={level.tag}
            title={level.title}
            subtitle={level.hint}
            intro={level.intro}
            listLabel="What we cover"
            items={level.covers}
            icon={level.icon}
            accent={level.accent}
          />
        ))}
      </CardGrid>
    </section>
  );
}
