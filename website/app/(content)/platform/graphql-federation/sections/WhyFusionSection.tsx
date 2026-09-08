import Link from "next/link";

import { ButtonRow } from "@/src/components/ButtonRow";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { InPractice, Intro, LINK_CLASS, Section, SubHeading } from "./shared";

const CONTACT_HREF =
  "/services/support/contact?subject=Sales&context=GraphQL%20Federation";

export function WhyFusionSection() {
  return (
    <Section id="fusion">
      <Intro title="Why choose Fusion for GraphQL Federation?">
        <p>
          Federation is an architecture, not a product. What you still choose is
          the gateway that composes your source schemas and executes queries
          across your subgraphs, and what protects your clients when one of
          those schemas changes.
        </p>
      </Intro>

      <div
        id="both-specifications"
        className="mt-16 max-w-2xl scroll-mt-24 sm:mt-24"
      >
        <SubHeading id="both-specifications-heading">
          Both specifications, one gateway
        </SubHeading>
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            Fusion is the only gateway that supports both the GraphQL Federation
            specification and Apollo Federation. Source schemas written to
            either protocol compose into the same composite schema, so a team
            can adopt either one, run both side by side, or move a single
            subgraph across without a coordinated cutover.
          </p>
          <p>
            Fusion also composes sources that are not GraphQL servers at all: a
            service that publishes an OpenAPI document or a gRPC definition
            joins the same composite schema, with its contract validated in the
            same composition step.
          </p>
          <InPractice href="/docs/fusion/migration/coming-from-apollo-federation">
            migrating a subgraph from Apollo Federation
          </InPractice>
        </div>
      </div>

      <div
        id="any-server"
        className="border-cc-card-border mt-16 max-w-2xl scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <SubHeading id="any-server-heading">
          Any GraphQL server, no plugin
        </SubHeading>
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            Subgraphs can be written in any language. A GraphQL subgraph stays
            an ordinary GraphQL server: it declares its keys and lookups in its
            own schema, the gateway calls it with ordinary GraphQL queries, and
            there is no distributed-runtime package or vendor protocol layer to
            install alongside it. Spring for GraphQL, GraphQL Yoga, gqlgen,
            Strawberry, async-graphql, graphql-ruby and Hot Chocolate all
            qualify on the same terms.
          </p>
          <p>
            The one build step you add is composition. It validates the source
            schemas against one another, and type conflicts, missing fields and
            incompatible enums fail the pipeline instead of the gateway.
          </p>
          <p className="text-cc-ink-dim text-sm">
            In practice:{" "}
            <Link className={LINK_CLASS} href="/docs/fusion/composition">
              how composition validates source schemas
            </Link>{" "}
            and{" "}
            <Link
              className={LINK_CLASS}
              href="/docs/fusion/deployment-and-ci-cd"
            >
              how to run it in your pipeline
            </Link>
            .
          </p>
        </div>
      </div>

      <div
        id="client-safety"
        className="border-cc-card-border mt-16 max-w-2xl scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <SubHeading id="client-safety-heading">
          Composition protects the graph, Nitro protects your clients
        </SubHeading>
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            Composition catches conflicts between subgraphs, and that is where
            federation&apos;s guarantees end. Nothing in it stops a team from
            removing a field that a mobile app still queries: the source schemas
            still compose, the build stays green, and the query fails in the
            hands of a client the subgraph team never sees.
          </p>
          <p>
            Nitro closes that gap. Its schema governance compares every schema
            change with the operations published by real clients and tells the
            team what is safe, risky or breaking before the change is merged.
          </p>
          <InPractice href="/products/nitro#schema">
            schema governance in Nitro
          </InPractice>
        </div>
      </div>

      <ButtonRow className="mt-12" align="start">
        <SolidButton href="/docs/fusion/getting-started">
          Start with Fusion
        </SolidButton>
        <OutlineButton href={CONTACT_HREF}>Contact an Expert</OutlineButton>
      </ButtonRow>
    </Section>
  );
}
