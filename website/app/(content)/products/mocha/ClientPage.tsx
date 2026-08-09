"use client";

import dynamic from "next/dynamic";
import type { ReactNode } from "react";

import { ArrowLink } from "@/src/components/ArrowLink";
import { ButtonRow } from "@/src/components/ButtonRow";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { HeroBoard } from "@/src/components/mocha/HeroBoard";
import {
  CORAL,
  CYAN,
  GREEN,
  TEAL,
  VIOLET,
} from "@/src/components/mocha/palette";
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

const WhyMessagingVisual = dynamic(() =>
  import("./visuals/WhyMessagingVisual").then((m) => m.WhyMessagingVisual),
);
const QuickstartVisual = dynamic(() =>
  import("./visuals/QuickstartVisual").then((m) => m.QuickstartVisual),
);
const TopologyVisual = dynamic(() =>
  import("./visuals/TopologyVisual").then((m) => m.TopologyVisual),
);
const MediatorVisual = dynamic(() =>
  import("./visuals/MediatorVisual").then((m) => m.MediatorVisual),
);
const FanoutVisual = dynamic(() =>
  import("./visuals/FanoutVisual").then((m) => m.FanoutVisual),
);
const SendVisual = dynamic(() =>
  import("./visuals/SendVisual").then((m) => m.SendVisual),
);
const RequestReplyVisual = dynamic(() =>
  import("./visuals/RequestReplyVisual").then((m) => m.RequestReplyVisual),
);
const BatchVisual = dynamic(() =>
  import("./visuals/BatchVisual").then((m) => m.BatchVisual),
);
const SchedulingVisual = dynamic(() =>
  import("./visuals/SchedulingVisual").then((m) => m.SchedulingVisual),
);
const OutboxVisual = dynamic(() =>
  import("./visuals/OutboxVisual").then((m) => m.OutboxVisual),
);
const SagaVisual = dynamic(() =>
  import("./visuals/SagaVisual").then((m) => m.SagaVisual),
);
const TraceBento = dynamic(() =>
  import("./visuals/TraceBento").then((m) => m.TraceBento),
);
const TransportsVisual = dynamic(() =>
  import("./visuals/TransportsVisual").then((m) => m.TransportsVisual),
);

const DOC_LINK_LABELS: Record<string, string> = {
  "/docs/mocha": "See how messaging works",
  "/docs/mocha/quick-start": "Open the quickstart",
  "/docs/mocha/routing-and-endpoints": "See routing and endpoints",
  "/docs/mocha/mediator": "Read about the mediator",
  "/docs/mocha/messaging-patterns": "See messaging patterns",
  "/docs/mocha/handlers-and-consumers": "Read about batch handlers",
  "/docs/mocha/scheduling": "Read about scheduling",
  "/docs/mocha/reliability": "Read about reliability",
  "/docs/mocha/sagas": "Read about sagas",
  "/docs/mocha/observability": "Set up observability",
  "/docs/mocha/transports": "Compare transports",
};

const SOLID_BUTTON_GRADIENT_CLASSNAME =
  "bg-[image:linear-gradient(180deg,#f0786a,#d9604f)] !text-white shadow-[0_14px_20px_-12px_rgba(240,120,106,0.5)] ring-1 ring-white/15 ring-inset";

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

interface IntroProps {
  readonly title: ReactNode;
  readonly docs?: string;
  readonly children?: ReactNode;
  readonly center?: boolean;
}

