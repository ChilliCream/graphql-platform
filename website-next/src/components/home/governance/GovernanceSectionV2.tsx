import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Governance, take 2: an illustration-forward, all-visible section for the
 * homepage landing (after ProtocolCards, above pricing, on the dark navy
 * canvas). The heading block sits beside the hero illustration: a `schema-lint`
 * result from CI that flags real naming, deprecation, and style violations with
 * their fixes. Coral marks the errors, amber the warning, teal the suggested
 * fix token. Everything is on screen at once; no tabs, stepper, or reveal.
 */

interface LintFinding {
  readonly severity: "error" | "warning";
  readonly rule: string;
  readonly message: string;
  readonly fix?: string;
}

// Real convention and deprecation violations a lint run would surface on a
// single change: two errors (coral) and one style warning (amber).
const FINDINGS: readonly LintFinding[] = [
  {
    severity: "error",
    rule: "naming",
    message: 'Field "get_user" must be camelCase',
    fix: "getUser",
  },
  {
    severity: "error",
    rule: "deprecation",
    message: '"User.email" removed without @deprecated first',
  },
  {
    severity: "warning",
    rule: "style",
    message: 'Type "product" should be PascalCase',
    fix: "Product",
  },
];

// The three areas the lead names, mirrored as restrained mono labels.
const AREAS = ["naming", "structure", "deprecation"] as const;

const errorCount = FINDINGS.filter((f) => f.severity === "error").length;
const warningCount = FINDINGS.filter((f) => f.severity === "warning").length;

/** Small checklist mark for the lint window title bar. */
function LintMark() {
  return (
    <svg
      viewBox="0 0 16 16"
      width="14"
      height="14"
      aria-hidden="true"
      className="text-cc-nav-label shrink-0"
    >
      <rect
        x="2.5"
        y="2"
        width="11"
        height="12"
        rx="2"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.1"
      />
      <path
        d="M5 6h6M5 8.5h6M5 11h3.5"
        stroke="currentColor"
        strokeWidth="1.1"
        strokeLinecap="round"
      />
    </svg>
  );
}

/** One lint finding row: severity glyph, rule, message, and optional fix. */
function FindingRow({ finding }: { readonly finding: LintFinding }) {
  const isError = finding.severity === "error";

  return (
    <div
      className={[
        "relative flex items-start gap-2.5 py-2 pr-3 pl-4 sm:pl-5",
        isError ? "bg-cc-status-firing/5" : "bg-cc-status-investigating/5",
      ].join(" ")}
    >
      <span
        aria-hidden="true"
        className={[
          "absolute top-0 left-0 h-full w-[3px]",
          isError ? "bg-cc-status-firing" : "bg-cc-status-investigating",
        ].join(" ")}
      />

      <span
        aria-hidden="true"
        className={[
          "shrink-0 font-mono text-[0.78rem] leading-[1.5] font-bold",
          isError ? "text-cc-status-firing" : "text-cc-status-investigating",
        ].join(" ")}
      >
        {isError ? "x" : "!"}
      </span>

      <code className="text-cc-ink min-w-0 flex-1 font-mono text-[0.78rem] leading-[1.5] break-words">
        <span className="text-cc-ink-dim sm:inline-block sm:w-24">
          {finding.rule}
        </span>{" "}
        {finding.message}
        {finding.fix ? (
          <>
            {" "}
            <span className="text-cc-ink-faint">-&gt;</span>{" "}
            <span className="text-cc-accent">{finding.fix}</span>
          </>
        ) : null}
      </code>
    </div>
  );
}

/** The hero illustration: a `schema-lint` window rendering the findings. */
function LintCard() {
  return (
    <div className="border-cc-card-border bg-cc-surface hover:border-cc-card-border-hover w-full overflow-hidden rounded-2xl border shadow-[0_1px_3px_rgba(2,6,16,0.6)] transition-colors">
      {/* Title bar: the lint run and the file under check, with a failed pill. */}
      <div className="border-cc-card-border flex flex-wrap items-center justify-between gap-x-2.5 gap-y-1 border-b px-4 py-3 sm:px-5">
        <div className="flex min-w-0 items-center gap-2.5">
          <LintMark />
          <span className="text-cc-heading font-mono text-[0.8rem] font-semibold">
            schema-lint
          </span>
          <span className="text-cc-nav-label truncate font-mono text-[0.78rem]">
            ./schema.graphql
          </span>
        </div>
        <span className="border-cc-status-firing/40 bg-cc-status-firing/10 text-cc-status-firing rounded-full border px-2.5 py-0.5 font-mono text-[0.62rem] font-semibold tracking-[0.08em] uppercase">
          failed
        </span>
      </div>

      {/* Findings: two coral errors and one amber warning, each with its fix. */}
      <div className="py-2">
        {FINDINGS.map((finding) => (
          <FindingRow
            key={`${finding.rule}-${finding.message}`}
            finding={finding}
          />
        ))}
      </div>

      {/* Summary: the counts and a reminder that the run is on every change. */}
      <div className="border-cc-card-border flex flex-wrap items-center justify-between gap-x-4 gap-y-1 border-t px-4 py-3 sm:px-5">
        <span className="font-mono text-[0.72rem]">
          <span className="text-cc-status-firing">
            {errorCount} {errorCount === 1 ? "error" : "errors"}
          </span>
          <span className="text-cc-ink-faint">, </span>
          <span className="text-cc-status-investigating">
            {warningCount} {warningCount === 1 ? "warning" : "warnings"}
          </span>
        </span>
        <span className="text-cc-nav-label font-mono text-[0.64rem] tracking-[0.08em] uppercase">
          linted on every change
        </span>
      </div>
    </div>
  );
}

/**
 * GovernanceSectionV2: heading block beside a `schema-lint` CI result. The lint
 * window shows naming, deprecation, and style checks firing on one change, so
 * the schema reads consistently no matter who, or what, wrote it.
 */
export function GovernanceSectionV2() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll className="grid items-center gap-10 lg:grid-cols-2 lg:gap-16">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Governance
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-4 leading-[1.1] font-semibold text-balance">
            One style, whoever&rsquo;s typing.
          </h2>
          <p className="text-cc-ink mt-5 max-w-3xl text-base text-pretty sm:text-lg">
            Linting and rules enforce naming, structure, and deprecation on
            every change, so the schema stays consistent no matter who, or what,
            writes it.
          </p>

          <ul className="mt-6 flex flex-wrap gap-2">
            {AREAS.map((area) => (
              <li
                key={area}
                className="border-cc-card-border bg-cc-surface text-cc-ink-dim rounded-full border px-3 py-1 font-mono text-[0.66rem] tracking-[0.08em] uppercase"
              >
                {area}
              </li>
            ))}
          </ul>

          <a
            href="/platform/release-safety"
            className="text-cc-accent hover:text-cc-accent-hover mt-7 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Learn more
            <span aria-hidden="true">&rarr;</span>
          </a>
        </div>

        <LintCard />
      </RevealOnScroll>
    </section>
  );
}
