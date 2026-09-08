import { CardGrid } from "@/src/components/CardGrid";
import { Card } from "@/src/design-system/Card";

import { COMPARISONS } from "../comparisons";
import { FEDERATION_DEFINITION } from "../terms";
import { Intro, InPractice, Section, SubHeading, Table } from "./shared";

const ALTERNATIVES: readonly (readonly string[])[] = [
  [
    "Individual APIs per service",
    "Every service exposes its own API; each client calls the services it needs and assembles the pieces itself.",
    "A client that needs one or two services and little assembly.",
    "Every client repeats the same assembly, and every client changes when a service does.",
  ],
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

/**
 * A comparison entry as a linked tile. `LinkCard` is the shared version of this
 * card, but its title is an `h2`; this page keeps one `h2` per section, so the
 * title has to sit below the `h3` of the surrounding sub-heading.
 */
function ComparisonCard({
  href,
  title,
  summary,
}: {
  readonly href: string;
  readonly title: string;
  readonly summary: string;
}) {
  return (
    <Card
      as="a"
      href={href}
      variant="tile"
      hoverBorder
      className="flex h-full flex-col no-underline"
    >
      <h4 className="font-heading text-cc-heading text-lg font-semibold text-balance">
        {title}
      </h4>
      <p className="text-cc-ink-dim mt-3 text-sm">{summary}</p>
    </Card>
  );
}

export function WhatIsSection() {
  return (
    <Section id="what-is">
      <Intro title="What is GraphQL Federation?">
        <p>
          {FEDERATION_DEFINITION} A GraphQL API describes the data it offers in
          a schema, a typed document, and answers one query with exactly the
          fields the client asked for.
        </p>
        <p>
          In a large organization, no single team owns all of that data.
          Checkout, catalog, and accounts are separate services with separate
          owners. Federation lets each team publish only the piece of the API it
          owns, as a source schema. A build step called composition then
          validates those schemas together and produces one composite schema.
          Clients see one API and never learn which team owns which field.
        </p>
        <p>
          The services themselves stay where they are. Each team keeps its own
          codebase, its own database, and its own release schedule; a service
          joins by describing what it owns in its schema. A client sends one
          query to the gateway, and the gateway fetches from every subgraph the
          query touches and returns one response.
        </p>
      </Intro>

      <div className="mt-16 sm:mt-24">
        <SubHeading id="comparisons">
          How GraphQL Federation compares
        </SubHeading>
        <p className="text-cc-ink mt-4 max-w-2xl text-base">
          Federation is one answer among several to the same question: how does
          a client get data that lives in more than one service? Each comparison
          below says when the alternative is the better buy.
        </p>
        <div className="mt-8">
          <CardGrid cols={2}>
            {COMPARISONS.map((comparison) => (
              <ComparisonCard
                key={comparison.slug}
                href={comparison.href}
                title={comparison.title}
                summary={comparison.summary}
              />
            ))}
          </CardGrid>
        </div>
      </div>

      <div
        id="alternatives"
        className="border-cc-card-border mt-16 scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <SubHeading id="alternatives-heading">
          GraphQL Federation vs schema stitching, BFFs, and a single server
        </SubHeading>
        <div className="text-cc-ink mt-4 max-w-2xl space-y-4 text-base">
          <p>
            Federation adds a gateway to run, a composition pipeline to own, and
            one extra network hop on every request. That is a fair price when
            several teams must ship one coherent API on their own schedules.
          </p>
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
      </div>

      <div
        id="vs-bff"
        className="border-cc-card-border mt-16 max-w-2xl scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <SubHeading id="vs-bff-heading">
          GraphQL Federation vs BFF (backend for frontend)
        </SubHeading>
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            A backend for frontend is a service one client team builds and owns,
            whose only job is to call the services that team needs and shape the
            result for its screens. It is the right answer when one
            client&apos;s needs are unusual enough that no other client would
            want the same aggregation. When several clients want the same data,
            each backend for frontend re-implements the same joins, and every
            change to a service lands on every one of those teams. Federation
            turns that around: the teams that own the data publish their source
            schemas, composition produces one composite schema, and every client
            asks it for exactly the fields that client needs.
          </p>
        </div>
      </div>

      <div
        id="vs-individual-apis"
        className="border-cc-card-border mt-16 max-w-2xl scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <SubHeading id="vs-individual-apis-heading">
          GraphQL Federation vs individual APIs
        </SubHeading>
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            With no aggregation layer at all, each client calls every service it
            needs and joins the results itself: one request for the order,
            another for the products in it, another for the customer. That is
            fine while a screen touches one or two services. It stops being fine
            when a screen touches five, because every client, web and mobile and
            partner integration alike, writes the same joining code, pays a
            round trip per service, and has to change whenever any of those
            services does. Federation moves that work behind the gateway: the
            client sends one query and gets one response.
          </p>
        </div>
      </div>

      <div
        id="vs-graphql-monolith"
        className="border-cc-card-border mt-16 max-w-2xl scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <SubHeading id="vs-graphql-monolith-heading">
          GraphQL Federation vs a GraphQL monolith
        </SubHeading>
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            A GraphQL monolith is one server exposing one schema: one codebase,
            one deploy, one place to look. A modular monolith stretches that
            model by keeping ownership internal to modules. Both are excellent
            while every team can ship on the same release train, and starting
            there is rarely a mistake. Once those teams need separate schedules,
            the shared codebase becomes the queue: every change waits for the
            same review and the same deploy. Federation gives each team a
            subgraph it deploys on its own, and moves the coordination into
            composition, which fails the build when two teams describe the same
            field in incompatible ways.
          </p>
        </div>
      </div>

      <div
        id="apollo-federation-vs-graphql-federation"
        className="border-cc-card-border mt-16 scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <div id="vs-apollo-federation" className="max-w-2xl scroll-mt-24">
          <SubHeading id="vs-apollo-federation-heading">
            GraphQL Federation vs Apollo Federation: two vocabularies, one
            architecture
          </SubHeading>
          <div className="text-cc-ink mt-4 space-y-4 text-base">
            <p>
              Apollo Federation is widely deployed, so you will meet its words
              as often as the specification&apos;s. Both describe the same
              architecture; they differ in what a server has to implement to
              join. Apollo Federation asks a subgraph to implement its subgraph
              specification. GraphQL Federation asks for nothing beyond the
              schema a server already publishes.
            </p>
            <InPractice href="/docs/fusion/migration/coming-from-apollo-federation">
              composing Apollo Federation subgraphs alongside GraphQL Federation
              subgraphs
            </InPractice>
          </div>
        </div>
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
