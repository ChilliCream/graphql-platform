import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

// v8 "Crumb Cards": GraphQL-aware snapshot testing for .NET, presented as a wall
// of snapshot file cards that crumble open on hover. The page stays a server
// component so Next.js metadata can be exported here, while the interactive
// surface (motion whileHover, whileInView, useReducedMotion) lives in
// ./ClientPage.tsx behind a "use client" directive.

export const metadata: Metadata = {
  title: "Cookie Crumble: GraphQL-aware snapshot testing for .NET",
  description:
    "Cookie Crumble is the open-source snapshot testing library for .NET with native formatters for Hot Chocolate IExecutionResult and GraphQLHttpResponse.",
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

export default function CookieCrumblePreviewV8Page() {
  return <ClientPage />;
}
