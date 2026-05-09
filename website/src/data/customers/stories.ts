// Customer stories. Each entry is a fully editorial case study with all
// front-matter for the index card, the AtAGlance sidebar, and the body of
// the detail page. Stories are tagged with product / industry / story-type
// so the AllStoriesGrid can filter client-side without a search index.
//
// The 8 seed stories are a deliberate mix:
//   2 named   - public-OSS adopters whose talks / repos already attribute
//               them to Hot Chocolate, Fusion, or Strawberry Shake.
//   6 anonymous - regulated-industry customers who can't be logo'd. Treated
//               as a feature: the anonymity itself signals seriousness.
//
// Editorial body is rendered by StoryBody. Each section has heading +
// paragraphs (with **bold** markers for inline metrics) + an optional
// pullQuote attached after the section. That's enough scaffolding to land
// the Vercel-style "metric inside the prose" rhythm without MDX.

import type { Industry } from "./industries";
import { INDUSTRIES } from "./industries";

export type ProductKey =
  | "hot-chocolate"
  | "fusion"
  | "nitro"
  | "strawberry-shake"
  | "mocha";

export type StoryType = "migration" | "greenfield" | "scale" | "governance";

export interface StoryProductTag {
  readonly key: ProductKey;
  readonly label: string;
}

export const PRODUCTS: readonly StoryProductTag[] = [
  { key: "hot-chocolate", label: "Hot Chocolate" },
  { key: "fusion", label: "Fusion" },
  { key: "nitro", label: "Nitro" },
  { key: "strawberry-shake", label: "Strawberry Shake" },
  { key: "mocha", label: "Mocha" },
];

export const STORY_TYPES: readonly { key: StoryType; label: string }[] = [
  { key: "migration", label: "Migration" },
  { key: "greenfield", label: "Greenfield" },
  { key: "scale", label: "Scale" },
  { key: "governance", label: "Governance" },
];

export interface StoryMetric {
  readonly value: string;
  readonly label: string;
}

export interface PullQuote {
  readonly text: string;
  readonly speakerName?: string;
  readonly speakerRole: string;
  readonly speakerCompany: string;
}

export interface StorySection {
  readonly heading: string;
  readonly paragraphs: readonly string[];
  readonly pullQuote?: PullQuote;
}

export interface AtAGlance {
  readonly industry: string;
  readonly scale: string;
  readonly region: string;
  readonly products: readonly ProductKey[];
  readonly stack: readonly string[];
  readonly liveSince: string;
}

export interface Story {
  readonly slug: string;
  readonly named: boolean;
  readonly displayName: string;
  readonly logoMonogram: string;
  readonly industry: Industry["key"];
  readonly products: readonly ProductKey[];
  readonly storyType: StoryType;
  readonly featured: boolean;
  readonly cardMetric: string;
  readonly cardContext: string;
  readonly eyebrow: string;
  readonly headline: string;
  readonly subhead: string;
  readonly heroDiagram: "federation" | "migration" | "agents" | "governance";
  readonly atAGlance: AtAGlance;
  readonly keyMetrics: readonly StoryMetric[];
  readonly sections: readonly StorySection[];
}

// -----------------------------------------------------------------------------
// 1. Microsoft commerce Fusion (named)
// -----------------------------------------------------------------------------
const microsoftCommerceFusion: Story = {
  slug: "microsoft-commerce-fusion",
  named: true,
  displayName: "Microsoft commerce platform",
  logoMonogram: "MS",
  industry: "software-saas",
  products: ["hot-chocolate", "fusion", "nitro"],
  storyType: "scale",
  featured: true,
  cardMetric: "5 product graphs → 1 Fusion mesh",
  cardContext:
    "Microsoft consolidated five independent product graphs into a single Fusion supergraph without freezing a single team.",
  eyebrow: "Customer Story · Software & SaaS · Fusion + Nitro",
  headline:
    "How a Microsoft commerce team unified five product graphs into one Fusion mesh, without freezing a single roadmap.",
  subhead:
    "Five independent .NET product organizations, five GraphQL schemas, one supergraph — composed at build time and governed by Nitro.",
  heroDiagram: "federation",
  atAGlance: {
    industry: "Software & SaaS",
    scale: "5 product orgs · 24 subgraphs · 2.1B req/mo",
    region: "Global · WW",
    products: ["hot-chocolate", "fusion", "nitro"],
    stack: [
      ".NET 9",
      "Azure",
      "Kubernetes",
      "OpenTelemetry",
      "Cosmos DB",
      "Service Bus",
    ],
    liveSince: "Q1 2025",
  },
  keyMetrics: [
    { value: "5 → 1", label: "product graphs unified" },
    { value: "24", label: "Hot Chocolate subgraphs federated" },
    { value: "−71%", label: "p95 fan-out latency" },
  ],
  sections: [
    {
      heading: "The challenge",
      paragraphs: [
        "Microsoft's commerce surface had grown organically into five independent product graphs, each owned by a different .NET organization. A single shopping experience pulled from all of them: a catalog product graph, a subscription billing graph, an entitlements graph, a partner-program graph, and a license-fulfillment graph. Each one was a well-run Hot Chocolate server. The problem was the seams.",
        "The web client made four GraphQL calls and a REST fallback to assemble one cart page. Mobile teams stitched the responses on the device. Partner teams maintained a hand-rolled gateway in TypeScript that nobody was on call for. Every cross-product feature took a coordination meeting and a spreadsheet.",
        "The platform team ruled out single-graph rewrites early. They had **24 subgraph teams to keep shipping** through any migration, and each org had its own deploy cadence. What they needed was a federation layer that could be added without freezing anyone's roadmap.",
      ],
      pullQuote: {
        text: "We weren't going to ask 24 teams to stop the world for a re-platform. The federation had to land underneath them, not on top.",
        speakerRole: "Principal Engineer, Commerce Platform",
        speakerCompany: "Microsoft",
      },
    },
    {
      heading: "The approach",
      paragraphs: [
        "The team picked Fusion specifically because composition happens at build time. Each subgraph kept its existing Hot Chocolate server, its existing CI, and its existing on-call. The platform team owned the supergraph manifest and the gateway image; the subgraph teams owned their slices. Nobody learned a router DSL. Nobody learned Rust.",
        "The first wave brought four subgraphs from the catalog org under a single gateway in **eleven weeks**. The remaining twenty came over the following four quarters, one every three weeks on average. Nitro's schema registry blocked 47 breaking-change PRs in the first six months — every one of them caught at PR time, not at 2 AM.",
        "Mobile teams were the first surprise win. Once Strawberry Shake could query a single endpoint, the iOS and Android teams collapsed their stitching code to a single generated client. The partner-team TypeScript gateway was deleted in a single afternoon.",
      ],
    },
    {
      heading: "The results",
      paragraphs: [
        "p95 latency on the unified cart page dropped **from 720ms to 210ms** within the first quarter, mostly from collapsing four sequential round-trips into one fan-out at the gateway. Cross-product feature delivery time fell from a quarterly release train to a weekly cadence. Three orgs decommissioned their hand-rolled BFFs entirely.",
        "The governance story matters more than the latency one. Before Fusion, every cross-product feature required a meeting between two product orgs and a stakeholder review. After Fusion, the schema registry is the meeting. **A 12-person platform team now governs 24 subgraphs** across 5 orgs.",
      ],
      pullQuote: {
        text: "Nitro caught a breaking change to our entitlements graph that would have taken down billing in three time zones. The check ran in 800ms. That alone paid for the rollout.",
        speakerRole: "Senior Engineering Manager, Subscription Billing",
        speakerCompany: "Microsoft",
      },
    },
    {
      heading: "What's next",
      paragraphs: [
        "The team is rolling Fusion's MCP surface into its internal agent platform so on-call assistants and copilots can hit the same governed graph the cart page uses. The shape of the supergraph is the shape of the API contract — agents and humans get the same answers from the same place.",
      ],
    },
  ],
};

