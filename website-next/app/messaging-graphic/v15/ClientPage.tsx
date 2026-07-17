/**
 * Mocha messaging page, v15. The circuit-board hero is scoped to the hero and
 * always visible; below it the page opens with a "why messaging" orientation
 * map, then walks the model in docs-accurate sections. Code lives in
 * standalone snippet blocks: one compact card beside the prose below xl, and
 * a full-width multi-panel band (message / dispatch / handler) from xl up.
 * Every section links to its docs chapter, and the animated visuals share one
 * handler idiom (a service is a titled panel, a handler is a boxed row inside
 * it).
 */

import Link from "next/link";
import type { ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { HeroBoard } from "./HeroBoard";
import { CORAL, CYAN, GREEN, TEAL, VIOLET } from "./palette";
import {
  BatchCodeWide,
  BatchSnippet,
  MediatorCodeWide,
  MediatorSnippet,
  PublishCodeWide,
  PublishSnippet,
  RequestCodeWide,
  RequestSnippet,
  ScheduleCodeWide,
  ScheduleSnippet,
  SendCodeWide,
  SendSnippet,
  TopologyCodeWide,
  TopologySnippet,
  TransportsCodeWide,
  TransportsSnippet,
} from "./snippets";
import { BatchVisual } from "./visuals/BatchVisual";
import { FanoutVisual } from "./visuals/FanoutVisual";
import { MediatorVisual } from "./visuals/MediatorVisual";
import { OutboxVisual } from "./visuals/OutboxVisual";
import { QuickstartVisual } from "./visuals/QuickstartVisual";
import { RequestReplyVisual } from "./visuals/RequestReplyVisual";
import { SagaVisual } from "./visuals/SagaVisual";
import { SchedulingVisual } from "./visuals/SchedulingVisual";
import { SendVisual } from "./visuals/SendVisual";
import { TopologyVisual } from "./visuals/TopologyVisual";
import { TraceBento } from "./visuals/TraceBento";
import { TransportsVisual } from "./visuals/TransportsVisual";
import { WhyMessagingVisual } from "./visuals/WhyMessagingVisual";

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
  readonly docs?: string;
  readonly children?: ReactNode;
  readonly center?: boolean;
}

