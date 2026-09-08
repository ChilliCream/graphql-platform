import { ComparisonPage } from "../comparison/ComparisonPage";
import { Code, InPractice } from "../sections/shared";

const ALTERNATIVE_LABEL = "a backend for frontend";

/** The copy of the "GraphQL Federation vs BFF" comparison page. */
export function VsBffComparison({ title }: { readonly title: string }) {
  return (
    <ComparisonPage
      slug="vs-bff"
      eyebrow="GraphQL Federation compared"
      title={title}
      intro={
        <>
          <p>
            Both approaches exist because clients rarely want data in the shape
            the services hold it. A backend for frontend solves that once per
            client, in a service that team writes; GraphQL Federation solves it
            once for every client, in a schema the teams that own the data
            publish.
          </p>
          <p>
            The question is not which is better in the abstract. It is how many
            clients need the same data assembled, and who you want doing the
            assembling.
          </p>
        </>
      }
      sections={[
        {
          id: "what-a-bff-is",
          heading: "What a backend for frontend is",
          body: (
            <>
              <p>
                A backend for frontend is a service one client team builds and
                owns. Its only job is to call the services that team needs, join
                what comes back, and shape it for that client&apos;s screens:
                one response per screen, in the shape the screen wants, over one
                round trip. Nothing else calls it, so it can be exactly as
                opinionated as its client is.
              </p>
              <p>
                That focus is the point, and it is why the pattern works. A
                backend for frontend is the right answer when one client&apos;s
                needs are unusual enough that no other client would want the
                same aggregation, and it is easiest to justify when a single
                team owns both the client and the backend. The two ship
                together, the contract between them never has to be negotiated
                with anyone, and the work the screens need that no schema should
                do &mdash; holding a session, calling a payment provider,
                driving a multi-step flow &mdash; lives in a place that belongs
                to that team alone.
              </p>
            </>
          ),
        },
        {
          id: "what-it-costs",
          heading: "What it costs once there are more clients",
          body: (
            <>
              <p>
                The bill arrives with the second and third client. Each one gets
                a backend of its own to build, secure, monitor, and keep on a
                supported runtime, and each of those backends re-implements the
                same joins over the same services. When a service changes a
                field, every backend for frontend that reads it changes too
                &mdash; and the client teams, not the team that owns the data,
                do that work.
              </p>
              <p>
                Because the aggregation logic is duplicated rather than shared,
                it also drifts. Two clients that ought to compute a total,
                resolve a permission, or page a list the same way end up doing
                it slightly differently, and the difference surfaces as a bug
                report against the data rather than against the client that got
                it wrong.
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
                GraphQL Federation moves the assembly behind one API instead of
                repeating it per client. Each team publishes a source schema for
                the service it owns; composition merges those source schemas
                into one composite schema before deploy, and fails the build
                when two teams describe the same field in incompatible ways; the
                gateway&apos;s distributed executor plans each incoming query
                across the subgraphs it needs, fetches from each of them, and
                assembles one response.
              </p>
              <p>
                Per-client shaping does not disappear. It moves into the query:
                a mobile screen and a web screen ask the same composite schema
                for different fields, so the thing that used to justify a
                backend per client is now a selection set rather than a service.
                The joins each backend for frontend used to hand-write are
                described once, in the schemas themselves &mdash; a type with a
                key (<Code>@key</Code>) declared in one subgraph is referenced
                from another, and the gateway resolves it through a lookup (
                <Code>@lookup</Code>).
              </p>
              <p>
                Nothing about this asks a client team to change stacks. Any
                GraphQL server, in any language, can be a subgraph, so the teams
                that own the data keep the servers they already run and add the
                keys and lookups that let their types be referenced from
                elsewhere.
              </p>
            </>
          ),
        },
        {
          id: "using-both",
          heading: "Using both",
          body: (
            <>
              <p>
                The two are not exclusive, and the hybrid is common. A backend
                for frontend can sit in front of the gateway: it keeps
                everything it does that a schema should not do &mdash; the
                session, the call out to a payment provider, the legacy endpoint
                &mdash; and gets its data from one composite schema instead of
                from six services. The joins leave the backend for frontend; the
                client-specific work stays.
              </p>
              <p>
                A backend for frontend can also become a subgraph. When it has
                grown data or behaviour that other clients want, it publishes a
                source schema like any other service and its types join the
                composite schema, which is usually the shortest way out of a
                client backend that quietly turned into a second product
                backend. Teams arrive here from the other direction too: build a
                backend for frontend for the first client, and add the gateway
                when the second and third client ask for the same data.
              </p>
              <InPractice href="/docs/fusion">
                running a gateway over your source schemas
              </InPractice>
            </>
          ),
        },
      ]}
      verdict={{
        federationWins: [
          "Several clients need the same data assembled, and every backend for frontend would re-implement the same joins over the same services.",
          "The teams that own the data should own how it is exposed, instead of client teams tracking every upstream change.",
          "Clients differ in what they need but not in where it comes from: one composite schema, and each client asks for its own fields.",
          "A new client should not mean one more backend to build, secure, monitor, and keep on a supported runtime.",
        ],
        alternativeWins: [
          "One or two clients have needs no other client shares, so the aggregation would never be reused anyway.",
          "One team owns the client and its backend, ships them together, and gains nothing from negotiating a shared contract.",
          "The screens need work that does not belong in a schema: session handling, third-party calls, a multi-step flow with its own state.",
          "A gateway to run and a composition pipeline to own is a price the team is not ready to pay yet.",
        ],
        alternativeLabel: ALTERNATIVE_LABEL,
      }}
    />
  );
}