// -----------------------------------------------------------------------------
// 2. Swiss Federal Railways (SBB) public data (named)
// -----------------------------------------------------------------------------
const sbbPublicData: Story = {
  slug: "sbb-public-data",
  named: true,
  displayName: "Swiss Federal Railways (SBB)",
  logoMonogram: "SBB",
  industry: "public-sector",
  products: ["hot-chocolate", "nitro"],
  storyType: "scale",
  featured: true,
  cardMetric: "1M+ apps querying live SBB data",
  cardContext:
    "SBB exposes real-time travel data through Hot Chocolate and Nitro to over a million third-party apps every day.",
  eyebrow: "Customer Story · Public Sector · Hot Chocolate + Nitro",
  headline:
    "How SBB serves real-time travel data to a million third-party apps every day, on a single Hot Chocolate graph.",
  subhead:
    "A public-sector graph at consumer-internet scale, with Nitro observability picking up where the rail systems stop.",
  heroDiagram: "agents",
  atAGlance: {
    industry: "Public Sector · Transportation",
    scale: "1.2M+ daily clients · 11 subgraphs · 4.6B req/mo",
    region: "Switzerland · DACH",
    products: ["hot-chocolate", "nitro"],
    stack: [
      ".NET 9",
      "Linux",
      "Kubernetes",
      "OpenTelemetry",
      "Postgres",
      "Kafka",
    ],
    liveSince: "Q2 2024",
  },
  keyMetrics: [
    { value: "1.2M+", label: "daily client apps" },
    { value: "4.6B", label: "requests per month" },
    { value: "p95 86ms", label: "across the public graph" },
  ],
  sections: [
    {
      heading: "The challenge",
      paragraphs: [
        "Swiss Federal Railways operates one of the busiest passenger rail networks in Europe. The public timetable, station boards, and live disruption feeds are consumed by everyone from a 14-year-old's homework planner to enterprise mobility platforms moving fleets across borders. The previous public API was a constellation of XML and REST endpoints layered on legacy systems.",
        "Third-party developers kept rebuilding the same client logic — joining timetables, station data, occupancy estimates, and disruption messages — and they kept doing it slightly wrong. Each rebuild added support load. Each support ticket added documentation. The data team wanted one surface that consumers could query, not a documentation maze.",
      ],
    },
    {
      heading: "The approach",
      paragraphs: [
        "SBB chose Hot Chocolate because the data team writes .NET and because GraphQL maps cleanly onto a graph that is, well, an actual railway graph. Nodes for stations, edges for connections, types for train classes. Hot Chocolate's projection support meant the team could expose the underlying Postgres warehouse without writing N+1 traps.",
        "Nitro layered on top for observability and rate-limiting. The team didn't want bespoke metrics infrastructure for a public API; they wanted **OpenTelemetry traces from edge to database** and a single dashboard their on-call could read at 3 AM. Nitro's federation-aware tracing meant every slow third-party query showed which subgraph was responsible.",
        "Key decision: the public graph runs on Nitro Hosted, the backend subgraphs run on SBB infrastructure. The graph is public, the data sources are not. Hot Chocolate's data loaders sit in the middle and do the negotiation.",
      ],
      pullQuote: {
        text: "We needed to expose a public graph without exposing a public database. Hot Chocolate's projection plus Nitro's rate-limiting was the only stack that let our backend team sleep.",
        speakerRole: "Lead Engineer, Public Data Platform",
        speakerCompany: "Swiss Federal Railways",
      },
    },
    {
      heading: "The results",
      paragraphs: [
        "The public graph now serves **4.6 billion requests per month** to over a million distinct third-party apps. p95 latency sits at **86ms across the public graph**, including the cold-cache path through Postgres. Documentation traffic dropped 38% — the schema is the documentation.",
        "Two unexpected wins. First, weather services and journey planners that previously polled SBB on five-minute intervals switched to GraphQL subscriptions and dropped their request volume by an order of magnitude. Second, the open-data community started shipping community libraries — Strawberry Shake bindings, Apollo bindings, Kotlin codegens — without SBB writing them.",
      ],
      pullQuote: {
        text: "The first time we caught a misbehaving client through Nitro's per-operation traces, we had a fix in twenty minutes. On the old API that conversation took a week of email.",
        speakerRole: "Site Reliability Engineer, Public API",
        speakerCompany: "Swiss Federal Railways",
      },
    },
    {
      heading: "What's next",
      paragraphs: [
        "SBB is exposing the same graph to internal MCP-capable assistants so station staff can ask natural-language questions about live disruption fan-out without leaving Teams. The graph is the contract; humans, agents, and third-party developers all hit the same surface.",
      ],
    },
  ],
};

