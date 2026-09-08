import { FEDERATION_DEFINITION } from "../terms";
import { Intro, InPractice, Section, SubHeading, Table } from "./shared";

const ALTERNATIVES: readonly (readonly string[])[] = [
  [
    "Single GraphQL server",
    "One server exposes one schema; one codebase, one deploy.",
    "One team and one API. Where almost everyone starts.",
    "Coordination happens in code review and scales only as far as one codebase.",
  ],
  [
    "Federation",
    "Each team publishes a source schema; composition merges them into one composite schema before deploy; the gateway's distributed executor plans each request across subgraphs. Conflicts fail the build.",
    "Several teams need to ship one coherent API on their own schedules.",
    "A gateway to run, a composition pipeline to own, one extra hop per request; entities need deliberate keys.",
  ],
  [
    "Schema stitching",
    "A gateway merges schemas at runtime with hand-written resolvers (the functions that produce each field's value) gluing types together.",
    "Quick aggregation of a few services you control.",
    "Glue resolvers drift silently as the underlying schemas change.",
  ],
  [
    "BFF (backend for frontend) per client",
    "Each frontend team builds its own backend that hand-aggregates the services it needs.",
    "One or two clients with very different needs.",
    "One backend per client to build, secure, and monitor.",
  ],
  [
    "Modular monolith",
    "One deployable exposes one schema; modules keep code ownership internal.",
    "One team, or several teams that ship together. Often the right start.",
    "One deploy train; coupling creeps back as teams multiply.",
  ],
];

const GLOSSARY: readonly (readonly string[])[] = [
  ["The service behind the gateway", "Subgraph", "Subgraph"],
  [
    "The schema document a subgraph publishes",
    "Subgraph schema",
    "Source schema",
  ],
  [
    "The build step that validates and merges the schemas",
    "Composition",
    "Composition",
  ],
  ["The single client-facing schema", "Supergraph", "Composite schema"],
  ["The public entry point that receives queries", "Router", "Gateway"],
  [
    "The part that plans a query and assembles one response",
    "Router (query planner and executor)",
    "Distributed executor",
  ],
  ["A type with a stable key, referenced across subgraphs", "Entity", "Entity"],
  ["The fields that identify an entity", "@key", "@key"],
  [
    "Fetching an entity by one of its keys",
    "_entities(representations:) with a reference resolver",
    "An ordinary query field marked @lookup",
  ],
  [
    "A field that needs data from another subgraph",
    "@requires (on the field)",
    "@require (on an argument)",
  ],
  [
    "Moving a field to another subgraph",
    "@override(from:)",
    "@override(from:)",
  ],
  [
    "What a server implements to join",
    "The Apollo subgraph specification: _entities, _service, reference resolvers",
    "Nothing beyond its schema; any GraphQL server",
  ],
  [
    "Fetching many entities at once",
    "A list of representations passed to _entities",
    "Variable batching, being added to GraphQL over HTTP",
  ],
];

export function WhatIsSection() {
  return (
    <Section id="what-is">
      <Intro title="What is GraphQL Federation?">
        <p>
          {FEDERATION_DEFINITION} A GraphQL API describes the data it offers in
          a schema, a typed document, and answers one query with exactly the
          fields the client asked for.
        </p>
      </Intro>

      <div
        id="alternatives"
        className="border-cc-card-border mt-16 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <Intro title="GraphQL Federation vs schema stitching, BFFs, and a single server.">
          <p>
            Federation adds a gateway to run, a composition pipeline to own, and
            one extra network hop on every request. That is a fair price when
            several teams must ship one coherent API on their own schedules.
          </p>
        </Intro>
        <div className="mt-10 max-w-2xl">
          <SubHeading id="when-not">
            When you do not need GraphQL Federation
          </SubHeading>
          <div className="text-cc-ink mt-4 space-y-4 text-base">
            <p>
              It is a poor price for one team, one service, or an early product
              whose domain boundaries are still moving. Those cases are better
              served by a single GraphQL server, and because every GraphQL
              server is already a valid subgraph, that server can join a
              composite schema later by declaring keys and lookups in its
              schema, without changing its clients.
            </p>
            <InPractice href="/docs/fusion/migration/migrating-from-schema-stitching">
              moving from schema stitching to federation
            </InPractice>
          </div>
        </div>
        <div className="mt-10">
          <Table
            caption="Federation compared with the alternatives"
            columns={[
              { header: "Approach", mono: true },
              { header: "How it works" },
              { header: "When it fits" },
              { header: "The cost" },
            ]}
            rows={ALTERNATIVES}
            minWidth="min-w-[840px]"
          />
        </div>
      </div>

      <div
        id="apollo-federation-vs-graphql-federation"
        className="border-cc-card-border mt-16 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <Intro title="Apollo Federation vs GraphQL Federation: two vocabularies, one architecture.">
          <p>
            Apollo Federation is widely deployed, so you will meet its words as
            often as the specification&apos;s. Both describe the same
            architecture; they differ in what a server has to implement to join.
          </p>
          <InPractice href="/docs/fusion/migration/coming-from-apollo-federation">
            composing Apollo Federation subgraphs alongside GraphQL Federation
            subgraphs
          </InPractice>
        </Intro>
        <div className="mt-10">
          <Table
            caption="Apollo Federation terms next to GraphQL Federation terms"
            columns={[
              { header: "Concept" },
              { header: "Apollo Federation", mono: true },
              { header: "GraphQL Federation", mono: true },
            ]}
            rows={GLOSSARY}
            minWidth="min-w-[720px]"
          />
        </div>
      </div>
    </Section>
  );
}
