import type { Metadata } from "next";
import NextLink from "next/link";
import type { ComponentType, CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CookieCrumble } from "@/src/icons/CookieCrumble";
import { Fusion } from "@/src/icons/Fusion";
import { GitHubIcon } from "@/src/icons/GitHub";
import { GreenDonut } from "@/src/icons/GreenDonut";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { StrawberryShake } from "@/src/icons/StrawberryShake";

export const metadata: Metadata = {
  title: "ChilliCream: GraphQL platform for .NET",
  description:
    "The ChilliCream GraphQL platform for .NET: Hot Chocolate server, Strawberry Shake client, Nitro registry, CI checks, observability, Fusion composition, Mocha.",
  keywords: [
    "GraphQL platform for .NET",
    "ChilliCream",
    "Hot Chocolate",
    "Nitro",
    "Strawberry Shake",
    "Fusion",
    "Mocha",
    "GraphQL observability",
    "schema registry",
  ],
  openGraph: {
    title: "ChilliCream: GraphQL platform for .NET",
    description:
      "Build, observe, and evolve your GraphQL platform on .NET. Hot Chocolate, Strawberry Shake, Nitro, Fusion, and Mocha, designed together. MIT licensed.",
  },
  robots: { index: false, follow: false },
};

interface FamilyEntry {
  readonly name: string;
  readonly tagline: string;
  readonly description: string;
  readonly href: string;
  readonly external?: boolean;
  readonly icon: ComponentType<{
    readonly className?: string;
    readonly style?: CSSProperties;
  }>;
}

const FAMILY: readonly FamilyEntry[] = [
  {
    name: "Hot Chocolate",
    tagline: "graphql server for .net",
    description:
      "Source-generated GraphQL server on ASP.NET Core. Schema-first or code-first, modern spec.",
    href: "/products/hotchocolate",
    icon: HotChocolate,
  },
  {
    name: "Strawberry Shake",
    tagline: "typed .net client",
    description:
      "MSBuild code generation turns each query into a fully typed C# API for your apps.",
    href: "/products/strawberryshake",
    icon: StrawberryShake,
  },
  {
    name: "Nitro",
    tagline: "control plane and ide",
    description:
      "Schema registry, client registry, CI checks, observability, and the IDE your team already uses.",
    href: "https://nitro.chillicream.com",
    external: true,
    icon: Nitro,
  },
  {
    name: "Fusion",
    tagline: "composition for many subgraphs",
    description:
      "Compose subgraphs at planning time, run the gateway in your own ASP.NET Core process.",
    href: "/products/fusion",
    icon: Fusion,
  },
  {
    name: "Mocha",
    tagline: "mediator and workflows",
    description:
      "Source-generated mediator. Validated sagas, exactly-once processing, no reflection on the hot path.",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    icon: Mocha,
  },
  {
    name: "Green Donut",
    tagline: "dataloader for .net",
    description:
      "Batches and caches resolver data access so N+1 stops being your problem.",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    icon: GreenDonut,
  },
  {
    name: "Cookie Crumble",
    tagline: "graphql-aware snapshot testing",
    description:
      "Snapshot testing with native support for execution results, HTTP responses, and Markdown snapshots.",
    href: "https://github.com/ChilliCream/graphql-platform",
    external: true,
    icon: CookieCrumble,
  },
];

interface PriceEntry {
  readonly label: string;
  readonly price: string;
  readonly note: string;
}

const PRICE_LEDGER: readonly PriceEntry[] = [
  { label: "shared instance", price: "Free", note: "pay as you go" },
  { label: "dedicated instance", price: "$400", note: "per month" },
  { label: "startup support", price: "$450", note: "per month" },
  { label: "business support", price: "$1,300", note: "per month" },
];

export default function LandingPreviewV5Page() {
  return (
    <div className="relative mx-auto max-w-4xl px-5 sm:px-8">
      <div
        aria-hidden
        className="border-cc-card-border pointer-events-none absolute top-0 bottom-0 left-2 border-l sm:left-20 lg:left-24"
      />
      <PreludeStep />
      <BuildStep />
      <ObserveStep />
      <EvolveStep />
      <AgenticStep />
      <FamilyStep />
      <PricingStep />
      <ClosingMarker />
    </div>
  );
}

interface StepBandProps {
  readonly numeral: string;
  readonly children: ReactNode;
  readonly first?: boolean;
}