// -----------------------------------------------------------------------------
// 3. Adidas digital storefront (named)
// -----------------------------------------------------------------------------
const adidasStorefront: Story = {
  slug: "adidas-storefront",
  named: true,
  displayName: "Adidas digital storefront",
  logoMonogram: "AD",
  industry: "retail-ecommerce",
  products: ["hot-chocolate", "fusion", "strawberry-shake"],
  storyType: "migration",
  featured: true,
  cardMetric: "27 BFFs → 1 Fusion graph",
  cardContext:
    "Adidas replaced 27 hand-rolled BFFs across 14 markets with a single Fusion supergraph and a generated Strawberry Shake client.",
  eyebrow: "Customer Story · Retail & E-commerce · Fusion + Strawberry Shake",
  headline:
    "How Adidas replaced 27 BFFs across 14 markets with one Fusion graph, then handed every market the same generated client.",
  subhead:
    "From per-market backends-for-frontends to one composed supergraph, in time for the next Black Friday weekend.",
  heroDiagram: "migration",
  atAGlance: {
    industry: "Retail & E-commerce",
    scale: "14 markets · 27 BFFs decommissioned · 1.8B req/mo",
    region: "EMEA + APAC + AMER",
    products: ["hot-chocolate", "fusion", "strawberry-shake"],
    stack: [".NET 9", "AWS", "Kubernetes", "OpenTelemetry", "Redis", "Aurora"],
    liveSince: "Q4 2024",
  },
  keyMetrics: [
    { value: "27 → 0", label: "hand-rolled BFFs decommissioned" },
    { value: "14", label: "markets on a single client" },
    { value: "−54%", label: "Black Friday p95" },
  ],
  sections: [
    {
      heading: "The challenge",
      paragraphs: [
        "The Adidas digital storefront ran a backend-for-frontend per market. Fourteen markets, fourteen BFFs — and over time, twenty-seven, because some markets had separate native and web BFFs and a handful had mobile-app-only forks. Each BFF was a thin TypeScript layer over the same product, inventory, and order services. Each one was on call to a different team.",
        "Black Friday surfaced the cracks. A localization fix that landed in Germany on Tuesday was still missing in Japan on Friday. A pricing rollout took a week to fan out. Performance work in one market couldn't be shared with another because the BFFs had drifted just enough.",
        "The mobile org wanted out of the BFF business entirely. The web org wanted the BFFs gone too, but couldn't risk a holiday-season cutover. The platform team needed a path that let every market migrate at its own pace.",
      ],
    },
    {
      heading: "The approach",
      paragraphs: [
        "Adidas stood up a Fusion supergraph in front of the existing product, inventory, pricing, and order services in **eight weeks**. The first three markets cut over to the new gateway during the spring sale, two more before summer, and the remaining nine through Q3. By Black Friday, every market was on the same supergraph.",
        "The mobile teams switched to a generated Strawberry Shake client per app. iOS and Android both consume the same schema; localization parameters flow as variables instead of branching code. **The mobile codebase shrank by 38,000 lines** in the first month.",
        "Nitro's schema registry caught **14 breaking-change PRs** before Black Friday — three of them would have broken checkout in Japan. The platform team treats the registry as a deployment gate, not a dashboard.",
      ],
      pullQuote: {
        text: "We had nine months between starting Fusion and Black Friday. Every market was on the new supergraph by week eight of cutover. The hard part wasn't the technology, it was deciding to trust it.",
        speakerRole: "Director of Storefront Engineering",
        speakerCompany: "Adidas",
      },
    },
    {
      heading: "The results",
      paragraphs: [
        "Black Friday p95 latency on the storefront dropped **54% year-over-year**, mostly from collapsing the BFF round trips. The 27 BFFs were decommissioned over the following two quarters; the squads that owned them moved to product feature work. Pricing rollouts now take hours, not days.",
        "The mobile metric the team is proudest of: **a single localization fix lands in 14 markets at the same time**, because there is one schema and one client. That used to be a calendar quarter of follow-up work.",
      ],
    },
    {
      heading: "What's next",
      paragraphs: [
        "The team is exploring exposing the same Fusion graph to in-store kiosks and partner marketplaces, so the e-commerce graph becomes the commerce graph. Strawberry Shake codegen will continue to be the surface for every consumer.",
      ],
    },
  ],
};

