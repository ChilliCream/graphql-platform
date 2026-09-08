import Link from "next/link";
import type { ReactNode } from "react";

import { GatewayScene } from "../hero/GatewayScene";
import { FEDERATION_TERMS } from "../terms";
import { BuildCheckVisual } from "../visuals/BuildCheckVisual";
import { EvolutionVisual } from "../visuals/EvolutionVisual";
import { LookupVisual } from "../visuals/LookupVisual";
import { RequireVisual } from "../visuals/RequireVisual";
import { PageSection } from "@/src/components/PageSection";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import {
  Code,
  DEVELOPER_EYEBROW,
  InPractice,
  Intro,
  LINK_CLASS,
  SceneReveal,
  SubHeading,
  Table,
} from "./shared";

const TERM_ROWS: readonly (readonly string[])[] = FEDERATION_TERMS.map(
  ({ term, meaning }) => [term, meaning],
);

const BENEFITS: readonly { readonly title: string; readonly body: string }[] = [
  {
    title: "Team autonomy",
    body: "Each team changes and deploys its subgraph on its own schedule.",
  },
  {
    title: "One endpoint, one schema",
    body: "Clients query the composite schema and never see the subgraphs behind it.",
  },
  {
    title: "Safe evolution",
    body: "Composition checks every schema change against the whole composite schema before it deploys.",
  },
  {
    title: "Any language, any server",
    body: "A subgraph is any GraphQL server; the executor talks to it with plain GraphQL queries.",
  },
];

const DIRECTIVE_GROUPS: readonly (readonly string[])[] = [
  [
    "Identity and recall",
    "@key, @lookup, @is",
    "Declare what identifies an entity and how to fetch it again.",
  ],
  [
    "Data dependencies",
    "@require, @provides",
    "Declare what a field needs from elsewhere, and what a subgraph can supply along the way.",
  ],
  [
    "Ownership",
    "@shareable, @external, @override",
    "Decide which subgraph resolves a field when more than one could.",
  ],
  [
    "Visibility",
    "@inaccessible, @internal",
    "Keep parts of a source schema out of the composite schema.",
  ],
  [
    "Interfaces",
    "@interfaceObject, @implement",
    "Contribute fields to an interface without owning its implementing types.",
  ],
];

interface DeepDiveProps {
  readonly eyebrow: string;
  readonly headingId: string;
  readonly title: ReactNode;
  readonly children: ReactNode;
}

/**
 * A developer deep dive under the section's single H2: a mono kicker, an h3,
 * and the prose. Same rhythm as `Intro`, one heading level down.
 */
function DeepDive({ eyebrow, headingId, title, children }: DeepDiveProps) {
  return (
    <div className="max-w-2xl">
      <Eyebrow color="ink-dim">{eyebrow}</Eyebrow>
      <div className="mt-3">
        <SubHeading id={headingId}>{title}</SubHeading>
      </div>
      <div className="text-cc-ink mt-5 space-y-4 text-base">{children}</div>
    </div>
  );
}

