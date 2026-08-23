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

function HeroMonolithChip() {
  return (
    <div className="border-cc-card-border rounded-lg border bg-[rgba(12,19,34,0.72)] px-4 py-2.5 text-left">
      <div className="text-cc-heading font-mono text-[11px] tracking-[0.14em] uppercase">
        One shared API
      </div>
      <div className="mt-1.5 flex flex-col gap-1">
        {HERO_SERVICES.map((svc) => (
          <div
            key={svc.name}
            className="text-cc-ink-dim flex items-center gap-2 font-mono text-[10.5px]"
          >
            <span
              aria-hidden="true"
              className="inline-block size-2 rounded-[3px]"
              style={{ backgroundColor: svc.color }}
            />
            {svc.name} team
          </div>
        ))}
      </div>
      <div className="border-cc-card-border text-cc-ink-dim mt-1.5 border-t pt-1.5 font-mono text-[10.5px]">
        one schema · one deploy · one queue
      </div>
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
      <HeroPanel label="Before · every team in one deploy">
        <div className="flex flex-col items-center gap-0 sm:flex-row sm:items-center">
          <HeroChip label="Client" sub="one query" />
          <HeroLink />
          <HeroMonolithChip />
        </div>
      </HeroPanel>
      <HeroPanel label="Federated · every team in its own service">
        <div className="flex flex-col items-center gap-0 sm:flex-row sm:items-center">
          <HeroChip label="Client" sub="one query" />
          <HeroLink />
          <HeroChip label="Gateway" sub="one schema · one endpoint" />
          <HeroFan />
          <div className="flex flex-col items-stretch gap-2 sm:gap-1.5">
            {HERO_SERVICES.map((svc) => (
              <HeroChip
                key={svc.name}
                label={svc.name}
                sub="own schema · own team"
                dotColor={svc.color}
              />
            ))}
          </div>
        </div>
      </HeroPanel>
    </div>
  );
}

const GLOSSARY: readonly {
  readonly concept: string;
  readonly apollo: string;
  readonly spec: string;
}[] = [
  {
    concept: "The merged schema clients query",
    apollo: "Supergraph",
    spec: "Composite schema",
  },
  {
    concept: "A team's independent service",
    apollo: "Subgraph",
    spec: "Source schema",
  },
  {
    concept: "The runtime in front of it all",
    apollo: "Router",
    spec: "Gateway",
  },
  {
    concept: "A type assembled across services",
    apollo: "Entity (@key)",
    spec: "Entity (@key + @lookup)",
  },
  {
    concept: "Merging and validating schemas",
    apollo: "Composition",
    spec: "Composition",
  },
  {
    concept: "Fetching an entity by its key",
    apollo: "_entities query field",
    spec: "@lookup query field",
  },
  {
    concept: "A field that needs another service's data",
    apollo: "@requires (on the field)",
    spec: "@require (on the argument)",
  },
  {
    concept: "Moving a field between teams",
    apollo: "@override(from:)",
    spec: "@override(from:)",
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
    how: "Schemas compose ahead of time; a gateway plans each operation across services. Conflicts fail the build.",
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
    q: "Who invented GraphQL Federation?",
    a: "Apollo introduced its Federation specification in 2019. Since then the idea has outgrown a single vendor: the GraphQL Foundation now develops the open GraphQL Federation specification, and multiple gateways implement one or both.",
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
    a: "Both terms name the same thing: the one merged schema clients see. Supergraph is the common word in the Apollo ecosystem; the GraphQL Federation spec calls it the composite schema.",
  },
  {
    q: "What is an entity?",
    a: "A type with a stable key, like an id, that the gateway can resolve across services. That identity is what lets several teams own different fields of the same Product: Catalog its name, Billing its price, Shipping its delivery window.",
  },
  {
    q: "Does the gateway add latency?",
    a: "A little, yes: one extra hop, plus planning time the first time it sees an operation shape. Plans are cached and services are called in parallel, and for clients, one round trip to one endpoint typically replaces several to separate APIs.",
  },
  {
    q: "Can Apollo Federation and GraphQL Federation be mixed?",
    a: "The specs are distinct, but one gateway can support both. Fusion reads services written to either specification and merges them into a single graph, so a migration can happen type by type, at whatever pace you choose, or not at all.",
  },
];

export function ExplainerPage() {
  return (
    <div className="bg-cc-bg relative left-1/2 -mt-8 min-h-screen w-screen -translate-x-1/2">
      <section className="border-cc-card-border relative flex flex-col items-center overflow-hidden border-b px-5 pt-20 pb-16 text-center sm:px-12 sm:pt-28 sm:pb-20">
        <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mx-auto w-full max-w-3xl text-balance">
          What is{" "}
          <span
            className="bg-clip-text text-transparent sm:whitespace-nowrap"
            style={{
              backgroundImage: "linear-gradient(90deg, #5eead4, #16b9e4)",
            }}
          >
            GraphQL Federation?
          </span>
        </h1>
        <p className="text-cc-ink mx-auto mt-7 max-w-2xl text-lg">
          As a GraphQL API grows, one schema ends up shared by many teams. Every
          change queues behind the same review, the same deploy, the same
          central team, and shipping slows for everyone. The bottleneck is
          organizational, not technical.
        </p>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-lg">
          Federation removes the queue. Each team owns its own service and
          publishes its own schema. A build step called composition merges the
          schemas into one, and fails if they conflict. A gateway serves the
          merged schema at a single endpoint. Clients see one API; teams ship
          independently.
        </p>
        <HeroDiagram />
        <p className="font-heading text-cc-heading mx-auto mt-10 text-xl text-balance">
          Federation merges the{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{
              backgroundImage: "linear-gradient(90deg, #5eead4, #16b9e4)",
            }}
          >
            schemas
          </span>
          , not the services.
        </p>
      </section>

      <TransitStory />

      <section className="border-cc-card-border overflow-hidden border-t">
        <PageSection maxWidth="6xl" className="pt-16 sm:pt-24">
          <Intro title="How one request runs.">
            <p>
              From the composed schema the gateway computes an operation plan:
              which service owns each field, and which calls can run in
              parallel. Plans are cached, so the client pays for one round trip
              while the gateway calls Catalog, Billing, and Shipping
              concurrently and waits only for the slowest.
            </p>
          </Intro>
        </PageSection>
        <div className="mt-2 w-full">
          <GatewayScene />
        </div>
      </section>

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
            Apollo invented federation in 2019, and its directive-based spec is
            widely deployed. The GraphQL Federation specification is the same
            idea as an open, vendor-neutral standard, developed in the open at
            the GraphQL Foundation. Both describe the same architecture, and you
            will meet both vocabularies in the wild:
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
                    GraphQL Federation
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
                      {row.spec}
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
            <p>
              Fusion is ChilliCream&apos;s federation gateway. It composes
              Apollo Federation subgraphs and GraphQL Federation source schemas
              in the same graph, so you can adopt either spec, or move between
              them, one service at a time.
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