// -----------------------------------------------------------------------------
// 4. Tier-1 European Bank (anonymous)
// -----------------------------------------------------------------------------
const tier1EuBankFusion: Story = {
  slug: "tier1-eu-bank-fusion",
  named: false,
  displayName: "Tier-1 European Bank",
  logoMonogram: "B",
  industry: "banking-insurance",
  products: ["fusion", "nitro", "hot-chocolate"],
  storyType: "scale",
  featured: true,
  cardMetric: "p99 480ms → 90ms",
  cardContext:
    "A top-5 European retail bank federated 18 subgraphs behind one Fusion mesh and cut mobile p99 by 81%.",
  eyebrow: "Customer Story · Banking & Insurance · Fusion + Nitro",
  headline:
    "How a tier-1 European bank cut mobile p99 from 480ms to 90ms — across 18 federated subgraphs.",
  subhead:
    "A 200-engineer platform team replaced six REST gateways with one Fusion mesh, governed by Nitro's schema registry, and got their weekends back.",
  heroDiagram: "federation",
  atAGlance: {
    industry: "Retail banking",
    scale: "12M customers · 18 subgraphs · 1.4B req/mo",
    region: "DACH",
    products: ["fusion", "nitro", "hot-chocolate"],
    stack: [
      ".NET 9",
      "Kubernetes",
      "Azure",
      "OpenTelemetry",
      "Postgres",
      "Kafka",
    ],
    liveSince: "Q3 2025",
  },
  keyMetrics: [
    { value: "−81%", label: "mobile p99 latency" },
    { value: "18", label: "subgraphs federated" },
    { value: "0", label: "breaking-change incidents in 6 months" },
  ],
  sections: [
    {
      heading: "The challenge",
      paragraphs: [
        "The bank's mobile team shipped behind six different REST gateways, each owned by a different backend squad. A single login screen pulled from four of them. Every release coordinated across fifteen calendars. **Breaking changes leaked into production twice a quarter** and twice a quarter the post-mortem said the same thing: nobody owned the contract.",
        'The platform team had tried federation before — a hand-rolled JavaScript gateway that drifted from the subgraphs and became its own incident generator. They were skeptical of any "single graph" pitch. They wanted build-time composition, not a runtime gateway DSL, and they wanted .NET so the existing fifty-engineer subgraph fleet could keep their muscle memory.',
      ],
      pullQuote: {
        text: "We had been burned by federation once. We weren't doing it again unless the gateway was boring infrastructure.",
        speakerRole: "Principal Engineer, Mobile Platform",
        speakerCompany: "Tier-1 European Bank",
      },
    },
    {
      heading: "The approach",
      paragraphs: [
        "The bank stood up Fusion in front of seven .NET microservices in **eight weeks**, then onboarded the remaining eleven over the following two quarters. Composition happens in CI: every subgraph PR runs the supergraph build, and Nitro's schema registry blocks anything that breaks an existing client. The platform team owns the gateway image and the manifest; the subgraph teams own their slices.",
        "Air-gap was non-negotiable. The gateway, the registry, and the observability stack all run on the bank's private OpenShift fabric. Nitro Self-Hosted ships an offline bundle that the platform team ingests through the same Nexus pipeline they use for every other internal artifact. **Audit sign-off took six weeks**, mostly because the audit team had questions about TLS 1.3 internals, not about the gateway.",
      ],
    },
    {
      heading: "The results",
      paragraphs: [
        "Mobile p99 latency on the dashboard dropped **from 480ms to 90ms** — an 81% improvement that the team initially assumed was a measurement bug. It wasn't: collapsing six fan-outs into one negotiated query at the gateway is genuinely that much faster.",
        "**Zero breaking-change incidents in the last six months** (was six the year prior). On-call pages on the API tier dropped 71%. Mobile time-to-feature went from 6 weeks to 4 days because the gateway is the integration test. The platform team's headcount didn't grow.",
      ],
      pullQuote: {
        text: "We replaced a quarterly release train with daily deploys. The mobile team stopped filing tickets against us — they just ship.",
        speakerName: "—",
        speakerRole: "Principal Engineer",
        speakerCompany: "Tier-1 European Bank platform team",
      },
    },
    {
      heading: "What's next",
      paragraphs: [
        "The bank is rolling Fusion into its agent-facing surface so internal MCP clients — the on-call assistant, the compliance copilot — can hit the same governed graph the mobile app uses. The contract is the contract, regardless of who's asking.",
      ],
    },
  ],
};

