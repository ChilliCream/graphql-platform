/**
 * Mocha messaging page, v14. The v13 circuit board survives, but scoped to the
 * hero and always visible at a faded strength: no torch, no scroll reveal.
 * Service nodes glow on the board and coral messages ride its lanes. Below the
 * hero the page follows the house marketing style: numbered sections divided
 * by hairlines, alternating copy/visual rows with animated concept diagrams,
 * and the real Nitro trace primitives for the observability chapter.
 */

import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

import { HeroBoard } from "./HeroBoard";
import { CORAL, CYAN, GREEN, VIOLET } from "./palette";
import { FanoutVisual } from "./visuals/FanoutVisual";
import { MediatorVisual } from "./visuals/MediatorVisual";
import { OutboxVisual } from "./visuals/OutboxVisual";
import { SagaVisual } from "./visuals/SagaVisual";
import { TraceBento } from "./visuals/TraceBento";
import { TransportsVisual } from "./visuals/TransportsVisual";

/* ============================================================================
   Section scaffolding
============================================================================ */

interface SectionProps {
  readonly children: ReactNode;
}

function Section({ children }: SectionProps) {
  return (
    <section className="border-cc-card-border border-t">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-12 sm:py-28">
        {children}
      </div>
    </section>
  );
}

interface EyebrowProps {
  readonly index: string;
  readonly children: ReactNode;
  readonly center?: boolean;
}

function Eyebrow({ index, children, center }: EyebrowProps) {
  return (
    <div
      className={`flex items-center gap-3 ${center ? "justify-center" : ""}`}
    >
      <span className="text-cc-ink-dim font-mono text-xs tabular-nums">
        {index}
      </span>
      <span
        className="font-mono text-xs font-medium tracking-[0.22em] uppercase"
        style={{ color: CORAL }}
      >
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
  readonly center?: boolean;
}

function Intro({ index, eyebrow, title, children, center }: IntroProps) {
  return (
    <div className={center ? "mx-auto max-w-2xl text-center" : "max-w-xl"}>
      <Eyebrow index={index} center={center}>
        {eyebrow}
      </Eyebrow>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 text-balance">
        {title}
      </h2>
      {children && (
        <div className="text-cc-ink mt-5 space-y-4 text-base">{children}</div>
      )}
    </div>
  );
}

interface RowProps {
  readonly copy: ReactNode;
  readonly visual: ReactNode;
  readonly reverse?: boolean;
}

function Row({ copy, visual, reverse }: RowProps) {
  return (
    <div className="grid grid-cols-1 items-center gap-12 lg:grid-cols-12 lg:gap-16">
      <div className={`lg:col-span-5 ${reverse ? "lg:order-2" : ""}`}>
        {copy}
      </div>
      <RevealOnScroll
        className={`lg:col-span-7 ${reverse ? "lg:order-1" : ""}`}
        hiddenClassName="translate-y-8 opacity-0"
      >
        {visual}
      </RevealOnScroll>
    </div>
  );
}

/* ============================================================================
   01 · Model — the three ranges
============================================================================ */

const RANGES = [
  {
    index: "a",
    range: "In process",
    name: "Mediator",
    api: "ISender.Send(cmd)",
    color: CYAN,
    body: "A command maps to one typed handler. Validation, caching, and telemetry sit around it as middleware.",
  },
  {
    index: "b",
    range: "Across services",
    name: "Bus",
    api: "IBus.PublishAsync(evt)",
    color: VIOLET,
    body: "Publish an event once. Every service that subscribes reacts on its own schedule, on any transport.",
  },
  {
    index: "c",
    range: "Over time",
    name: "Sagas",
    api: "Saga<ReviewState>",
    color: CORAL,
    body: "Long-running work keeps its state, advances as events arrive, and compensates when a step fails.",
  },
] as const;

function RangeCards() {
  return (
    <div className="mt-12 grid gap-4 md:grid-cols-3">
      {RANGES.map((range) => (
        <article
          key={range.index}
          className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-6 backdrop-blur"
        >
          <div
            aria-hidden="true"
            className="absolute -top-24 right-0 h-56 w-56 opacity-40 blur-3xl"
            style={{
              background: `radial-gradient(closest-side, ${range.color}29, transparent)`,
            }}
          />
          <div className="flex items-baseline justify-between">
            <span className="text-cc-ink-dim font-mono text-xs">
              {range.index}
            </span>
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.16em] uppercase">
              {range.range}
            </span>
          </div>
          <h3 className="font-heading text-cc-heading text-h5 mt-4">
            {range.name}
          </h3>
          <p className="text-cc-ink mt-3 text-sm leading-relaxed">
            {range.body}
          </p>
          <code
            className="border-cc-card-border bg-cc-surface mt-5 inline-block rounded-md border px-2.5 py-1.5 font-mono text-xs"
            style={{ color: range.color }}
          >
            {range.api}
          </code>
        </article>
      ))}
    </div>
  );
}