export function HowItWorksSection() {
  return (
    <section
      id="how-it-works"
      className="border-cc-card-border scroll-mt-24 overflow-hidden border-t"
    >
      <PageSection maxWidth="6xl" className="pt-16 sm:pt-24">
        <Intro title="How does GraphQL Federation work?">
          <p>
            It takes three steps, and the rest of the model follows from them.
            First, each team keeps its own service, and every service is a
            subgraph that publishes a source schema: the document naming the
            types and fields that service owns.
          </p>
          <p>
            Second, composition runs at build time. It validates the source
            schemas against one another and combines them into a single
            composite schema. When two of them disagree, composition fails the
            build, so the conflict is caught before anything deploys.
          </p>
          <p>
            Third, the gateway serves that composite schema at one endpoint.
            Clients send their queries there, and the gateway&apos;s distributed
            executor plans each query, fetches from every subgraph the query
            touches, and assembles one response.
          </p>
          <p>
            The composite schema is also called the graph: its types link to
            each other, and a query walks those links. The vocabulary, as the
            GraphQL Federation specification (an open standard developed at the
            GraphQL Foundation) defines it:
          </p>
        </Intro>
        <div className="mt-10">
          <Table
            caption="GraphQL Federation terminology"
            columns={[{ header: "Term", mono: true }, { header: "Meaning" }]}
            rows={TERM_ROWS}
            minWidth="min-w-[560px]"
          />
        </div>
        <div id="benefits" className="mt-14 scroll-mt-24">
          <SubHeading id="benefits-heading">
            Benefits of GraphQL Federation
          </SubHeading>
        </div>
        <ul className="mt-6 grid gap-6 sm:grid-cols-2">
          {BENEFITS.map((benefit) => (
            <li
              key={benefit.title}
              className="border-cc-card-border rounded-xl border bg-[rgba(12,19,34,0.5)] p-5"
            >
              <h4 className="text-cc-heading font-heading text-base font-semibold">
                {benefit.title}
              </h4>
              <p className="text-cc-ink mt-2 text-sm">{benefit.body}</p>
            </li>
          ))}
        </ul>

        <div
          id="request"
          className="border-cc-card-border mt-16 scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
        >
          <DeepDive
            eyebrow="Step three, in detail"
            headingId="request-heading"
            title="How a GraphQL Federation gateway runs one request."
          >
            <p>
              The gateway is the public entry point. Behind it, a distributed
              executor turns each query into a plan from the composite schema:
              which subgraph answers each field, which calls can run at once,
              and which must wait for data another subgraph holds.
            </p>
            <p>
              Take the product page query: name, price, delivery. Billing and
              Catalog answer in parallel, and the executor asks Catalog for the
              product&apos;s weight as well, a field the client never requested,
              because Shipping&apos;s delivery estimate needs it. Shipping runs
              second with the weight passed in as an ordinary argument. The
              executor merges the three answers into exactly the shape the
              client asked for and sends one response. Subgraphs never call each
              other. Every call is an ordinary GraphQL query.
            </p>
          </DeepDive>
        </div>
      </PageSection>
      <div className="mt-2 w-full">
        <GatewayScene />
      </div>
      <PageSection maxWidth="6xl" className="pb-16 sm:pb-24">
        <div className="border-cc-card-border mt-16 border-t pt-10 sm:mt-24">
          <Eyebrow color="accent">{DEVELOPER_EYEBROW}</Eyebrow>
          <p className="text-cc-ink mt-3 max-w-2xl text-base">
            The same three steps in schema terms: how a type gets identity, how
            a field declares what it needs, what composition checks, and how the
            graph changes without the client noticing.
          </p>
        </div>

        <div id="entities-keys-lookups" className="mt-14 scroll-mt-24 sm:mt-16">
          <DeepDive
            eyebrow="@key and @lookup"
            headingId="entities-keys-lookups-heading"
            title="Entities: a key gives identity, a lookup gives recall."
          >
            <p>
              Catalog, Billing, and Shipping each define a Product. What makes
              them the same product is a key, declared with{" "}
              <Code>{'@key(fields: "id")'}</Code>, a directive: an annotation
              inside the schema. A type with a stable key that other subgraphs
              can refer to is called an entity, and the key gives it identity.
              What lets the executor fetch that product again inside another
              subgraph is a lookup: a plain query field such as{" "}
              <Code>productById(id: ID!): Product</Code>, marked{" "}
              <Code>@lookup</Code>. A lookup gives recall.
            </p>
            <p>
              Once the executor holds a product&apos;s id, here straight from
              the client&apos;s query, it calls Billing&apos;s lookup to get the
              price, with the same query Billing would answer for any client.
              Composition pairs the lookup&apos;s argument with the key field by
              name; when the names differ, <Code>@is</Code> maps them. An entity
              can have several lookups, in one subgraph or across subgraphs,
              each fetching it by one of its keys, and a key with no lookup
              still identifies the entity, for caching or comparison, without
              being able to fetch it.
            </p>
            <InPractice href="/docs/fusion/entities-and-lookups">
              declaring entities and lookups
            </InPractice>
          </DeepDive>
          <SceneReveal>
            <LookupVisual />
          </SceneReveal>
        </div>

        <div
          id="require"
          className="border-cc-card-border mt-16 scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
        >
          <DeepDive
            eyebrow="@require"
            headingId="require-heading"
            title="Requirements: a dependency is an ordinary argument."
          >
            <p>
              Shipping estimates delivery time by weight, but Catalog provides
              weight. Shipping declares the dependency on an argument of its own
              field:{" "}
              <Code>
                {'delivery(weight: Float! @require(field: "weight"))'}
              </Code>
              . Composition removes that argument from the composite schema, so
              clients see <Code>delivery</Code> with no argument at all. At
              runtime the executor fetches weight from Catalog first, then calls
              Shipping and passes it as a plain argument.
            </p>
            <p>
              <Code>@require</Code> can also reshape what it pulls in, mapping
              several fields from other subgraphs into one input object, so a
              subgraph asks for exactly the shape it wants.
            </p>
            <InPractice href="/docs/fusion/data-requirements-and-mapping">
              declaring data requirements
            </InPractice>
          </DeepDive>
          <SceneReveal>
            <RequireVisual />
          </SceneReveal>
        </div>

        <div
          id="composition"
          className="border-cc-card-border mt-16 scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
        >
          <DeepDive
            eyebrow="Composition"
            headingId="composition-heading"
            title="Composition fails the build, not the client."
          >
            <p>
              Composition runs in CI on every pull request, before anything
              deploys. When two source schemas disagree, the build stops with
              the field and the mismatch spelled out. In the example, Billing
              changes the type of id, the key every subgraph shares, and
              composition reports that <Code>Product.id</Code> is{" "}
              <Code>Int!</Code> in Billing and <Code>ID!</Code> in Catalog.
              Nothing ships. Once Billing restores the type, composition emits
              the composite schema as one artifact that the gateway loads. A
              breaking change is a failed check on a branch, seen by the team
              that made it, not an incident discovered by a client.
            </p>
            <InPractice href="/docs/fusion/composition">
              running composition in CI
            </InPractice>
          </DeepDive>
          <SceneReveal>
            <BuildCheckVisual />
          </SceneReveal>
        </div>

        <div
          id="evolution"
          className="border-cc-card-border mt-16 scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
        >
          <DeepDive
            eyebrow="@override"
            headingId="evolution-heading"
            title="The graph evolves. Clients never notice."
          >
            <p>
              The composite schema is the stable contract. Behind it, ownership
              moves. Suppose price began in Catalog, back when Catalog was the
              only service. When Billing takes it over, it declares price in its
              own source schema with <Code>{'@override(from: "catalog")'}</Code>
              . On the next build, composition routes price to Billing, and
              Catalog can delete its copy of the field on its own schedule. The
              client&apos;s query does not change and neither does its response.
              Teams split subgraphs, merge them, or rewrite one in another
              language, and none of that is a migration on the client side.
            </p>
            <InPractice href="/docs/fusion/schema-exposure-and-evolution">
              evolving a composite schema
            </InPractice>
          </DeepDive>
          <SceneReveal>
            <EvolutionVisual />
          </SceneReveal>
        </div>

        <div
          id="any-language"
          className="border-cc-card-border mt-16 scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
        >
          <DeepDive
            eyebrow="Any language"
            headingId="any-language-heading"
            title="Every GraphQL server, in any language, is already a subgraph."
          >
            <p>
              A key is a directive on a type. A lookup is a query field the
              subgraph would expose anyway, marked <Code>@lookup</Code>. A
              requirement is an argument. Every call the executor makes is an
              ordinary GraphQL query, so there is no subgraph specification to
              implement and the server stays as it is.
            </p>
            <p>
              Apollo Federation, the older design, instead asks a subgraph to
              implement a hidden <Code>_entities</Code> field and reference
              resolvers (
              <Link
                className={LINK_CLASS}
                href="/platform/graphql-federation/vs-apollo-federation"
              >
                the two designs side by side
              </Link>
              ).
            </p>
          </DeepDive>

          <div id="batching" className="mt-12 max-w-2xl scroll-mt-24">
            <SubHeading id="batching-heading">
              Does GraphQL Federation cause N+1 requests? Batching is a
              transport concern.
            </SubHeading>
            <div className="text-cc-ink mt-4 space-y-4 text-base">
              <p>
                The rule that the server stays as it is also covers batching: it
                is solved in the transport, not in the schema. Ask for a hundred
                products and the executor would send a hundred lookups into
                Shipping, each the same query with different arguments. So
                variable batching, an open proposal to the GraphQL over HTTP
                specification, sends one query with a list of variable sets,
                which a server can fold into a single execution. The source
                schema does not change to get it.
              </p>
            </div>
          </div>
        </div>

        <div
          id="directives"
          className="border-cc-card-border mt-16 scroll-mt-24 border-t pt-16 sm:mt-24 sm:pt-24"
        >
          <DeepDive
            eyebrow="Reference"
            headingId="directives-heading"
            title="GraphQL Federation directives, grouped by what they do."
          >
            <p>
              <Code>@key</Code>, <Code>@lookup</Code>, <Code>@require</Code>,
              and <Code>@override</Code> are four of the directives the
              specification defines. Identity and recall, and data dependencies,
              are the core of every federated graph; the ownership, visibility,
              and interface directives are refinements for a graph that has
              grown.
            </p>
          </DeepDive>
          <div className="mt-10">
            <Table
              caption="GraphQL Federation directives by group"
              columns={[
                { header: "Group" },
                { header: "Directives", mono: true },
                { header: "What they are for" },
              ]}
              rows={DIRECTIVE_GROUPS}
              minWidth="min-w-[640px]"
            />
          </div>
        </div>
      </PageSection>
    </section>
  );
}
