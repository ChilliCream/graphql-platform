// Objection-led FAQ for /pricing.
// Features live in the comparison table — these answer "will I get
// surprise-billed", "what's actually free", "what triggers an upgrade".

export interface Faq {
  readonly q: string;
  readonly a: string;
}

export const FAQ: readonly Faq[] = [
  {
    q: "What's actually free, and forever?",
    a: "Hot Chocolate (server), Strawberry Shake (client), Mocha (messaging), and the OSS edition of Fusion are MIT-licensed. You can build, ship, and scale a production GraphQL platform on them without ever opening a ChilliCream account.",
  },
  {
    q: "When do I need Nitro?",
    a: "When you want the schema registry, breaking-change detection in CI, an operations console, OpenTelemetry dashboards retained beyond your own infra, or when you want us to run it for you. Most teams start on Nitro Free the first time they need to track schema versions across environments.",
  },
  {
    q: "Can I get surprise-billed?",
    a: "No. Every Nitro tier has a hard usage cap by default and a configurable budget alert. If you hit the cap, traffic is throttled, never silently overcharged. Pay-as-you-go is opt-in.",
  },
  {
    q: "How is a request counted?",
    a: "One inbound GraphQL operation against your Nitro endpoint counts as one request. Subgraph fan-out from Fusion is not double-billed. Persisted-operation lookups, health checks, and introspection are free.",
  },
  {
    q: "I went over my included quota. What now?",
    a: "If you've enabled pay-as-you-go, overage is billed at the unit price shown in the Compute & throughput row of the comparison table ($0.40 per 1M requests on Nitro Hosted). If you haven't, traffic is throttled at the cap and you get a budget alert. Nothing happens silently.",
  },
  {
    q: "How do I upgrade from Nitro Free to Hosted?",
    a: "From the Nitro console, one click. Your schemas, environments, and history move with you. No export/import, no downtime.",
  },
  {
    q: "Can I run Nitro fully on my own infrastructure?",
    a: "Yes. Nitro Self-Hosted ships as a Helm chart, a Docker image, and an air-gapped tarball. You bring your own database, your own object store, and your own observability backend. We provide the binaries and the license.",
  },
  {
    q: "What's the difference between Nitro Self-Hosted and Enterprise?",
    a: "Self-Hosted is the license that lets you run Nitro on your infrastructure. Enterprise is everything in Self-Hosted plus a dedicated solution architect, 24x7 oncall, custom SLA, federation governance, and procurement-ready compliance evidence (SOC 2, ISO 27001, HIPAA BAA).",
  },
  {
    q: "Do you offer a free trial of Nitro Hosted?",
    a: "Nitro Free is the trial. It's a real plan, not a 14-day timer. Upgrade to Hosted when you need reserved capacity, SSO, or longer retention.",
  },
  {
    q: "I'm a procurement-adjacent reader. What do I send my security team?",
    a: "Email contact@chillicream.com from a corporate domain and we'll share our SOC 2 Type II report, our DPA, our subprocessor list, and answer your security questionnaire under NDA. Most reviews close in under two weeks.",
  },
];