function StepBand({ numeral, children, first = false }: StepBandProps) {
  return (
    <section
      className={`relative grid grid-cols-[3rem_1fr] gap-x-4 sm:grid-cols-[5rem_1fr] sm:gap-x-8 lg:grid-cols-[6rem_1fr] ${
        first
          ? "min-h-[80svh] pt-12 pb-24 sm:pt-20 sm:pb-32"
          : "min-h-[80svh] py-24 sm:py-32"
      }`}
    >
      <div className="text-cc-accent font-mono text-4xl leading-none font-medium tracking-tight sm:text-5xl lg:text-6xl">
        {numeral}
      </div>
      <div className="min-w-0">{children}</div>
    </section>
  );
}

function PreludeStep() {
  return (
    <StepBand numeral="00" first>
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.2em] uppercase">
        the chillicream graphql platform
      </p>
      <h1 className="font-heading text-cc-heading text-hero mt-5 font-semibold tracking-[-0.02em] text-balance">
        <span className="text-cc-heading block">Your GraphQL platform,</span>
        <span className="text-cc-accent block">running on .NET.</span>
      </h1>
      <p className="text-cc-ink text-lead mt-8 max-w-2xl text-pretty">
        Hot Chocolate ships the server. Strawberry Shake ships the typed client.
        Nitro ships the control plane, registry, and observability. One
        platform, designed together, open source on GitHub.
      </p>
      <div className="mt-9 flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="https://nitro.chillicream.com">
          Launch Nitro
        </OutlineButton>
      </div>
      <p className="text-cc-nav-label mt-6 font-mono text-[0.65rem] tracking-[0.2em] uppercase">
        mit licensed. self-host or run on nitro cloud.
      </p>

      <div className="border-cc-card-border mt-16 border-t pt-10">
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.2em] uppercase">
          trusted by teams shipping on .net
        </p>
        <div className="mt-6">
          <LogoCloud />
        </div>
      </div>
    </StepBand>
  );
}

function BuildStep() {
  return (
    <StepBand numeral="01">
      <p className="text-cc-accent font-mono text-[0.65rem] tracking-[0.2em] uppercase">
        build
      </p>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold">
        Source-generated GraphQL, end to end.
      </h2>
      <p className="text-cc-ink text-body mt-6 max-w-2xl leading-relaxed">
        Hot Chocolate generates resolver dispatch, type bindings, and execution
        plans at compile time. Strawberry Shake generates the typed C# client
        from your operations via MSBuild. The schema you ship is the schema both
        sides agree on.
      </p>
      <ul className="mt-7 flex max-w-2xl flex-col gap-3">
        <BulletRow>Code-first and schema-first authoring</BulletRow>
        <BulletRow>Compile-time validated execution plans</BulletRow>
        <BulletRow>MSBuild code generation for the client</BulletRow>
      </ul>

      <div className="mt-10 max-w-2xl">
        <div className="text-cc-nav-label mb-3 flex items-center justify-between font-mono text-[0.6rem] tracking-[0.2em] uppercase">
          <span>program.cs</span>
          <span className="text-cc-accent">source generated</span>
        </div>
        <pre className="text-cc-ink font-mono text-[0.78rem] leading-relaxed">
          <code>
            <span className="text-cc-ink-dim">{"// Hot Chocolate server"}</span>
            {"\n"}
            <span className="text-cc-accent">builder</span>.Services
            {"\n"}
            {"  "}.AddGraphQLServer()
            {"\n"}
            {"  "}.AddQueryType&lt;<span className="text-cc-accent">Query</span>
            &gt;()
            {"\n"}
            {"  "}.AddMutationType&lt;
            <span className="text-cc-accent">Mutation</span>&gt;()
            {"\n"}
            {"  "}.AddInstrumentation()
            {"\n"}
            {"  "}.AddNitroExporter();
            {"\n\n"}
            <span className="text-cc-ink-dim">
              {"// Strawberry Shake client"}
            </span>
            {"\n"}
            dotnet graphql init{" "}
            <span className="text-cc-accent">https://api/graphql</span>
          </code>
        </pre>
      </div>
    </StepBand>
  );
}

