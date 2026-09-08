import { ComparisonPage } from "../comparison/ComparisonPage";
import { Code, InPractice } from "../sections/shared";

const ALTERNATIVE_LABEL = "one GraphQL server";

/**
 * The copy of the "GraphQL Federation vs a GraphQL monolith" comparison page.
 */
export function VsGraphQLMonolithComparison({
  title,
}: {
  readonly title: string;
}) {
  return (
    <ComparisonPage
      slug="vs-graphql-monolith"
      eyebrow="GraphQL Federation compared"
      title={title}
      intro={
        <>
          <p>
            One GraphQL server, one schema, one deploy: that is where almost
            every GraphQL API starts, and for one team it stays the cheapest
            thing that works. Nothing has to be composed, nothing has to be
            planned across services, and the whole API can be read in one
            codebase.
          </p>
          <p>
            The question is not whether a monolith is good architecture. It is
            what happens to it once several teams have to ship changes to the
            same schema, and whether the single deploy train is still carrying
            them or holding them up.
          </p>
        </>
      }
      sections={[
        {
          id: "where-one-server-wins",
          heading: "Where one GraphQL server is the right start",
          body: (
            <>
              <p>
                With one server, the schema is whatever the codebase says it is.
                A field is added, a type is renamed and a resolver is rewritten
                in one change, reviewed once and deployed once. There is no
                gateway in the request path, no composition step to keep green,
                and no network hop between one part of the graph and another
                &mdash; a join across two areas of the schema is a call inside
                the same process.
              </p>
              <p>
                Coordination is cheap for the same reason: it happens in code
                review. Two people changing neighbouring parts of the schema see
                each other&apos;s work in the same pull request, and the build
                that runs is the build that ships. For one team, or for a few
                teams that are happy to release together, that is not a
                compromise. It is the smallest system that answers the problem.
              </p>
            </>
          ),
        },
        {
          id: "modular-monolith",
          heading: "The modular monolith as the middle step",
          body: (
            <>
              <p>
                The usual next move is not to split the deployment but to split
                the code. One deployable still exposes one schema, while modules
                draw internal boundaries: each team owns its module, its types
                and its data access, and the module boundary is enforced by the
                build rather than by a network. Ownership becomes explicit long
                before anything is distributed.
              </p>
              <p>
                It buys a great deal and it is often the right place to stop. It
                does not buy independent delivery, though. Every module still
                rides the same deploy train, so the release cadence of the whole
                API is the cadence of its slowest, riskiest change, and coupling
                creeps back at the seams as the number of teams grows.
              </p>
            </>
          ),
        },
        {
          id: "deploy-train-bottleneck",
          heading: "The signals that the deploy train is the bottleneck",
          body: (
            <>
              <p>
                The change is rarely announced. It shows up as release
                coordination: a date that four teams have to agree on, a freeze
                while someone else&apos;s migration lands, a finished feature
                waiting on an unrelated change to be reverted before it can go
                out. The work is done; the train is not leaving.
              </p>
              <p>
                It shows up in CI as a shared queue. One pipeline builds and
                tests the whole API, so every team&apos;s change waits behind
                every other team&apos;s change, and a flaky test in a corner
                nobody touched blocks the release for all of them. The feedback
                loop lengthens for everyone at the pace of the largest
                contributor.
              </p>
              <p>
                And it shows up as schema conflicts found in code review. Two
                teams name the same concept differently, or both add a field to
                the same type with different semantics, and the only thing
                standing between that and production is a reviewer who happens
                to know both areas. The schema has become a shared resource with
                no owner per part, and reviews are doing the work that a
                contract should be doing.
              </p>
            </>
          ),
        },
        {
          id: "how-federation-differs",
          heading: "What GraphQL Federation changes",
          body: (
            <>
              <p>
                Federation splits the delivery, not the API that clients see.
                Each team owns a subgraph and publishes the source schema for
                it, deploying on its own schedule. Composition validates those
                source schemas against each other and produces one composite
                schema; the gateway&apos;s distributed executor plans each
                incoming query across the subgraphs it needs and returns one
                response. The client still sends one query to one endpoint.
              </p>
              <p>
                The conflicts that used to depend on a reviewer become a build
                failure. If two subgraphs disagree about a type, composition
                fails before anything is deployed, so the contract is checked
                mechanically and on every change rather than by whoever
                remembers both sides. Where the schema spans teams, an entity
                carries a key (<Code>@key</Code>) in one subgraph and is fetched
                from another through a lookup (<Code>@lookup</Code>), which
                makes the seam explicit and reviewable.
              </p>
              <p>
                None of that is tied to a language or a framework: any GraphQL
                server, in any language, can be a subgraph, so teams keep the
                stacks they already run.
              </p>
              <InPractice href="/docs/fusion/composition">
                how composition validates and merges source schemas
              </InPractice>
            </>
          ),
        },
        {
          id: "what-it-costs",
          heading: "What federation costs",
          body: (
            <>
              <p>
                There is a gateway in the request path to deploy, scale and
                observe, and a composition pipeline to own: a build step that
                has to pass before a subgraph ships, plus somewhere to publish
                and store the composed schema. Both are real operational
                surface, and both have to be on call.
              </p>
              <p>
                Every request that crosses a subgraph boundary also crosses the
                network. A join that was a method call inside one process
                becomes one extra hop, which is fast on an internal link but is
                no longer free, and it is worth measuring rather than assuming.
              </p>
              <p>
                Entities need deliberate keys. Deciding which types are shared,
                which fields identify them, and which subgraph owns which part
                of a shared type is design work that a single codebase never
                asked for. Done casually it produces chatty query plans and
                awkward ownership; done deliberately it is what makes
                independent delivery hold together.
              </p>
            </>
          ),
        },
        {
          id: "migration-path",
          heading: "The migration path out of the monolith",
          body: (
            <>
              <p>
                The step is smaller than it sounds, because every GraphQL server
                is already a valid subgraph. The existing monolith becomes the
                first subgraph as it stands: it keeps its schema, its resolvers
                and its clients, and declares keys (<Code>@key</Code>) and
                lookups (<Code>@lookup</Code>) for the types other teams will
                need to reference. Its clients are pointed at the gateway, which
                serves a composite schema that starts out as that one source
                schema.
              </p>
              <p>
                From there the monolith stops being the only place new work can
                land. A new area ships as its own subgraph, and an existing type
                can be extended from a second subgraph without touching the
                first. Splitting the monolith further becomes a series of small
                moves, each one composed and validated before it is deployed,
                rather than one rewrite that has to succeed all at once.
              </p>
              <InPractice href="/docs/fusion/adding-a-subgraph">
                adding a subgraph to a composite schema
              </InPractice>
            </>
          ),
        },
      ]}
      verdict={{
        federationWins: [
          "Several teams change the same schema and want to release their part without agreeing on a date.",
          "The shared CI queue and release coordination, not the work itself, decide how fast a change reaches production.",
          "Cross-team schema conflicts are being caught in code review, and only when the right reviewer is in the room.",
          "Parts of the graph want different scaling, runtimes or languages behind one API.",
        ],
        alternativeWins: [
          "One team owns the API, or several teams are still happy to ship on one train.",
          "Modules inside one deployable already give the code ownership the teams were missing.",
          "The joins are cheap in-process and the team is not ready to pay for a gateway and a composition pipeline.",
          "The schema is small enough that a reviewer can hold all of it, and conflicts are rare.",
        ],
        alternativeLabel: ALTERNATIVE_LABEL,
      }}
    />
  );
}
