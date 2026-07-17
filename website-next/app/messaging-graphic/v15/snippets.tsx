import type { ReactNode } from "react";

import { CORAL, CYAN, GREEN, VIOLET } from "./palette";

/* ============================================================================
   Standalone code blocks for the sections. The animated visuals contain no
   code; below xl each section shows one compact snippet beside its prose, and
   from xl up a full-width band shows the same code split into titled panels
   (message / dispatch / handler implementation).
============================================================================ */

const KEYWORDS =
  /^(?:public|sealed|record|class|var|await|new|async|static|int|decimal|bool|return|readonly)$/;

/**
 * Minimal deterministic C# highlighter for these curated snippets: comments,
 * strings, dotted method calls, keywords, and capitalized type names. It is
 * not a parser; the snippets are written to read correctly under these rules.
 */
function hl(code: string): ReactNode[] {
  const out: ReactNode[] = [];
  let key = 0;
  const push = (text: string, color?: string, dim?: boolean) => {
    if (!text) {
      return;
    }
    out.push(
      dim ? (
        <span key={key++} className="text-cc-ink-dim">
          {text}
        </span>
      ) : (
        <span
          key={key++}
          className={color ? "" : "text-cc-ink"}
          style={color ? { color } : undefined}
        >
          {text}
        </span>
      ),
    );
  };
  const plain = (text: string) => {
    // Identifiers read as ink, punctuation/whitespace as dim.
    const parts = text.split(/([A-Za-z0-9_]+)/);
    for (const part of parts) {
      if (!part) {
        continue;
      }
      if (/^[A-Za-z0-9_]+$/.test(part) && !/^\s+$/.test(part)) {
        push(part);
      } else {
        push(part, undefined, true);
      }
    }
  };
  const token =
    /(\/\/[^\n]*)|("(?:[^"\\]|\\.)*")|(\.)([A-Z][A-Za-z0-9_]*)(?=\s*[(<])|(\.)([A-Z][A-Za-z0-9_]*)|\b([a-z]+)\b|([A-Z][A-Za-z0-9_]*)/g;
  let last = 0;
  for (const m of code.matchAll(token)) {
    plain(code.slice(last, m.index));
    last = m.index + m[0].length;
    if (m[1]) {
      push(m[1], undefined, true); // comment
    } else if (m[2]) {
      push(m[2], GREEN); // string
    } else if (m[4]) {
      push(m[3], undefined, true); // dot
      push(m[4], CORAL); // method call
    } else if (m[6]) {
      push(m[5], undefined, true); // dot
      push(m[6]); // property access
    } else if (m[7]) {
      if (KEYWORDS.test(m[7])) {
        push(m[7], VIOLET);
      } else {
        push(m[7]);
      }
    } else if (m[8]) {
      push(m[8], CYAN); // type name
    }
  }
  plain(code.slice(last));
  return out;
}

interface CodeCardProps {
  readonly code: string;
  readonly title?: string;
}

function CodeCard({ code, title }: CodeCardProps) {
  return (
    <div>
      {title && (
        <div className="text-cc-nav-label mb-2 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          {title}
        </div>
      )}
      <div
        className="border-cc-card-border/60 overflow-x-auto rounded-lg border bg-black/30 p-4 font-mono text-[12.5px] leading-[1.7]"
        style={{ scrollbarWidth: "none" }}
      >
        <div className="whitespace-pre">{hl(code)}</div>
      </div>
    </div>
  );
}

interface WideGridProps {
  readonly three?: boolean;
  readonly children: ReactNode;
}

/** From xl the code band shows 2 panels side by side; 3-panel bands widen to
 * a full 3-up row on very wide screens (the third panel spans below xl-2col).
 */
function WideGrid({ three, children }: WideGridProps) {
  return (
    <div
      className={`grid grid-cols-1 gap-4 xl:grid-cols-2 ${three ? "min-[1900px]:grid-cols-3" : ""}`}
    >
      {children}
    </div>
  );
}

