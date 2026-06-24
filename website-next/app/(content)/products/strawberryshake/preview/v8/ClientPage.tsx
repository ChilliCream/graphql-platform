"use client";

import { motion } from "motion/react";
import type { CSSProperties, ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

// -----------------------------------------------------------------------------
// Tokens local to this stance.
// Coral is the single accent for this page. The brand spectrum appears AT MOST
// once, as the twine across the closing CTA card.
// -----------------------------------------------------------------------------

const CORAL = "#f0786a";
const CORAL_SOFT = "rgba(240, 120, 106, 0.18)";
const CORAL_INK = "rgba(240, 120, 106, 0.85)";

const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// Faint paper grain layered onto the desk surface. Inline CSS only.
const DESK_BG: CSSProperties = {
  backgroundColor: "#0b0f1a",
  backgroundImage: [
    "radial-gradient(900px 480px at 18% 6%, rgba(245, 241, 234, 0.04), transparent 65%)",
    "radial-gradient(720px 420px at 92% 88%, rgba(240, 120, 106, 0.05), transparent 70%)",
    "repeating-linear-gradient(115deg, rgba(245, 241, 234, 0.012) 0px, rgba(245, 241, 234, 0.012) 1px, transparent 1px, transparent 7px)",
    "repeating-linear-gradient(0deg, rgba(245, 241, 234, 0.018) 0px, rgba(245, 241, 234, 0.018) 1px, transparent 1px, transparent 96px)",
  ].join(", "),
};

// GitHub-dark token approximations, scoped to the inline code blocks.
const T: Record<string, CSSProperties> = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  str: { color: "#a5d6ff" },
  num: { color: "#79c0ff" },
  comment: { color: "#8b949e", fontStyle: "italic" },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  punct: { color: "#c9d1d9" },
  plain: { color: "#c9d1d9" },
  dim: { color: "#8b949e" },
  gqlKw: { color: "#ff7b72" },
  gqlType: { color: "#ffa657" },
  gqlField: { color: "#7ee787" },
  gqlVar: { color: "#79c0ff" },
};

// -----------------------------------------------------------------------------
// Small primitives.
// -----------------------------------------------------------------------------

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <span
      className="text-caption font-mono font-medium tracking-[0.2em] uppercase"
      style={{ color: CORAL }}
    >
      {children}
    </span>
  );
}

interface CodeLineProps {
  readonly n: number;
  readonly children: ReactNode;
}

