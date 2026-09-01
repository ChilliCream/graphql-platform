import Link from "next/link";
import type { ReactNode } from "react";

import { ButtonRow } from "@/src/components/ButtonRow";
import { FaqSection } from "@/src/components/FaqSection";
import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { FEDERATION_FAQ_ITEMS } from "./faq";
import { GatewayScene } from "./hero/GatewayScene";
import { FEDERATION_DEFINITION, FEDERATION_TERMS } from "./terms";
import { TransitStory } from "./TransitStory";
import { BuildCheckVisual } from "./visuals/BuildCheckVisual";
import { EvolutionVisual } from "./visuals/EvolutionVisual";
import { LookupVisual } from "./visuals/LookupVisual";
import { RequireVisual } from "./visuals/RequireVisual";

const SPEC_URL = "https://graphql.github.io/composite-schemas-spec/draft/";
const WORKING_GROUP_ANNOUNCEMENT_URL =
  "https://graphql.org/blog/2024-05-16-composite-schemas-announcement/";
const GRAPHQL_ORG_FEDERATION_URL = "https://graphql.org/learn/federation/";

const GRADIENT = "linear-gradient(90deg, #5eead4, #16b9e4)";

const DEVELOPER_EYEBROW = "For GraphQL developers";

interface SectionProps {
  readonly id: string;
  readonly children: ReactNode;
}

function Section({ id, children }: SectionProps) {
  return (
    <section id={id} className="border-cc-card-border scroll-mt-24 border-t">
      <PageSection maxWidth="6xl" className="py-16 sm:py-24">
        {children}
      </PageSection>
    </section>
  );
}