function Intro({ title, docs, children, center }: IntroProps) {
  return (
    <div className={center ? "mx-auto max-w-2xl text-center" : "max-w-xl"}>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
        {title}
      </h2>
      {children && (
        <div className="text-cc-ink text-body mt-5 space-y-4">{children}</div>
      )}
      {docs && (
        <ArrowLink
          href={docs}
          aria-label={
            typeof title === "string"
              ? `Read Mocha documentation: ${title}`
              : "Read the Mocha documentation"
          }
          className="mt-5"
        >
          {DOC_LINK_LABELS[docs] ?? "Read the Mocha docs"}
        </ArrowLink>
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

interface CompactCodeProps {
  readonly children: ReactNode;
}

function CompactCode({ children }: CompactCodeProps) {
  return <div className="xl:hidden">{children}</div>;
}

interface WideCodeProps {
  readonly children: ReactNode;
}

function WideCode({ children }: WideCodeProps) {
  return <div className="mt-10 hidden xl:block">{children}</div>;
}

export function ClientPage() {
  return (
    <div className="bg-cc-bg relative left-1/2 isolate -mt-26 -mb-18 w-screen -translate-x-1/2 overflow-hidden">
      <section className="relative flex min-h-[92svh] items-center overflow-hidden">
        <HeroBoard />
        <div className="relative z-10 mx-auto w-full max-w-6xl px-5 sm:px-12">
          <div className="max-w-xl py-24 xl:max-w-2xl">
            <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 text-balance">
              Messaging for .NET,{" "}
              <span
                className="bg-clip-text text-transparent"
                style={{
                  backgroundImage: `linear-gradient(90deg, ${TEAL}, ${CORAL})`,
                }}
              >
                within and between services.
              </span>
            </h1>
            <p className="text-cc-ink mt-6 max-w-xl text-base sm:text-lg">
              Mocha sends commands and events between your services, for example
              telling the shipping service that an order was placed. It also
              runs commands and queries inside a single service, with no broker
              involved. Define messages and handlers in C#, and Mocha generates
              the handler registration at build time.
            </p>
            <ButtonRow align="start" className="mt-9">
              <SolidButton
                href="/docs/mocha/quick-start"
                className={SOLID_BUTTON_GRADIENT_CLASSNAME}
              >
                Publish your first message
              </SolidButton>
              <OutlineButton href="/docs/mocha">Read the docs</OutlineButton>
            </ButtonRow>
          </div>
        </div>
      </section>

      <Section>
        <Intro
          title="Answer the caller now. Finish the rest in the background."
          docs="/docs/mocha"
          center
        >
          <p>
            When a customer checks out, the order service can confirm the order
            right away. Billing, inventory, shipping, and search keep working in
            the background, each at its own pace. Mocha gives you two ways to do
            this: send a message you don&apos;t need to wait for, or send a
            request and wait for the reply.
          </p>
        </Intro>
        <RevealOnScroll
          className="mt-12"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <WhyMessagingVisual />
        </RevealOnScroll>
      </Section>

      <Section>
        <Row
          copy={
            <Intro
              title="Start with a message and a handler."
              docs="/docs/mocha/quick-start"
            >
              <p>
                Messages in Mocha are represented as C# records and processed by
                handler classes. Define the message, implement its handler
                interface, and Mocha takes care of registering the handler at
                build time.
              </p>
              <p>
                Its analyzers also detect invalid or duplicate handlers before
                the application starts.
              </p>
            </Intro>
          }
          visual={<QuickstartVisual />}
        />
      </Section>

      <Section>
        <Row
          reverse
          copy={
            <Intro
              title="You write the code. Mocha wires up the broker."
              docs="/docs/mocha/routing-and-endpoints"
            >
              <p>
                Mocha can automatically configure the messaging infrastructure
                your services need. Based on the messages they send and receive,
                it creates the appropriate routes and transport resources. This
                configuration is called the topology.
              </p>
              <p>
                Mocha sets up the topology at startup, so naming and
                configuration conflicts surface early. You can also define the
                transport configuration yourself when you need more control.
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

      <Section>
        <Row
          copy={
            <Intro
              title="Mocha is also a mediator."
              docs="/docs/mocha/mediator"
            >
              <p>
                Mocha does more than move messages between services. Its
                mediator dispatches commands and queries inside your own
                process, with no broker involved. It builds the dispatch
                pipeline at build time and runs your middleware around each
                handler, so the hot path avoids reflection.
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

      <Section>
        <Row
          reverse
          copy={
            <Intro
              title="Publish an event. Each subscriber handles it at its own pace."
              docs="/docs/mocha/messaging-patterns"
            >
              <p>
                Each subscriber gets its own durable queue, set up before you
                start publishing. From there, it processes events at its own
                pace and picks up where it left off after a restart. How
                reliably messages are delivered, and how long they&apos;re kept,
                depends on the transport (in-memory, broker-backed, or
                database-backed) and the topology you configure.
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

      <Section>
        <Row
          copy={
            <Intro
              title="Hand off a command without waiting for the handler."
              docs="/docs/mocha/messaging-patterns"
            >
              <p>
                Use a command when one service needs to do work without making
                the caller wait for it to finish. Mocha routes the command to
                its handler. You choose the transport and reliability settings
                that fit the job.
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

      <Section>
        <Row
          reverse
          copy={
            <Intro
              title="Wait for a typed response from another service."
              docs="/docs/mocha/messaging-patterns"
            >
              <p>
                Use request/reply when a caller needs an answer from one service
                and can wait for it. Mocha matches the reply to the original
                request and returns a typed response, or times out if none
                arrives. If the handler fails, it surfaces the error through the
                same reply channel.
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

      <Section>
        <Row
          copy={
            <Intro
              title="Process messages in batches."
              docs="/docs/mocha/handlers-and-consumers"
            >
              <p>
                When several messages can be processed together, batching can
                reduce the amount of work your application has to do. Mocha
                collects messages until the batch reaches a configured size or a
                timeout expires, then passes them to the handler in a single
                call.
              </p>
              <p>
                This is especially useful for bulk operations, such as writing
                many records to a database at once instead of issuing a separate
                request for each message.
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

      <Section>
        <Row
          reverse
          copy={
            <Intro
              title="Publish a message at a time you choose."
              docs="/docs/mocha/scheduling"
            >
              <p>
                Sometimes you don&apos;t want to process a message immediately.
                Mocha lets you schedule a message for delivery at a specific
                time or after a delay, making it useful for reminders, delayed
                retries, and other deferred work.
              </p>
              <p>
                Whether a scheduled message survives restarts or can be
                cancelled depends on the scheduling store that holds it until it
                is due.
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

      <Section>
        <Row
          copy={
            <Intro
              title="Avoid duplicate writes when a broker redelivers a message."
              docs="/docs/mocha/reliability"
            >
              <p>
                Message brokers may deliver the same message more than once,
                such as after a crash or a lost acknowledgment. Mocha prevents
                those redeliveries from repeating the same work by recording
                which messages have already been processed.
              </p>
              <p className="text-cc-ink-dim text-caption">
                The inbox and the handler&apos;s changes are committed together,
                while any outgoing messages are held until that transaction
                succeeds. For database operations, this provides effectively
                exactly-once <span style={{ color: GREEN }}>processing</span>{" "}
                for as long as the inbox record is retained.
              </p>
            </Intro>
          }
          visual={<OutboxVisual />}
        />
      </Section>

      <Section>
        <Row
          reverse
          copy={
            <Intro
              title="Keep long-running workflows in one state machine."
              docs="/docs/mocha/sagas"
            >
              <p>
                Some workflows cannot be completed by a single message. They
                unfold over several steps and may need to react to events,
                failures, or timeouts along the way.
              </p>
              <p>
                A saga keeps track of that progress and determines what should
                happen next. Its state can be stored in memory or persisted when
                the workflow needs to survive restarts.
              </p>
              <p>
                If part of the workflow fails, the saga can trigger compensating
                actions to undo earlier steps. Timeout handling requires a
                scheduling store that supports delayed messages.
              </p>
            </Intro>
          }
          visual={<SagaVisual />}
        />
      </Section>

      <Section>
        <Intro
          title="See where a message went and what handled it."
          docs="/docs/mocha/observability"
          center
        >
          <p>
            When a message passes through several services, it can be difficult
            to see where time was spent or where something went wrong. Mocha
            integrates with OpenTelemetry to connect dispatch, receive, and
            handler activity into a single trace that follows the message across
            the system.
          </p>
          <p>
            Those traces can be sent to Nitro, where you can inspect slow
            handlers, failed messages, and delays between services.
          </p>
        </Intro>
        <RevealOnScroll
          className="mt-12"
          hiddenClassName="translate-y-8 opacity-0"
        >
          <TraceBento />
        </RevealOnScroll>
      </Section>

      <Section>
        <Row
          copy={
            <Intro
              title="Separate application logic from infrastructure."
              docs="/docs/mocha/transports"
            >
              <p>
                Your messaging infrastructure will often change as your
                application grows. You might start with an in-memory transport
                during development, move to a database-backed option for simpler
                deployments, or use a message broker in a distributed system.
              </p>
              <p>
                Mocha keeps those infrastructure choices out of your handlers.
                The same message-handling code works across transports, while
                each transport provides its own delivery guarantees, durability,
                scheduling support, and routing model.
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
            Publish your first message with Mocha.
          </h2>
          <p className="text-cc-ink mt-6">
            You can start with a single process using the in-memory transport, a
            message, and a handler. As your application grows, switching to a
            broker-backed or database-backed transport doesn&apos;t require
            changing your handlers. You simply gain the delivery, durability,
            and reliability features your application needs.
          </p>
          <ButtonRow className="mt-9">
            <SolidButton
              href="/docs/mocha/quick-start"
              className={SOLID_BUTTON_GRADIENT_CLASSNAME}
            >
              Publish your first message
            </SolidButton>
            <OutlineButton href="/docs/mocha">Read the docs</OutlineButton>
          </ButtonRow>
        </div>
      </Section>
    </div>
  );
}
