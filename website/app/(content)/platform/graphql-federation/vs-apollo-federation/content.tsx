import { ComparisonPage } from "../comparison/ComparisonPage";
import { Code, InPractice, Table } from "../sections/shared";

/**
 * Apollo Federation terms next to the terms the GraphQL Federation
 * specification uses for the same thing. Moved here from the explainer's
 * "What is GraphQL Federation?" section, which now links to this page.
 */
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
 * The copy of the "GraphQL Federation vs Apollo Federation" comparison page.
 */
export function VsApolloFederationComparison({
  title,
}: {
  readonly title: string;
}) {
  return (
    <ComparisonPage
      slug="vs-apollo-federation"
      eyebrow="GraphQL Federation compared"
      title={title}
      intro={
        <>
          <p>
            Apollo Federation is the model most teams meet first, and it is
            widely deployed, so its words are the words the industry reaches
            for: supergraph, router, subgraph. The GraphQL Federation
            specification describes the same architecture with a different
            vocabulary &mdash; and with a different answer to one question: what
            does a server have to implement in order to take part?
          </p>
          <p>
            This page puts them side by side: the architecture they share, the
            terms that map onto each other, the design difference underneath the
            names, who each specification belongs to, and how one gateway
            composes subgraphs written to either of them.
          </p>
        </>
      }
      sections={[
        {
          id: "shared-architecture",
          heading: "The architecture they share",
          body: (
            <>
              <p>
                Both start from the same premise. Each team runs its own service
                &mdash; a subgraph &mdash; and publishes a source schema
                describing the part of the API it owns. A build step,
                composition, validates those source schemas against each other
                and produces one composite schema. A gateway serves that
                composite schema, and its distributed executor plans every
                incoming query across the subgraphs the query touches, fetches
                from each and returns one response.
              </p>
              <p>
                Both answer the cross-service problem the same way as well. A
                type that more than one subgraph contributes to is an entity; it
                is identified by a key, and one subgraph fetches it on behalf of
                another when a query spans both. Draw the topology for either
                one and you get the same picture: clients, a gateway, several
                subgraphs behind it, and a composition step in CI. Team
                boundaries, ownership and the request path are unchanged between
                them.
              </p>
            </>
          ),
        },
        {
          id: "two-vocabularies",
          heading: "Two vocabularies, one architecture",
          body: (
            <>
              <p>
                Because Apollo Federation is so widely deployed, you will meet
                its words as often as the specification&apos;s. Where Apollo
                says supergraph, the specification says composite schema; where
                Apollo says router, it says gateway. Most of the distance
                between the two is a translation table.
              </p>
              <div className="mt-8">
                <Table
                  caption="Apollo Federation terms next to GraphQL Federation terms"
                  columns={[
                    { header: "Concept" },
                    { header: "Apollo Federation", mono: true },
                    { header: "GraphQL Federation", mono: true },
                  ]}
                  rows={GLOSSARY}
                  minWidth="min-w-[640px]"
                />
              </div>
              <p>
                Not every row is a rename, though. The last few describe the one
                place where the two models genuinely differ.
              </p>
            </>
          ),
        },
        {
          id: "what-a-server-implements",
          heading: "What a server has to implement to join",
          body: (
            <>
              <p>
                Apollo Federation asks a subgraph to implement its subgraph
                specification. The gateway resolves entities by calling a hidden{" "}
                <Code>_entities</Code> root field with typed representations, so
                each subgraph adds that field, a reference resolver per entity
                type, and a <Code>_service</Code> field that returns its SDL. In
                practice that means a federation library for the language the
                service is written in; a server without one cannot join.
              </p>
              <p>
                The GraphQL Federation specification asks for ordinary schema
                instead. An entity is fetched through a normal query field
                marked <Code>@lookup</Code>, and a field that needs data from
                another subgraph declares that on an argument marked{" "}
                <Code>@require</Code>. There is no hidden root field and no
                reference-resolver protocol, so any GraphQL server, in any
                language, can be a subgraph with nothing beyond the schema it
                already publishes.
              </p>
              <p>
                Two things follow from that. Teams keep the servers they already
                run, including the ones no federation library was ever written
                for. And entity resolution stops being an internal protocol: a
                lookup is a field like any other, so it can be queried, reviewed
                and tested from any GraphQL client instead of only through the
                gateway.
              </p>
            </>
          ),
        },
        {
          id: "governance",
          heading: "Who each specification belongs to",
          body: (
            <>
              <p>
                Apollo Federation is Apollo&apos;s specification. Apollo defines
                the directives and the subgraph protocol, and ships the router
                and the subgraph libraries that implement them. What the model
                does next, and on what terms it is used, follows Apollo&apos;s
                roadmap and licensing.
              </p>
              <p>
                The GraphQL Federation specification is developed in the open
                under the GraphQL Foundation, the same home as the GraphQL
                specification itself. It is written for implementers rather than
                for one product: the directives and the execution model are the
                contract, and anyone can implement either side of it. For an
                organization choosing where to put a decade of API surface, that
                is the difference between an architecture defined by a vendor
                and one defined by a standard that vendors compete inside.
              </p>
            </>
          ),
        },
        {
          id: "running-both",
          heading: "Running both in one graph",
          body: (
            <>
              <p>
                At the gateway the two are not an either/or. Fusion is the only
                gateway that supports both the GraphQL Federation specification
                and Apollo Federation: composition reads each source schema,
                detects which of the two it is written to, and composes Apollo
                Federation subgraphs and GraphQL Federation subgraphs into one
                composite schema. At runtime the distributed executor speaks
                Apollo&apos;s <Code>_entities</Code> protocol to the subgraphs
                that expect it and calls lookup fields on the ones that publish
                them, inside the same query plan.
              </p>
              <p>
                That turns the choice between the two into a schedule rather
                than a cutover. Existing Apollo Federation subgraphs keep their
                SDL, their reference resolvers and their deployment model and
                run behind the gateway unchanged; a team that wants the open
                specification converts one subgraph at a time, and the graph
                keeps answering queries throughout.
              </p>
              <InPractice href="/docs/fusion/migration/coming-from-apollo-federation">
                moving a subgraph from Apollo Federation to the GraphQL
                Federation protocol
              </InPractice>
            </>
          ),
        },
      ]}
      verdict={{
        federationWins: [
          "The graph should rest on an open standard, developed under the GraphQL Foundation, rather than on one vendor's model.",
          "Subgraphs run on servers or in languages with no Apollo Federation library, and adding one is not on anyone's plan.",
          "Teams want entity resolution they can call and test as an ordinary query field, instead of through a hidden protocol field.",
          "The graph is new, and nothing is invested in Apollo's router, its subgraph libraries or its registry yet.",
        ],
        alternativeWins: [
          "Subgraphs, CI and the schema registry are already built on Apollo Federation and are running well.",
          "The team depends on specific Apollo Router or GraphOS features that would have to be replaced first.",
          "The people on call know Apollo's tooling, and their time is better spent on the product right now.",
          "Nothing is forcing the question: subgraphs can move to the open specification one at a time later, behind a gateway that speaks both.",
        ],
      }}
    />
  );
}
