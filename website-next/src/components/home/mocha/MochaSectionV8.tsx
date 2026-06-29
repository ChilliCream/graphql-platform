import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Mocha messaging section, take v8 "Simple, and it scales".
 *
 * The only code-forward take, because the elegance is in how little you write.
 * A short code panel shows the whole pattern: a resolver that just dispatches a
 * query, and a two-line handler that loads through a DataLoader. Two light
 * annotations name each half. Below it, three small panels show the SAME small
 * shape scaling a different axis at once: traffic (reads in one tick collapse
 * into one batched query), codebase (a column of identical handlers), and team
 * (one more handler added in minutes). Everything is on screen at once; thin 1px
 * strokes and mono labels, teal accent only. No svg ids are used, so nothing can
 * collide.
 */

/** Locked cc-* palette for the inline traffic diagram. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  faint: "rgba(245, 241, 234, 0.42)",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Restrained GitHub-dark syntax tokens that read on the navy code surface. */
const SYN = {
  kw: "#f97583",
  type: "#b392f0",
  mem: "#79b8ff",
  txt: "#c9d4e3",
} as const;

type TokenKind = keyof typeof SYN;
type Token = readonly [text: string, kind: TokenKind];
type CodeLine = readonly Token[];

// The resolver: a query field that just dispatches an IQuery through the mediator.
const RESOLVER: readonly CodeLine[] = [
  [
    ["[", "txt"],
    ["QueryType", "type"],
    ["]", "txt"],
  ],
  [
    ["public ", "kw"],
    ["Task", "type"],
    ["<", "txt"],
    ["User", "type"],
    ["> ", "txt"],
    ["GetUser", "mem"],
    ["(", "txt"],
    ["int ", "kw"],
    ["id", "txt"],
    [", ", "txt"],
    ["IMediator ", "type"],
    ["mediator", "txt"],
    [")", "txt"],
  ],
  [
    ["    => ", "txt"],
    ["mediator", "txt"],
    [".", "txt"],
    ["QueryAsync", "mem"],
    ["(", "txt"],
    ["new ", "kw"],
    ["GetUserById", "type"],
    ["(", "txt"],
    ["id", "txt"],
    ["));", "txt"],
  ],
];

// The handler: loads through the generated DataLoader, which batches the reads.
const HANDLER: readonly CodeLine[] = [
  [
    ["public ", "kw"],
    ["Task", "type"],
    ["<", "txt"],
    ["User", "type"],
    ["> ", "txt"],
    ["Handle", "mem"],
    ["(", "txt"],
    ["GetUserById ", "type"],
    ["query", "txt"],
    [", ", "txt"],
    ["IUserByIdDataLoader ", "type"],
    ["users", "txt"],
    [")", "txt"],
  ],
  [
    ["    => ", "txt"],
    ["users", "txt"],
    [".", "txt"],
    ["LoadAsync", "mem"],
    ["(", "txt"],
    ["query", "txt"],
    [".", "txt"],
    ["Id", "mem"],
    [");", "txt"],
  ],
];

/** A code-identifier rendered in mono within the prose lead. */
function Mono({ children }: { readonly children: string }) {
  return <span className="font-mono text-[0.92em]">{children}</span>;
}

/** One block of syntax-tinted code lines. */
function CodeBlock({ lines }: { readonly lines: readonly CodeLine[] }) {
  return (
    <pre className="font-mono text-[0.72rem] leading-[1.75] sm:text-[0.82rem]">
      <code>
        {lines.map((line) => (
          <div key={line.map((token) => token[0]).join("")}>
            {line.map((token, index) => (
              <span
                key={`${token[0]}-${index}`}
                style={{ color: SYN[token[1]] }}
              >
                {token[0]}
              </span>
            ))}
          </div>
        ))}
      </code>
    </pre>
  );
}

/** A light note naming one half of the pattern. */
function Annotation({
  label,
  text,
}: {
  readonly label: string;
  readonly text: string;
}) {
  return (
    <div className="flex gap-3">
      <span
        aria-hidden="true"
        className="bg-cc-accent mt-1.5 size-2 shrink-0 rounded-full"
      />
      <div>
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
          {label}
        </p>
        <p className="text-cc-ink-dim mt-1 text-sm text-pretty">{text}</p>
      </div>
    </div>
  );
}

/** One instance of the small shape: a read and its handler, reused everywhere. */
function ShapeRow({ query }: { readonly query: string }) {
  return (
    <div className="border-cc-card-border bg-cc-surface flex items-center justify-between gap-3 rounded-lg border px-3 py-2">
      <span className="text-cc-accent font-mono text-[0.72rem]">{query}</span>
      <span className="text-cc-ink-dim font-mono text-[0.58rem]">
        <span aria-hidden="true" className="mr-1">
          &rarr;
        </span>
        DataLoader
      </span>
    </div>
  );
}

