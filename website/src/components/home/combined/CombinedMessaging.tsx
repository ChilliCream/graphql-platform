import dynamic from "next/dynamic";
import Link from "next/link";

import { ArrowLink } from "@/src/components/ArrowLink";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

// The mocha product page's "why messaging" topology board, reused as-is: one
// request comes in (CLIENT -> ORDERS, answered in milliseconds) while the
// services behind it talk over messaging through buffered queues, with a
// message lighting up its destination on arrival. Client component, loaded as
// its own async chunk below the fold.
const WhyMessagingVisual = dynamic(() =>
  import("@/app/(content)/products/mocha/visuals/WhyMessagingVisual").then(
    (m) => m.WhyMessagingVisual,
  ),
);

/**
 * Combined Messaging section: one compacted take that sells Mocha under a
 * single header, on the PCB band. Instead of a facet-card row it carries ONE
 * wide topology board (the mocha page's WhyMessagingVisual): the request/reply
 * edge answers in milliseconds while the services behind it exchange events
 * through queues. The numbered silkscreen captions on the board narrate the
 * story; the header copy names the configuration behind reliability and tracing.
 * The whole board links to the Mocha product page.
 */
export function CombinedMessaging() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* shared header */}
        <div className="mx-auto max-w-3xl text-center">
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 leading-[1.1] font-semibold text-balance">
            Messaging for work that outlives a request.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            Mocha lets .NET services publish events, send commands, request
            replies, and orchestrate sagas across RabbitMQ, Azure Event Hubs,
            in-memory, and a preview PostgreSQL transport. Configure outbox and
            inbox middleware for effectively exactly-once processing, and enable
            OpenTelemetry to trace message flows.
          </p>
          <ArrowLink href="/products/mocha" className="mt-6">
            Learn more
          </ArrowLink>
        </div>

        {/* one wide topology board instead of a facet row */}
        <Link
          href="/products/mocha"
          aria-label="How services talk over messaging — learn more on the Mocha product page"
          className="mx-auto mt-10 block max-w-5xl no-underline sm:mt-12"
        >
          <WhyMessagingVisual />
        </Link>
      </RevealOnScroll>
    </section>
  );
}
