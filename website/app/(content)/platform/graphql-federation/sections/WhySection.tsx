import { ExternalLink, Intro, Section, SPEC_URL } from "./shared";

const WORKING_GROUP_ANNOUNCEMENT_URL =
  "https://graphql.org/blog/2024-05-16-composite-schemas-announcement/";
const GRAPHQL_ORG_FEDERATION_URL = "https://graphql.org/learn/federation/";

export function WhySection() {
  return (
    <Section id="why">
      <Intro title="Why GraphQL Federation?" />

      <div id="specification" className="mt-10 scroll-mt-24">
        <Intro title="The GraphQL Federation specification: one idea, one open standard.">
          <p>
            Apollo Federation has solved this for years, and so have other
            gateways and many in-house systems, each in its own way, so a
            subgraph built for one could not move to another. In 2023 Apollo,
            ChilliCream, and The Guild formed a working group at the GraphQL
            Foundation to write one vendor-neutral specification for defining
            and composing GraphQL schemas across services; engineers from
            Graphile, Hasura, Netflix, WunderGraph, and others take part. The
            Composite Schemas Specification that came out of it is becoming the
            GraphQL Federation Specification.
          </p>
          <p>
            The specification keeps the GraphQL type system as it is and adds
            batching to the GraphQL over HTTP specification, the transport
            between gateway and subgraph, so that every GraphQL server can be a
            subgraph without changing its schema. Read{" "}
            <ExternalLink href={SPEC_URL}>the specification</ExternalLink>,{" "}
            <ExternalLink href={WORKING_GROUP_ANNOUNCEMENT_URL}>
              the working group&apos;s announcement
            </ExternalLink>
            , and{" "}
            <ExternalLink href={GRAPHQL_ORG_FEDERATION_URL}>
              the federation chapter on graphql.org
            </ExternalLink>
            .
          </p>
        </Intro>
      </div>

      <div className="border-cc-card-border mt-16 border-t pt-16 sm:mt-24 sm:pt-24">
        <div className="mx-auto max-w-2xl text-center">
          <div className="text-cc-ink space-y-4 text-base">
            <p>
              It holds for a server in any language. Spring for GraphQL in Java
              or Kotlin, NestJS or GraphQL Yoga in Node.js, gqlgen in Go,
              Strawberry in Python, async-graphql in Rust, graphql-ruby, Hot
              Chocolate in .NET: each publishes a schema and answers a query,
              and that is all the executor needs.
            </p>
          </div>
        </div>
      </div>
    </Section>
  );
}
