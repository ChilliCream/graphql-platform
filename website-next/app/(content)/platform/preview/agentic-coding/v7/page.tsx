import type { Metadata } from "next";

import { AgenticCodingV7Client } from "./client";

/**
 * Preview variant (v7) of the Agentic coding page. Motion-showcase stance: a
 * scroll-driven "Lifecycle Conveyor" centerpiece glides one tool token
 * (mutation tagProduct) along an SVG path through four stations (Author /
 * Validate / Stage / Trace) while a paired agent terminal animates the
 * matching transcript line, approval gate, and a p95 sparkline in lockstep.
 * Same cc-* dark palette, same brand accents, same fonts as the rest of the
 * site, motion narrates the governed agent-tool lifecycle.
 *
 * This route file stays a Server Component so it can export `metadata`. All
 * motion lives in the colocated client component, which is the single
 * self-contained implementation file for this preview.
 */

export const metadata: Metadata = {
  title: "Agentic Coding: Watch the Governed Tool Lifecycle in Motion",
  description:
    "GraphQL MCP for coding agents. A scroll-driven lifecycle conveyor narrates how a published operation becomes a governed, validated, staged, and traced tool you can ship.",
  keywords: [
    "GraphQL MCP for coding agents",
    "agentic coding feedback loop",
    "operations as MCP tools",
    "agent tool lifecycle governance",
    "MCP behavior annotations",
    "idempotent destructive openWorld hints",
    "client registry grounding for agents",
    "skillz agent conventions",
    "validate MCP tools in CI",
    ".NET GraphQL agents",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Watch the Governed Agent-Tool Lifecycle in Motion",
    description:
      "A scroll-driven lifecycle conveyor narrates GraphQL MCP for coding agents: one operation rides from repo to traced production tool in lockstep with the agent terminal.",
  },
};

export default function AgenticCodingPreviewV7() {
  return <AgenticCodingV7Client />;
}
