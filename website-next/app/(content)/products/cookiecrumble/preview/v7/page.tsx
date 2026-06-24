import type { Metadata } from "next";

import { CookieCrumbleV7View } from "./view";

// v7 "Mismatch Reel": GraphQL snapshot testing .NET, presented as a motion
// showcase. The page itself stays a server component so Next.js metadata can be
// exported here, while the entire interactive surface (motion hooks, scroll
// progress, in-view gating, reduced-motion fallbacks) lives in ./view.tsx with
// "use client" at the top.

export const metadata: Metadata = {
  title: "Cookie Crumble: GraphQL-aware snapshot testing for .NET",
  description:
    "GraphQL snapshot testing .NET, shown live: a failing Cookie Crumble snapshot is captured to __mismatch__, diffed line by line, then accepted into __snapshots__.",
  keywords: [
    "Cookie Crumble",
    "GraphQL snapshot testing .NET",
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
      "GraphQL snapshot testing .NET shown as motion: capture to __mismatch__, diff line by line, accept into __snapshots__. MIT licensed.",
    type: "website",
  },
};

export default function CookieCrumblePreviewV7Page() {
  return <CookieCrumbleV7View />;
}
