import { ComparisonPage } from "../comparison/ComparisonPage";
import { Code, InPractice } from "../sections/shared";

const ALTERNATIVE_LABEL = "individual APIs";

/**
 * The copy of the "GraphQL Federation vs individual APIs" comparison page.
 */
export function VsIndividualApisComparison({
  title,
}: {
  readonly title: string;
}) {
  return (
    <ComparisonPage
      slug="vs-individual-apis"
      eyebrow="GraphQL Federation compared"
      title={title}
      intro={
        <>
          <p>
            Calling each service directly is where almost every system starts,
            and for a long while there is nothing wrong with it. The client
            knows which services it needs, talks to each of them, and puts the
            answers together itself.
          </p>
          <p>
            The question is not whether that works. It is what happens to the
            clients once a single screen needs four services instead of one, and
            who ends up paying for the assembly.
          </p>
        </>
      }
      sections={[
        {
          id: "calling-services-directly",
          heading: "What calling services directly looks like",
          body: (
            <>
              <p>
                With no aggregation layer, every client is an integration
                client. It holds a base URL per service, authenticates against
                each of them, and carries the retry, timeout, paging and error
                handling each one expects. Whether those services speak REST or
                GraphQL changes the shape of the calls, not the amount of work:
                the client is still the only place that knows how the pieces fit
                together.
              </p>
              <p>
                It also owns the joins. An order id comes back from one service,
                the customer behind it from a second, the shipping status from a
                third, and the client stitches the three into the object its
                screen actually wanted. Nothing describes that relationship
                except the code that walks it, so each client walks it again.
              </p>
            </>
          ),
        },
        {
          id: "where-it-is-fine",
          heading: "Where it stays the right call",
          body: (
            <>
              <p>
                A handful of services, one serious client, contracts that barely
                move: at that size the direct calls are the simplest thing that
                can work, and adding an aggregation layer would buy nothing that
                is not already cheap. A screen that reads one service does not
                need a gateway to read one service.
              </p>
              <p>
                Direct calls also stay right where the traffic is not
                screen-shaped at all &mdash; a batch job pulling a full export,
                a service-to-service call on a hot path, a webhook receiver.
                Those callers want one endpoint, one payload and no negotiation,
                and federation is not trying to take them away.
              </p>
            </>
          ),
        },
        {
          id: "what-it-costs",
          heading: "What it costs as the services multiply",
          body: (
            <>
              <p>
                The arithmetic turns on you quietly. Each new service is another
                integration in every client that touches it, and every client
                that assembles the same view assembles it independently. Two
                clients that ought to resolve a permission, compute a total or
                page a list identically end up slightly out of step, and the
                difference reads like a bug in the data rather than in the
                client that got it wrong.
              </p>
              <p>
                Screens get chatty in the same motion. The client cannot ask for
                a shape it does not have an endpoint for, so it fetches a list,
                then loops over it to fetch the details, then fetches what those
                details reference &mdash; a sequence of dependent round trips
                that is fine on a laptop and painful on a phone. Over-fetching
                arrives with it: whole payloads are pulled down for the two
                fields the screen shows.
              </p>
              <p>
                And every service change becomes a client change. A renamed
                field, a split resource, a new required parameter: the team that
                owns the data cannot finish that change, because the code that
                depends on it lives in applications they do not own and cannot
                deploy. Versioning turns into the tool for that, and now each
                service carries its own version timeline that every client has
                to track separately.
              </p>
            </>
          ),
        },
        {
          id: "how-federation-differs",
          heading: "How GraphQL Federation changes the picture",
          body: (
            <>
              <p>
                Federation puts one API in front of the services instead of one
                integration in front of each. Every team publishes a source
                schema for the service it owns; composition validates those
                source schemas against each other and merges them into one
                composite schema before deploy; the gateway&apos;s distributed
                executor plans each incoming query across the subgraphs it
                needs, fetches from each of them, and returns one response. The
                client makes one call and asks for exactly the fields its screen
                renders.
              </p>
              <p>
                The joins move with it. A type with a key (<Code>@key</Code>)
                declared in one subgraph is referenced from another, and the
                gateway resolves it through a lookup (<Code>@lookup</Code>), so
                the traversal the client used to hand-write is described once,
                in the schemas themselves, and executed server-side across fast
                internal links rather than over a mobile connection.
              </p>
              <p>
                Ownership does not move. Each team keeps its service, its
                database and its deployment schedule, and any GraphQL server, in
                any language, can be a subgraph. Fusion also composes
                OpenAPI-based REST services and gRPC services into the same
                composite schema, so a service does not have to be rewritten to
                take part.
              </p>
              <InPractice href="/docs/fusion">
                composing your services into one composite schema
              </InPractice>
            </>
          ),
        },
        {
          id: "what-it-does-not-do",
          heading: "What it does not do",
          body: (
            <>
              <p>
                A composite schema is not a fix for service boundaries drawn in
                the wrong place. If two services both half-own the same concept,
                composition surfaces that as a conflict rather than hiding it,
                and the answer is to settle who owns the type &mdash; the
                schemas will keep asking until someone does. Federation makes
                good boundaries visible and legible to clients; it does not
                supply them.
              </p>
              <p>
                It is also one more thing to run. There is a gateway in the
                request path to deploy, scale and observe, and a composition
                step in CI that has to pass before a subgraph ships. That is a
                real cost, paid once for all clients rather than repeatedly in
                each of them, but a team with three services and one client has
                not yet reached the point where it pays for itself.
              </p>
            </>
          ),
        },
      ]}
      verdict={{
        federationWins: [
          "A single screen needs data from several services, and every client would otherwise assemble the same pieces itself.",
          "Dependent round trips and over-fetching are what makes screens slow, especially on mobile connections.",
          "The teams that own the data want to evolve it without a coordinated change in every client that reads it.",
          "The same aggregation logic is drifting apart across clients, and the differences surface as data bugs.",
        ],
        alternativeWins: [
          "A screen reads one or two services, and the contracts between them barely move.",
          "There is one serious client, so nothing about the assembly would be reused if it were shared.",
          "The callers are not screens at all: batch exports, service-to-service calls, webhook receivers.",
          "A gateway to run and a composition step to keep green is a price the team is not ready to pay yet.",
        ],
        alternativeLabel: ALTERNATIVE_LABEL,
      }}
    />
  );
}