function SpanLast({ children }: { readonly children: ReactNode }) {
  return (
    <div className="min-[1900px]:col-span-1 xl:col-span-2">{children}</div>
  );
}

/* ============================================================================
   Mediator
============================================================================ */

const MEDIATOR_COMMAND = `public record PlaceOrderCommand(
    Guid ProductId, int Quantity)
    : ICommand<PlaceOrderResult>;`;

const MEDIATOR_DISPATCH = `var result = await sender.SendAsync(
    new PlaceOrderCommand(productId, 2), ct);`;

const MEDIATOR_HANDLER = `public class PlaceOrderCommandHandler(AppDbContext db)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async ValueTask<PlaceOrderResult> HandleAsync(
        PlaceOrderCommand command, CancellationToken ct)
    {
        // create the order, return the result
    }
}`;

export function MediatorSnippet() {
  return <CodeCard code={`${MEDIATOR_COMMAND}\n\n${MEDIATOR_DISPATCH}`} />;
}

export function MediatorCodeWide() {
  return (
    <WideGrid three>
      <CodeCard title="The command" code={MEDIATOR_COMMAND} />
      <CodeCard title="Dispatch" code={MEDIATOR_DISPATCH} />
      <SpanLast>
        <CodeCard title="The handler" code={MEDIATOR_HANDLER} />
      </SpanLast>
    </WideGrid>
  );
}

/* ============================================================================
   Broadcast
============================================================================ */

const PUBLISH_EVENT = `public sealed record OrderPlaced(
    Guid OrderId, decimal Amount);`;

const PUBLISH_CALL = `await bus.PublishAsync(orderPlaced, ct);`;

const PUBLISH_HANDLER = `public class OrderPlacedHandler(AppDbContext db)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message, CancellationToken ct)
    {
        // react on this service's schedule
    }
}`;

export function PublishSnippet() {
  return <CodeCard code={PUBLISH_CALL} />;
}

export function PublishCodeWide() {
  return (
    <WideGrid three>
      <CodeCard title="The event" code={PUBLISH_EVENT} />
      <CodeCard title="Publish" code={PUBLISH_CALL} />
      <SpanLast>
        <CodeCard title="A subscriber" code={PUBLISH_HANDLER} />
      </SpanLast>
    </WideGrid>
  );
}

/* ============================================================================
   Send
============================================================================ */

const SEND_COMMAND = `public sealed record ReserveInventoryCommand(
    Guid OrderId);`;

const SEND_CALL = `await bus.SendAsync(
    new ReserveInventoryCommand(orderId), ct);`;

const SEND_HANDLER = `public class ReserveInventoryHandler(Warehouse wh)
    : IEventRequestHandler<ReserveInventoryCommand>
{
    public async ValueTask HandleAsync(
        ReserveInventoryCommand command,
        CancellationToken ct)
    {
        // runs later, on the queue's time
    }
}`;

export function SendSnippet() {
  return <CodeCard code={SEND_CALL} />;
}

export function SendCodeWide() {
  return (
    <WideGrid three>
      <CodeCard title="The command" code={SEND_COMMAND} />
      <CodeCard title="Send" code={SEND_CALL} />
      <SpanLast>
        <CodeCard title="The handler" code={SEND_HANDLER} />
      </SpanLast>
    </WideGrid>
  );
}

/* ============================================================================
   Request / reply
============================================================================ */

const REQUEST_MESSAGE = `public sealed record GetProductRequest(Guid Id)
    : IEventRequest<ProductResponse>;`;

const REQUEST_CALL = `var product = await bus.RequestAsync(
    new GetProductRequest(id), ct);`;

const REQUEST_HANDLER = `public class GetProductHandler(Catalog catalog)
    : IEventRequestHandler<
        GetProductRequest, ProductResponse>
{
    public async ValueTask<ProductResponse> HandleAsync(
        GetProductRequest request, CancellationToken ct)
    {
        // the returned value rides back as the reply
    }
}`;