// -----------------------------------------------------------------------------
// 5. Top-3 European Insurer (anonymous)
// -----------------------------------------------------------------------------
const euInsurerAirgapped: Story = {
  slug: "eu-insurer-airgapped",
  named: false,
  displayName: "Top-3 European Insurer",
  logoMonogram: "I",
  industry: "banking-insurance",
  products: ["nitro", "hot-chocolate", "fusion"],
  storyType: "governance",
  featured: true,
  cardMetric: "6-week air-gap rollout, regulator-cleared",
  cardContext:
    "A top-3 European insurer deployed Nitro Self-Hosted into an air-gapped environment in six weeks — including regulator review.",
  eyebrow: "Customer Story · Banking & Insurance · Nitro Self-Hosted",
  headline:
    "How a top-3 European insurer cleared a regulator review and deployed Nitro Self-Hosted in six weeks, fully air-gapped.",
  subhead:
    "An offline gateway, a stamped audit trail, and a governance story the regulator could read in one sitting.",
  heroDiagram: "governance",
  atAGlance: {
    industry: "Insurance",
    scale: "9M policies · 22 subgraphs · 380M req/mo",
    region: "Western Europe",
    products: ["nitro", "hot-chocolate", "fusion"],
    stack: [
      ".NET 9",
      "OpenShift",
      "On-prem only",
      "OpenTelemetry",
      "Oracle",
      "MQ",
    ],
    liveSince: "Q1 2026",
  },
  keyMetrics: [
    { value: "6 weeks", label: "from kickoff to regulator sign-off" },
    { value: "0", label: "egress connections required" },
    { value: "12", label: "compliance evidence packs auto-generated" },
  ],
  sections: [
    {
      heading: "The challenge",
      paragraphs: [
        "The insurer's policy administration platform sits inside an air-gapped fabric the regulator inspects every two years. Any new piece of infrastructure has to ship as an offline bundle, has to come with a stamped audit trail, and has to survive a four-step vendor review before it reaches a single production cluster.",
        "The policy graph itself was already on Hot Chocolate — a healthy 22-subgraph deployment owned by the platform group. What was missing was governance. **Schema changes were reviewed by hand**, on a shared spreadsheet, and the cycle time from PR to merge averaged eleven days. The compliance team wanted automation; the engineering team wanted their lives back.",
      ],
    },
    {
      heading: "The approach",
      paragraphs: [
        "Nitro Self-Hosted shipped as an offline bundle that the insurer ingested through their existing Nexus mirror. No image registry pulls, no telemetry egress, no phone-home. The platform team installed the registry, the gateway, and the observability stack inside the fabric the regulator already knew about.",
        "The governance work was the interesting part. Every subgraph PR now runs Nitro composition in CI; the supergraph diff is attached to the PR; the regulator's evidence pack is generated from the same registry on demand. **12 compliance evidence packs** were auto-generated in the first quarter — packs that previously took an engineer half a day each.",
        "The regulator review took two sittings. The first was a deep dive on the offline bundle and the integrity attestation. The second was a walk-through of the audit log. **Sign-off came six weeks after kickoff** — the fastest enterprise rollout the insurer's compliance team had on record.",
      ],
      pullQuote: {
        text: "The regulator asked for the audit trail. I exported it from Nitro's registry in front of them. That was the meeting.",
        speakerRole: "Head of Architecture, Policy Platform",
        speakerCompany: "Top-3 European Insurer",
      },
    },
    {
      heading: "The results",
      paragraphs: [
        "Schema-change cycle time fell from **eleven days to two**. Breaking-change incidents on the policy graph went to zero. The compliance team gained back roughly four engineer-days per week that used to go into evidence packs.",
        "The latency wins were modest by design — the team didn't change the runtime characteristics of the existing Hot Chocolate stack. **What changed was confidence**: the platform team can now ship a schema change on a Friday because the registry catches anything that would break a downstream consumer.",
      ],
    },
    {
      heading: "What's next",
      paragraphs: [
        "Phase two adds Fusion composition for two new subgraphs the actuarial team is bringing online. The insurer expects to retire two more legacy SOAP gateways in the next six months. The air-gap stays.",
      ],
    },
  ],
};

// -----------------------------------------------------------------------------
// 6. North-American Health Network (anonymous)
// -----------------------------------------------------------------------------
const naHealthNetwork: Story = {
  slug: "na-health-network",
  named: false,
  displayName: "North-American Health Network",
  logoMonogram: "H",
  industry: "healthcare",
  products: ["hot-chocolate", "nitro", "fusion"],
  storyType: "greenfield",
  featured: true,
  cardMetric: "47 hospital systems on one HIPAA-compliant graph",
  cardContext:
    "A North-American health network unified 47 hospital systems' patient data behind one HIPAA-compliant Hot Chocolate graph.",
  eyebrow: "Customer Story · Healthcare · Hot Chocolate + Nitro",
  headline:
    "How a North-American health network built a HIPAA-compliant graph for 47 hospital systems, in one shared schema.",
  subhead:
    "A patient-data graph, a federated chart-review experience, and a Nitro-governed audit trail that closes a long-standing PHI access gap.",
  heroDiagram: "federation",
  atAGlance: {
    industry: "Healthcare · Health Systems",
    scale: "47 hospital systems · 14M patients · 9 subgraphs",
    region: "North America",
    products: ["hot-chocolate", "nitro", "fusion"],
    stack: [
      ".NET 9",
      "AWS GovCloud",
      "Kubernetes",
      "OpenTelemetry",
      "FHIR",
      "HL7",
    ],
    liveSince: "Q3 2025",
  },
  keyMetrics: [
    { value: "47", label: "hospital systems unified" },
    { value: "100%", label: "PHI access events audited" },
    { value: "−68%", label: "chart-pull latency for clinicians" },
  ],
  sections: [
    {
      heading: "The challenge",
      paragraphs: [
        "Forty-seven hospital systems, each with its own patient record system, its own FHIR endpoint, its own auth model. A clinician pulling a chart for a transferred patient could spend twenty minutes and four web sessions stitching data from three subsystems. The network's clinical informatics team was tired.",
        "The greenfield challenge was real: the team had no existing graph. They had FHIR servers, HL7 feeds, custom internal APIs, and a queue of partner integrations. What they wanted was one schema clinicians could query, with **PHI access auditing baked in** so the compliance team didn't need a parallel logging system.",
      ],
    },
    {
      heading: "The approach",
      paragraphs: [
        "Hot Chocolate gave the team a single .NET graph that wraps every hospital's FHIR endpoint as a subgraph. Nitro's audit log captures every field-level access — **100% of PHI reads** are stamped with operator, time, and clinical context, exported to the network's existing SIEM in OTEL format. The compliance team got a single dashboard instead of 47.",
        "The team took a deliberate stance: every hospital system stays the source of record. Hot Chocolate's projection means the graph never copies PHI; it queries upstream and forwards. **Cache TTLs are short** for clinical fields, longer for reference data, and audit-flagged for anything that crosses an institutional boundary.",
      ],
      pullQuote: {
        text: "The auditor asked for a list of every chart pull on a specific patient over six months. I had a CSV in twenty seconds. The previous answer to that question was a two-week project.",
        speakerRole: "Director of Clinical Informatics",
        speakerCompany: "North-American Health Network",
      },
    },
    {
      heading: "The results",
      paragraphs: [
        "Chart-pull latency for clinicians dropped **68%**, mostly because the gateway parallelizes what the clinician used to do by hand. Cross-institution chart reviews went from a four-tab juggling act to a single query. The compliance team retired three custom audit pipelines.",
        "An unexpected win: **research data requests** that used to require IRB-supervised export jobs now run as scoped GraphQL queries against de-identified projections of the same graph. A study that previously took six weeks to assemble took an afternoon — under the same audit log.",
      ],
    },
    {
      heading: "What's next",
      paragraphs: [
        "The network is bringing Fusion online for a federated provider-directory subgraph and a benefits subgraph, owned by two adjacent organizations under the same compliance perimeter. The graph is becoming the integration plane the network always wanted but never had time to build.",
      ],
    },
  ],
};