function CodeLine({ n, children }: CodeLineProps) {
  return (
    <div className="flex gap-3 px-4">
      <span
        className="w-5 shrink-0 text-right font-mono text-[10.5px] text-[#484f58] tabular-nums select-none"
        aria-hidden
      >
        {n}
      </span>
      <span className="font-mono text-[11.5px] leading-5 whitespace-pre">
        {children}
      </span>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Postcard chrome. The collage primitive: dotted perforated top edge, stamp box
// with serial, circular postmark, handwritten subject, body, footer address.
// -----------------------------------------------------------------------------

interface PostcardProps {
  readonly serial: string;
  readonly postmark: string;
  readonly date: string;
  readonly subject: string;
  readonly body?: ReactNode;
  readonly photo?: ReactNode;
  readonly bullets?: readonly string[];
  readonly footerTo?: string;
  readonly tilt?: number;
  readonly className?: string;
  readonly size?: "sm" | "md" | "lg" | "xl";
}

function Postcard({
  serial,
  postmark,
  date,
  subject,
  body,
  photo,
  bullets,
  footerTo = ".NET teams",
  tilt = 0,
  className = "",
  size = "md",
}: PostcardProps) {
  return (
    <motion.article
      initial={{ opacity: 0, rotate: tilt * 1.6, y: 16 }}
      whileInView={{ opacity: 1, rotate: tilt, y: 0 }}
      viewport={{ once: true, margin: "-60px" }}
      transition={{ duration: 0.55, ease: "easeOut" }}
      className={[
        "group bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover relative rounded-md border shadow-[0_18px_40px_-24px_rgba(0,0,0,0.7)] transition-[transform,border-color] duration-300 hover:-translate-y-1 hover:!rotate-0",
        size === "sm" ? "p-4" : "p-5 sm:p-6",
        className,
      ].join(" ")}
      style={{ transformOrigin: "50% 50%" }}
    >
      {/* Perforated top edge */}
      <div
        aria-hidden
        className="border-cc-card-border absolute inset-x-3 top-0 border-t border-dotted"
        style={{ borderColor: "rgba(245, 241, 234, 0.28)" }}
      />

      {/* Stamp box, top-right: serial + coral wash */}
      <div
        className="absolute top-3 right-3 flex h-16 w-12 flex-col items-center justify-center rounded-[3px] border"
        style={{
          borderColor: CORAL_INK,
          background: CORAL_SOFT,
          boxShadow: "inset 0 0 0 1px rgba(240, 120, 106, 0.18)",
        }}
        aria-hidden
      >
        <span
          className="font-mono text-[8.5px] tracking-widest uppercase"
          style={{ color: CORAL }}
        >
          ChilliCream
        </span>
        <span
          className="mt-1 font-mono text-[10px] font-semibold tabular-nums"
          style={{ color: CORAL }}
        >
          {serial}
        </span>
        <span
          className="mt-0.5 font-mono text-[7.5px] tracking-wider uppercase"
          style={{ color: CORAL_INK }}
        >
          POSTAGE
        </span>
      </div>

      {/* Header strip: POSTMARKED + date */}
      <div className="mt-4 mb-3 flex items-baseline justify-between gap-4 pr-14">
        <span className="text-cc-ink-dim font-mono text-[10px] tracking-[0.18em] uppercase">
          POSTMARKED <span className="text-cc-ink">{postmark}</span>
        </span>
        <span className="text-cc-ink-dim font-mono text-[10px] tabular-nums">
          {date}
        </span>
      </div>

      {/* Handwritten subject */}
      <h3
        className={[
          "text-cc-heading font-heading italic",
          size === "xl"
            ? "text-3xl sm:text-4xl"
            : size === "lg"
              ? "text-2xl sm:text-[1.75rem]"
              : size === "sm"
                ? "text-lg"
                : "text-xl sm:text-2xl",
          "font-semibold tracking-tight text-balance",
        ].join(" ")}
      >
        {subject}
      </h3>

      {/* Photo area (optional) */}
      {photo ? <div className="mt-4">{photo}</div> : null}

      {/* Body fact */}
      {body ? (
        <p className="text-cc-prose mt-3 text-sm leading-relaxed sm:text-[15px]">
          {body}
        </p>
      ) : null}

      {/* Bullet facts */}
      {bullets ? (
        <ul className="mt-3 flex flex-col gap-2">
          {bullets.map((b) => (
            <li
              key={b}
              className="text-cc-ink flex items-start gap-2.5 text-[13.5px] leading-relaxed"
            >
              <span className="mt-1 shrink-0" style={{ color: CORAL }}>
                <CheckIcon size={12} />
              </span>
              <span>{b}</span>
            </li>
          ))}
        </ul>
      ) : null}

      {/* Footer: FROM / TO + circular postmark */}
      <div className="border-cc-card-border mt-5 flex items-end justify-between gap-4 border-t pt-3">
        <div className="flex flex-col gap-0.5">
          <span className="text-cc-ink-dim font-mono text-[9.5px] tracking-[0.2em] uppercase">
            FROM: ChilliCream
          </span>
          <span className="text-cc-ink-dim font-mono text-[9.5px] tracking-[0.2em] uppercase">
            TO: {footerTo}
          </span>
        </div>
        <PostmarkSeal />
      </div>
    </motion.article>
  );
}

// Circular ink postmark, lower-right of each card.
function PostmarkSeal() {
  return (
    <motion.svg
      aria-hidden
      viewBox="0 0 64 64"
      className="h-12 w-12 shrink-0"
      style={{ transform: "rotate(-12deg)", color: CORAL }}
      initial={{ opacity: 0, scale: 0.85 }}
      whileInView={{ opacity: 1, scale: 1 }}
      viewport={{ once: true }}
      transition={{ duration: 0.6, ease: "easeOut", delay: 0.2 }}
    >
      <circle
        cx="32"
        cy="32"
        r="26"
        fill="none"
        stroke="currentColor"
        strokeOpacity="0.7"
        strokeWidth="1.2"
      />
      <circle
        cx="32"
        cy="32"
        r="20"
        fill="none"
        stroke="currentColor"
        strokeOpacity="0.45"
        strokeWidth="0.8"
        strokeDasharray="2 3"
      />
      <text
        x="32"
        y="29"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="6.5"
        fill="currentColor"
        fillOpacity="0.85"
        letterSpacing="1.2"
      >
        DOTNET
      </text>
      <text
        x="32"
        y="37"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="5.5"
        fill="currentColor"
        fillOpacity="0.7"
        letterSpacing="0.9"
      >
        BUILD
      </text>
      <text
        x="32"
        y="46"
        textAnchor="middle"
        fontFamily="ui-monospace, monospace"
        fontSize="4.5"
        fill="currentColor"
        fillOpacity="0.55"
        letterSpacing="0.7"
      >
        TYPED
      </text>
    </motion.svg>
  );
}

// -----------------------------------------------------------------------------
// Photo content blocks (the "image" area inside a postcard).
// -----------------------------------------------------------------------------

interface PhotoFrameProps {
  readonly file: string;
  readonly tag: string;
  readonly children: ReactNode;
}

function PhotoFrame({ file, tag, children }: PhotoFrameProps) {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-md border">
      <div className="bg-cc-code-header border-cc-card-border flex items-center gap-2 border-b px-3 py-2">
        <span className="text-cc-ink-dim font-mono text-[10px]">{file}</span>
        <span
          className="border-cc-card-border text-cc-ink-dim ml-auto inline-flex items-center rounded-full border px-2 py-0.5 font-mono text-[9px] tracking-wider uppercase"
          style={{ borderColor: CORAL_INK, color: CORAL }}
        >
          {tag}
        </span>
      </div>
      <div className="py-2.5">{children}</div>
    </div>
  );
}

function GraphqlOperationPhoto() {
  return (
    <PhotoFrame file="Catalog/GetProduct.graphql" tag="GraphQL">
      <CodeLine n={1}>
        <span style={T.gqlKw}>query</span>{" "}
        <span style={T.gqlType}>GetProduct</span>
        <span style={T.punct}>(</span>
        <span style={T.gqlVar}>$id</span>
        <span style={T.punct}>: </span>
        <span style={T.gqlType}>ID!</span>
        <span style={T.punct}>) {`{`}</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={T.plain}>{`  `}</span>
        <span style={T.gqlField}>productById</span>
        <span style={T.punct}>(</span>
        <span style={T.gqlField}>id</span>
        <span style={T.punct}>: </span>
        <span style={T.gqlVar}>$id</span>
        <span style={T.punct}>) {`{`}</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={T.plain}>{`    `}</span>
        <span style={T.gqlField}>id</span>
        <span style={T.plain}> </span>
        <span style={T.gqlField}>name</span>
        <span style={T.plain}> </span>
        <span style={T.gqlField}>priceCents</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={T.plain}>{`    `}</span>
        <span style={T.gqlField}>variants</span>
        <span style={T.punct}> {`{`} </span>
        <span style={T.gqlField}>sku</span>
        <span style={T.plain}> </span>
        <span style={T.gqlField}>inStock</span>
        <span style={T.punct}> {`}`}</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={T.plain}>{`  `}</span>
        <span style={T.punct}>{`}`}</span>
      </CodeLine>
      <CodeLine n={6}>
        <span style={T.punct}>{`}`}</span>
      </CodeLine>
    </PhotoFrame>
  );
}

function CallSitePhoto() {
  return (
    <PhotoFrame file="ProductService.cs" tag="C#">
      <CodeLine n={1}>
        <span style={T.kw}>using</span>{" "}
        <span style={T.plain}>Catalog.Client;</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={T.plain}>&nbsp;</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={T.kw}>var</span> <span style={T.param}>client</span>{" "}
        <span style={T.punct}>=</span> <span style={T.param}>services</span>
        <span style={T.punct}>.</span>
        <span style={T.fn}>GetRequiredService</span>
        <span style={T.punct}>{`<`}</span>
        <span style={T.type}>ICatalogClient</span>
        <span style={T.punct}>{`>`}</span>
        <span style={T.punct}>();</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={T.kw}>var</span> <span style={T.param}>result</span>{" "}
        <span style={T.punct}>=</span> <span style={T.kw}>await</span>{" "}
        <span style={T.param}>client</span>
        <span style={T.punct}>.</span>
        <span style={T.plain}>GetProduct</span>
        <span style={T.punct}>.</span>
        <span style={T.fn}>ExecuteAsync</span>
        <span style={T.punct}>(</span>
        <span style={T.param}>id</span>
        <span style={T.punct}>);</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={T.type}>Product</span> <span style={T.param}>product</span>{" "}
        <span style={T.punct}>=</span> <span style={T.param}>result</span>
        <span style={T.punct}>.</span>
        <span style={T.plain}>Data</span>
        <span style={T.punct}>!.</span>
        <span style={T.plain}>ProductById</span>
        <span style={T.punct}>;</span>
      </CodeLine>
      <CodeLine n={6}>
        <span
          style={T.comment}
        >{`// typed record, nullable-aware, IDE-completed`}</span>
      </CodeLine>
    </PhotoFrame>
  );
}

function RazorSubscriptionPhoto() {
  return (
    <PhotoFrame file="PriceTicker.razor" tag="Razor">
      <CodeLine n={1}>
        <span style={T.punct}>&lt;</span>
        <span style={T.type}>UseSubscription</span>{" "}
        <span style={T.param}>TResult</span>
        <span style={T.punct}>=</span>
        <span style={T.str}>&quot;OnPriceChangedResult&quot;</span>
      </CodeLine>
      <CodeLine n={2}>
        <span style={T.plain}>{`  `}</span>
        <span style={T.param}>Subscribe</span>
        <span style={T.punct}>=</span>
        <span style={T.str}>
          &quot;@(c =&gt; c.OnPriceChanged.Watch(sku))&quot;
        </span>
        <span style={T.punct}>&gt;</span>
      </CodeLine>
      <CodeLine n={3}>
        <span style={T.plain}>{`  `}</span>
        <span style={T.punct}>&lt;</span>
        <span style={T.type}>ChildContent</span>
        <span style={T.punct}>&gt;</span>
      </CodeLine>
      <CodeLine n={4}>
        <span style={T.plain}>{`    `}</span>
        <span style={T.punct}>@</span>
        <span style={T.kw}>if</span>{" "}
        <span style={T.punct}>(context.Data is {} d)</span>
      </CodeLine>
      <CodeLine n={5}>
        <span style={T.plain}>{`      `}</span>
        <span style={T.punct}>&lt;</span>
        <span style={T.type}>span</span>
        <span style={T.punct}>&gt;@d.PriceChanged.PriceCents&lt;/</span>
        <span style={T.type}>span</span>
        <span style={T.punct}>&gt;</span>
      </CodeLine>
      <CodeLine n={6}>
        <span style={T.plain}>{`  `}</span>
        <span style={T.punct}>&lt;/</span>
        <span style={T.type}>ChildContent</span>
        <span style={T.punct}>&gt;</span>
      </CodeLine>
      <CodeLine n={7}>
        <span style={T.punct}>&lt;/</span>
        <span style={T.type}>UseSubscription</span>
        <span style={T.punct}>&gt;</span>
      </CodeLine>
    </PhotoFrame>
  );
}

// Inline SVG diagram, postcard "photo" version of the MSBuild codegen flow.
function CodegenFlowPhoto() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-md border p-3">
      <svg
        viewBox="0 0 360 160"
        className="h-auto w-full"
        role="img"
        aria-label="dotnet build runs MSBuild codegen across .graphql operations and schema.graphql to emit typed C# clients"
      >
        {[
          { y: 12, label: "schema.graphql", sub: "downloaded by CLI" },
          { y: 60, label: "GetProduct.graphql", sub: "your operations" },
          { y: 108, label: ".graphqlrc.json", sub: "name + namespace" },
        ].map((n) => (
          <g key={n.label}>
            <rect
              x="6"
              y={n.y}
              width="128"
              height="34"
              rx="4"
              fill="rgba(245,241,234,0.04)"
              stroke="rgba(245,241,234,0.18)"
            />
            <text
              x="14"
              y={n.y + 15}
              fontFamily="ui-monospace, monospace"
              fontSize="9"
              fill="#f5f0ea"
            >
              {n.label}
            </text>
            <text
              x="14"
              y={n.y + 27}
              fontFamily="ui-monospace, monospace"
              fontSize="8"
              fill="rgba(245,241,234,0.55)"
            >
              {n.sub}
            </text>
            <path
              d={`M 134 ${n.y + 17} L 196 80`}
              stroke={CORAL_INK}
              strokeOpacity="0.6"
              strokeWidth="1"
              fill="none"
            />
          </g>
        ))}
        <rect
          x="196"
          y="62"
          width="84"
          height="38"
          rx="6"
          fill={CORAL_SOFT}
          stroke={CORAL_INK}
        />
        <text
          x="238"
          y="78"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="9"
          fill={CORAL}
        >
          MSBuild
        </text>
        <text
          x="238"
          y="91"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="8"
          fill="rgba(245,241,234,0.7)"
        >
          codegen task
        </text>
        <path
          d="M 280 80 L 326 80"
          stroke={CORAL}
          strokeOpacity="0.8"
          strokeWidth="1.2"
          fill="none"
        />
        <polygon points="326,76 338,80 326,84" fill={CORAL} />
        <text
          x="352"
          y="73"
          textAnchor="end"
          fontFamily="ui-monospace, monospace"
          fontSize="9"
          fill="#f5f0ea"
        >
          CatalogClient.cs
        </text>
        <text
          x="352"
          y="86"
          textAnchor="end"
          fontFamily="ui-monospace, monospace"
          fontSize="8"
          fill="rgba(245,241,234,0.55)"
        >
          records, DI, store
        </text>
      </svg>
    </div>
  );
}

