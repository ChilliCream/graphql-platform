import type { Metadata } from "next";

import { GatewayScene } from "./GatewayScene";

export const metadata: Metadata = {
  title: "What is GraphQL Federation?",
  description:
    "GraphQL federation combines the schemas of multiple independent services into one unified graph served at a single endpoint. Learn how composition, entities, and query planning work.",
  robots: { index: false, follow: false },
};

/**
 * v12: hero iteration only. Slower loop with in-scene narration; the
 * captions are the hero's only copy, so there is no eyebrow, subtitle,
 * or CTA around the headline.
 */
export default function FederationGraphicV12Page() {
  return (
    <div className="bg-cc-bg relative">
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
    </div>
  );
}