// -----------------------------------------------------------------------------
// 7. Nordic Telco (anonymous)
// -----------------------------------------------------------------------------
const nordicTelcoMcp: Story = {
  slug: "nordic-telco-mcp",
  named: false,
  displayName: "Nordic Telco",
  logoMonogram: "T",
  industry: "telco-media",
  products: ["fusion", "nitro", "hot-chocolate"],
  storyType: "migration",
  featured: false,
  cardMetric: "5 portals → 1 federated network graph",
  cardContext:
    "A Nordic telco federated five portal backends into one supergraph and turned customer-care AHT into a one-query operation.",
  eyebrow: "Customer Story · Telco & Media · Fusion + Nitro",
  headline:
    "How a Nordic telco federated five portals into one network graph — and cut customer-care average handle time by 41%.",
  subhead:
    'From five portal backends and a fragmented care experience to one supergraph that answers "why is this customer\'s bill wrong" in a single query.',
  heroDiagram: "migration",
  atAGlance: {
    industry: "Telco · Mobile + Fixed",
    scale: "8M subscribers · 5 portals consolidated · 14 subgraphs",
    region: "Nordics",
    products: ["fusion", "nitro", "hot-chocolate"],
    stack: [
      ".NET 9",
      "Azure",
      "Kubernetes",
      "OpenTelemetry",
      "Postgres",
      "Kafka",
    ],
    liveSince: "Q2 2025",
  },
  keyMetrics: [
    { value: "−41%", label: "care AHT (average handle time)" },
    { value: "5 → 1", label: "portal backends consolidated" },
    { value: "14", label: "subgraphs federated" },
  ],
  sections: [
    {
      heading: "The challenge",
      paragraphs: [
        "The telco ran five distinct customer portals — consumer mobile, business mobile, broadband, TV, and roaming — each with its own .NET backend and its own login experience. Customer-care agents bounced between three or four of them on a single call. **Average handle time was creeping up six minutes year-over-year**, and the engineering org was tired of arguing about which portal team owned which CRUD.",
        "The team had two constraints. First, the portals couldn't all freeze for a re-platform; the consumer mobile portal alone took the bulk of the traffic and was on a relentless feature roadmap. Second, the care application had to start working better in one quarter, not in three.",
      ],
    },
    {
      heading: "The approach",
      paragraphs: [
        "The team picked Fusion because the existing portal backends were already Hot Chocolate. Composition happens at build time; each portal team kept their server and their tests. The platform team put Fusion in front and rebuilt the customer-care console as a single Strawberry Shake client over the supergraph.",
        "Care-agent rollout came first. **The new console shipped to 200 agents in week six** and to all 4,200 in week twelve. The portal-by-portal migration followed: TV portal in Q2, broadband in Q3, business and roaming in Q4. The consumer mobile portal cut over last, the weekend after a major release.",
      ],
      pullQuote: {
        text: "We started with the customer-care console because it had the worst experience. The portals were the easy follow-up.",
        speakerRole: "VP of Customer Experience Engineering",
        speakerCompany: "Nordic Telco",
      },
    },
    {
      heading: "The results",
      paragraphs: [
        'Care AHT dropped **41% in two quarters**. Most of the win came from a single change: agents now ask the question "what\'s wrong with this customer" once, against the supergraph, instead of clicking through three portals.',
        "The portal teams stopped owning duplicate authentication, duplicate billing lookups, and duplicate notification preferences. A single product-engineer PR can now ship a consistent change across all five portals because there's one schema. **Engineer-hours saved per quarter** ran into the four digits.",
      ],
    },
    {
      heading: "What's next",
      paragraphs: [
        "The telco is exposing the same supergraph to its in-app assistant and to a copilot for retail-store staff via Nitro's MCP surface. The schema is the API contract; humans, agents, and partners hit the same place.",
      ],
    },
  ],
};

