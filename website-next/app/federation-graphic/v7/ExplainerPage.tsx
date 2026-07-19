/**
 * "What is GraphQL Federation?" explainer, v3. Educator-first: vendor-neutral
 * through the teaching sections, Fusion enters only at the end. One living
 * diagram grammar carries the whole page: the same three team schemas, the
 * same Product entity, the same gateway, staged scene by scene.
 */

import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { CYAN, TEAL } from "./palette";
import { StoryScroller } from "./StoryScroller";
import { CheckVisual } from "./visuals/CheckVisual";
import { HeroFlow } from "./visuals/HeroFlow";
import { OwnershipVisual } from "./visuals/OwnershipVisual";
import { QueryPlanVisual } from "./visuals/QueryPlanVisual";

/* ============================================================================
   Section scaffolding
============================================================================ */

function Section({ children }: { readonly children: ReactNode }) {
  return (
    <section className="border-cc-card-border border-t">
      <div className="mx-auto max-w-6xl px-5 py-16 sm:px-12 sm:py-24">
        {children}
      </div>
    </section>
  );
}

interface EyebrowProps {
  readonly index: string;
  readonly children: ReactNode;
}

function Eyebrow({ index, children }: EyebrowProps) {
  return (
    <div className="flex items-center gap-3">
      <span className="text-cc-accent font-mono text-xs tracking-[0.2em]">
        {index}
      </span>
      <span aria-hidden="true" className="bg-cc-card-border h-px w-8" />
      <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
        {children}
      </span>
    </div>
  );
}

