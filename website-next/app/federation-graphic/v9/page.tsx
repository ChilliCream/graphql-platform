import type { Metadata } from "next";

import { OutlineButton } from "@/src/design-system/Button";

import { GatewayScene } from "./GatewayScene";
import { CYAN, TEAL } from "./palette";

export const metadata: Metadata = {
  title: "What is GraphQL Federation?",
  description:
    "GraphQL federation combines the schemas of multiple independent services into one unified graph served at a single endpoint. Learn how composition, entities, and query planning work.",
  robots: { index: false, follow: false },
};

/**
 * v9: hero iteration only. The rest of the explainer lives in v8; this
 * route exists to iterate on the hero scene in isolation.
 */
export default function FederationGraphicV9Page() {
  return (
    <div className="bg-cc-bg relative">
      <section className="border-cc-card-border relative flex min-h-[92svh] flex-col justify-center overflow-hidden border-b">
        <div className="mx-auto w-full max-w-3xl px-5 pt-20 text-center sm:px-12">
          <div className="flex items-center justify-center gap-3">
            <span
              aria-hidden="true"
              className="h-px w-12 rounded-full"
              style={{
                background: `linear-gradient(90deg, transparent, ${TEAL})`,
              }}
            />
            <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
              Learn · GraphQL Federation
            </span>
            <span
              aria-hidden="true"
              className="h-px w-12 rounded-full"
              style={{
                background: `linear-gradient(90deg, ${TEAL}, transparent)`,
              }}
            />
          </div>
          <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-6 text-balance">
            What is{" "}
            <span
              className="bg-clip-text whitespace-nowrap text-transparent"
              style={{
                backgroundImage: `linear-gradient(90deg, ${TEAL}, ${CYAN})`,
              }}
            >
              GraphQL federation?
            </span>
          </h1>
          <p className="text-cc-ink mx-auto mt-5 max-w-2xl text-base sm:text-lg">
            One query goes in. The gateway plans the steps, runs services in
            parallel where it can, and merges the results into one response.
            Watch it happen:
          </p>
          <div className="mt-8">
            <OutlineButton href="/docs/fusion">
              Read the Fusion Docs
            </OutlineButton>
          </div>
        </div>
        <div className="mx-auto mt-4 w-full max-w-[1480px]">
          <GatewayScene />
        </div>
      </section>
    </div>
  );
}
