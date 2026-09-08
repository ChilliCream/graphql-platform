import { ButtonRow } from "@/src/components/ButtonRow";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { Intro, Section } from "./shared";

export function WhyFusionSection() {
  return (
    <Section id="fusion">
      <Intro title="Why choose Fusion for GraphQL Federation?">
        <p>
          Fusion is ChilliCream&apos;s GraphQL Federation gateway and, today,
          the only gateway that implements the GraphQL Federation specification.
          It also composes Apollo Federation subgraphs, so source schemas
          written to either specification compose into the same composite
          schema, and a team can adopt either, mix the two, or move between them
          one subgraph at a time.
        </p>
        <p>
          Subgraphs can be written in any language. The gateway talks to them
          with ordinary GraphQL queries, there is no plugin to install, and
          services that only speak REST (OpenAPI) or gRPC can join as well. What
          you take on is a gateway to run and a composition step in CI that
          fails the build on conflicts.
        </p>
      </Intro>
      <ButtonRow className="mt-9" align="start">
        <SolidButton href="/docs/fusion/getting-started">
          Start with Fusion
        </SolidButton>
        <OutlineButton href="/docs/fusion">Fusion documentation</OutlineButton>
      </ButtonRow>
    </Section>
  );
}