function ObserveStep() {
  return (
    <StepBand numeral="02">
      <p className="text-cc-accent font-mono text-[0.65rem] tracking-[0.2em] uppercase">
        observe
      </p>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold">
        OpenTelemetry-native, from gateway to resolver.
      </h2>
      <p className="text-cc-ink text-body mt-6 max-w-2xl leading-relaxed">
        Once Nitro is configured in the server, every operation carries
        OpenTelemetry traces, metrics, and logs. Operation insights surface p95,
        throughput, error rate, and impact, with per-client tracking via
        GraphQL-Client-Id and Version.
      </p>

      <div className="mt-10 max-w-2xl">
        <div className="text-cc-nav-label mb-4 flex items-center justify-between font-mono text-[0.6rem] tracking-[0.2em] uppercase">
          <span>checkout.graphql / getCart</span>
          <span className="text-cc-accent">live</span>
        </div>
        <div className="border-cc-card-border grid grid-cols-3 gap-6 border-y py-5">
          <StatRow label="p95" value="142ms" />
          <StatRow label="rate" value="3.4k/m" />
          <StatRow label="errors" value="0.21%" />
        </div>
        <div className="text-cc-ink-dim mt-5 font-mono text-[0.72rem]">
          per-client &nbsp;
          <span className="text-cc-ink">web@2.4.0</span>
          {"  "}
          <span className="text-cc-ink">ios@1.9.2</span>
          {"  "}
          <span className="text-cc-ink">android@1.8.7</span>
        </div>
      </div>
    </StepBand>
  );
}

function EvolveStep() {
  return (
    <StepBand numeral="03">
      <p className="text-cc-accent font-mono text-[0.65rem] tracking-[0.2em] uppercase">
        evolve
      </p>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold">
        Know which published clients a change affects.
      </h2>
      <p className="text-cc-ink text-body mt-6 max-w-2xl leading-relaxed">
        The schema registry tracks every published version. The client registry
        knows every operation each app actually sends. Nitro CI compares them
        and reports the published clients affected before you deploy.
      </p>

      <div className="mt-10 max-w-2xl">
        <div className="text-cc-nav-label mb-4 flex items-center justify-between font-mono text-[0.6rem] tracking-[0.2em] uppercase">
          <span>schema check / pr #2417</span>
          <span className="text-cc-warning">3 affected</span>
        </div>
        <ul className="flex flex-col font-mono text-[0.78rem]">
          <DiffLedgerRow tag="+" tone="add">
            type Cart {"{"} totals: Money! {"}"}
          </DiffLedgerRow>
          <DiffLedgerRow tag="-" tone="remove">
            type Cart {"{"} total: Float! {"}"}
          </DiffLedgerRow>
          <DiffLedgerRow tag="!" tone="warn">
            BREAKING: Cart.total removed
          </DiffLedgerRow>
        </ul>

        <div className="mt-8">
          <div className="text-cc-nav-label mb-3 font-mono text-[0.6rem] tracking-[0.2em] uppercase">
            published clients affected
          </div>
          <ul className="text-cc-ink flex flex-col font-mono text-[0.78rem]">
            <ClientRow client="web@2.3.x" op="getCart" />
            <ClientRow client="ios@1.9.x" op="getCart" />
            <ClientRow client="android@1.8.x" op="getCart" />
          </ul>
        </div>
      </div>
    </StepBand>
  );
}

function AgenticStep() {
  return (
    <StepBand numeral="04">
      <p className="text-cc-accent font-mono text-[0.65rem] tracking-[0.2em] uppercase">
        ship to agents, run the work
      </p>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold">
        An MCP endpoint your agents can use, with Mocha behind the work.
      </h2>
      <p className="text-cc-ink text-body mt-6 max-w-2xl leading-relaxed">
        Hot Chocolate exposes an MCP server over Streamable HTTP. Curate feature
        collections of .graphql operations, JSON descriptions, and HTML context,
        then watch per-tool telemetry beside per-operation in Nitro. Mocha runs
        the work between services with validated sagas and exactly-once
        processing.
      </p>

      <div className="mt-10 max-w-2xl">
        <div className="text-cc-nav-label mb-4 flex items-center justify-between font-mono text-[0.6rem] tracking-[0.2em] uppercase">
          <span>mcp / featureset: orders</span>
          <span className="text-cc-accent">4 tools</span>
        </div>
        <ul className="flex flex-col font-mono text-[0.78rem]">
          <McpRow name="placeOrder" ops="142/m" p95="186ms" />
          <McpRow name="cancelOrder" ops="9/m" p95="94ms" />
          <McpRow name="lookupOrder" ops="612/m" p95="42ms" />
          <McpRow name="refundOrder" ops="3/m" p95="220ms" />
        </ul>
      </div>

      <div className="mt-10 max-w-2xl">
        <div className="text-cc-nav-label mb-4 flex items-center justify-between font-mono text-[0.6rem] tracking-[0.2em] uppercase">
          <span>mocha / placeOrderSaga</span>
          <span className="text-cc-success">validated</span>
        </div>
        <ol className="flex flex-col font-mono text-[0.78rem]">
          <SagaRow step="01" label="ReserveInventory" state="ok" />
          <SagaRow step="02" label="ChargePayment" state="ok" />
          <SagaRow step="03" label="DispatchShipment" state="run" />
          <SagaRow step="04" label="NotifyCustomer" state="idle" />
        </ol>
        <p className="text-cc-ink-dim mt-5 font-mono text-[0.72rem]">
          exactly-once processing. source-generated handlers. no reflection on
          the hot path.
        </p>
      </div>
    </StepBand>
  );
}