export function RequestSnippet() {
  return <CodeCard code={REQUEST_CALL} />;
}

export function RequestCodeWide() {
  return (
    <WideGrid three>
      <CodeCard title="The request" code={REQUEST_MESSAGE} />
      <CodeCard title="Ask" code={REQUEST_CALL} />
      <SpanLast>
        <CodeCard title="The handler" code={REQUEST_HANDLER} />
      </SpanLast>
    </WideGrid>
  );
}

/* ============================================================================
   Batches
============================================================================ */

const BATCH_REGISTRATION = `.AddBatchHandler<OrderPlacedHandler>(
    o => o.MaxBatchSize = 100);`;

const BATCH_HANDLER = `public class OrderPlacedHandler(AppDbContext db)
    : IBatchEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        IMessageBatch<OrderPlaced> batch,
        CancellationToken ct)
    {
        // one call, up to 100 messages
    }
}`;

export function BatchSnippet() {
  return <CodeCard code={BATCH_REGISTRATION} />;
}

export function BatchCodeWide() {
  return (
    <WideGrid>
      <CodeCard title="Registration" code={BATCH_REGISTRATION} />
      <CodeCard title="The handler" code={BATCH_HANDLER} />
    </WideGrid>
  );
}

/* ============================================================================
   Scheduling
============================================================================ */

const SCHEDULE_CALL = `var result = await bus.SchedulePublishAsync(
    new SendWelcomeEmail(userId),
    DateTimeOffset.UtcNow.AddMinutes(30), ct);`;

const SCHEDULE_CANCEL = `// still cancellable until it is dispatched
await bus.CancelScheduledMessageAsync(
    result.Token!, ct);`;

export function ScheduleSnippet() {
  return <CodeCard code={SCHEDULE_CALL} />;
}

export function ScheduleCodeWide() {
  return (
    <WideGrid>
      <CodeCard title="Schedule" code={SCHEDULE_CALL} />
      <CodeCard title="Cancel" code={SCHEDULE_CANCEL} />
    </WideGrid>
  );
}

/* ============================================================================
   Topology
============================================================================ */

const TOPOLOGY_DEFAULT = `builder.Services
    .AddMessageBus()
    .AddOrderService()
    .AddRabbitMQ();

// exchanges, queues, and bindings are derived
// from your handlers, validated at startup`;

const TOPOLOGY_OPT_OUT = `transport
    .DeclareExchange("region-events")
    .Type(RabbitMQExchangeType.Topic)
    .Durable();

transport.Queue("orders")
    .BindExplicitly()
    .MaxConcurrency(10);`;

export function TopologySnippet() {
  return <CodeCard code={TOPOLOGY_DEFAULT} />;
}

export function TopologyCodeWide() {
  return (
    <WideGrid>
      <CodeCard title="The default" code={TOPOLOGY_DEFAULT} />
      <CodeCard title="Opt out" code={TOPOLOGY_OPT_OUT} />
    </WideGrid>
  );
}

/* ============================================================================
   Transports
============================================================================ */

const TRANSPORTS_REGISTRATION = `builder.Services
    .AddMessageBus()
    .AddOrderService() // source-generated
    .AddRabbitMQ(t => t.IsDefaultTransport())
    .AddEventHub(t =>
        t.Handler<DeviceTelemetryHandler>());`;

const TRANSPORTS_HANDLER = `public class DeviceTelemetryHandler(Ingest ingest)
    : IEventHandler<DeviceTelemetry>
{
    public async ValueTask HandleAsync(
        DeviceTelemetry message, CancellationToken ct)
    {
        // same shape, different transport
    }
}`;

export function TransportsSnippet() {
  return <CodeCard code={TRANSPORTS_REGISTRATION} />;
}

export function TransportsCodeWide() {
  return (
    <WideGrid>
      <CodeCard title="Registration" code={TRANSPORTS_REGISTRATION} />
      <CodeCard title="The claimed handler" code={TRANSPORTS_HANDLER} />
    </WideGrid>
  );
}