function ReactiveStorePhoto() {
  return (
    <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-md border p-3">
      <svg
        viewBox="0 0 360 160"
        className="h-auto w-full"
        role="img"
        aria-label="Two queries denormalized into one entity row, components subscribe to changes"
      >
        {[
          { y: 16, label: "GetProduct(id: 42)" },
          { y: 68, label: "ListProducts(first: 10)" },
        ].map((q) => (
          <g key={q.label}>
            <rect
              x="6"
              y={q.y}
              width="130"
              height="28"
              rx="4"
              fill="rgba(245,241,234,0.04)"
              stroke="rgba(245,241,234,0.18)"
            />
            <text
              x="14"
              y={q.y + 18}
              fontFamily="ui-monospace, monospace"
              fontSize="9"
              fill="rgba(245,241,234,0.7)"
            >
              {q.label}
            </text>
            <path
              d={`M 136 ${q.y + 14} C 174 ${q.y + 14}, 174 80, 204 80`}
              stroke={CORAL_INK}
              strokeOpacity="0.7"
              strokeWidth="1.1"
              fill="none"
            />
          </g>
        ))}
        <rect
          x="204"
          y="60"
          width="104"
          height="40"
          rx="6"
          fill={CORAL_SOFT}
          stroke={CORAL_INK}
        />
        <text
          x="256"
          y="76"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="9"
          fill={CORAL}
        >
          EntityStore
        </text>
        <text
          x="256"
          y="90"
          textAnchor="middle"
          fontFamily="ui-monospace, monospace"
          fontSize="8"
          fill="rgba(245,241,234,0.7)"
        >
          Product#42 (one row)
        </text>
        {[116, 142].map((y, i) => (
          <g key={y}>
            <path
              d={`M 308 80 C 332 80, 332 ${y}, 354 ${y}`}
              stroke={CORAL_INK}
              strokeOpacity="0.55"
              strokeWidth="1"
              fill="none"
            />
            <text
              x="354"
              y={y + 3}
              textAnchor="end"
              fontFamily="ui-monospace, monospace"
              fontSize="8"
              fill="rgba(245,241,234,0.7)"
            >
              {i === 0 ? "Watch()" : "UseQuery"}
            </text>
          </g>
        ))}
        <text
          x="6"
          y="148"
          fontFamily="ui-monospace, monospace"
          fontSize="8"
          fill="rgba(245,241,234,0.45)"
        >
          normalized, deduplicated, reactive
        </text>
      </svg>
    </div>
  );
}