function FamilyStep() {
  return (
    <StepBand numeral="05">
      <p className="text-cc-accent font-mono text-[0.65rem] tracking-[0.2em] uppercase">
        the family
      </p>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold">
        One family of products. MIT licensed.
      </h2>
      <p className="text-cc-ink text-body mt-6 max-w-2xl leading-relaxed">
        The ChilliCream platform is a coherent set of libraries, designed
        together for .NET. Each one stands on its own, and each one assumes the
        others exist.
      </p>

      <ul className="mt-10 flex max-w-3xl flex-col">
        {FAMILY.map((entry) => (
          <FamilyRow key={entry.name} entry={entry} />
        ))}
      </ul>

      <a
        href="https://github.com/ChilliCream/graphql-platform"
        target="_blank"
        rel="noopener noreferrer"
        className="border-cc-card-border hover:border-cc-card-border-hover text-cc-heading mt-10 inline-flex items-center gap-3 border-b py-3 text-sm transition-colors"
      >
        <GitHubIcon className="text-cc-heading h-5 w-5" />
        <span className="font-mono text-[0.78rem]">
          github.com/ChilliCream/graphql-platform
        </span>
        <span aria-hidden className="text-cc-accent">
          {"->"}
        </span>
      </a>
    </StepBand>
  );
}

function PricingStep() {
  return (
    <StepBand numeral="06">
      <p className="text-cc-accent font-mono text-[0.65rem] tracking-[0.2em] uppercase">
        pricing &amp; open source
      </p>
      <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 font-semibold">
        Free to start. Honest to scale.
      </h2>
      <p className="text-cc-ink text-body mt-6 max-w-2xl leading-relaxed">
        The Hot Chocolate platform itself is open source and free under MIT.
        Nitro Cloud has a free Shared Instance and a $400 Dedicated Instance.
        Support plans start at $450 Startup and $1,300 Business, with Custom
        Enterprise for the rest.
      </p>

      <ul className="mt-10 flex max-w-2xl flex-col">
        {PRICE_LEDGER.map((entry) => (
          <PriceRow key={entry.label} entry={entry} />
        ))}
      </ul>

      <div className="mt-10 flex flex-wrap items-center gap-3">
        <SolidButton href="/pricing">See full pricing</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to us about Enterprise
        </OutlineButton>
      </div>
      <p className="text-cc-nav-label mt-8 font-mono text-[0.65rem] tracking-[0.2em] uppercase">
        mit, on github, built in the open.
      </p>
    </StepBand>
  );
}

function ClosingMarker() {
  return (
    <section className="relative grid grid-cols-[3rem_1fr] gap-x-4 py-24 sm:grid-cols-[5rem_1fr] sm:gap-x-8 sm:py-32 lg:grid-cols-[6rem_1fr]">
      <div className="flex justify-center pt-2 sm:justify-start">
        <span
          aria-hidden
          className="bg-cc-accent block h-3 w-3"
          style={{ boxShadow: "0 0 0 4px rgba(11,15,26,1)" }}
        />
      </div>
      <div className="text-left">
        <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 font-semibold">
          Ship your GraphQL platform on .NET.
        </h2>
        <p className="text-cc-ink text-lead mt-5 max-w-2xl">
          Start with Hot Chocolate, wire up Strawberry Shake, plug in Nitro for
          the registry and telemetry. Free to start, MIT to keep.
        </p>
        <div className="mt-9 flex flex-wrap items-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="https://nitro.chillicream.com">
            Launch Nitro
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}

function BulletRow({ children }: { readonly children: ReactNode }) {
  return (
    <li className="flex items-start gap-3">
      <span className="text-cc-accent mt-[5px] flex-none">
        <CheckIcon />
      </span>
      <span className="text-cc-ink text-sm">{children}</span>
    </li>
  );
}

function StatRow({
  label,
  value,
}: {
  readonly label: string;
  readonly value: string;
}) {
  return (
    <div>
      <div className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.2em] uppercase">
        {label}
      </div>
      <div className="font-heading text-cc-heading mt-2 text-xl font-semibold">
        {value}
      </div>
    </div>
  );
}

