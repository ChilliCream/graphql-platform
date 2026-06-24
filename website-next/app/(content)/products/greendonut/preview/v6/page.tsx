import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { GreenDonut } from "@/src/icons/GreenDonut";
import { PourOver } from "@/src/icons/PourOver";

export const metadata: Metadata = {
  title: "Green Donut: DataLoader for .NET",
  description:
    "Green Donut is the DataLoader for .NET. Kill N+1 in your resolvers with batching, per-request caching, dedup, and source-generated [DataLoader] wiring.",
  keywords: [
    "Green Donut",
    "DataLoader",
    ".NET DataLoader",
    "N+1 problem",
    "GraphQL batching",
    "request scoped cache",
    "Hot Chocolate",
    "C# resolvers",
    "AOT-friendly",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Green Donut: DataLoader for .NET",
    description:
      "Kill N+1 in your .NET resolvers. Batching, per-request caching, dedup, and the [DataLoader] attribute generate the wiring for you. MIT-licensed.",
    type: "website",
  },
};

// Brand spectrum, used at most once on this page (closing CTA rule).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// -----------------------------------------------------------------------------
// Small primitives
// -----------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span className="text-cc-accent text-caption font-mono font-medium tracking-[0.2em] uppercase">
      {children}
    </span>
  );
}

interface MonoCaptionProps {
  readonly children: ReactNode;
}

function MonoCaption({ children }: MonoCaptionProps) {
  return (
    <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.18em] uppercase">
      {children}
    </span>
  );
}

// -----------------------------------------------------------------------------
// Hero centerpiece: six tickets funnel through a single batch (an espresso
// shot) and emerge as six filled cups. Inline SVG, cc-* tokens, thin steam.
// -----------------------------------------------------------------------------

const TICKET_KEYS = ["id: 7", "id: 12", "id: 3", "id: 9", "id: 21", "id: 4"];