/** "your traffic": three reads in one tick collapse into one batched query. */
function TrafficViz() {
  const requests = [
    { label: "GetUserById(1)", cy: 21 },
    { label: "GetUserById(2)", cy: 75 },
    { label: "GetUserById(3)", cy: 129 },
  ];

  return (
    <svg
      viewBox="0 0 280 150"
      width="100%"
      aria-hidden="true"
      style={{ display: "block", fontFamily: MONO }}
    >
      {/* converge lines: each read bends into one junction */}
      {requests.map((r) => (
        <path
          key={`line-${r.cy}`}
          d={`M124 ${r.cy} C 150 ${r.cy} 150 75 176 75`}
          fill="none"
          stroke={C.accent}
          strokeOpacity="0.5"
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
      ))}

      {/* the three incoming reads, same shape, arriving together */}
      {requests.map((r) => (
        <g key={`chip-${r.cy}`}>
          <rect
            x="4"
            y={r.cy - 15}
            width="120"
            height="30"
            rx="8"
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text x="15" y={r.cy + 3} fontSize="9" fill={C.accent}>
            {r.label}
          </text>
        </g>
      ))}

      {/* junction where the batch forms */}
      <circle cx="176" cy="75" r="2.5" fill={C.accent} />

      {/* the single batched query that goes to the database */}
      <rect
        x="176"
        y="52"
        width="100"
        height="46"
        rx="10"
        fill={C.surface}
        stroke={C.accent}
        strokeWidth="1.25"
        vectorEffect="non-scaling-stroke"
      />
      <text x="187" y="70" fontSize="6" letterSpacing="1.2" fill={C.navLabel}>
        ONE QUERY
      </text>
      <text x="187" y="84" fontSize="9.5" fill={C.inkDim}>
        id IN (1,2,3)
      </text>

      {/* the read group arrives within a single tick */}
      <text x="64" y="148" textAnchor="middle" fontSize="6" fill={C.faint}>
        ONE TICK
      </text>
    </svg>
  );
}

/**
 * Mocha messaging section, take v8 "Simple, and it scales". See the file header
 * for the structure. Rendered after ProtocolCards and above NitroPricing.
 */
export function MochaSectionV8() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* frame: eyebrow, headline, lead, jump-off */}
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Messaging
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            Simple, and it scales.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            A resolver is just a query. <Mono>GetUserById</Mono> is an{" "}
            <Mono>IQuery</Mono>, and its handler loads through a{" "}
            <Mono>DataLoader</Mono>. Every read is the same small shape, so it
            scales in every direction that matters: a new engineer or an agent
            writes one with no ramp-up, the codebase stays uniform as it grows
            instead of turning into spaghetti, and the <Mono>DataLoader</Mono>{" "}
            batches each request so performance holds. It scales because it
            stays simple.
          </p>
          <Link
            href="/platform/workflows"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Open workflows
            <span aria-hidden="true">&rarr;</span>
          </Link>
        </div>

        {/* hero: the whole pattern in one short code panel */}
        <div className="border-cc-card-border bg-cc-surface hover:border-cc-card-border-hover mt-10 overflow-hidden rounded-3xl border transition-colors">
          {/* editor chrome */}
          <div className="border-cc-card-border flex items-center gap-3 border-b px-4 py-3 sm:px-5">
            <span aria-hidden="true" className="flex gap-1.5">
              <span className="bg-cc-ink-faint size-2.5 rounded-full" />
              <span className="bg-cc-ink-faint size-2.5 rounded-full" />
              <span className="bg-cc-ink-faint size-2.5 rounded-full" />
            </span>
            <span className="text-cc-ink-dim font-mono text-[0.7rem]">
              UserResolvers.cs
            </span>
            <span className="border-cc-card-border text-cc-nav-label ml-auto rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.12em] uppercase">
              C#
            </span>
          </div>

          {/* code on the left, the two annotations on the right */}
          <div className="grid gap-6 p-4 sm:p-6 lg:grid-cols-[minmax(0,1fr)_16rem] lg:gap-10">
            <div className="overflow-x-auto">
              <CodeBlock lines={RESOLVER} />
              <div className="border-cc-card-border my-4 border-t border-dashed" />
              <CodeBlock lines={HANDLER} />
            </div>

            <div className="border-cc-card-border flex flex-col justify-center gap-5 lg:border-l lg:pl-8">
              <Annotation label="resolver" text="a resolver is just a query" />
              <Annotation
                label="handler"
                text="the handler batches with a DataLoader"
              />
            </div>
          </div>
        </div>

        {/* the same small shape, scaling three axes at once */}
        <div className="mt-6 grid grid-cols-1 gap-4 md:grid-cols-3">
          {/* traffic */}
          <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-2xl border p-5 transition-colors">
            <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
              your traffic
            </p>
            <div className="mt-4">
              <TrafficViz />
            </div>
            <p className="text-cc-ink-dim mt-auto pt-4 text-sm text-pretty">
              Reads arriving in one tick collapse into one batched query, so it
              stays fast.
            </p>
          </div>

          {/* codebase */}
          <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-2xl border p-5 transition-colors">
            <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
              your codebase
            </p>
            <div className="mt-4 space-y-2">
              <ShapeRow query="GetUserById" />
              <ShapeRow query="GetOrderById" />
              <ShapeRow query="GetReviewById" />
            </div>
            <p className="text-cc-ink-dim mt-auto pt-4 text-sm text-pretty">
              Every read is the same shape, so the codebase stays uniform as it
              grows.
            </p>
          </div>

          {/* team */}
          <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-2xl border p-5 transition-colors">
            <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
              your team
            </p>
            <div className="mt-4 space-y-2">
              <ShapeRow query="GetUserById" />
              <ShapeRow query="GetOrderById" />
              <div className="border-cc-accent/40 bg-cc-accent/5 flex items-center justify-between gap-3 rounded-lg border border-dashed px-3 py-2">
                <span className="text-cc-accent font-mono text-[0.72rem]">
                  <span aria-hidden="true" className="mr-1.5">
                    +
                  </span>
                  GetReviewById
                </span>
                <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.1em] uppercase">
                  person or agent
                </span>
              </div>
            </div>
            <p className="text-cc-ink-dim mt-auto pt-4 text-sm text-pretty">
              A person or an agent adds one more in minutes, with no ramp-up.
            </p>
          </div>
        </div>

        <p className="text-cc-ink-dim mt-8 max-w-3xl text-sm text-pretty">
          The same small shape, more of it, in any direction.
        </p>
      </RevealOnScroll>
    </section>
  );
}
