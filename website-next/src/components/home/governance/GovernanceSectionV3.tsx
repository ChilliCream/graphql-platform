import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Governance section, take 3: the registry as the source of truth, carrying both
 * visibility (what exists and what is used) and traceability (what changed, who
 * changed it, and when). One all-visible registry panel: a schema version
 * history on the left, a current-state strip on the right. Native to the dark
 * navy landing canvas, plain voice, no tabs or click-to-reveal.
 */

type ChangeKind = "added" | "deprecated";

interface ChangeEntry {
  readonly version: string;
  readonly kind: ChangeKind;
  readonly field: string;
  readonly author: string;
  /** True when the author is a coding agent, traced like any human author. */
  readonly agent?: boolean;
  readonly time: string;
}

// Schema version history. The agent/codex row shows that agent changes are
// traced the same way human changes are.
const CHANGES: readonly ChangeEntry[] = [
  {
    version: "v14",
    kind: "added",
    field: "Product.rating",
    author: "alice",
    time: "2d ago",
  },
  {
    version: "v13",
    kind: "deprecated",
    field: "User.email",
    author: "agent/codex",
    agent: true,
    time: "5d ago",
  },
  {
    version: "v12",
    kind: "added",
    field: "Order.totalAmount",
    author: "bob",
    time: "1w ago",
  },
  {
    version: "v11",
    kind: "added",
    field: "Cart.currency",
    author: "dana",
    time: "2w ago",
  },
];

/** Status colors used as data: green for an additive change, amber for a deprecation. */
const KIND: Record<
  ChangeKind,
  { readonly dot: string; readonly text: string }
> = {
  added: { dot: "bg-cc-status-healthy", text: "text-cc-status-healthy" },
  deprecated: {
    dot: "bg-cc-status-investigating",
    text: "text-cc-status-investigating",
  },
};

/** Registry / schema-version node glyph. */
function RegistryNodeIcon() {
  return (
    <svg
      viewBox="0 0 16 16"
      width={14}
      height={14}
      aria-hidden="true"
      className="text-cc-accent shrink-0"
    >
      <path
        fill="currentColor"
        d="M8 1.2 13.9 4.6v6.8L8 14.8 2.1 11.4V4.6L8 1.2Zm0 1.5L3.4 5.3v5.4L8 13.3l4.6-2.6V5.3L8 2.7Z"
      />
      <circle cx="8" cy="8" r="1.7" fill="currentColor" />
    </svg>
  );
}

function ChangeRow({ entry }: { readonly entry: ChangeEntry }) {
  const kind = KIND[entry.kind];

  return (
    <li className="hover:bg-cc-surface/60 flex flex-col gap-1.5 rounded-lg px-3 py-2.5 transition-colors sm:flex-row sm:items-center sm:gap-4">
      <div className="flex min-w-0 flex-1 items-baseline gap-3">
        <span className="text-cc-ink w-10 shrink-0 font-mono text-xs">
          {entry.version}
        </span>
        <span className="flex min-w-0 items-baseline gap-2 font-mono text-xs">
          <span
            aria-hidden="true"
            className={`${kind.dot} mt-1 size-1.5 shrink-0 self-start rounded-full`}
          />
          <span className="flex min-w-0 items-baseline gap-1.5">
            <span className={kind.text}>{entry.kind}</span>
            <span className="text-cc-heading truncate">{entry.field}</span>
          </span>
        </span>
      </div>

      <div className="flex items-center gap-4 pl-[3.25rem] font-mono text-xs sm:pl-0">
        <span
          className={`${entry.agent ? "text-cc-accent" : "text-cc-ink"} sm:w-24 sm:text-right`}
        >
          {entry.author}
        </span>
        <span className="text-cc-nav-label sm:w-12 sm:text-right">
          {entry.time}
        </span>
      </div>
    </li>
  );
}

