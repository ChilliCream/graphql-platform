import { SUBGRAPH_SERVER_EXAMPLES } from "../terms";
import {
  ExternalLink,
  Intro,
  LINK_CLASS,
  Section,
  SPEC_URL,
  SubHeading,
} from "./shared";

const SUBGRAPH_SERVER_LIST = SUBGRAPH_SERVER_EXAMPLES.map(
  ({ server, language }) => `${server} in ${language}`,
).join(", ");

const WORKING_GROUP_ANNOUNCEMENT_URL =
  "https://graphql.org/blog/2024-05-16-composite-schemas-announcement/";
const GRAPHQL_ORG_FEDERATION_URL = "https://graphql.org/learn/federation/";

export function WhySection() {
  return (
    <Section id="why">
      <Intro title="Why GraphQL Federation?">
        <p>
          Federation answers an organizational problem before a technical one:
          several teams own different parts of the same product, and clients
          want to see one API. The four points below are the case for it, and
          the last one is the case against it.
        </p>
      </Intro>

      <div id="team-autonomy" className="mt-16 max-w-2xl scroll-mt-24 sm:mt-24">
        <SubHeading id="team-autonomy-heading">
          Each team owns and deploys its own subgraph
        </SubHeading>
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            A team owns one subgraph: its codebase, its database, its release
            schedule. It adds a field, renames one, or splits a type without
            asking the other teams for a slot on a shared deploy train.
          </p>
          <p>
            What the teams agree on is not the code but the source schema each
            subgraph publishes. Composition takes those source schemas and
            checks them together before anything ships: if one team removes a
            field another team&apos;s schema depends on, or two teams describe
            the same field in ways that cannot be composed, the build fails
            instead of the gateway. The contract between teams is a document,
            and it is checked automatically.
          </p>
        </div>
      </div>

      <div
        id="one-api"
        className="border-cc-card-border mt-16 max-w-2xl scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <SubHeading id="one-api-heading">
          Clients get one API instead of several
        </SubHeading>
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            A front end sends one query to the gateway and gets one response. It
            asks the composite schema for the fields its screen needs, and the
            distributed executor works out which subgraphs hold them. No client
            writes the code that calls three services and joins their answers,
            and no client learns which team owns which field.
          </p>
          <p>
            That indifference is what makes the backend free to move. When a
            team splits a service in two, or hands a field to the subgraph that
            is the better owner of it, composition produces the same composite
            schema and the query the client already sends keeps working. Service
            boundaries change behind the gateway without arriving in client
            code.
          </p>
        </div>
      </div>

      <div
        id="specification"
        className="border-cc-card-border mt-16 max-w-2xl scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <SubHeading id="specification-heading">
          One open standard, any language
        </SubHeading>
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            Apollo Federation has solved this for years, and so have other
            gateways and many in-house systems, each in its own way, so a
            subgraph built for one could not move to another. In 2023 Apollo,
            ChilliCream, and The Guild formed a working group at the GraphQL
            Foundation to write one vendor-neutral specification for defining
            and composing GraphQL schemas across services; engineers from
            Graphile, Hasura, Netflix, WunderGraph, and others take part. The
            Composite Schemas Specification that came out of that work is the
            GraphQL Federation specification, an open standard under the GraphQL
            Foundation.
          </p>
          <p>
            The specification keeps the GraphQL type system as it is and adds
            batching to the GraphQL over HTTP specification, the transport
            between gateway and subgraph, so that every GraphQL server can be a
            subgraph without changing its schema.
          </p>
          <p>
            That holds for a server in any language. {SUBGRAPH_SERVER_LIST}:
            each publishes a schema and answers a query, and that is all the
            distributed executor needs. Nothing in the architecture ties your
            teams to one language or one vendor.
          </p>
          <p>
            Read <ExternalLink href={SPEC_URL}>the specification</ExternalLink>,{" "}
            <ExternalLink href={WORKING_GROUP_ANNOUNCEMENT_URL}>
              the working group&apos;s announcement
            </ExternalLink>
            , and{" "}
            <ExternalLink href={GRAPHQL_ORG_FEDERATION_URL}>
              the federation chapter on graphql.org
            </ExternalLink>
            .
          </p>
        </div>
      </div>

      <div
        id="when-it-fits"
        className="border-cc-card-border mt-16 max-w-2xl scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
      >
        <SubHeading id="when-it-fits-heading">
          When it is worth it, and when it is not
        </SubHeading>
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            Everything above is bought with a gateway to run, a composition
            pipeline to own, one extra hop on each request, and entities whose
            keys have to be chosen deliberately. That is a fair price when
            several teams have to ship one coherent API on their own schedules,
            and the coordination they would otherwise do by hand is the thing
            slowing them down.
          </p>
          <p>
            It is a poor price for one team, one service, or a product whose
            domain boundaries are still moving. Start with a single GraphQL
            server. Because every GraphQL server is already a valid subgraph, it
            can join a composite schema later by declaring keys and lookups in
            its schema, without its clients noticing the change.
          </p>
          <p className="text-cc-ink-dim text-sm">
            Weighing federation against schema stitching, a backend for
            frontend, or a single server?{" "}
            <a className={LINK_CLASS} href="#what-is">
              The comparisons above
            </a>{" "}
            take each alternative in turn.
          </p>
        </div>
      </div>
    </Section>
  );
}