/* ============================================================================
   04 · Patterns — the message vocabulary
============================================================================ */

const PATTERNS = [
  {
    name: "Event",
    question: "Who needs to know?",
    api: "PublishAsync",
  },
  {
    name: "Send",
    question: "Who should act?",
    api: "SendAsync",
  },
  {
    name: "Request-Reply",
    question: "What is the result?",
    api: "RequestAsync",
  },
] as const;

function PatternCards() {
  return (
    <div className="mt-12 grid gap-4 md:grid-cols-3">
      {PATTERNS.map((pattern) => (
        <article
          key={pattern.name}
          className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-6 backdrop-blur"
        >
          <h3 className="font-heading text-cc-heading text-h6">
            {pattern.name}
          </h3>
          <p className="text-cc-ink-dim mt-1 text-sm">{pattern.question}</p>
          <code className="border-cc-card-border bg-cc-surface text-cc-accent mt-5 inline-block rounded-md border px-2.5 py-1.5 font-mono text-xs">
            {pattern.api}
          </code>
        </article>
      ))}
    </div>
  );
}

/* ============================================================================
   Page
============================================================================ */

export function ClientPage() {
  return (
    <div className="bg-cc-bg relative">
      {/* Hero — the circuit board lives here and only here. */}
      <section className="relative flex min-h-[92svh] items-center overflow-hidden">
        <HeroBoard />
        <div className="relative z-10 mx-auto w-full max-w-6xl px-5 sm:px-12">
          <div className="max-w-xl py-24 xl:max-w-2xl">
            <div className="flex items-center gap-3">
              <span
                aria-hidden="true"
                className="h-px w-12 rounded-full"
                style={{
                  background: `linear-gradient(90deg, ${CORAL}, transparent)`,
                }}
              />
              <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
                Mocha · Messaging for .NET
              </span>
            </div>
            <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-6 text-balance">
              Every app runs on{" "}
              <span
                className="bg-clip-text text-transparent"
                style={{
                  backgroundImage: `linear-gradient(90deg, ${CYAN}, ${CORAL})`,
                }}
              >
                events.
              </span>
            </h1>
            <p className="text-cc-ink mt-6 max-w-xl text-base sm:text-lg">
              A request returns in milliseconds. The work it sets off runs for
              days. Mocha carries that work: commands, events, handlers, and
              sagas, with every hop traced in Nitro.
            </p>
            <div className="mt-9 flex flex-wrap gap-3">
              <SolidButton
                href="/get-started"
                className="bg-[image:linear-gradient(180deg,#f0786a,#d9604f)] !text-white shadow-[0_14px_20px_-12px_rgba(240,120,106,0.5)] ring-1 ring-white/15 ring-inset"
              >
                Start for Free
              </SolidButton>
              <OutlineButton href="/docs/mocha">Read the Docs</OutlineButton>
            </div>
            <div className="text-cc-nav-label mt-12 flex items-center gap-3 font-mono text-[0.65rem] tracking-[0.24em] uppercase">
              <span aria-hidden="true" className="bg-cc-card-border h-px w-8" />
              RabbitMQ · Postgres · In-Process
            </div>
          </div>
        </div>
      </section>

      {/* 01 · Model */}
      <Section>
        <Intro
          index="01"
          eyebrow="Model"
          title="The same message model, three ranges."
          center
        >
          <p>
            A method call, a hop between services, and a process that runs for
            days. One handler-first model spans all three.
          </p>
        </Intro>
        <RevealOnScroll>
          <RangeCards />
        </RevealOnScroll>
      </Section>

      {/* 02 · Dispatch */}
      <Section>
        <Row
          copy={
            <Intro
              index="02"
              eyebrow="Dispatch"
              title="A command dispatches through the in-process mediator."
            >
              <p>
                ISender.Send(CreateReview) lands on a [Handler] method. A source
                generator discovers it at compile time and emits typed
                registration plus a pre-compiled pipeline. Dispatch is a direct
                call, zero-reflection and AOT-friendly, not a reflective lookup
                on the hot path.
              </p>
            </Intro>
          }
          visual={<MediatorVisual />}
        />
      </Section>

      {/* 03 · Broadcast */}
      <Section>
        <Row
          reverse
          copy={
            <Intro
              index="03"
              eyebrow="Broadcast"
              title="The same model crosses service boundaries."
            >
              <p>
                The handler publishes ReviewCreated. The shape that powers an
                in-process notification powers a cross-service consumer without
                changes. PublishAsync and SendAsync read the same whether the
                message stays in-process or rides a transport to another
                service.
              </p>
            </Intro>
          }
          visual={<FanoutVisual />}
        />
      </Section>

      {/* 04 · Patterns */}
      <Section>
        <Intro
          index="04"
          eyebrow="Patterns"
          title="Three patterns, one handler-first model."
          center
        >
          <p>
            The same API covers pub/sub, fire-and-forget, and request/reply. The
            catalog is the Enterprise Integration Patterns, not a bespoke
            vocabulary.
          </p>
        </Intro>
        <RevealOnScroll>
          <PatternCards />
        </RevealOnScroll>
      </Section>

      {/* 05 · Deliver */}
      <Section>
        <Row
          copy={
            <Intro
              index="05"
              eyebrow="Deliver"
              title="At-least-once delivery, exactly-once processing."
            >
              <p>
                Brokers redeliver; pretending otherwise is the bug. The Postgres
                domain write and the ReviewCreated message commit together, and
                the inbox dedupes by message id, so the handler runs once even
                when the broker hands you the same message twice.
              </p>
              <p className="text-cc-ink-dim text-sm">
                That is exactly-once{" "}
                <span style={{ color: GREEN }}>processing</span>, not
                exactly-once delivery, with retry, dead-letter routing, and
                circuit breaker as pipeline middleware.
              </p>
            </Intro>
          }
          visual={<OutboxVisual />}
        />
      </Section>

      {/* 06 · Orchestrate */}
      <Section>
        <Row
          reverse
          copy={
            <Intro
              index="06"
              eyebrow="Orchestrate"
              title="A saga carries the workflow across services."
            >
              <p>
                The review saga is a C# state machine: Draft to Checked to
                Published. At startup Mocha validates the shape, that every
                state is reachable and every path reaches a final state, so a
                workflow cannot silently get stuck on the way to production.
              </p>
            </Intro>
          }
          visual={<SagaVisual />}
        />
      </Section>

      {/* 07 · Observe */}
      <Section>
        <Intro
          index="07"
          eyebrow="Observe"
          title="Every hop is a span in Nitro."
          center
        >
          <p>
            Decoupled work does not have to be a black box. Mocha is
            OpenTelemetry-native: every dispatch, transport hop, and handler
            execution emits spans, and correlation propagates across service
            boundaries. Follow a message from publish to consume as real spans.
          </p>
        </Intro>
        <RevealOnScroll
          className="mt-12"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <TraceBento />
        </RevealOnScroll>
      </Section>

      {/* 08 · Transport */}
      <Section>
        <Row
          copy={
            <Intro
              index="08"
              eyebrow="Transport"
              title="Swap transports without touching handlers."
            >
              <p>
                Lead with RabbitMQ, Postgres, and in-process, and swap them
                without changing a handler. Kafka, Azure Service Bus, and Event
                Hub also exist in source.
              </p>
              <div className="flex flex-wrap gap-2 pt-2">
                {["RabbitMQ", "Postgres", "in-process"].map((transport) => (
                  <code
                    key={transport}
                    className="border-cc-card-border bg-cc-surface text-cc-accent rounded-md border px-2.5 py-1.5 font-mono text-xs"
                  >
                    {transport}
                  </code>
                ))}
              </div>
            </Intro>
          }
          visual={<TransportsVisual />}
        />
      </Section>

      {/* 09 · CTA */}
      <Section>
        <div className="mx-auto max-w-2xl text-center">
          <div
            aria-hidden="true"
            className="mx-auto h-px w-24"
            style={{
              background: `linear-gradient(90deg, ${CYAN}, ${VIOLET}, ${CORAL})`,
            }}
          />
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-10 text-balance">
            Keep the workflow moving without losing the thread.
          </h2>
          <p className="text-cc-ink mt-6">
            Write a handler, attribute it, dispatch it. The bus, the outbox, the
            inbox, the sagas, and the traces are part of the framework, not
            packages you wire yourself.
          </p>
          <div className="mt-9 flex flex-wrap justify-center gap-3">
            <SolidButton
              href="/get-started"
              className="bg-[image:linear-gradient(180deg,#f0786a,#d9604f)] !text-white shadow-[0_14px_20px_-12px_rgba(240,120,106,0.5)] ring-1 ring-white/15 ring-inset"
            >
              Start for Free
            </SolidButton>
            <OutlineButton href="/platform">See the platform</OutlineButton>
          </div>
        </div>
      </Section>
    </div>
  );
}