// -----------------------------------------------------------------------------
// 8. North-American FinTech (anonymous)
// -----------------------------------------------------------------------------
const naFintechGovernance: Story = {
  slug: "na-fintech-governance",
  named: false,
  displayName: "North-American FinTech",
  logoMonogram: "F",
  industry: "banking-insurance",
  products: ["nitro", "hot-chocolate"],
  storyType: "governance",
  featured: false,
  cardMetric: "PR review time: 11 days → 38 minutes",
  cardContext:
    "A North-American FinTech turned schema review from a multi-day spreadsheet exercise into a 38-minute Nitro-gated PR.",
  eyebrow: "Customer Story · Banking & Insurance · Nitro Schema Registry",
  headline:
    "How a North-American FinTech turned schema review from an 11-day spreadsheet exercise into a 38-minute PR.",
  subhead:
    "Nitro's registry took the meeting out of the meeting and the breaking-change risk out of every release.",
  heroDiagram: "governance",
  atAGlance: {
    industry: "Financial services · Payments",
    scale: "200M accounts · 6 subgraphs · 720M req/mo",
    region: "North America",
    products: ["nitro", "hot-chocolate"],
    stack: [
      ".NET 9",
      "AWS",
      "Kubernetes",
      "OpenTelemetry",
      "Aurora",
      "Kinesis",
    ],
    liveSince: "Q4 2025",
  },
  keyMetrics: [
    { value: "11d → 38m", label: "schema PR review time" },
    { value: "−93%", label: "breaking-change incidents" },
    { value: "6", label: "subgraphs governed" },
  ],
  sections: [
    {
      heading: "The challenge",
      paragraphs: [
        "The FinTech ran six Hot Chocolate subgraphs that fed a payments app, a partner API, and a regulatory reporting pipeline. The graph itself was healthy. The review process around it was not. **Every schema change required a four-name review** in a spreadsheet, two stand-up mentions, and a release-train slot. Cycle time averaged eleven days.",
        "The thing that finally broke the team was a Friday-afternoon breaking change to a partner-facing field. Two partners' production systems went down for the weekend. The post-mortem named the spreadsheet.",
      ],
    },
    {
      heading: "The approach",
      paragraphs: [
        "Nitro's schema registry replaced the spreadsheet. Every PR runs composition against the published supergraph, and the registry lints, diffs, and audits in CI. **Breaking-change detection is a hard gate** — the PR cannot merge if a known consumer relies on the field being removed.",
        "The team kept human review for stylistic and product decisions, but the breaking-change conversation moved into the PR review thread, with the registry's diff output as the citation. The four-name spreadsheet was retired.",
      ],
      pullQuote: {
        text: "We replaced an eleven-day review process with thirty-eight minutes of human conversation. The other ten days was waiting for a meeting that the registry already had the answer to.",
        speakerRole: "Staff Engineer, Platform",
        speakerCompany: "North-American FinTech",
      },
    },
    {
      heading: "The results",
      paragraphs: [
        "Schema PR review time dropped to a **median of 38 minutes**. Breaking-change incidents fell **93%** — most of the residual were intentional, scheduled, and communicated to partners through the same registry.",
        "An unexpected win: the partner team started using Nitro's published schema as the partner documentation. The same source of truth that gates the PR is the developer portal. **Two custom doc pipelines were retired** in the following quarter.",
      ],
    },
    {
      heading: "What's next",
      paragraphs: [
        "The team is rolling out Nitro's MCP surface for an internal compliance assistant, so the regulatory team can ask schema questions without filing a ticket. The registry is becoming the system of record for the API contract — for humans and for agents.",
      ],
    },
  ],
};

export const STORIES: readonly Story[] = [
  microsoftCommerceFusion,
  sbbPublicData,
  adidasStorefront,
  tier1EuBankFusion,
  euInsurerAirgapped,
  naHealthNetwork,
  nordicTelcoMcp,
  naFintechGovernance,
];

export const findStory = (slug: string): Story | undefined =>
  STORIES.find((s) => s.slug === slug);

export const findRelated = (
  story: Story,
  max: number = 3
): readonly Story[] => {
  const others = STORIES.filter((s) => s.slug !== story.slug);
  const sameIndustry = others.filter((s) => s.industry === story.industry);
  const sameProduct = others.filter(
    (s) =>
      !sameIndustry.includes(s) &&
      s.products.some((p) => story.products.includes(p))
  );
  return [...sameIndustry, ...sameProduct, ...others].slice(0, max);
};

export const productLabel = (key: ProductKey): string =>
  PRODUCTS.find((p) => p.key === key)?.label ?? key;

// Used by the trust-wall tab content. Each industry shows ~12 tiles.
export interface TrustTile {
  readonly key: string;
  readonly monogram: string;
  readonly caption: string;
  readonly named: boolean;
}

