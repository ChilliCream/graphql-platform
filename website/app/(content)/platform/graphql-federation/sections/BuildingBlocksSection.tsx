import { CardGrid } from "@/src/components/CardGrid";
import { Card } from "@/src/design-system/Card";
import { DistributedExecutionIcon } from "@/src/icons/DistributedExecutionIcon";
import { SchemaCompositionIcon } from "@/src/icons/SchemaCompositionIcon";
import { SubgraphsIcon } from "@/src/icons/SubgraphsIcon";

import { Section } from "./shared";

const BUILDING_BLOCKS = [
  {
    title: "Subgraphs",
    text: "Independent GraphQL services that contribute types and fields to the overall graph.",
    Icon: SubgraphsIcon,
  },
  {
    title: "Schema composition",
    text: "Combines source schemas, establishes relationships, and validates their compatibility.",
    Icon: SchemaCompositionIcon,
  },
  {
    title: "Distributed query execution",
    text: "A GraphQL gateway executes a client query across the necessary subgraphs and assembles one response.",
    Icon: DistributedExecutionIcon,
  },
];

export function BuildingBlocksSection() {
  return (
    <Section id="how-it-works">
      <h2 className="text-cc-ink mb-10 text-center text-3xl font-semibold tracking-tight sm:text-4xl">
        How does GraphQL Federation work?
      </h2>
      <CardGrid cols={3} step="progressive" gap={6}>
        {BUILDING_BLOCKS.map(({ title, text, Icon }) => (
          <Card key={title} variant="tile" className="@container">
            <div className="flex flex-col gap-4 @[16rem]:flex-row @[16rem]:items-start">
              <span className="flex-none">
                <Icon className="size-20" />
              </span>
              <div className="min-w-0">
                <h3 className="text-cc-ink text-lg font-semibold">{title}</h3>
                <p className="text-cc-ink-dim mt-2 text-sm">{text}</p>
              </div>
            </div>
          </Card>
        ))}
      </CardGrid>
    </Section>
  );
}
