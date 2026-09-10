import type { ReactNode } from "react";

import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { ContentSection } from "@/src/components/ContentSection";
import { Section } from "@/src/components/Section";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Link } from "@/src/design-system/Link";

const FEDERATION_HREF = "/platform/graphql-federation";

interface ProseSectionProps {
  readonly title: string;
  readonly children: ReactNode;
}

/** A titled section whose body is a column of prose rather than cards. */
function ProseSection({ title, children }: ProseSectionProps) {
  return (
    <Section title={title}>
      <div className="text-cc-ink-dim mx-auto max-w-2xl space-y-4 text-base">
        {children}
      </div>
    </Section>
  );
}

interface InPracticeProps {
  readonly children: ReactNode;
}

function InPractice({ children }: InPracticeProps) {
  return <p className="text-cc-ink-dim text-sm">In practice: {children}.</p>;
}

export function WhatIsFusionSection() {
  return (
    <ContentSection
      title="What is Fusion?"
      text={
        <>
          <p>
            Fusion is an API gateway. Your teams keep their own services and
            their own schemas; Fusion composes those source schemas into one
            composite schema and serves it at a single endpoint. A client sends
            one query, the gateway works out which subgraphs hold the data,
            calls them, and returns one response.
          </p>
          <p className="mt-4">
            Composition happens in your build, not at runtime, so contract
            conflicts are caught before anything is deployed. A subgraph is an
            ordinary service: a GraphQL server in any language, or a service
            that publishes an OpenAPI document or a gRPC definition. If
            federation itself is new to you, start with{" "}
            <Link href={FEDERATION_HREF}>the GraphQL Federation page</Link>.
          </p>
        </>
      }
    />
  );
}

export function BothSpecificationsSection() {
  return (
    <ProseSection title="Both specifications, one gateway">
      <p>
        Fusion is the only gateway that supports both the GraphQL Federation
        specification and Apollo Federation. Source schemas written to either
        protocol compose into the same composite schema, so a team can adopt
        either one, run both side by side, or move a single subgraph across
        without a coordinated cutover.
      </p>
      <p>
        Fusion also composes sources that are not GraphQL servers at all: a
        service that publishes an OpenAPI document or a gRPC definition joins
        the same composite schema, with its contract validated in the same
        composition step.
      </p>
      <InPractice>
        <Link href="/docs/fusion/migration/coming-from-apollo-federation">
          migrating a subgraph from Apollo Federation
        </Link>
      </InPractice>
    </ProseSection>
  );
}

export function AnyServerSection() {
  return (
    <ProseSection title="Any GraphQL server, no plugin">
      <p>
        Subgraphs can be written in any language. A GraphQL subgraph stays an
        ordinary GraphQL server: it declares its keys and lookups in its own
        schema, the gateway calls it with ordinary GraphQL queries, and there is
        no distributed-runtime package or vendor protocol layer to install
        alongside it. The servers{" "}
        <Link href={`${FEDERATION_HREF}#specification`}>
          listed on the GraphQL Federation page
        </Link>{" "}
        qualify on the same terms, and so does any other GraphQL server.
      </p>
      <p>
        The one build step you add is composition. It validates the source
        schemas against one another, and type conflicts, missing fields and
        incompatible enums fail the pipeline instead of the gateway.
      </p>
      <p className="text-cc-ink-dim text-sm">
        In practice:{" "}
        <Link href="/docs/fusion/composition">
          how composition validates source schemas
        </Link>{" "}
        and{" "}
        <Link href="/docs/fusion/deployment-and-ci-cd">
          how to run it in your pipeline
        </Link>
        .
      </p>
    </ProseSection>
  );
}

export function ClientSafetySection() {
  return (
    <ProseSection title="Composition protects the graph, Nitro protects your clients">
      <p>
        Composition catches conflicts between subgraphs, and that is where
        federation&apos;s guarantees end. Nothing in it stops a team from
        removing a field that a mobile app still queries: the source schemas
        still compose, the build stays green, and the query fails in the hands
        of a client the subgraph team never sees.
      </p>
      <p>
        Nitro closes that gap. Its schema governance compares every schema
        change with the operations published by real clients and tells the team
        what is safe, risky or breaking before the change is merged.
      </p>
      <InPractice>
        <Link href="/products/nitro#schema">schema governance in Nitro</Link>
      </InPractice>
    </ProseSection>
  );
}

export function NitroCta() {
  return (
    <div id="nitro" className="scroll-mt-24">
      <Band className="py-16" skin="accent" layout="centered">
        <SectionHeading
          align="center"
          title="Know what a schema change does to real clients."
          description="Nitro validates every schema change against the operations your registered clients actually run, and its Fusion dashboard reports latency, throughput and error rate for the gateway and for each subgraph behind it."
        />
        <ButtonRow align="center" className="mt-9">
          <SolidButton href="https://nitro.chillicream.com">
            Start Nitro for Free
          </SolidButton>
          <OutlineButton href="/products/nitro">Meet Nitro</OutlineButton>
        </ButtonRow>
      </Band>
    </div>
  );
}