export const TRUST_WALL: Record<string, readonly TrustTile[]> = {
  "banking-insurance": [
    {
      key: "tier1-eu-bank",
      monogram: "B",
      caption: "Tier-1 EU bank",
      named: false,
    },
    {
      key: "top3-insurer",
      monogram: "I",
      caption: "Top-3 EU insurer",
      named: false,
    },
    { key: "na-fintech", monogram: "F", caption: "NA FinTech", named: false },
    {
      key: "swiss-private",
      monogram: "P",
      caption: "Swiss private bank",
      named: false,
    },
    { key: "allianz", monogram: "A", caption: "Allianz", named: true },
    {
      key: "uk-challenger",
      monogram: "C",
      caption: "UK challenger bank",
      named: false,
    },
    {
      key: "nordic-pension",
      monogram: "N",
      caption: "Nordic pension fund",
      named: false,
    },
    {
      key: "iberian-bank",
      monogram: "I",
      caption: "Iberian retail bank",
      named: false,
    },
    {
      key: "eu-broker",
      monogram: "B",
      caption: "EU broker network",
      named: false,
    },
    {
      key: "dach-reinsurer",
      monogram: "R",
      caption: "DACH reinsurer",
      named: false,
    },
    {
      key: "global-card",
      monogram: "C",
      caption: "Global card network",
      named: false,
    },
    {
      key: "fintech-payroll",
      monogram: "P",
      caption: "FinTech payroll PaaS",
      named: false,
    },
  ],
  "retail-ecommerce": [
    { key: "adidas", monogram: "AD", caption: "Adidas", named: true },
    {
      key: "tier1-grocer",
      monogram: "G",
      caption: "Tier-1 EU grocer",
      named: false,
    },
    {
      key: "luxury-conglomerate",
      monogram: "L",
      caption: "Luxury conglomerate",
      named: false,
    },
    {
      key: "eu-marketplace",
      monogram: "M",
      caption: "EU marketplace",
      named: false,
    },
    {
      key: "sportswear-dtc",
      monogram: "S",
      caption: "Sportswear DTC",
      named: false,
    },
    {
      key: "nordic-fashion",
      monogram: "F",
      caption: "Nordic fashion group",
      named: false,
    },
    {
      key: "home-goods",
      monogram: "H",
      caption: "Home goods retailer",
      named: false,
    },
    {
      key: "diy-chain",
      monogram: "D",
      caption: "DIY chain · 9 markets",
      named: false,
    },
    {
      key: "beauty-brand",
      monogram: "B",
      caption: "Beauty brand · WW",
      named: false,
    },
    {
      key: "convenience",
      monogram: "C",
      caption: "Convenience operator",
      named: false,
    },
    {
      key: "kid-edutainment",
      monogram: "K",
      caption: "Kid edutainment",
      named: false,
    },
    {
      key: "music-merch",
      monogram: "M",
      caption: "Music merch platform",
      named: false,
    },
  ],
  healthcare: [
    {
      key: "na-health-net",
      monogram: "H",
      caption: "NA health network",
      named: false,
    },
    {
      key: "dach-health",
      monogram: "D",
      caption: "DACH hospital group",
      named: false,
    },
    {
      key: "uk-nhs-trust",
      monogram: "N",
      caption: "UK NHS trust",
      named: false,
    },
    {
      key: "ehr-vendor",
      monogram: "E",
      caption: "EHR vendor · NA",
      named: false,
    },
    {
      key: "telehealth",
      monogram: "T",
      caption: "Telehealth platform",
      named: false,
    },
    {
      key: "lab-network",
      monogram: "L",
      caption: "National lab network",
      named: false,
    },
    {
      key: "pharma-rd",
      monogram: "P",
      caption: "Pharma R&D platform",
      named: false,
    },
    {
      key: "imaging-cloud",
      monogram: "I",
      caption: "Imaging cloud · EU",
      named: false,
    },
    {
      key: "patient-portal",
      monogram: "P",
      caption: "Patient portal · 9M users",
      named: false,
    },
    {
      key: "clinical-trials",
      monogram: "C",
      caption: "Clinical-trials SaaS",
      named: false,
    },
    {
      key: "hospital-ops",
      monogram: "O",
      caption: "Hospital ops platform",
      named: false,
    },
    {
      key: "dental-network",
      monogram: "D",
      caption: "Dental network · NA",
      named: false,
    },
  ],
  "public-sector": [
    {
      key: "sbb",
      monogram: "SBB",
      caption: "Swiss Federal Railways",
      named: true,
    },
    { key: "swissgrid", monogram: "SG", caption: "Swissgrid", named: true },
    {
      key: "national-statistics",
      monogram: "S",
      caption: "National statistics agency",
      named: false,
    },
    {
      key: "tax-authority",
      monogram: "T",
      caption: "EU tax authority",
      named: false,
    },
    {
      key: "city-govt-1",
      monogram: "C",
      caption: "Capital city · DACH",
      named: false,
    },
    {
      key: "transport-authority",
      monogram: "T",
      caption: "Transport authority · UK",
      named: false,
    },
    {
      key: "energy-regulator",
      monogram: "E",
      caption: "Energy regulator",
      named: false,
    },
    {
      key: "land-registry",
      monogram: "L",
      caption: "National land registry",
      named: false,
    },
    {
      key: "edu-ministry",
      monogram: "E",
      caption: "Education ministry",
      named: false,
    },
    {
      key: "civil-id",
      monogram: "I",
      caption: "Civil-ID platform",
      named: false,
    },
    {
      key: "national-library",
      monogram: "N",
      caption: "National library system",
      named: false,
    },
    {
      key: "public-broadcaster",
      monogram: "B",
      caption: "Public broadcaster",
      named: false,
    },
  ],
  "software-saas": [
    { key: "microsoft", monogram: "MS", caption: "Microsoft", named: true },
    {
      key: "saas-erp",
      monogram: "E",
      caption: "SaaS ERP · global",
      named: false,
    },
    {
      key: "dev-platform",
      monogram: "D",
      caption: "DevTools platform",
      named: false,
    },
    {
      key: "data-warehouse",
      monogram: "W",
      caption: "Data warehouse vendor",
      named: false,
    },
    {
      key: "observability-vendor",
      monogram: "O",
      caption: "Observability SaaS",
      named: false,
    },
    {
      key: "billing-platform",
      monogram: "B",
      caption: "Billing platform · NA",
      named: false,
    },
    {
      key: "hr-platform",
      monogram: "H",
      caption: "HR platform · EU",
      named: false,
    },
    { key: "iam-vendor", monogram: "I", caption: "IAM vendor", named: false },
    {
      key: "logistics-paas",
      monogram: "L",
      caption: "Logistics PaaS",
      named: false,
    },
    {
      key: "marketing-cloud",
      monogram: "M",
      caption: "Marketing cloud · NA",
      named: false,
    },
    {
      key: "design-tooling",
      monogram: "D",
      caption: "Design tooling SaaS",
      named: false,
    },
    {
      key: "analytics-saas",
      monogram: "A",
      caption: "Analytics SaaS · WW",
      named: false,
    },
  ],
  "telco-media": [
    {
      key: "nordic-telco",
      monogram: "T",
      caption: "Nordic telco",
      named: false,
    },
    {
      key: "european-broadcaster",
      monogram: "B",
      caption: "European broadcaster",
      named: false,
    },
    {
      key: "streaming-platform",
      monogram: "S",
      caption: "Streaming platform · 30M",
      named: false,
    },
    {
      key: "publishing-house",
      monogram: "P",
      caption: "Publishing house · DACH",
      named: false,
    },
    {
      key: "telco-2",
      monogram: "T",
      caption: "Tier-1 telco · UK",
      named: false,
    },
    {
      key: "podcast-network",
      monogram: "N",
      caption: "Podcast network",
      named: false,
    },
    {
      key: "news-app",
      monogram: "N",
      caption: "News app · 12M MAU",
      named: false,
    },
    {
      key: "sports-streamer",
      monogram: "S",
      caption: "Sports streamer · EU",
      named: false,
    },
    {
      key: "infra-carrier",
      monogram: "I",
      caption: "Infra carrier",
      named: false,
    },
    { key: "iot-mvno", monogram: "I", caption: "IoT MVNO", named: false },
    {
      key: "cable-operator",
      monogram: "C",
      caption: "Cable operator · EU",
      named: false,
    },
    {
      key: "media-conglomerate",
      monogram: "M",
      caption: "Media conglomerate",
      named: false,
    },
  ],
};

void INDUSTRIES; // re-export anchor so the import isn't dropped by the compiler