function RegistryStat({
  figure,
  label,
}: {
  readonly figure: string;
  readonly label: string;
}) {
  return (
    <div>
      <p className="font-heading text-cc-heading text-h4 leading-none font-semibold tabular-nums">
        {figure}
      </p>
      <p className="text-cc-ink-dim mt-1.5 text-xs">{label}</p>
    </div>
  );
}

export function GovernanceSectionV3() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        {/* Heading block */}
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Governance
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            Nothing changes without a trace.
          </h2>
          <p className="text-cc-ink mt-6 max-w-3xl text-base text-pretty sm:text-lg">
            The registry keeps every schema version, every published client, and
            the usage behind each field, with a record of what changed, who
            changed it, and when.
          </p>
          <a
            href="/platform/release-safety"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </a>
        </div>

        {/* Registry panel: source of truth carrying traceability + visibility. */}
        <div className="border-cc-card-border bg-cc-card-bg mt-12 overflow-hidden rounded-2xl border">
          {/* Title bar: the file under registry, with the current version head. */}
          <div className="border-cc-card-border bg-cc-surface flex items-center gap-2 border-b px-4 py-3 sm:px-5">
            <RegistryNodeIcon />
            <span className="text-cc-heading font-mono text-xs font-semibold sm:text-sm">
              schema.graphql
            </span>
            <span
              aria-hidden="true"
              className="text-cc-nav-label font-mono text-xs"
            >
              -
            </span>
            <span className="text-cc-nav-label font-mono text-xs">history</span>
            <span className="flex-1" />
            <span className="border-cc-accent/40 text-cc-accent hidden shrink-0 items-center gap-1.5 rounded-full border px-2.5 py-0.5 font-mono text-[0.6rem] tracking-[0.06em] uppercase sm:inline-flex">
              <span
                aria-hidden="true"
                className="bg-cc-accent size-1.5 rounded-full"
              />
              current v14
            </span>
          </div>

          {/* Body: version history (traceability) | current state (visibility). */}
          <div className="grid lg:grid-cols-[minmax(0,1fr)_15rem]">
            {/* Traceability: schema version history / changelog. */}
            <div className="p-3 sm:p-4">
              <div className="text-cc-nav-label hidden px-3 pb-1 font-mono text-[0.58rem] tracking-[0.12em] uppercase sm:flex sm:items-center sm:gap-4">
                <div className="flex flex-1 items-center gap-3">
                  <span className="w-10 shrink-0">version</span>
                  <span>change</span>
                </div>
                <div className="flex items-center gap-4">
                  <span className="w-24 text-right">author</span>
                  <span className="w-12 text-right">when</span>
                </div>
              </div>

              <ol>
                {CHANGES.map((entry) => (
                  <ChangeRow key={entry.version} entry={entry} />
                ))}
              </ol>
            </div>

            {/* Visibility: the current state of the API in the registry. */}
            <aside className="border-cc-card-border bg-cc-surface/30 border-t p-4 sm:p-5 lg:border-t-0 lg:border-l">
              <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
                Current state
              </p>

              <div className="mt-4 grid grid-cols-2 gap-4 lg:grid-cols-1 lg:gap-5">
                <RegistryStat figure="12" label="published clients" />
                <RegistryStat figure="3,184" label="operations" />
              </div>

              <div className="border-cc-card-border mt-5 border-t pt-4">
                <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.12em] uppercase">
                  Field usage
                </p>
                <div className="border-cc-card-border bg-cc-surface mt-2 flex items-center justify-between gap-3 rounded-lg border px-3 py-2">
                  <span className="text-cc-ink truncate font-mono text-xs">
                    Product.rating
                  </span>
                  <span className="text-cc-accent shrink-0 font-mono text-xs tabular-nums">
                    4.2k req/7d
                  </span>
                </div>
                <p className="text-cc-nav-label mt-2 font-mono text-[0.62rem]">
                  added in v14
                </p>
              </div>
            </aside>
          </div>
        </div>
      </RevealOnScroll>
    </section>
  );
}
