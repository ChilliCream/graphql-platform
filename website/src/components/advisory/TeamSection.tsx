import { CardGrid } from "@/src/components/CardGrid";
import { PerkCard } from "@/src/components/PerkCard";
import { SectionHeading } from "@/src/components/SectionHeading";

const CREDENTIALS = [
  {
    title: "Who you work with",
    body: "Senior engineers from the core team. The same people who maintain Hot Chocolate, design Fusion, and ship Nitro.",
    bullets: [
      "Direct line to the maintainers",
      "Pull-request authors on the core code",
      "Same team across consulting and contracting",
    ],
  },
  {
    title: "What we work on",
    body: "The full GraphQL stack we build: schema design, federation with Fusion, ASP.NET Core integration, performance and resolver tuning, MCP, and Nitro observability and CI.",
    bullets: [
      "Schema and federation design",
      "Fusion composition and rollout",
      "Nitro observability, CI, and persisted ops",
    ],
  },
  {
    title: "How we work",
    body: "In your repo, in your channels, in your timezone window. Written status every week. Honest answers when something is not the right fit, even when that means a smaller engagement.",
    bullets: [
      "Embedded in your codebase",
      "Written weekly status reports",
      "We will say no when no is the right answer",
    ],
  },
];

/**
 * The team behind the stack: three credential columns (who, what, how) over a
 * row of product badges for Hot Chocolate, Fusion, and Nitro.
 */
export function TeamSection() {
  return (
    <section aria-labelledby="team-heading" className="mt-20 sm:mt-28">
      <SectionHeading
        align="center"
        eyebrow="Who you work with"
        title="The team behind Hot Chocolate, Fusion, and Nitro."
        titleId="team-heading"
        description="ChilliCream advisory is not a generalist consultancy that learned GraphQL last quarter. The engineers on the call are the ones who write the framework you depend on."
      />

      <div className="mt-10">
        <CardGrid cols={3} gap={6}>
          {CREDENTIALS.map((column) => (
            <PerkCard
              key={column.title}
              title={column.title}
              intro={column.body}
              items={column.bullets}
            />
          ))}
        </CardGrid>
      </div>
    </section>
  );
}
