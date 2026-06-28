import type { Metadata } from "next";

import { MessagingGraphicV6 } from "@/src/components/home/mocha/messaging-graphic/MessagingGraphicV6";

export const metadata: Metadata = {
  title: "Messaging Graphic v6",
  robots: { index: false, follow: false },
};

export default function MessagingGraphicV6Page() {
  return (
    <section className="mx-auto max-w-6xl px-5 pt-16 pb-28 sm:px-12 sm:pt-24">
      <div className="max-w-3xl">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Messaging
        </p>
        <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
          Every app runs on events.
        </h2>
        <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
          Under the request and response, an app is a set of parts reacting to
          each other. Mocha is the messaging that carries those events: an
          in-process mediator, a bus across services, and sagas for the work
          that takes time.
        </p>
      </div>
      <div className="mt-10 sm:mt-12">
        <MessagingGraphicV6 />
      </div>
    </section>
  );
}