function Intro({ index, eyebrow, title, docs, children, center }: IntroProps) {
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
      {docs && (
        <Link
          href={docs}
          className="text-cc-accent hover:text-cc-accent-hover mt-5 inline-block text-sm no-underline"
        >
          Learn more →
        </Link>
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

/** The compact snippet lives beside the prose below xl; from xl the section
 * shows the full multi-panel code band under the row instead. */
function CompactCode({ children }: { readonly children: ReactNode }) {
  return <div className="xl:hidden">{children}</div>;
}

function WideCode({ children }: { readonly children: ReactNode }) {
  return <div className="mt-10 hidden xl:block">{children}</div>;
}

/* ============================================================================
   Page
============================================================================ */

export function ClientPage() {
  return (
    <div className="bg-cc-bg relative">
      {/* Hero — the circuit board lives here and only here, re-tinted to the
          site's teal signature (coral stays the message accent). */}
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
                  backgroundImage: `linear-gradient(90deg, ${TEAL}, ${CORAL})`,
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
              RabbitMQ · Postgres · In-Memory
            </div>
          </div>
        </div>
      </section>

      {/* 01 · Why messaging — the orientation map. */}
      <Section>
        <Intro
          index="01"
          eyebrow="Why messaging"
          title="The request is the tip of the iceberg."
          docs="/docs/mocha"
          center
        >
          <p>
            A user clicks buy. One service answers the request in milliseconds,
            but billing, inventory, shipping, and search still have work to do.
            In a distributed system the services must talk to each other, and
            messaging is how they do it: events on queues, delivered reliably,
            consumed at each service&apos;s own pace.
          </p>
        </Intro>
        <RevealOnScroll
          className="mt-12"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <WhyMessagingVisual />
        </RevealOnScroll>
      </Section>

      {/* 02 · Quick start */}
      <Section>
        <Row
          copy={
            <Intro
              index="02"
              eyebrow="Quick start"
              title="This is all the code messaging takes."
              docs="/docs/mocha/quick-start"
            >
              <p>
                Any record is a message: no base class, no marker interface. A
                handler is one interface with one method. A source generator
                discovers every handler at compile time and registers them in
                one call, and the analyzer flags a missing or duplicate handler
                before your code ever runs.
              </p>
            </Intro>
          }
          visual={<QuickstartVisual />}
        />
      </Section>

      {/* 03 · Topology */}
      <Section>
        <Row
          reverse
          copy={
            <Intro
              index="03"
              eyebrow="Topology"
              title="The broker topology infers itself."
              docs="/docs/mocha/routing-and-endpoints"
            >
              <p>
                Declare what you handle; Mocha derives the exchanges, queues,
                and bindings, provisions them on the broker, and validates the
                whole picture at startup, before the first message moves. When
                the defaults do not fit, opt out and declare the topology
                yourself, down to the queue arguments.
              </p>
              <CompactCode>
                <TopologySnippet />
              </CompactCode>
            </Intro>
          }
          visual={<TopologyVisual />}
        />
        <WideCode>
          <TopologyCodeWide />
        </WideCode>
      </Section>

      {/* 04 · Mediator */}
      <Section>
        <Row
          copy={
            <Intro
              index="04"
              eyebrow="Mediator"
              title="In process, dispatch is compiled, not reflected."
              docs="/docs/mocha/mediator"
            >
              <p>
                For CQRS inside a service, the mediator dispatches commands and
                queries through a pipeline the source generator emits at build
                time: a direct call with your middleware around it, zero
                reflection on the hot path. If you have used MediatR, the
                concepts are familiar; the dispatch is compiled.
              </p>
              <CompactCode>
                <MediatorSnippet />
              </CompactCode>
            </Intro>
          }
          visual={<MediatorVisual />}
        />
        <WideCode>
          <MediatorCodeWide />
        </WideCode>
      </Section>

      {/* 05 · Broadcast */}
      <Section>
        <Row
          reverse
          copy={
            <Intro
              index="05"
              eyebrow="Broadcast"
              title="Publish once. Every subscriber works at its own pace."
              docs="/docs/mocha/messaging-patterns"
            >
              <p>
                PublishAsync hands OrderPlaced to every service that subscribes,
                each through its own queue. A slow consumer backs up without
                slowing anyone else, and a consumer that is down loses nothing:
                its queue holds the events until it returns.
              </p>
              <CompactCode>
                <PublishSnippet />
              </CompactCode>
            </Intro>
          }
          visual={<FanoutVisual />}
        />
        <WideCode>
          <PublishCodeWide />
        </WideCode>
      </Section>

      {/* 06 · Send */}
      <Section>
        <Row
          copy={
            <Intro
              index="06"
              eyebrow="Send"
              title="Respond now. Do the work later."
              docs="/docs/mocha/messaging-patterns"
            >
              <p>
                SendAsync points a command at exactly one handler and completes
                as soon as the message is handed to the transport. The request
                that triggered it returns immediately; the work rides the queue
                and happens on its own time, with delivery guaranteed by the
                broker, not by the caller staying around.
              </p>
              <CompactCode>
                <SendSnippet />
              </CompactCode>
            </Intro>
          }
          visual={<SendVisual />}
        />
        <WideCode>
          <SendCodeWide />
        </WideCode>
      </Section>

      {/* 07 · Request / reply */}
      <Section>
        <Row
          reverse
          copy={
            <Intro
              index="07"
              eyebrow="Request / reply"
              title="Ask across the broker. Get a typed answer."
              docs="/docs/mocha/messaging-patterns"
            >
              <p>
                RequestAsync sends a request to whichever service handles it and
                returns the typed response: correlation, reply routing, and
                timeouts are the framework&apos;s job, not yours. And a failed
                request comes back as a NotAcknowledgedEvent, not as an endless
                wait.
              </p>
              <CompactCode>
                <RequestSnippet />
              </CompactCode>
            </Intro>
          }
          visual={<RequestReplyVisual />}
        />
        <WideCode>
          <RequestCodeWide />
        </WideCode>
      </Section>

      {/* 08 · Batches */}
      <Section>
        <Row
          copy={
            <Intro
              index="08"
              eyebrow="Batches"
              title="When throughput matters, consume in batches."
              docs="/docs/mocha/handlers-and-consumers"
            >
              <p>
                A batch handler receives up to a hundred messages in one call.
                Size and timeout are configurable, the batch flushes when either
                is reached, and one database round-trip replaces a hundred. The
                handler shape stays the same: one interface, one method.
              </p>
              <CompactCode>
                <BatchSnippet />
              </CompactCode>
            </Intro>
          }
          visual={<BatchVisual />}
        />
        <WideCode>
          <BatchCodeWide />
        </WideCode>
      </Section>

      {/* 09 · Schedule */}
      <Section>
        <Row
          reverse
          copy={
            <Intro
              index="09"
              eyebrow="Schedule"
              title="Deliver it later. To the minute."
              docs="/docs/mocha/scheduling"
            >
              <p>
                SchedulePublishAsync holds a message until an absolute time and
                releases it into the queue on the dot, cancellable until the
                moment it dispatches. Scheduling is native where the transport
                supports it, and falls back to a Postgres-backed scheduler where
                it does not; your code reads the same either way.
              </p>
              <CompactCode>
                <ScheduleSnippet />
              </CompactCode>
            </Intro>
          }
          visual={<SchedulingVisual />}
        />
        <WideCode>
          <ScheduleCodeWide />
        </WideCode>
      </Section>

      {/* 10 · Deliver */}
      <Section>
        <Row
          copy={
            <Intro
              index="10"
              eyebrow="Deliver"
              title="At-least-once delivery, exactly-once processing."
              docs="/docs/mocha/reliability"
            >
              <p>
                Brokers redeliver; pretending otherwise is the bug. In the
                producing service, the domain write and the outgoing message
                commit in one transaction. In the consuming service, the inbox
                records the message id and the handler runs in another.
                Redelivery is absorbed by the id, not by hope.
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

      {/* 11 · Orchestrate */}
      <Section>
        <Row
          reverse
          copy={
            <Intro
              index="11"
              eyebrow="Orchestrate"
              title="A saga carries the workflow across services."
              docs="/docs/mocha/sagas"
            >
              <p>
                A saga is a C# state machine: subclass Saga&lt;TState&gt; and
                declare transitions with OnEvent, OnRequest, OnTimeout, and
                OnFault. State persists between messages, timeouts fire when the
                next message never arrives, and compensation runs when a step
                fails, so long-running work cannot silently get stuck.
              </p>
            </Intro>
          }
          visual={<SagaVisual />}
        />
      </Section>

      {/* 12 · Observe */}
      <Section>
        <Intro
          index="12"
          eyebrow="Observe"
          title="Every hop is a span in Nitro."
          docs="/docs/mocha/observability"
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

      {/* 13 · Transports */}
      <Section>
        <Row
          copy={
            <Intro
              index="13"
              eyebrow="Transports"
              title="One service. Many transports."
              docs="/docs/mocha/transports"
            >
              <p>
                Transports are not either-or. Take orders over RabbitMQ and
                stream millions of device events in over Event Hub, in the same
                service, with the same handler model. One transport is the
                default; any handler can claim another. RabbitMQ, PostgreSQL,
                in-memory, and Event Hub today, and the list keeps growing.
              </p>
              <CompactCode>
                <TransportsSnippet />
              </CompactCode>
            </Intro>
          }
          visual={<TransportsVisual />}
        />
        <WideCode>
          <TransportsCodeWide />
        </WideCode>
      </Section>

      {/* 14 · CTA */}
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
            Write a handler, publish a message. The bus, the outbox, the inbox,
            the sagas, and the traces are part of the framework, not packages
            you wire yourself.
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
