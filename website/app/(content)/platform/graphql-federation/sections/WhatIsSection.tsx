import { FeatureRow } from "@/src/components/FeatureRow";
import LayeredDiagram from "@/src/components/LayeredDiagram/index";
import { CLIENT_NODES } from "@/src/components/LayeredDiagram/diagram";
import type {
  Request,
  TierNode,
} from "@/src/components/LayeredDiagram/diagram";
import { specTag } from "@/src/components/LayeredDiagram/palette";

import { FEDERATION_DEFINITION } from "../terms";
import { Section } from "./shared";

const PANEL_CLASS = "border-cc-card-border bg-cc-card-bg rounded-xl border";

/**
 * The generic architecture the specification describes: seven subgraphs
 * behind one gateway, no product name, both federation specs represented
 * (this is the definition drawn, not the Fusion product).
 */
const WHAT_IS_TIERS: readonly TierNode[] = [
  {
    name: "Checkout",
    language: "C#",
    badge: specTag("GraphQL Federation"),
    spec: "GraphQL Federation",
  },
  {
    name: "Catalog",
    language: "TypeScript",
    badge: specTag("GraphQL Federation"),
    spec: "GraphQL Federation",
  },
  {
    name: "Accounts",
    language: "C#",
    badge: specTag("GraphQL Federation"),
    spec: "GraphQL Federation",
  },
  {
    name: "Billing",
    language: "Java",
    badge: specTag("Apollo Federation"),
    spec: "Apollo Federation",
  },
  {
    name: "Ordering",
    language: "Go",
    badge: specTag("GraphQL Federation"),
    spec: "GraphQL Federation",
  },
  {
    name: "Shipping",
    language: "Ruby",
    badge: specTag("GraphQL Federation"),
    spec: "GraphQL Federation",
  },
  {
    name: "Inventory",
    language: "Kotlin",
    badge: specTag("Apollo Federation"),
    spec: "Apollo Federation",
  },
];

const WHAT_IS_REQUESTS: readonly Request[] = [
  {
    client: 0,
    operation: "query Storefront",
    targets: ["Catalog", "Inventory"],
  },
  {
    client: 1,
    operation: "query Checkout",
    targets: ["Checkout", "Catalog", "Billing"],
  },
  {
    client: 2,
    operation: "query Fulfillment",
    targets: ["Ordering", "Shipping"],
  },
  {
    client: 3,
    operation: "query AccountSummary",
    targets: ["Accounts", "Billing"],
  },
];

export function WhatIsSection() {
  return (
    <Section id="what-is">
      <FeatureRow
        title="What is GraphQL Federation?"
        body={
          <>
            {FEDERATION_DEFINITION} Each subgraph contributes part of the
            overall schema, while a GraphQL gateway provides a single entry
            point for clients.
          </>
        }
        visual={
          <div className={`${PANEL_CLASS} overflow-hidden`}>
            <LayeredDiagram
              gatewayLabel="GATEWAY"
              compositionLine="Composite schema · 7 subgraphs · one endpoint"
              clients={CLIENT_NODES}
              tiers={WHAT_IS_TIERS}
              requests={WHAT_IS_REQUESTS}
            />
          </div>
        }
      >
        <div className="text-cc-ink mt-4 space-y-4 text-base">
          <p>
            Instead of maintaining one large GraphQL server, teams can own and
            develop their services independently. Each team defines the types
            and fields its service provides, and a process called schema
            composition brings these definitions together into one coherent
            graph.
          </p>
          <p>
            When a client sends a query, the gateway determines which subgraphs
            are needed, fetches the requested data, and assembles the results
            into a single response. To the client, the entire system behaves
            like one GraphQL API, regardless of how many services are involved.
          </p>
        </div>
      </FeatureRow>
    </Section>
  );
}