function DiffLedgerRow({
  tag,
  tone,
  children,
}: {
  readonly tag: string;
  readonly tone: "add" | "remove" | "warn";
  readonly children: ReactNode;
}) {
  const toneClass =
    tone === "add"
      ? "text-cc-success"
      : tone === "remove"
        ? "text-cc-danger"
        : "text-cc-warning";
  return (
    <li className="border-cc-card-border flex items-start gap-3 border-b py-2.5 last:border-b-0">
      <span className={`w-3 flex-none font-bold ${toneClass}`}>{tag}</span>
      <span className="text-cc-ink">{children}</span>
    </li>
  );
}

function ClientRow({
  client,
  op,
}: {
  readonly client: string;
  readonly op: string;
}) {
  return (
    <li className="border-cc-card-border flex items-center justify-between border-b py-2.5 last:border-b-0">
      <span>{client}</span>
      <span className="text-cc-warning">{op}</span>
    </li>
  );
}

function McpRow({
  name,
  ops,
  p95,
}: {
  readonly name: string;
  readonly ops: string;
  readonly p95: string;
}) {
  return (
    <li className="border-cc-card-border flex items-center justify-between gap-4 border-b py-2.5 last:border-b-0">
      <span className="text-cc-heading">{name}</span>
      <span className="text-cc-ink-dim flex gap-5">
        <span>{ops}</span>
        <span className="text-cc-accent">{p95}</span>
      </span>
    </li>
  );
}

function SagaRow({
  step,
  label,
  state,
}: {
  readonly step: string;
  readonly label: string;
  readonly state: "ok" | "run" | "idle";
}) {
  const dot =
    state === "ok"
      ? "bg-cc-success"
      : state === "run"
        ? "bg-cc-accent animate-pulse"
        : "bg-cc-ink-faint";
  return (
    <li className="border-cc-card-border flex items-center gap-4 border-b py-2.5 last:border-b-0">
      <span className="text-cc-nav-label w-6 flex-none">{step}</span>
      <span className={`h-2 w-2 flex-none rounded-full ${dot}`} aria-hidden />
      <span className="text-cc-ink">{label}</span>
    </li>
  );
}

function FamilyRow({ entry }: { readonly entry: FamilyEntry }) {
  const Icon = entry.icon;
  const arrow = (
    <span
      aria-hidden
      className="text-cc-accent transition-transform group-hover:translate-x-0.5"
    >
      {"->"}
    </span>
  );
  const inner = (
    <>
      <Icon className="h-8 w-8 flex-none" />
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-baseline gap-x-3">
          <span className="font-heading text-cc-heading text-base font-semibold">
            {entry.name}
          </span>
          <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.2em] uppercase">
            {entry.tagline}
          </span>
        </div>
        <p className="text-cc-ink mt-1 text-sm leading-relaxed">
          {entry.description}
        </p>
      </div>
      <span className="self-center">{arrow}</span>
    </>
  );

  return (
    <li className="border-cc-card-border hover:border-cc-card-border-hover border-b last:border-b-0">
      {entry.external ? (
        <a
          href={entry.href}
          target="_blank"
          rel="noopener noreferrer"
          className="group flex items-start gap-5 py-5"
        >
          {inner}
        </a>
      ) : (
        <NextLink
          href={entry.href}
          className="group flex items-start gap-5 py-5"
        >
          {inner}
        </NextLink>
      )}
    </li>
  );
}

function PriceRow({ entry }: { readonly entry: PriceEntry }) {
  return (
    <li className="border-cc-card-border flex items-baseline justify-between gap-4 border-b py-4 last:border-b-0">
      <div className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.2em] uppercase">
        {entry.label}
      </div>
      <div className="flex items-baseline gap-4">
        <span className="font-heading text-cc-heading text-xl font-semibold">
          {entry.price}
        </span>
        <span className="text-cc-ink-dim font-mono text-[0.7rem]">
          {entry.note}
        </span>
      </div>
    </li>
  );
}