interface IntroProps {
  readonly eyebrow?: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

function Intro({ eyebrow, title, children }: IntroProps) {
  return (
    <div className="max-w-2xl">
      <SectionHeading eyebrow={eyebrow} title={title} />
      {children && (
        <div className="text-cc-ink mt-5 space-y-4 text-base">{children}</div>
      )}
    </div>
  );
}

function SubHeading({
  id,
  children,
}: {
  readonly id: string;
  readonly children: ReactNode;
}) {
  return (
    <h3
      id={id}
      className="font-heading text-cc-heading text-h5 scroll-mt-24 font-semibold text-balance"
    >
      {children}
    </h3>
  );
}

function SceneReveal({ children }: { readonly children: ReactNode }) {
  return (
    <RevealOnScroll className="mt-12" hiddenClassName="translate-y-8 opacity-0">
      {children}
    </RevealOnScroll>
  );
}

function Code({ children }: { readonly children: string }) {
  return (
    <code className="text-cc-heading rounded bg-[rgba(245,241,234,0.06)] px-1 py-0.5 font-mono text-[0.85em] whitespace-nowrap">
      {children}
    </code>
  );
}

const LINK_CLASS =
  "text-cc-accent hover:text-cc-accent-hover underline underline-offset-4";

function ExternalLink({
  href,
  children,
}: {
  readonly href: string;
  readonly children: ReactNode;
}) {
  return (
    <a className={LINK_CLASS} href={href} rel="noopener" target="_blank">
      {children}
    </a>
  );
}

function InPractice({
  href,
  children,
}: {
  readonly href: string;
  readonly children: ReactNode;
}) {
  return (
    <p className="text-cc-ink-dim text-sm">
      In practice:{" "}
      <Link className={LINK_CLASS} href={href}>
        {children}
      </Link>
      .
    </p>
  );
}

interface TableColumn {
  readonly header: string;
  readonly mono?: boolean;
}

interface TableProps {
  readonly caption: string;
  readonly columns: readonly TableColumn[];
  readonly rows: readonly (readonly string[])[];
  readonly minWidth: string;
}

function Table({ caption, columns, rows, minWidth }: TableProps) {
  return (
    <div className="overflow-x-auto">
      <table className={`w-full border-collapse text-left ${minWidth}`}>
        <caption className="sr-only">{caption}</caption>
        <thead>
          <tr className="text-cc-nav-label font-mono text-xs tracking-[0.16em] uppercase">
            {columns.map((column, i) => (
              <th
                key={column.header}
                scope="col"
                className={`border-cc-card-border border-b py-3 font-semibold ${
                  i < columns.length - 1 ? "pr-6" : ""
                }`}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="text-cc-ink text-sm">
          {rows.map((row) => (
            <tr key={row[0]}>
              {row.map((cell, i) => (
                <td
                  key={columns[i].header}
                  className={`border-cc-card-border border-b py-4 align-top ${
                    i < columns.length - 1 ? "pr-6" : ""
                  } ${
                    columns[i].mono
                      ? "text-cc-heading font-mono text-[13px]"
                      : ""
                  }`}
                >
                  {cell}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

const HERO_SERVICES: readonly {
  readonly name: string;
  readonly color: string;
}[] = [
  { name: "Catalog", color: "#f27765" },
  { name: "Billing", color: "#eabd21" },
  { name: "Shipping", color: "#00bce5" },
];

function HeroChip({
  label,
  sub,
  dotColor,
}: {
  readonly label: string;
  readonly sub?: string;
  readonly dotColor?: string;
}) {
  return (
    <div className="border-cc-card-border rounded-lg border bg-[rgba(12,19,34,0.72)] px-4 py-2.5 text-left">
      <div className="text-cc-heading flex items-center gap-2 font-mono text-[11px] tracking-[0.14em] uppercase">
        {dotColor && (
          <span
            aria-hidden="true"
            className="inline-block size-2 rounded-[3px]"
            style={{ backgroundColor: dotColor }}
          />
        )}
        {label}
      </div>
      {sub && (
        <div className="text-cc-ink-dim mt-0.5 font-mono text-[10.5px]">
          {sub}
        </div>
      )}
    </div>
  );
}

function HeroLink() {
  return (
    <div
      aria-hidden="true"
      className="bg-cc-card-border h-6 w-px sm:h-px sm:w-8"
    />
  );
}

function HeroFan() {
  return (
    <>
      <div
        aria-hidden="true"
        className="bg-cc-card-border h-6 w-px sm:hidden"
      />
      <svg
        aria-hidden="true"
        viewBox="0 0 32 187"
        className="text-cc-ink-faint hidden h-[187px] w-8 sm:block"
      >
        <path
          d="M0 94 C 18 94, 14 29, 32 29 M0 94 H 32 M0 94 C 18 94, 14 158, 32 158"
          fill="none"
          stroke="currentColor"
          strokeWidth="1"
        />
      </svg>
    </>
  );
}

function HeroServices({ sub }: { readonly sub: string }) {
  return (
    <div className="flex flex-col items-stretch gap-2 sm:gap-1.5">
      {HERO_SERVICES.map((svc) => (
        <HeroChip
          key={svc.name}
          label={svc.name}
          sub={sub}
          dotColor={svc.color}
        />
      ))}
    </div>
  );
}

function HeroPanel({
  label,
  children,
}: {
  readonly label: string;
  readonly children: ReactNode;
}) {
  return (
    <figure className="m-0 flex flex-col items-center gap-4">
      <figcaption className="text-cc-nav-label font-mono text-[11px] tracking-[0.16em] uppercase">
        {label}
      </figcaption>
      {children}
    </figure>
  );
}

function HeroDiagram() {
  return (
    <div className="mt-10 flex flex-col items-center gap-10 lg:flex-row lg:items-start lg:gap-14">
      <HeroPanel label="Before · every app merges by hand">
        <div className="flex flex-col items-center gap-0 sm:flex-row sm:items-center">
          <HeroChip label="Client" sub="three calls · merged by hand" />
          <HeroFan />
          <HeroServices sub="own API · own team" />
        </div>
      </HeroPanel>
      <HeroPanel label="Federated · one gateway, one query">
        <div className="flex flex-col items-center gap-0 sm:flex-row sm:items-center">
          <HeroChip label="Client" sub="one query" />
          <HeroLink />
          <HeroChip label="Gateway" sub="one schema · one endpoint" />
          <HeroFan />
          <HeroServices sub="own schema · own team" />
        </div>
      </HeroPanel>
    </div>
  );
}

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

export function ExplainerPage() {
  return (
    <div className="bg-cc-bg relative left-1/2 -mt-8 w-screen -translate-x-1/2">
      <section className="border-cc-card-border relative flex flex-col items-center overflow-hidden border-b px-5 pt-6 pb-16 text-center sm:px-12 sm:pt-10 sm:pb-20">
        <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mx-auto w-full max-w-3xl text-balance">
          What is{" "}
          <span
            className="bg-clip-text text-transparent sm:whitespace-nowrap"
            style={{ backgroundImage: GRADIENT }}
          >
            GraphQL Federation?
          </span>
        </h1>
        <p className="text-cc-ink mx-auto mt-7 max-w-2xl text-lg">
          {FEDERATION_DEFINITION} A GraphQL API describes the data it offers in
          a schema, a typed document, and answers one query with exactly the
          fields the client asked for.
        </p>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-lg">
          Clients want one API for a screen that many teams build. Teams want to
          ship without waiting on anyone. Federation gives both, with any
          GraphQL server, in any language.
        </p>
        <HeroDiagram />
        <p className="font-heading text-cc-heading mx-auto mt-10 text-xl text-balance">
          Federation merges the{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: GRADIENT }}
          >
            schemas
          </span>
          , not the services.
        </p>
      </section>

      <TransitStory />

      <Section id="how-it-works">
        <Intro title="How GraphQL Federation works: subgraphs, composition, and the gateway.">
          <p>
            Each team&apos;s service is a subgraph that publishes a source
            schema. Composition, a build step, validates the source schemas,
            merges them into one composite schema, and fails the build on
            conflicts. At runtime a gateway serves the composite schema at one
            endpoint, and its distributed executor plans each query across the
            subgraphs. The composite schema is also called the graph: its types
            link to each other, and a query walks those links. The vocabulary,
            as the GraphQL Federation specification (an open standard developed
            at the GraphQL Foundation) defines it:
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
        <div className="mt-14">
          <SubHeading id="benefits">Benefits of GraphQL Federation</SubHeading>
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
      </Section>

      <section
        id="request"
        className="border-cc-card-border scroll-mt-24 overflow-hidden border-t"
      >
        <PageSection maxWidth="6xl" className="pt-16 sm:pt-24">
          <Intro title="How a GraphQL Federation gateway runs one request.">
            <p>
              The gateway is the public entry point. Behind it, a distributed
              executor turns each query into a plan from the composite schema:
              which subgraph answers each field, which calls can run at once,
              and which must wait for data another subgraph holds. Take the
              product page query: name, price, delivery. Billing and Catalog
              answer in parallel, and the executor asks Catalog for the
              product&apos;s weight as well, a field the client never requested,
              because Shipping&apos;s delivery estimate needs it. Shipping runs
              second with the weight passed in as an ordinary argument. The
              executor merges the three answers into exactly the shape the
              client asked for and sends one response. Subgraphs never call each
              other. Every call is an ordinary GraphQL query.
            </p>
          </Intro>
        </PageSection>
        <div className="mt-2 w-full">
          <GatewayScene />
        </div>
      </section>

      <Section id="entities-keys-lookups">
        <Intro
          eyebrow={`${DEVELOPER_EYEBROW} · @key and @lookup`}
          title="Entities: a key gives identity, a lookup gives recall."
        >
          <p>
            Catalog, Billing, and Shipping each define a Product. What makes
            them the same product is a key, declared with{" "}
            <Code>{'@key(fields: "id")'}</Code>, a directive: an annotation
            inside the schema. A type with a stable key that other subgraphs can
            refer to is called an entity, and the key gives it identity. What
            lets the executor fetch that product again inside another subgraph
            is a lookup: a plain query field such as{" "}
            <Code>productById(id: ID!): Product</Code>, marked{" "}
            <Code>@lookup</Code>. A lookup gives recall.
          </p>
          <p>
            Once the executor holds a product&apos;s id, here straight from the
            client&apos;s query, it calls Billing&apos;s lookup to get the
            price, with the same query Billing would answer for any client.
            Composition pairs the lookup&apos;s argument with the key field by
            name; when the names differ, <Code>@is</Code> maps them. An entity
            can have several lookups, in one subgraph or across subgraphs, each
            fetching it by one of its keys, and a key with no lookup still
            identifies the entity, for caching or comparison, without being able
            to fetch it.
          </p>
          <InPractice href="/docs/fusion/entities-and-lookups">
            declaring entities and lookups
          </InPractice>
        </Intro>
        <SceneReveal>
          <LookupVisual />
        </SceneReveal>
      </Section>

      <Section id="require">
        <Intro
          eyebrow={`${DEVELOPER_EYEBROW} · @require`}
          title="Requirements: a dependency is an ordinary argument."
        >
          <p>
            Shipping estimates delivery time by weight, but Catalog provides
            weight. Shipping declares the dependency on an argument of its own
            field:{" "}
            <Code>{'delivery(weight: Float! @require(field: "weight"))'}</Code>.
            Composition removes that argument from the composite schema, so
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
        </Intro>
        <SceneReveal>
          <RequireVisual />
        </SceneReveal>
      </Section>

      <Section id="composition">
        <Intro
          eyebrow="Composition"
          title="Composition fails the build, not the client."
        >
          <p>
            Composition runs in CI on every pull request, before anything
            deploys. When two source schemas disagree, the build stops with the
            field and the mismatch spelled out. In the example, Billing changes
            the type of id, the key every subgraph shares, and composition
            reports that <Code>Product.id</Code> is <Code>Int!</Code> in Billing
            and <Code>ID!</Code> in Catalog. Nothing ships. Once Billing
            restores the type, composition emits the composite schema as one
            artifact that the gateway loads. A breaking change is a failed check
            on a branch, seen by the team that made it, not an incident
            discovered by a client.
          </p>
          <InPractice href="/docs/fusion/composition">
            running composition in CI
          </InPractice>
        </Intro>
        <SceneReveal>
          <BuildCheckVisual />
        </SceneReveal>
      </Section>

      <Section id="evolution">
        <Intro
          eyebrow={`${DEVELOPER_EYEBROW} · @override`}
          title="The graph evolves. Clients never notice."
        >
          <p>
            The composite schema is the stable contract. Behind it, ownership
            moves. Suppose price began in Catalog, back when Catalog was the
            only service. When Billing takes it over, it declares price in its
            own source schema with <Code>{'@override(from: "catalog")'}</Code>.
            On the next build, composition routes price to Billing, and Catalog
            can delete its copy of the field on its own schedule. The
            client&apos;s query does not change and neither does its response.
            Teams split subgraphs, merge them, or rewrite one in another
            language, and none of that is a migration on the client side.
          </p>
          <InPractice href="/docs/fusion/schema-exposure-and-evolution">
            evolving a composite schema
          </InPractice>
        </Intro>
        <SceneReveal>
          <EvolutionVisual />
        </SceneReveal>
      </Section>

      <Section id="any-language">
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
            Every GraphQL server, in any language, is already a subgraph.
          </h2>
          <div className="text-cc-ink mt-5 space-y-4 text-base">
            <p>
              A key is a directive on a type. A lookup is a query field the
              subgraph would expose anyway, marked <Code>@lookup</Code>. A
              requirement is an argument. Every call the executor makes is an
              ordinary GraphQL query. Apollo Federation, the older design, asks
              a subgraph to implement a hidden <Code>_entities</Code> field,
              write reference resolvers, and reason about the representations
              they carry. Here there is nothing of that kind and no subgraph
              specification to implement. You declare what your schema means,
              and the server stays as it is. That is the design rule of the
              GraphQL Federation specification.
            </p>
            <p>
              It holds for a server in any language. Spring for GraphQL in Java
              or Kotlin, NestJS or GraphQL Yoga in Node.js, gqlgen in Go,
              Strawberry in Python, async-graphql in Rust, graphql-ruby, Hot
              Chocolate in .NET: each publishes a schema and answers a query,
              and that is all the executor needs.
            </p>
          </div>
          <div className="mt-10">
            <SubHeading id="batching">
              Does GraphQL Federation cause N+1 requests? Batching is a
              transport concern.
            </SubHeading>
          </div>
          <div className="text-cc-ink mt-4 space-y-4 text-base">
            <p>
              The rule that the server stays as it is also covers batching: it
              is solved in the transport, not in the schema. Ask for a hundred
              products and the executor would send a hundred lookups into
              Shipping, each the same query with different arguments. So the
              specification&apos;s working group at the GraphQL Foundation is
              adding variable batching to the GraphQL over HTTP specification:
              one query sent with a list of variable sets, which a server can
              fold into a single execution. The source schema does not change to
              get it.
            </p>
          </div>
        </div>
      </Section>

      <Section id="directives">
        <Intro title="GraphQL Federation directives, grouped by what they do.">
          <p>
            <Code>@key</Code>, <Code>@lookup</Code>, <Code>@require</Code>, and{" "}
            <Code>@override</Code> are four of the directives the specification
            defines. Identity and recall, and data dependencies, are the core of
            every federated graph; the ownership, visibility, and interface
            directives are refinements for a graph that has grown.
          </p>
        </Intro>
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
      </Section>

      <Section id="alternatives">
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
      </Section>

      <Section id="specification">
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
      </Section>

      <Section id="apollo-federation-vs-graphql-federation">
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
      </Section>

      <Section id="fusion">
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
            Fusion: one gateway that speaks both.
          </h2>
          <div className="text-cc-ink mt-5 space-y-4 text-base">
            <p>
              Fusion is ChilliCream&apos;s GraphQL Federation gateway and,
              today, the only gateway that implements the GraphQL Federation
              specification. It also composes Apollo Federation subgraphs, so
              source schemas written to either specification compose into the
              same composite schema, and a team can adopt either, mix the two,
              or move between them one subgraph at a time.
            </p>
            <p>
              Subgraphs can be written in any language. The gateway talks to
              them with ordinary GraphQL queries, there is no plugin to install,
              and services that only speak REST (OpenAPI) or gRPC can join as
              well. What you take on is a gateway to run and a composition step
              in CI that fails the build on conflicts.
            </p>
          </div>
          <ButtonRow className="mt-9">
            <SolidButton href="/docs/fusion/getting-started">
              Start with Fusion
            </SolidButton>
            <OutlineButton href="/docs/fusion">
              Fusion documentation
            </OutlineButton>
          </ButtonRow>
        </div>
      </Section>

      <Section id="faq">
        <FaqSection
          id="federation-faq"
          align="left"
          heading="Common questions about GraphQL Federation."
          items={FEDERATION_FAQ_ITEMS}
        />
      </Section>
    </div>
  );
}
