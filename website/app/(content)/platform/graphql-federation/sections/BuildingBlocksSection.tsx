import { DistributedExecutionIcon } from "@/src/icons/DistributedExecutionIcon";
import { SchemaCompositionIcon } from "@/src/icons/SchemaCompositionIcon";
import { SubgraphsIcon } from "@/src/icons/SubgraphsIcon";

import { Intro, Section } from "./shared";

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
      <Intro title="How does GraphQL Federation work?" />
      <ul className="mt-10 grid gap-8 lg:grid-cols-3">
        {BUILDING_BLOCKS.map(({ title, text, Icon }) => (
          <li key={title} className="flex items-start gap-4">
            <Icon className="size-16 flex-none" />
            <div className="min-w-0">
              <h3 className="text-cc-ink text-lg font-semibold">{title}</h3>
              <p className="text-cc-ink-dim mt-2 text-sm">{text}</p>
            </div>
          </li>
        ))}
      </ul>
    </Section>
  );
}
