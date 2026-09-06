import Link from "next/link";
import type { ReactNode } from "react";

import { ButtonRow } from "@/src/components/ButtonRow";
import { FaqSection } from "@/src/components/FaqSection";
import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { GatewayScene } from "./hero/GatewayScene";
import { TransitStory } from "./TransitStory";
import { BuildCheckVisual } from "./visuals/BuildCheckVisual";
import { EvolutionVisual } from "./visuals/EvolutionVisual";
import { LookupVisual } from "./visuals/LookupVisual";
import { QueryPlanVisual } from "./visuals/QueryPlanVisual";
import { RequireVisual } from "./visuals/RequireVisual";

function Section({ children }: { readonly children: ReactNode }) {
  return (
    <section className="border-cc-card-border border-t">
      <PageSection maxWidth="6xl" className="py-16 sm:py-24">
        {children}
      </PageSection>
    </section>
  );
}

interface IntroProps {
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

function Intro({ title, children }: IntroProps) {
  return (
    <div className="max-w-2xl">
      <SectionHeading title={title} />
      {children && (
        <div className="text-cc-ink mt-5 space-y-4 text-base">{children}</div>
      )}
    </div>
  );
}

function SceneReveal({ children }: { readonly children: ReactNode }) {
  return (
    <RevealOnScroll className="mt-12" hiddenClassName="translate-y-8 opacity-0">
      {children}
    </RevealOnScroll>
  );
}

const GLOSSARY: readonly {
  readonly concept: string;
  readonly apollo: string;
  readonly composite: string;
}[] = [
  {
    concept: "The merged schema clients query",
    apollo: "Supergraph",
    composite: "Composite schema",
  },
  {
    concept: "A team's independent service",
    apollo: "Subgraph",
    composite: "Source schema",
  },
  {
    concept: "The runtime in front of it all",
    apollo: "Router",
    composite: "Gateway",
  },
  {
    concept: "A type assembled across services",
    apollo: "Entity (@key)",
    composite: "Entity (lookups, inferred keys)",
  },
  {
    concept: "Merging and validating schemas",
    apollo: "Composition",
    composite: "Composition",
  },
  {
    concept: "Fetching an entity by its key",
    apollo: "_entities + __resolveReference",
    composite: "@lookup query field",
  },
  {
    concept: "A field that needs another service's data",
    apollo: "@requires (on the field)",
    composite: "@require (on the argument)",
  },
  {
    concept: "Moving a field between teams",
    apollo: "@override(from:)",
    composite: "@override(from:)",
  },
];

const ALTERNATIVES: readonly {
  readonly name: string;
  readonly how: string;
  readonly when: string;
  readonly cost: string;
}[] = [
  {
    name: "Single GraphQL server",
    how: "One server exposes one schema; one codebase, one deploy.",
    when: "One team and one API. Where almost everyone starts.",
    cost: "Coordination happens in code review and scales only as far as one codebase.",
  },
  {
    name: "Federation",
    how: "Schemas compose ahead of time; a gateway plans each query across services. Conflicts fail the build.",
    when: "Several teams need to ship one coherent API independently.",
    cost: "A gateway to run and a composition pipeline to own; entities must be modeled deliberately.",
  },
  {
    name: "Schema stitching",
    how: "A gateway merges schemas at runtime with hand-written resolvers gluing types together.",
    when: "Quick aggregation of a few services you control.",
    cost: "Glue resolvers drift silently as the underlying schemas change.",
  },
  {
    name: "BFF per client",
    how: "Each frontend team builds its own backend that hand-aggregates the services it needs.",
    when: "One or two clients with very different needs.",
    cost: "N backends to build, secure, and monitor.",
  },
  {
    name: "Modular monolith",
    how: "One deployable exposes one schema; modules keep code ownership internal.",
    when: "One team, or several teams that genuinely ship together. Often the right start.",
    cost: "One deploy train; coupling creeps back as teams multiply.",
  },
];

const FAQ: readonly { readonly q: string; readonly a: string }[] = [
  {
    q: "What problem does GraphQL federation solve?",
    a: "It lets multiple teams own parts of one API without a central team in the critical path of every change. Each team publishes its own schema; composition merges them into one graph that clients query at a single endpoint.",
  },
  {
    q: "Who invented GraphQL federation?",
    a: "Apollo introduced the Federation specification in 2019. Since then the idea has outgrown a single vendor: the GraphQL Foundation now develops the open Composite Schemas specification, and multiple gateways implement one or both.",
  },
  {
    q: "How is federation different from schema stitching?",
    a: "Stitching merges schemas at runtime with hand-written glue code in the gateway. Federation moves the relationships into the schemas themselves and validates the merged graph ahead of time, so conflicts surface as build failures instead of runtime surprises.",
  },
  {
    q: "Is federation overkill for a small team?",
    a: "Usually, yes. One team on one service is better served by a single GraphQL server. You can start with a single Hot Chocolate server and federate later without changing your clients. Federation earns its keep once coordinating schema changes across teams starts to slow every team down.",
  },
  {
    q: "What is a supergraph or composite schema?",
    a: "Both terms name the same thing: the one merged schema clients see. Supergraph is Apollo Federation vocabulary; composite schema is the GraphQL Composite Schemas term.",
  },
  {
    q: "What is an entity?",
    a: "A type with a stable key, like an id, that the gateway can resolve across services. That identity is what lets several teams own different fields of the same Product: Catalog its name, Billing its price, Shipping its delivery window.",
  },
  {
    q: "Does the gateway add latency?",
    a: "A little, yes: one extra hop, plus planning time the first time it sees a query shape. Plans are cached and services are called in parallel, and for clients, one round trip to one endpoint typically replaces several to separate APIs.",
  },
  {
    q: "Can Apollo Federation and Composite Schemas be mixed?",
    a: "The specs are distinct, but one gateway can support both. Fusion reads subgraphs written to either specification and merges them into a single graph, so a migration can happen type by type, at whatever pace you choose, or not at all.",
  },
];

export function ExplainerPage() {
  return (
    <div className="bg-cc-bg relative left-1/2 -mt-8 min-h-screen w-screen -translate-x-1/2">
      <section className="border-cc-card-border relative flex min-h-[92svh] flex-col justify-center overflow-hidden border-b">
        <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mx-auto w-full max-w-3xl px-5 pt-16 text-center text-balance sm:px-12">
          What is{" "}
          <span
            className="bg-clip-text text-transparent sm:whitespace-nowrap"
            style={{
              backgroundImage: "linear-gradient(90deg, #5eead4, #16b9e4)",
            }}
          >
            GraphQL federation?
          </span>
        </h1>
        <p className="sr-only">
          GraphQL federation combines many services, called subgraphs, into one
          graph behind a single gateway. A client sends one query to one
          endpoint. The gateway writes a query plan that maps every field to the
          subgraph that owns it. Independent fields are fetched in parallel:
          Billing returns the price using only the product id. Fields with
          requirements wait for their inputs: Shipping needs the product&apos;s
          weight from Catalog, and the gateway carries it over, because services
          never talk to each other. The gateway then merges every result into
          one response and returns it. Many services, one graph: that is
          federation.
        </p>
        <div className="mt-2 w-full">
          <GatewayScene />
        </div>
      </section>

      <TransitStory />

      <Section>
        <Intro title="The gateway plans every query. Once.">
          <p>
            From the composed schema the gateway computes a query plan: which
            service owns each field, and which calls can run in parallel. Plans
            are cached, so at runtime the client pays for one round trip while
            the gateway calls Catalog, Billing, and Shipping concurrently and
            waits only for the slowest.
          </p>
        </Intro>
        <SceneReveal>
          <QueryPlanVisual />
        </SceneReveal>
      </Section>

      <Section>
        <Intro title="Lookups let the gateway fetch an entity by id.">
          <p>
            Partway through a plan the gateway often holds only a product&apos;s
            id and needs a way to fetch the rest. A lookup is an ordinary query
            field, marked @lookup, that returns an entity by its key. Authors
            mark the lookups; composition maps each lookup&apos;s argument to
            the entity&apos;s key. The argument&apos;s name, id, tells
            composition which key the lookup accepts. No extra directive is
            needed.
          </p>
        </Intro>
        <SceneReveal>
          <LookupVisual />
        </SceneReveal>
      </Section>

      <Section>
        <Intro title="Fields can require data from other services.">
          <p>
            Shipping computes a product&apos;s delivery window from its weight,
            but Catalog owns weight. With @require, Shipping declares that
            dependency on an argument of its own delivery field. The gateway
            fetches the weight first and passes it in. The argument never
            appears in the composite schema, and services never call each other.
          </p>
        </Intro>
        <SceneReveal>
          <RequireVisual />
        </SceneReveal>
      </Section>

      <Section>
        <Intro title="Broken graphs fail the build, not the client.">
          <p>
            Because composition happens ahead of time, conflicts between teams
            surface as build errors with exact diagnostics, not as runtime
            surprises for clients. Schema checks run at pull-request time, so a
            breaking change is a failed check on a branch, not an incident.
          </p>
        </Intro>
        <SceneReveal>
          <BuildCheckVisual />
        </SceneReveal>
      </Section>

      <Section>
        <Intro title="The graph evolves. Clients never notice.">
          <p>
            Ownership of a field can move to a different team through @override
            while clients keep querying the same schema. The composed graph is
            the stable surface; everything behind it stays in motion.
          </p>
        </Intro>
        <SceneReveal>
          <EvolutionVisual />
        </SceneReveal>
      </Section>

      <Section>
        <Intro title="Federation next to the alternatives.">
          <p>
            Federation is not the only way to put one API in front of many
            services, and it is not always the best one. Here is how they
            compare:
          </p>
        </Intro>
        <RevealOnScroll
          className="mt-10"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <div className="overflow-x-auto">
            <table className="w-full min-w-[840px] border-collapse text-left">
              <thead>
                <tr className="text-cc-nav-label font-mono text-xs tracking-[0.16em] uppercase">
                  <th className="border-cc-card-border border-b py-3 pr-6 font-semibold">
                    Approach
                  </th>
                  <th className="border-cc-card-border border-b py-3 pr-6 font-semibold">
                    How it works
                  </th>
                  <th className="border-cc-card-border border-b py-3 pr-6 font-semibold">
                    When it fits
                  </th>
                  <th className="border-cc-card-border border-b py-3 font-semibold">
                    The cost
                  </th>
                </tr>
              </thead>
              <tbody className="text-cc-ink text-sm">
                {ALTERNATIVES.map((row) => (
                  <tr key={row.name}>
                    <td className="border-cc-card-border text-cc-heading border-b py-4 pr-6 align-top font-mono text-[13px]">
                      {row.name}
                    </td>
                    <td className="border-cc-card-border border-b py-4 pr-6 align-top">
                      {row.how}
                    </td>
                    <td className="border-cc-card-border border-b py-4 pr-6 align-top">
                      {row.when}
                    </td>
                    <td className="border-cc-card-border border-b py-4 align-top">
                      {row.cost}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </RevealOnScroll>
      </Section>

      <Section>
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="font-heading text-cc-heading text-h4 text-balance">
            You might not need federation.
          </h2>
          <p className="text-cc-ink-dim mt-5 text-base">
            One team on one service, a small or early API, or domains without
            clear boundaries are better served by a single GraphQL server.
            Federation is for the moment when coordination becomes the
            bottleneck. It will be waiting when you get there.
          </p>
          <p className="text-cc-ink-dim mt-4 text-sm">
            Just getting started?{" "}
            <Link
              className="text-cc-accent hover:text-cc-accent-hover underline underline-offset-4"
              href="/docs/hotchocolate/get-started-with-graphql-in-net-core"
            >
              Stand up a single Hot Chocolate server
            </Link>{" "}
            and federate later.
          </p>
        </div>
      </Section>

      <Section>
        <Intro title="Two specs. One idea.">
          <p>
            Apollo invented federation and its directive-based spec is widely
            deployed. The GraphQL Foundation now develops the Composite Schemas
            specification: the same idea as an open, vendor-neutral standard.
            Both vocabularies describe the same architecture, and you will meet
            both in the wild:
          </p>
        </Intro>
        <RevealOnScroll
          className="mt-10"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <div className="overflow-x-auto">
            <table className="w-full min-w-[640px] border-collapse text-left">
              <thead>
                <tr className="text-cc-nav-label font-mono text-xs tracking-[0.16em] uppercase">
                  <th className="border-cc-card-border border-b py-3 pr-6 font-semibold">
                    Concept
                  </th>
                  <th className="border-cc-card-border border-b py-3 pr-6 font-semibold">
                    Apollo Federation
                  </th>
                  <th className="border-cc-card-border border-b py-3 font-semibold">
                    Composite Schemas
                  </th>
                </tr>
              </thead>
              <tbody className="text-cc-ink text-sm">
                {GLOSSARY.map((row) => (
                  <tr key={row.concept}>
                    <td className="border-cc-card-border border-b py-4 pr-6 align-top">
                      {row.concept}
                    </td>
                    <td className="border-cc-card-border border-b py-4 pr-6 align-top font-mono text-[13px]">
                      {row.apollo}
                    </td>
                    <td className="border-cc-card-border border-b py-4 align-top font-mono text-[13px]">
                      {row.composite}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </RevealOnScroll>
      </Section>

      <Section>
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
            One gateway that speaks both.
          </h2>
          <div className="text-cc-ink mt-5 space-y-4 text-base">
            <p>Fusion is ChilliCream&apos;s federation gateway.</p>
            <p>
              It composes Apollo Federation v2 subgraphs and Composite Schemas
              source schemas in the same graph.
            </p>
            <p>
              It scores{" "}
              <span className="text-cc-heading font-semibold">
                100% (199/199)
              </span>{" "}
              on The Guild&apos;s public federation compatibility audit.
            </p>
            <p>
              It runs inside your own ASP.NET Core application: your DI, your
              authentication, your OpenTelemetry pipeline.
            </p>
          </div>
          <ButtonRow className="mt-9">
            <SolidButton href="/docs/fusion/getting-started">
              Start with Fusion
            </SolidButton>
            <OutlineButton href="/docs/fusion">Read the Docs</OutlineButton>
          </ButtonRow>
        </div>
      </Section>

      <Section>
        <FaqSection
          id="federation-faq"
          align="left"
          heading="Common questions."
          items={FAQ.map(({ q: question, a: answer }) => ({
            question,
            answer,
          }))}
        />
      </Section>
    </div>
  );
}