function TicketRailDiagram() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-6 shadow-2xl">
      <div className="flex items-center justify-between">
        <Eyebrow>Today&apos;s pour</Eyebrow>
        <MonoCaption>Behind the bar</MonoCaption>
      </div>

      <div className="mt-5 grid grid-cols-12 items-center gap-3">
        {/* Tickets rail */}
        <div className="col-span-5">
          <MonoCaption>Six tickets</MonoCaption>
          <ul className="mt-3 space-y-1.5">
            {TICKET_KEYS.map((k) => (
              <li
                key={k}
                className="border-cc-card-border bg-cc-bg/60 flex items-center justify-between rounded-md border px-3 py-1.5 font-mono text-[12px]"
              >
                <span className="text-cc-ink">{k}</span>
                <span className="text-cc-ink-dim text-[10.5px] tracking-[0.16em] uppercase">
                  GetUser
                </span>
              </li>
            ))}
          </ul>
        </div>

        {/* Funnel + espresso shot */}
        <div className="col-span-3 flex flex-col items-center">
          <svg
            viewBox="0 0 120 200"
            className="h-44 w-full"
            role="img"
            aria-label="Six tickets funnel into one batched pour"
          >
            <defs>
              <linearGradient id="gd-funnel" x1="0" y1="0" x2="1" y2="0">
                <stop offset="0%" stopColor="#5eead4" stopOpacity="0.25" />
                <stop offset="100%" stopColor="#5eead4" stopOpacity="0.9" />
              </linearGradient>
              <linearGradient id="gd-steam" x1="0" y1="1" x2="0" y2="0">
                <stop offset="0%" stopColor="#5eead4" stopOpacity="0" />
                <stop offset="100%" stopColor="#5eead4" stopOpacity="0.55" />
              </linearGradient>
            </defs>
            {/* Six lines converge to center */}
            {[20, 40, 60, 80, 100, 120].map((y, i) => (
              <path
                key={y}
                d={`M0,${y} C 40,${y} 50,100 60,100`}
                fill="none"
                stroke="url(#gd-funnel)"
                strokeWidth="1.25"
                strokeLinecap="round"
                opacity={0.55 + i * 0.06}
              />
            ))}
            {/* Espresso shot pill */}
            <rect
              x="44"
              y="92"
              width="32"
              height="16"
              rx="4"
              fill="#5eead4"
              fillOpacity="0.18"
              stroke="#5eead4"
              strokeOpacity="0.7"
            />
            <text
              x="60"
              y="103"
              textAnchor="middle"
              fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
              fontSize="9"
              fill="#5eead4"
            >
              BATCH
            </text>
            {/* Single pour line out */}
            <path
              d="M60,108 C 60,130 60,140 60,160"
              fill="none"
              stroke="#5eead4"
              strokeOpacity="0.85"
              strokeWidth="1.5"
              strokeLinecap="round"
            />
            {/* Steam (thin wavy line above the shot) */}
            <path
              d="M54,86 C 56,80 58,78 56,72 C 54,66 58,62 60,58"
              fill="none"
              stroke="url(#gd-steam)"
              strokeWidth="1.1"
              strokeLinecap="round"
            />
            <path
              d="M66,86 C 64,80 62,78 64,72 C 66,66 62,62 60,58"
              fill="none"
              stroke="url(#gd-steam)"
              strokeWidth="1.1"
              strokeLinecap="round"
              opacity="0.6"
            />
            {/* Cup outline at the bottom of the pour */}
            <path
              d="M50,160 L70,160 L67,178 L53,178 Z"
              fill="#5eead4"
              fillOpacity="0.15"
              stroke="#5eead4"
              strokeOpacity="0.7"
              strokeWidth="1.1"
            />
          </svg>
          <MonoCaption>One pour</MonoCaption>
        </div>

        {/* Cups served */}
        <div className="col-span-4">
          <MonoCaption>Six cups</MonoCaption>
          <ul className="mt-3 grid grid-cols-3 gap-2">
            {TICKET_KEYS.map((k) => (
              <li
                key={k}
                className="border-cc-card-border bg-cc-bg/60 flex flex-col items-center gap-1 rounded-md border px-2 py-2"
              >
                <svg viewBox="0 0 24 24" className="h-6 w-6" aria-hidden="true">
                  <path
                    d="M5 6 H18 L16 19 H7 Z"
                    fill="#5eead4"
                    fillOpacity="0.2"
                    stroke="#5eead4"
                    strokeOpacity="0.8"
                    strokeWidth="1.2"
                  />
                </svg>
                <span className="text-cc-ink font-mono text-[10.5px]">{k}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>

      {/* House blend strip */}
      <div className="mt-6">
        <div className="text-cc-ink-dim mb-2 font-mono text-[10.5px] tracking-[0.18em] uppercase">
          House blend
        </div>
        <div className="border-cc-card-border grid grid-cols-3 divide-x divide-[var(--color-cc-card-border)] overflow-hidden rounded-lg border text-center">
          <div className="px-3 py-3">
            <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
              Batch
            </div>
            <div className="text-cc-heading mt-1 font-mono text-[13px] font-semibold">
              one fetch
            </div>
          </div>
          <div className="px-3 py-3">
            <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
              Cache
            </div>
            <div className="text-cc-heading mt-1 font-mono text-[13px] font-semibold">
              per request
            </div>
          </div>
          <div className="px-3 py-3">
            <div className="text-cc-ink-dim font-mono text-[10.5px] tracking-[0.16em] uppercase">
              Dedup
            </div>
            <div className="text-cc-heading mt-1 font-mono text-[13px] font-semibold">
              same key, once
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Inline C# code snippet for the [DataLoader] attribute. Tokens scoped to this
// snippet only.
// -----------------------------------------------------------------------------

const C = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  comment: { color: "#8b949e", fontStyle: "italic" as const },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  plain: { color: "#c9d1d9" },
};

interface CodeLineProps {
  readonly n: number;
  readonly children: ReactNode;
}

function CodeLine({ n, children }: CodeLineProps) {
  return (
    <div className="flex gap-4 px-5">
      <span
        className="w-6 shrink-0 text-right font-mono text-[11px] text-[#484f58] tabular-nums select-none"
        aria-hidden
      >
        {n}
      </span>
      <span className="font-mono text-[12.5px] leading-6 whitespace-pre">
        {children}
      </span>
    </div>
  );
}

function DataLoaderCodeCard() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border relative overflow-hidden rounded-xl border shadow-2xl">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-4 py-3">
        <span
          className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span
          className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span
          className="bg-cc-success/70 h-2.5 w-2.5 rounded-full"
          aria-hidden
        />
        <span className="ml-3 font-mono text-[11px] text-[#8b949e]">
          UserDataLoader.cs
        </span>
      </div>

      <div className="py-4">
        <CodeLine n={1}>
          <span style={C.comment}>{"// One method. Wiring is generated."}</span>
        </CodeLine>
        <CodeLine n={2}>
          <span style={C.kw}>public static class</span>{" "}
          <span style={C.type}>UserDataLoader</span>
        </CodeLine>
        <CodeLine n={3}>
          <span style={C.plain}>{"{"}</span>
        </CodeLine>
        <CodeLine n={4}>
          {"    "}
          <span style={C.attr}>[DataLoader]</span>
        </CodeLine>
        <CodeLine n={5}>
          {"    "}
          <span style={C.kw}>public static async</span>{" "}
          <span style={C.type}>{"Task<IReadOnlyDictionary<int, User>>"}</span>{" "}
          <span style={C.fn}>GetUsersAsync</span>
          <span style={C.plain}>(</span>
        </CodeLine>
        <CodeLine n={6}>
          {"        "}
          <span style={C.type}>{"IReadOnlyList<int>"}</span>{" "}
          <span style={C.param}>ids</span>
          <span style={C.plain}>,</span>
        </CodeLine>
        <CodeLine n={7}>
          {"        "}
          <span style={C.type}>AppDbContext</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.plain}>,</span>
        </CodeLine>
        <CodeLine n={8}>
          {"        "}
          <span style={C.type}>CancellationToken</span>{" "}
          <span style={C.param}>ct</span>
          <span style={C.plain}>{")"}</span>
        </CodeLine>
        <CodeLine n={9}>
          {"        "}
          <span style={C.plain}>{"=>"}</span> <span style={C.kw}>await</span>{" "}
          <span style={C.param}>db</span>
          <span style={C.plain}>.Users</span>
        </CodeLine>
        <CodeLine n={10}>
          {"            "}
          <span style={C.plain}>.</span>
          <span style={C.fn}>Where</span>
          <span style={C.plain}>(u {"=>"} </span>
          <span style={C.param}>ids</span>
          <span style={C.plain}>.</span>
          <span style={C.fn}>Contains</span>
          <span style={C.plain}>(u.Id))</span>
        </CodeLine>
        <CodeLine n={11}>
          {"            "}
          <span style={C.plain}>.</span>
          <span style={C.fn}>ToDictionaryAsync</span>
          <span style={C.plain}>(u {"=>"} u.Id, </span>
          <span style={C.param}>ct</span>
          <span style={C.plain}>);</span>
        </CodeLine>
        <CodeLine n={12}>
          <span style={C.plain}>{"}"}</span>
        </CodeLine>
        <CodeLine n={13}> </CodeLine>
        <CodeLine n={14}>
          <span style={C.comment}>
            {"// Inject IUserDataLoader anywhere. Six keys arrive,"}
          </span>
        </CodeLine>
        <CodeLine n={15}>
          <span style={C.comment}>
            {"// one batched call goes out. Same key in the same"}
          </span>
        </CodeLine>
        <CodeLine n={16}>
          <span style={C.comment}>
            {"// request is served from the cache."}
          </span>
        </CodeLine>
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// House blend cards: Batching / Per-request cache / Dedup
// -----------------------------------------------------------------------------

interface BlendCardProps {
  readonly icon: ReactNode;
  readonly title: string;
  readonly body: string;
  readonly caption: string;
}

function BlendCard({ icon, title, body, caption }: BlendCardProps) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-full flex-col rounded-xl border p-6 transition-colors">
      <div className="text-cc-accent h-8 w-8">{icon}</div>
      <h3 className="text-cc-heading font-heading text-h5 mt-4">{title}</h3>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{body}</p>
      <div className="mt-auto pt-4">
        <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.16em] uppercase">
          {caption}
        </span>
      </div>
    </article>
  );
}

