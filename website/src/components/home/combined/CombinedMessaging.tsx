import dynamic from "next/dynamic";
import Link from "next/link";

import { ArrowLink } from "@/src/components/ArrowLink";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

const WhyMessagingVisual = dynamic(() =>
  import("@/app/(content)/products/mocha/visuals/WhyMessagingVisual").then(
    (m) => m.WhyMessagingVisual,
  ),
);

export function CombinedMessaging() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
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
            Explore Mocha messaging
          </ArrowLink>
        </div>

        <Link
          href="/products/mocha"
          aria-label="Explore .NET messaging on the Mocha product page"
          className="mx-auto mt-10 block max-w-5xl no-underline sm:mt-12"
        >
          <WhyMessagingVisual />
        </Link>
      </RevealOnScroll>
    </section>
  );
}
