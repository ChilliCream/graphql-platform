import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { SolidButton } from "@/src/design-system/Button";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";

import { Section } from "./shared";

export function FusionCta() {
  return (
    <Section id="fusion">
      <Band skin="accent" layout="centered">
        <SectionHeading
          align="center"
          title="Get started with GraphQL Federation"
        />
        <div className="text-cc-ink mx-auto mt-4 max-w-3xl space-y-4 text-base text-pretty">
          <p>
            Getting started with GraphQL Federation doesn&apos;t require
            rebuilding your existing APIs. With Fusion, you can bring together
            GraphQL, REST, and gRPC services into one unified graph.
          </p>
          <p>
            Fusion supports both Apollo Federation and the GraphQL Federation
            specification, allowing you to use existing subgraphs without
            modification and adopt the open standard at your own pace.
          </p>
          <p>
            Start with a single subgraph, connect additional services as your
            architecture grows, and let Fusion handle schema composition and
            distributed query execution.
          </p>
        </div>
        <ButtonRow align="center" className="mt-9">
          <SolidButton href="/docs/fusion/getting-started">
            <span className="inline-flex items-center gap-1.5 whitespace-nowrap">
              Get started with Fusion
              <ArrowRightIcon className="size-3.5" />
            </span>
          </SolidButton>
        </ButtonRow>
      </Band>
    </Section>
  );
}
