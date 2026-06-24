import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

// v9 "Heartbeat of the Suite": GraphQL-aware snapshot testing for .NET, read as
// a calm test runner left running on a second monitor. Status dots, the active
// __mismatch__ step, and a single live-run indicator pulse on time-driven motion
// loops (never scroll-coupled). The page stays a server component so Next.js
// metadata can be exported here, while the interactive surface (motion
// animate={{}} loops, useReducedMotion) lives in ./ClientPage.tsx behind a
// "use client" directive.

export const metadata: Metadata = {
  title: "Cookie Crumble: GraphQL-aware snapshot testing for .NET",
  description:
    "Cookie Crumble is the open-source snapshot testing library for .NET with native formatters for Hot Chocolate IExecutionResult and GraphQLHttpResponse across xUnit, NUnit, TUnit, and MSTest.",
  keywords: [
    "Cookie Crumble",
    "snapshot testing",
    ".NET testing",
    "GraphQL testing",
    "Hot Chocolate testing",
    "IExecutionResult",
    "GraphQLHttpResponse",
    "MatchSnapshot",
    "MatchInlineSnapshot",
    "MatchMarkdownSnapshot",
    "xUnit",
    "NUnit",
    "TUnit",
    "MSTest",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Cookie Crumble: GraphQL-aware snapshot testing for .NET",
    description:
      "Snapshot testing with native formatters for IExecutionResult and GraphQLHttpResponse. Inline, file, and Markdown snapshots. MIT-licensed.",
    type: "website",
  },
};

export default function CookieCrumblePreviewV9Page() {
  return <ClientPage />;
}