// -----------------------------------------------------------------------------
// Recipe steps for the [DataLoader] walkthrough
// -----------------------------------------------------------------------------

interface RecipeStepProps {
  readonly n: string;
  readonly title: string;
  readonly body: ReactNode;
}

function RecipeStep({ n, title, body }: RecipeStepProps) {
  return (
    <li className="flex gap-4">
      <span className="text-cc-accent shrink-0 font-mono text-[13px] tabular-nums">
        {n}
      </span>
      <div>
        <div className="text-cc-heading text-sm font-semibold">{title}</div>
        <div className="text-cc-ink-dim mt-1 text-sm leading-relaxed">
          {body}
        </div>
      </div>
    </li>
  );
}

// -----------------------------------------------------------------------------
// Order flow node (counter pipeline)
// -----------------------------------------------------------------------------

interface FlowNodeProps {
  readonly label: string;
  readonly sub: string;
  readonly glyph?: ReactNode;
}

function FlowNode({ label, sub, glyph }: FlowNodeProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative flex w-full flex-col items-center rounded-xl border px-4 py-5 text-center">
      <div className="text-cc-accent flex h-10 items-center justify-center">
        {glyph ?? (
          <span className="bg-cc-accent h-2 w-2 rounded-full" aria-hidden />
        )}
      </div>
      <div className="text-cc-heading mt-2 text-sm font-semibold">{label}</div>
      <div className="text-cc-ink-dim mt-1 font-mono text-[11px] tracking-[0.16em] uppercase">
        {sub}
      </div>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export default function GreenDonutV6Page() {
  return (
    <main className="mx-auto w-full max-w-7xl px-6 pt-16 pb-24 sm:px-8 lg:px-10">
      {/* HERO */}
      <section className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-12">
        <div className="lg:col-span-6">
          <div className="flex items-center gap-3">
            <GreenDonut className="h-9 w-9" />
            <Eyebrow>On the menu</Eyebrow>
          </div>

          <h1 className="font-heading text-cc-heading text-h1 mt-6">
            DataLoader for .NET.
          </h1>

          <p className="lead text-cc-ink mt-5 max-w-xl">
            Green Donut kills N+1 in your resolvers the way a barista kills a
            morning rush. Six tickets arrive, one batch goes out, six cups come
            back. Per request caching, automatic dedup, source generated wiring,
            MIT licensed.
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-3">
            <SolidButton href="/docs/greendonut">Get the bag</SolidButton>
            <OutlineButton href="/docs/greendonut">
              Read the brew guide
            </OutlineButton>
          </div>

          <ul className="text-cc-ink-dim mt-8 grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
              Batching, caching, dedup
            </li>
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
              [DataLoader] attribute
            </li>
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
              Per request scoped cache
            </li>
            <li className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
              Auto discovered by Hot Chocolate
            </li>
          </ul>
        </div>

        <div className="lg:col-span-6">
          <TicketRailDiagram />
        </div>
      </section>

      {/* HOUSE BLEND */}
      <section className="mt-24">
        <div className="flex flex-col items-start gap-3">
          <Eyebrow>House blend</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h2 max-w-3xl">
            Three things the barista does without thinking.
          </h2>
          <p className="text-cc-ink max-w-3xl">
            Batching, per request caching, and dedup. They sound like a list of
            tricks. In Green Donut they are the default behavior of every loader
            you write.
          </p>
        </div>

        <div className="mt-10 grid grid-cols-1 gap-5 md:grid-cols-3">
          <BlendCard
            icon={<DripBrewer className="h-8 w-8" />}
            title="Batching"
            body="Resolvers ask for keys. Green Donut collects the keys that arrive on the same tick, sends one batched fetch, and returns each result in the original key order."
            caption="one pour, many cups"
          />
          <BlendCard
            icon={<FrenchPress className="h-8 w-8" />}
            title="Per-request cache"
            body="Every loader has a per request scoped cache. The same key resolved twice in one request returns the same task. Concurrent requests never share each other's data."
            caption="same request, same cup"
          />
          <BlendCard
            icon={<PourOver className="h-8 w-8" />}
            title="Dedup"
            body="Repeat keys inside a single batch collapse. The fetch goes out with distinct keys, the cache hands each caller its result, and the database never sees the duplicate."
            caption="one ticket per key"
          />
        </div>
      </section>

      {/* BEHIND THE BAR, recipe card */}
      <section className="mt-24">
        <div className="grid grid-cols-1 gap-10 lg:grid-cols-12 lg:gap-12">
          <div className="lg:col-span-7">
            <DataLoaderCodeCard />
          </div>

          <div className="lg:col-span-5">
            <Eyebrow>Behind the bar</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h2 mt-4">
              The recipe card.
            </h2>
            <p className="text-cc-ink mt-4">
              Mark a static method with{" "}
              <span className="text-cc-heading font-mono text-[13px]">
                [DataLoader]
              </span>
              . Take an{" "}
              <span className="text-cc-heading font-mono text-[13px]">
                IReadOnlyList&lt;TKey&gt;
              </span>
              , return an{" "}
              <span className="text-cc-heading font-mono text-[13px]">
                IReadOnlyDictionary&lt;TKey, TValue&gt;
              </span>
              . The source generator handles the rest.
            </p>

            <ol className="mt-6 space-y-4">
              <RecipeStep
                n="01"
                title="Write one method"
                body="A static method that takes the keys and any DI dependencies it needs. Plain C#, no base class, no ceremony."
              />
              <RecipeStep
                n="02"
                title="Mark with [DataLoader]"
                body={
                  <>
                    Add the{" "}
                    <span className="text-cc-heading font-mono text-[12.5px]">
                      [DataLoader]
                    </span>{" "}
                    attribute. That is the entire opt in.
                  </>
                }
              />
              <RecipeStep
                n="03"
                title="Source generator pours the wiring"
                body="At build time, Green Donut emits the loader type, the interface, and the DI registration. No reflection, AOT friendly."
              />
              <RecipeStep
                n="04"
                title="Inject IUserDataLoader anywhere"
                body="Resolvers, workers, controllers, console apps. Same LoadAsync entry point, same batching, same per request scope."
              />
            </ol>
          </div>
        </div>
      </section>

      {/* THE ORDER FLOW, counter pipeline */}
      <section className="mt-24">
        <div className="flex flex-col items-start gap-3">
          <Eyebrow>The order flow</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h2 max-w-3xl">
            Resolver asks, ticket queued, one batch pulled, cup served.
          </h2>
        </div>

        <div className="mt-10">
          <div className="grid grid-cols-1 items-stretch gap-4 sm:grid-cols-4">
            <FlowNode
              label="Resolver asks"
              sub="LoadAsync(key)"
              glyph={
                <span
                  className="bg-cc-accent h-2 w-2 rounded-full"
                  aria-hidden
                />
              }
            />
            <FlowNode
              label="Ticket queued"
              sub="same tick"
              glyph={
                <svg viewBox="0 0 24 24" className="h-8 w-8" aria-hidden="true">
                  <rect
                    x="5"
                    y="6"
                    width="14"
                    height="12"
                    rx="1.5"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.4"
                  />
                  <line
                    x1="8"
                    y1="10"
                    x2="16"
                    y2="10"
                    stroke="currentColor"
                    strokeWidth="1.2"
                  />
                  <line
                    x1="8"
                    y1="13"
                    x2="13"
                    y2="13"
                    stroke="currentColor"
                    strokeWidth="1.2"
                  />
                </svg>
              }
            />
            <FlowNode
              label="One batch pulled"
              sub="single fetch"
              glyph={<Espresso className="h-9 w-9" />}
            />
            <FlowNode
              label="Cup served"
              sub="results by key"
              glyph={<CoffeeTray className="h-9 w-9" />}
            />
          </div>
          <p className="text-cc-ink-dim mt-6 font-mono text-[12px] tracking-[0.16em] uppercase">
            One tick of the event loop = one trip to the bar.
          </p>
        </div>
      </section>

      {/* TASTING NOTES, feature grid */}
      <section className="mt-24">
        <div className="flex flex-col items-start gap-3">
          <Eyebrow>Tasting notes</Eyebrow>
          <h2 className="font-heading text-cc-heading text-h2 max-w-3xl">
            What the library actually gives you.
          </h2>
        </div>

        <div className="border-cc-card-border bg-cc-card-bg mt-10 overflow-hidden rounded-xl border">
          <div className="grid grid-cols-1 divide-y divide-[var(--color-cc-card-border)] sm:grid-cols-2 sm:divide-x sm:divide-y-0">
            <ul className="divide-y divide-[var(--color-cc-card-border)]">
              <li className="flex items-start gap-3 px-6 py-5">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <div>
                  <div className="text-cc-heading text-sm font-semibold">
                    AOT friendly
                  </div>
                  <div className="text-cc-ink-dim text-sm">
                    No runtime reflection in the generated wiring. Works under
                    NativeAOT and trimmed builds.
                  </div>
                </div>
              </li>
              <li className="flex items-start gap-3 px-6 py-5">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <div>
                  <div className="text-cc-heading text-sm font-semibold">
                    Source-generated wiring
                  </div>
                  <div className="text-cc-ink-dim text-sm">
                    The loader type, its interface, and the DI registration are
                    emitted at build time from a single attributed method.
                  </div>
                </div>
              </li>
              <li className="flex items-start gap-3 px-6 py-5">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <div>
                  <div className="text-cc-heading text-sm font-semibold">
                    IReadOnlyDictionary returns
                  </div>
                  <div className="text-cc-ink-dim text-sm">
                    Return one shape and Green Donut looks results up by key.
                    Missing keys yield null at the call site.
                  </div>
                </div>
              </li>
            </ul>
            <ul className="divide-y divide-[var(--color-cc-card-border)]">
              <li className="flex items-start gap-3 px-6 py-5">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <div>
                  <div className="text-cc-heading text-sm font-semibold">
                    Request-scoped lifetime
                  </div>
                  <div className="text-cc-ink-dim text-sm">
                    The default cache scope is the request. Concurrent requests
                    cannot see each other&apos;s entries.
                  </div>
                </div>
              </li>
              <li className="flex items-start gap-3 px-6 py-5">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <div>
                  <div className="text-cc-heading text-sm font-semibold">
                    Cancellation-aware
                  </div>
                  <div className="text-cc-ink-dim text-sm">
                    The request cancellation token flows through to the database
                    driver, not just to the loader boundary.
                  </div>
                </div>
              </li>
              <li className="flex items-start gap-3 px-6 py-5">
                <span className="text-cc-accent mt-[3px]">
                  <CheckIcon />
                </span>
                <div>
                  <div className="text-cc-heading text-sm font-semibold">
                    MIT licensed
                  </div>
                  <div className="text-cc-ink-dim text-sm">
                    Ships in the same repo as Hot Chocolate, Fusion, Strawberry
                    Shake, and Cookie Crumble. No per request fee.
                  </div>
                </div>
              </li>
            </ul>
          </div>
        </div>
      </section>

      {/* PAIRS WELL WITH */}
      <section className="mt-24">
        <div className="flex items-center gap-3">
          <CoffeeTray className="h-8 w-8" />
          <Eyebrow>Pairs well with</Eyebrow>
        </div>
        <h2 className="font-heading text-cc-heading text-h2 mt-4 max-w-3xl">
          Sits at home in a Hot Chocolate server, or runs anywhere .NET runs.
        </h2>

        <div className="mt-10 grid grid-cols-1 gap-5 md:grid-cols-3">
          <article className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-xl border p-6">
            <MonoCaption>Hot Chocolate</MonoCaption>
            <h3 className="text-cc-heading font-heading text-h5 mt-3">
              Auto discovered
            </h3>
            <p className="text-cc-ink mt-3 text-sm leading-relaxed">
              Loaders marked with{" "}
              <span className="text-cc-heading font-mono text-[12.5px]">
                [DataLoader]
              </span>{" "}
              are picked up at startup and registered in DI. Resolvers just
              inject the generated interface.
            </p>
          </article>
          <article className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-xl border p-6">
            <MonoCaption>Standalone .NET</MonoCaption>
            <h3 className="text-cc-heading font-heading text-h5 mt-3">
              Workers, APIs, CLIs
            </h3>
            <p className="text-cc-ink mt-3 text-sm leading-relaxed">
              Use Green Donut in a background worker, a REST controller, or a
              console app. Same{" "}
              <span className="text-cc-heading font-mono text-[12.5px]">
                LoadAsync
              </span>{" "}
              entry point, same batching, same per request scope.
            </p>
          </article>
          <article className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-xl border p-6">
            <MonoCaption>The ChilliCream bag</MonoCaption>
            <h3 className="text-cc-heading font-heading text-h5 mt-3">
              Same repo, same release
            </h3>
            <p className="text-cc-ink mt-3 text-sm leading-relaxed">
              Green Donut ships alongside Hot Chocolate, Fusion, Strawberry
              Shake, and Cookie Crumble. One repository, one release train, MIT
              licensed.
            </p>
          </article>
        </div>
      </section>

      {/* FROM THE REGULARS, quote card */}
      <section className="mt-24">
        <div className="border-cc-card-border bg-cc-card-bg rounded-xl border p-8 sm:p-10">
          <Eyebrow>From the regulars</Eyebrow>
          <p className="text-cc-heading font-heading text-h4 mt-5 max-w-4xl">
            &ldquo;The DataLoader attribute removed an entire layer of
            boilerplate. We mark the method, the wiring shows up, and the N+1 on
            our customer and line item resolvers is gone.&rdquo;
          </p>
          <div className="text-cc-ink-dim mt-6 text-sm">
            ChilliCream community, on the{" "}
            <span className="text-cc-heading font-mono text-[12.5px]">
              [DataLoader]
            </span>{" "}
            attribute.
          </div>
        </div>
      </section>

      {/* CLOSING CTA, full bleed strip, spectrum used once */}
      <section className="mt-24">
        <div className="rounded-2xl p-[1px]" style={{ background: SPECTRUM }}>
          <div className="bg-cc-surface rounded-[15px] px-8 py-14 text-center sm:px-12">
            <Eyebrow>Ready to pour?</Eyebrow>
            <h2 className="font-heading text-cc-heading text-h2 mx-auto mt-4 max-w-3xl">
              Six resolver round trips. One batched fetch. Same code path.
            </h2>
            <div className="mt-6 flex justify-center">
              <span className="border-cc-card-border text-cc-heading inline-flex items-center rounded-full border px-4 py-2 font-mono text-[12.5px]">
                dotnet add package GreenDonut
              </span>
            </div>
            <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
              <SolidButton href="/docs/greendonut">Get the bag</SolidButton>
              <OutlineButton href="/docs/greendonut">
                Open the brew guide
              </OutlineButton>
            </div>
            <p className="text-cc-ink-dim mt-8 text-sm">
              MIT-licensed. Brewed by ChilliCream.
            </p>
          </div>
        </div>
      </section>
    </main>
  );
}