// -----------------------------------------------------------------------------
// Page
// -----------------------------------------------------------------------------

export function ClientPage() {
  return (
    <div className="relative" style={DESK_BG}>
      {/* HERO: overlapping postcards + address-label copy */}
      <section className="pt-12 pb-16 sm:pt-20 sm:pb-20">
        <div className="grid items-center gap-12 lg:grid-cols-12 lg:gap-10">
          {/* Address block, left */}
          <div className="lg:col-span-5">
            <Eyebrow>Field postcards / SS pipeline</Eyebrow>
            <h1 className="text-cc-heading font-heading mt-5 text-5xl leading-[1.05] font-semibold tracking-tight text-balance sm:text-6xl">
              Greetings from a typed GraphQL client for .NET.
            </h1>
            <p className="text-cc-prose mt-6 max-w-xl text-lg leading-relaxed">
              Strawberry Shake is the open-source, strongly-typed GraphQL client
              for .NET. Drop your operations into .graphql files, run
              <code
                className="mx-1 font-mono text-base"
                style={{ color: CORAL }}
              >
                dotnet build
              </code>
              , and MSBuild codegen emits a typed client, immutable records, a
              normalized reactive store, and the DI registration. Each artifact
              is a small dated, addressable thing you can pin to a board and
              reason about.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <SolidButton href="/docs/strawberryshake">
                Get Started
              </SolidButton>
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
            </div>

            {/* META strip styled as an address label */}
            <dl
              className="border-cc-card-border mt-10 grid grid-cols-3 gap-6 rounded-md border bg-[rgba(245,241,234,0.02)] p-5"
              style={{
                backgroundImage:
                  "repeating-linear-gradient(0deg, rgba(245, 241, 234, 0.04) 0px, rgba(245, 241, 234, 0.04) 1px, transparent 1px, transparent 22px)",
              }}
            >
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10px] tracking-widest uppercase">
                  License
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">MIT</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10px] tracking-widest uppercase">
                  Codegen
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">MSBuild</dd>
              </div>
              <div>
                <dt className="text-cc-ink-dim font-mono text-[10px] tracking-widest uppercase">
                  Runtimes
                </dt>
                <dd className="text-cc-ink mt-1 text-sm">.NET, Blazor, MAUI</dd>
              </div>
            </dl>
          </div>

          {/* Hero collage: two overlapping large postcards */}
          <div className="relative lg:col-span-7">
            <div className="relative mx-auto max-w-2xl">
              <Postcard
                serial="SS-001"
                postmark="Catalog/GetProduct.graphql"
                date="Day 0"
                subject="The .graphql file IS the contract."
                photo={<GraphqlOperationPhoto />}
                body="Queries, mutations, and subscriptions live in plain .graphql files. Schema lives in schema.graphql. The CLI keeps them honest."
                tilt={-2.5}
                size="lg"
                className="relative z-10"
              />
              <div className="relative -mt-8 ml-12 sm:-mt-10 sm:ml-20">
                <Postcard
                  serial="SS-002"
                  postmark="ProductService.cs"
                  date="Day 0"
                  subject="Call sites read like ordinary async C#."
                  photo={<CallSitePhoto />}
                  body="The generated client lives in DI. Results are typed, nullable-aware records. IntelliSense, refactor, the whole .NET tooling."
                  tilt={3}
                  size="lg"
                />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* CAPABILITY POSTMARK STRIP */}
      <section
        aria-label="Capabilities at a glance"
        className="border-cc-card-border border-y py-6"
      >
        <div className="mb-4 flex items-baseline justify-between">
          <span className="text-cc-ink-dim font-mono text-[10px] tracking-[0.22em] uppercase">
            Capability stamps
          </span>
          <span
            className="font-mono text-[10px] tracking-[0.18em] uppercase"
            style={{ color: CORAL }}
          >
            SS-CAP / 6 of 6
          </span>
        </div>
        <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
          {[
            "MSBuild codegen",
            "Normalized store",
            "Three fetch strategies",
            "WebSocket subs",
            "Persisted operations",
            "Blazor + Razor",
          ].map((label) => (
            <li
              key={label}
              className="border-cc-card-border bg-cc-card-bg flex flex-col gap-1.5 rounded-sm border p-3"
              style={{
                backgroundImage:
                  "repeating-linear-gradient(45deg, rgba(245, 241, 234, 0.025) 0px, rgba(245, 241, 234, 0.025) 1px, transparent 1px, transparent 4px)",
              }}
            >
              <span className="flex items-center gap-2">
                <span style={{ color: CORAL }} aria-hidden>
                  <CheckIcon size={11} />
                </span>
                <span className="text-cc-ink font-mono text-[10.5px] tracking-tight uppercase">
                  {label}
                </span>
              </span>
              <span
                className="font-mono text-[8.5px] tracking-wider"
                style={{ color: CORAL_INK }}
              >
                STAMPED
              </span>
            </li>
          ))}
        </ul>
      </section>

      {/* SS-003 single full-width: dotnet build / MSBuild flow */}
      <section className="py-16 sm:py-20">
        <div className="mx-auto max-w-3xl">
          <Postcard
            serial="SS-003"
            postmark="dotnet build"
            date="Day 1"
            subject="The codegen runs at build time, not at runtime."
            photo={<CodegenFlowPhoto />}
            bullets={[
              "dotnet graphql init scaffolds the config and downloads the schema in one step.",
              "MSBuild emits typed client, records, fragments, and the DI extension method.",
              "Generated code is checked source, diffable in code review, no surprise at startup.",
            ]}
            tilt={-1.5}
            size="lg"
          />
        </div>
      </section>

      {/* SS-004 & SS-005: tilted pair */}
      <section className="py-12 sm:py-16">
        <div className="mb-8">
          <Eyebrow>Postcards SS-004 & SS-005</Eyebrow>
          <h2 className="text-cc-heading font-heading mt-3 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            One pipeline. Five emitted artifacts.
          </h2>
        </div>
        <div className="grid gap-10 md:grid-cols-2 md:gap-6">
          <Postcard
            serial="SS-004"
            postmark="CatalogClient.cs"
            date="Day 1"
            subject="Emitted artifacts arrive in one envelope."
            tilt={-2.5}
            bullets={[
              "Typed client class with one method per operation.",
              "Immutable records, nullable-aware, deconstructible.",
              "Fragments as reusable sub-records across components.",
              "AddCatalogClient() DI extension wired into the host.",
            ]}
            footerTo="Catalog.Client.csproj"
          />
          <Postcard
            serial="SS-005"
            postmark=".graphqlrc.json"
            date="Day 1"
            subject="A single config names the package."
            tilt={2.5}
            photo={
              <PhotoFrame file=".graphqlrc.json" tag="JSON">
                <CodeLine n={1}>
                  <span style={T.punct}>{`{`}</span>
                </CodeLine>
                <CodeLine n={2}>
                  <span style={T.plain}>{`  `}</span>
                  <span style={T.str}>&quot;schema&quot;</span>
                  <span style={T.punct}>: </span>
                  <span style={T.str}>&quot;schema.graphql&quot;</span>
                  <span style={T.punct}>,</span>
                </CodeLine>
                <CodeLine n={3}>
                  <span style={T.plain}>{`  `}</span>
                  <span style={T.str}>&quot;documents&quot;</span>
                  <span style={T.punct}>: </span>
                  <span style={T.str}>&quot;**/*.graphql&quot;</span>
                  <span style={T.punct}>,</span>
                </CodeLine>
                <CodeLine n={4}>
                  <span style={T.plain}>{`  `}</span>
                  <span style={T.str}>&quot;extensions&quot;</span>
                  <span style={T.punct}>: {`{`}</span>
                </CodeLine>
                <CodeLine n={5}>
                  <span style={T.plain}>{`    `}</span>
                  <span style={T.str}>&quot;strawberryShake&quot;</span>
                  <span style={T.punct}>: {`{`}</span>
                </CodeLine>
                <CodeLine n={6}>
                  <span style={T.plain}>{`      `}</span>
                  <span style={T.str}>&quot;name&quot;</span>
                  <span style={T.punct}>: </span>
                  <span style={T.str}>&quot;CatalogClient&quot;</span>
                  <span style={T.punct}>,</span>
                </CodeLine>
                <CodeLine n={7}>
                  <span style={T.plain}>{`      `}</span>
                  <span style={T.str}>&quot;namespace&quot;</span>
                  <span style={T.punct}>: </span>
                  <span style={T.str}>&quot;Catalog.Client&quot;</span>
                </CodeLine>
                <CodeLine n={8}>
                  <span style={T.plain}>{`    `}</span>
                  <span style={T.punct}>{`}`}</span>
                </CodeLine>
                <CodeLine n={9}>
                  <span style={T.plain}>{`  `}</span>
                  <span style={T.punct}>{`}`}</span>
                </CodeLine>
                <CodeLine n={10}>
                  <span style={T.punct}>{`}`}</span>
                </CodeLine>
              </PhotoFrame>
            }
            footerTo="dotnet graphql update"
          />
        </div>
      </section>

      {/* SS-006 & SS-007: tilted pair, opposite tilts */}
      <section className="py-12 sm:py-16">
        <div className="mb-8">
          <Eyebrow>Postcards SS-006 & SS-007</Eyebrow>
          <h2 className="text-cc-heading font-heading mt-3 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            One store. Three fetch strategies.
          </h2>
        </div>
        <div className="grid gap-10 md:grid-cols-2 md:gap-6">
          <Postcard
            serial="SS-006"
            postmark="EntityStore"
            date="Day 2"
            subject="A normalized store, Relay and Apollo vocabulary."
            photo={<ReactiveStorePhoto />}
            tilt={2}
            body="Every result denormalizes into a row keyed by type and id. The same product as a list and as a detail shares one row. Watch a query, the component re-renders on any change."
            footerTo="reactive .NET components"
          />
          <Postcard
            serial="SS-007"
            postmark="client.GetProduct.Watch"
            date="Day 2"
            subject="Set globally. Override per call."
            tilt={-2}
            bullets={[
              "CacheFirst returns the store entry without a request when it has one.",
              "NetworkOnly always fetches and writes through the store.",
              "CacheAndNetwork yields the cached entry, refreshes in the background.",
              "Result stream emits both cache and network values, in order.",
            ]}
            footerTo="IObservable<Result>"
          />
        </div>
      </section>

      {/* SS-008 & SS-009: subscriptions + Blazor */}
      <section className="py-12 sm:py-16">
        <div className="mb-8">
          <Eyebrow>Postcards SS-008 & SS-009</Eyebrow>
          <h2 className="text-cc-heading font-heading mt-3 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            Realtime through the same store, into Razor markup.
          </h2>
        </div>
        <div className="grid gap-10 md:grid-cols-2 md:gap-6">
          <Postcard
            serial="SS-008"
            postmark="WebSocket /graphql"
            date="Day 3"
            subject="Pushed values write the same store."
            photo={<RazorSubscriptionPhoto />}
            tilt={-2.5}
            body="Subscription operations look like queries: declare them in a .graphql file, get a typed Watch on the generated client. Token refresh and reconnect live in the transport, not your code."
            footerTo="@OnPriceChanged"
          />
          <Postcard
            serial="SS-009"
            postmark="StrawberryShake.Razor"
            date="Day 3"
            subject="UseQuery, UseSubscription, UseFragment."
            tilt={2.5}
            bullets={[
              "Pending, Error, and ChildContent slots cover the common UI states.",
              "Server, WebAssembly, and hybrid Blazor projects all use the same client.",
              "Fragments map to typed sub-records you can reuse across components.",
              "Works inside .NET MAUI for typed GraphQL on iOS, Android, and desktop.",
            ]}
            footerTo="Blazor / MAUI"
          />
        </div>
      </section>

      {/* INVENTORY MANIFEST CARD: SS-010 */}
      <section className="py-16 sm:py-20">
        <motion.article
          initial={{ opacity: 0, rotate: -0.4, y: 14 }}
          whileInView={{ opacity: 1, rotate: 0.6, y: 0 }}
          viewport={{ once: true, margin: "-60px" }}
          transition={{ duration: 0.6, ease: "easeOut" }}
          className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover relative rounded-md border p-6 shadow-[0_18px_40px_-24px_rgba(0,0,0,0.7)] transition-[transform,border-color] duration-300 hover:-translate-y-1 hover:!rotate-0 sm:p-8"
        >
          <div
            aria-hidden
            className="absolute inset-x-3 top-0 border-t border-dotted"
            style={{ borderColor: "rgba(245, 241, 234, 0.28)" }}
          />
          {/* Wax-seal stamp */}
          <div
            className="absolute top-5 right-5 flex h-20 w-20 items-center justify-center rounded-full"
            style={{
              background: CORAL_SOFT,
              border: `2px solid ${CORAL_INK}`,
              transform: "rotate(-8deg)",
            }}
            aria-hidden
          >
            <div className="flex flex-col items-center">
              <span
                className="font-mono text-[9px] tracking-widest uppercase"
                style={{ color: CORAL }}
              >
                CHECKED
              </span>
              <span
                className="font-heading text-base font-semibold italic"
                style={{ color: CORAL }}
              >
                MIT
              </span>
              <span
                className="font-mono text-[8px] tracking-wider uppercase"
                style={{ color: CORAL_INK }}
              >
                cleared
              </span>
            </div>
          </div>

          <div className="mt-4 mb-4 flex items-baseline justify-between gap-4 pr-24">
            <span className="text-cc-ink-dim font-mono text-[10px] tracking-[0.18em] uppercase">
              POSTMARKED <span className="text-cc-ink">Inventory Manifest</span>
            </span>
            <span
              className="font-mono text-[10px] font-semibold tracking-widest tabular-nums"
              style={{ color: CORAL }}
            >
              SS-010 / DECLARED
            </span>
          </div>

          <h2 className="text-cc-heading font-heading text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
            What ships in the envelope.
          </h2>
          <p className="text-cc-prose mt-3 max-w-2xl text-base leading-relaxed sm:text-lg">
            Open source, in production, free to use. Released under MIT, part of
            the ChilliCream GraphQL platform, on the same repo as Hot Chocolate.
          </p>

          <dl className="border-cc-card-border mt-6 grid grid-cols-1 gap-x-8 gap-y-4 border-t pt-6 sm:grid-cols-2">
            {[
              { label: "License", value: "MIT" },
              { label: "Codegen", value: "MSBuild, build time" },
              { label: "Store", value: "Normalized, reactive" },
              { label: "Transports", value: "HTTP / WebSocket" },
              { label: "UI", value: "Blazor / Razor / MAUI" },
              { label: "Server", value: "Hot Chocolate (or any spec server)" },
            ].map((row) => (
              <div
                key={row.label}
                className="border-cc-card-border flex items-baseline justify-between gap-4 border-b border-dotted pb-2"
              >
                <dt className="text-cc-ink-dim font-mono text-[10.5px] tracking-widest uppercase">
                  {row.label}
                </dt>
                <dd className="text-cc-heading font-heading text-base font-semibold tracking-tight sm:text-lg">
                  {row.value}
                </dd>
              </div>
            ))}
          </dl>

          <div className="mt-8 flex flex-wrap items-center justify-between gap-4">
            <div className="flex flex-col gap-0.5">
              <span className="text-cc-ink-dim font-mono text-[9.5px] tracking-[0.2em] uppercase">
                FROM: ChilliCream
              </span>
              <span className="text-cc-ink-dim font-mono text-[9.5px] tracking-[0.2em] uppercase">
                TO: .NET teams
              </span>
            </div>
            <PostmarkSeal />
          </div>
        </motion.article>
      </section>

      {/* SS-011 Nitro side trip: NitroCompose inside a postcard "photo" */}
      <section className="border-cc-card-border border-t py-20 sm:py-24">
        <div className="mb-10 grid items-end gap-6 lg:grid-cols-12">
          <div className="lg:col-span-7">
            <Eyebrow>Postcard SS-011 / side trip</Eyebrow>
            <h2 className="text-cc-heading font-heading mt-3 text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
              Draft the operation, then check in the .graphql file.
            </h2>
            <p className="text-cc-prose mt-4 max-w-2xl text-base leading-relaxed sm:text-lg">
              Nitro is the GraphQL IDE that ships with the Hot Chocolate server,
              and it is the same surface most teams use to draft operations
              before saving them as .graphql files in the client project. Browse
              the schema, run a query against the live server, and copy the
              document into the codegen pipeline.
            </p>
          </div>
          <div className="lg:col-span-5 lg:text-right">
            <p className="text-cc-ink-dim font-mono text-[11px] tracking-widest uppercase">
              POSTMARKED /graphql
            </p>
          </div>
        </div>
        <motion.div
          initial={{ opacity: 0, rotate: -0.8, y: 16 }}
          whileInView={{ opacity: 1, rotate: 0.4, y: 0 }}
          viewport={{ once: true, margin: "-60px" }}
          transition={{ duration: 0.6, ease: "easeOut" }}
          className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover relative mx-auto max-w-5xl rounded-md border p-5 shadow-[0_18px_40px_-24px_rgba(0,0,0,0.7)] transition-[transform,border-color] duration-300 hover:!rotate-0"
        >
          <div
            aria-hidden
            className="absolute inset-x-3 top-0 border-t border-dotted"
            style={{ borderColor: "rgba(245, 241, 234, 0.28)" }}
          />
          <div className="mt-3 mb-4 flex items-baseline justify-between gap-4">
            <span className="text-cc-ink-dim font-mono text-[10px] tracking-[0.18em] uppercase">
              POSTMARKED <span className="text-cc-ink">/graphql</span>
            </span>
            <span
              className="font-mono text-[10px] font-semibold tracking-widest tabular-nums"
              style={{ color: CORAL }}
            >
              SS-011
            </span>
          </div>
          <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-md border">
            <NitroCompose />
          </div>
          <p className="text-cc-ink-dim mt-3 font-mono text-[10.5px] tracking-tight">
            caption: draft, then check in the .graphql file.
          </p>
        </motion.div>
      </section>

      {/* CLOSING CTA POSTCARD: SS-012, oversized, with the lone brand-spectrum twine */}
      <section className="py-20 sm:py-28">
        <motion.div
          initial={{ opacity: 0, rotate: -1.6, y: 22 }}
          whileInView={{ opacity: 1, rotate: -0.8, y: 0 }}
          viewport={{ once: true, margin: "-80px" }}
          transition={{ duration: 0.65, ease: "easeOut" }}
          className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover relative mx-auto max-w-3xl overflow-hidden rounded-md border p-8 shadow-[0_28px_60px_-28px_rgba(0,0,0,0.8)] transition-[transform,border-color] duration-300 hover:!rotate-0 sm:p-12"
        >
          {/* Brand-spectrum twine, the ONE spectrum event on the page */}
          <div
            aria-hidden
            className="pointer-events-none absolute -top-6 -right-10 h-[2px] w-[420px] origin-top-right rotate-[28deg]"
            style={{
              background: SPECTRUM,
              boxShadow: "0 0 12px rgba(124, 146, 198, 0.35)",
            }}
          />
          {/* Twine "knot" at the corner */}
          <div
            aria-hidden
            className="absolute top-5 right-5 h-2 w-2 rounded-full"
            style={{
              background: CORAL,
              boxShadow: "0 0 0 4px rgba(240, 120, 106, 0.18)",
            }}
          />

          <div
            aria-hidden
            className="absolute inset-x-3 top-0 border-t border-dotted"
            style={{ borderColor: "rgba(245, 241, 234, 0.28)" }}
          />

          <div className="mt-3 mb-4 flex items-baseline justify-between gap-4">
            <span className="text-cc-ink-dim font-mono text-[10px] tracking-[0.18em] uppercase">
              POSTMARKED{" "}
              <span className="text-cc-ink">Wish you were typed</span>
            </span>
            <span
              className="font-mono text-[10px] font-semibold tracking-widest tabular-nums"
              style={{ color: CORAL }}
            >
              SS-012 / FINAL
            </span>
          </div>

          <Eyebrow>Get started</Eyebrow>
          <p className="text-cc-heading font-heading mt-4 text-4xl leading-[1.05] font-semibold tracking-tight text-balance italic sm:text-5xl">
            Wish you were typed.
          </p>
          <p className="text-cc-prose mt-5 max-w-2xl text-base leading-relaxed sm:text-lg">
            A few .graphql files, a .graphqlrc.json, and a NuGet reference. The
            client, the records, the store, and the DI wiring are emitted for
            you at build time, and the runtime is the .NET you already ship. Pin
            the postcards to the board.
          </p>
          <div className="mt-8 flex flex-wrap gap-3">
            <SolidButton href="/docs/strawberryshake">Get Started</SolidButton>
            <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
              View on GitHub
            </OutlineButton>
          </div>

          <div className="border-cc-card-border mt-10 flex items-end justify-between gap-4 border-t pt-4">
            <div className="flex flex-col gap-0.5">
              <span className="text-cc-ink-dim font-mono text-[9.5px] tracking-[0.2em] uppercase">
                FROM: ChilliCream
              </span>
              <span className="text-cc-ink-dim font-mono text-[9.5px] tracking-[0.2em] uppercase">
                TO: .NET teams everywhere
              </span>
            </div>
            <PostmarkSeal />
          </div>
        </motion.div>
      </section>
    </div>
  );
}