interface IntroProps {
  readonly index: string;
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

function Intro({ index, eyebrow, title, children }: IntroProps) {
  return (
    <div className="max-w-2xl">
      <Eyebrow index={index}>{eyebrow}</Eyebrow>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 text-balance">
        {title}
      </h2>
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

/* ============================================================================
   Content blocks
============================================================================ */

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
];

const ALTERNATIVES: readonly {
  readonly name: string;
  readonly how: string;
  readonly when: string;
}[] = [
  {
    name: "Federation",
    how: "Schemas compose ahead of time; a gateway plans each query across services. Conflicts fail the build.",
    when: "Several teams need to ship one coherent API independently.",
  },
  {
    name: "Schema stitching",
    how: "A gateway merges schemas at runtime with hand-written resolvers gluing types together.",
    when: "Quick aggregation of a few services you control; the glue code is yours to maintain.",
  },
  {
    name: "BFF per client",
    how: "Each frontend team builds its own backend that hand-aggregates the services it needs.",
    when: "One or two clients with very different needs; duplication grows with every new client.",
  },
  {
    name: "Modular monolith",
    how: "One deployable exposes one schema; modules keep code ownership internal.",
    when: "One team, or several teams that genuinely ship together. Often the right start.",
  },
];

const FAQ: readonly { readonly q: string; readonly a: string }[] = [
  {
    q: "What problem does GraphQL federation solve?",
    a: "It lets multiple teams own parts of one API without a central team becoming the bottleneck. Each team publishes its own schema; composition merges them into one graph that clients query at a single endpoint.",
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
    a: "Usually, yes. One team on one service is better served by a single GraphQL server. Federation earns its keep when coordinating schema changes across teams becomes the bottleneck.",
  },
  {
    q: "What is a supergraph or composite schema?",
    a: "Both terms name the same thing: the one merged schema clients see. Supergraph is Apollo Federation vocabulary; composite schema is the GraphQL Composite Schemas term.",
  },
  {
    q: "What is an entity?",
    a: "A type whose fields are owned by more than one service, joined by identity. Catalog knows a product's name, billing knows its price, shipping knows its delivery window; the same id makes them one Product.",
  },
  {
    q: "Does the gateway add latency?",
    a: "The gateway plans each query once (plans are cached) and calls services in parallel where the shape allows. For clients, one round trip to one endpoint typically replaces several round trips to separate APIs.",
  },
  {
    q: "Can Apollo Federation and Composite Schemas be mixed?",
    a: "The specs are distinct, but a gateway can support both. Fusion composes Apollo Federation v2 subgraphs and Composite Schemas source schemas in the same graph, so you can migrate at your own pace or never.",
  },
];

/* ============================================================================
   Page
============================================================================ */

export function ExplainerPage() {
  return (
    <div className="bg-cc-bg relative">
      {/* Hero: the definition plus the living diagram. */}
      <section className="relative overflow-hidden">
        <div className="mx-auto w-full max-w-6xl px-5 py-16 sm:px-12 sm:py-24">
          <div className="grid grid-cols-1 items-center gap-14 lg:grid-cols-12 lg:gap-10">
            <div className="lg:col-span-5">
              <div className="flex items-center gap-3">
                <span
                  aria-hidden="true"
                  className="h-px w-12 rounded-full"
                  style={{
                    background: `linear-gradient(90deg, ${TEAL}, transparent)`,
                  }}
                />
                <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
                  Learn · GraphQL Federation
                </span>
              </div>
              <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-6 text-balance">
                What is{" "}
                <span
                  className="bg-clip-text text-transparent"
                  style={{
                    backgroundImage: `linear-gradient(90deg, ${TEAL}, ${CYAN})`,
                  }}
                >
                  GraphQL federation?
                </span>
              </h1>
              <p className="text-cc-ink mt-6 text-base sm:text-lg">
                GraphQL federation combines the schemas of multiple independent
                services into one unified graph, served at a single endpoint.
                Each team owns and deploys its own service; a gateway composes
                the schemas and plans every query across them. Clients see one
                API, never the seams.
              </p>
              <div className="mt-9">
                <OutlineButton href="/docs/fusion">
                  Read the Fusion Docs
                </OutlineButton>
              </div>
            </div>
            <div className="lg:col-span-7">
              <HeroFlow />
            </div>
          </div>
        </div>
      </section>

      {/* The story: one service to five, to entities, to composition. */}
      <StoryScroller />

      {/* 08 · Query planning */}
      <Section>
        <Intro
          index="08"
          eyebrow="How it works"
          title="One request in. One response out."
        >
          <p>
            A client sends one request to one endpoint. The gateway breaks it
            into the pieces each service can answer, calls them, and merges the
            results into a single response. The plan is computed from the
            composed schema and cached, so the client pays for one round trip
            while the graph does the traveling.
          </p>
        </Intro>
        <SceneReveal>
          <QueryPlanVisual />
        </SceneReveal>
      </Section>

      {/* 09 · Checks */}
      <Section>
        <Intro
          index="09"
          eyebrow="Staying safe"
          title="Broken graphs fail the build, not the client."
        >
          <p>
            Because composition happens ahead of time, conflicts between teams
            surface as build errors with exact diagnostics, not as runtime
            surprises for clients. Schema checks run at pull-request time, so a
            breaking change is a failed check on a branch, not an incident.
          </p>
        </Intro>
        <SceneReveal>
          <CheckVisual />
        </SceneReveal>
      </Section>

      {/* 10 · Evolution */}
      <Section>
        <Intro
          index="10"
          eyebrow="Staying safe"
          title="The graph evolves. Clients never notice."
        >
          <p>
            Ownership of a field can move to a different team while clients keep
            querying the same schema. Fields are deprecated and retired on a
            schedule instead of breaking overnight. The composed graph is the
            stable surface; everything behind it stays in motion.
          </p>
        </Intro>
        <SceneReveal>
          <OwnershipVisual />
        </SceneReveal>
      </Section>

      {/* 11 · Alternatives */}
      <Section>
        <Intro
          index="11"
          eyebrow="Context"
          title="Federation next to the alternatives."
        >
          <p>
            Federation is not the only way to put one API in front of many
            services, and it is not always the best one. The honest comparison:
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
                    Approach
                  </th>
                  <th className="border-cc-card-border border-b py-3 pr-6 font-semibold">
                    How it works
                  </th>
                  <th className="border-cc-card-border border-b py-3 font-semibold">
                    When it fits
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
                    <td className="border-cc-card-border border-b py-4 align-top">
                      {row.when}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </RevealOnScroll>
      </Section>

      {/* 08 · Honesty */}
      <Section>
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="font-heading text-cc-heading text-h4 text-balance">
            You might not need federation.
          </h2>
          <p className="text-cc-ink-dim mt-5 text-base">
            One team on one service, a small or early API, or domains without
            clear boundaries are better served by a single GraphQL server.
            Federation is for the moment coordination becomes the bottleneck. It
            will be waiting when you get there.
          </p>
        </div>
      </Section>

      {/* 12 · The two specs */}
      <Section>
        <Intro
          index="12"
          eyebrow="The spec landscape"
          title="Two specs. One idea."
        >
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

      {/* 10 · Meet Fusion */}
      <Section>
        <div className="mx-auto max-w-2xl text-center">
          <Eyebrow index="13">Meet Fusion</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 text-balance">
            One gateway that speaks both.
          </h2>
          <div className="text-cc-ink mt-5 space-y-4 text-base">
            <p>
              Fusion is ChilliCream&apos;s federation gateway. It composes
              Apollo Federation v2 subgraphs and Composite Schemas source
              schemas in the same graph, scores 100% (199/199) on The
              Guild&apos;s public federation compatibility audit, and runs as
              part of your own ASP.NET Core application: your DI, your
              authentication, your OpenTelemetry pipeline.
            </p>
          </div>
          <div className="mt-9 flex flex-wrap justify-center gap-3">
            <SolidButton href="/get-started">Start for Free</SolidButton>
            <OutlineButton href="/docs/fusion">Read the Docs</OutlineButton>
          </div>
        </div>
      </Section>

      {/* 11 · FAQ */}
      <Section>
        <Intro index="14" eyebrow="FAQ" title="Common questions." />
        <div className="mt-10 max-w-3xl">
          {FAQ.map((item) => (
            <details
              key={item.q}
              className="group border-cc-card-border border-b py-5"
            >
              <summary className="text-cc-heading flex cursor-pointer list-none items-center justify-between gap-4 text-base font-medium">
                {item.q}
                <span
                  aria-hidden="true"
                  className="text-cc-ink-dim transition-transform group-open:rotate-45"
                >
                  +
                </span>
              </summary>
              <p className="text-cc-ink mt-4 max-w-2xl text-sm leading-relaxed">
                {item.a}
              </p>
            </details>
          ))}
        </div>
      </Section>
    </div>
  );
}
