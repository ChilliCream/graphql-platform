import {
  COMPOSITION_GROUPS,
  formatNumber,
  orderedScenarios,
  SCENARIO_INFO,
  scenarioVerdict,
  type ScenarioVerdict,
} from "@/src/components/benchmarks/benchmarkView";
import { Link } from "@/src/design-system/Link";
import {
  BENCHMARK_FUSION_DOCS_URL,
  benchmarkVariants,
  formatRuntimeTag,
  gatewayLink,
  variantName,
  type BenchmarkCatalogEntry,
  type BenchmarkCompletedVariantResult,
  type BenchmarkReport,
  type BenchmarkVariantResult,
} from "@/src/helpers/benchmarkReport";
import { CrownIcon } from "@/src/icons/CrownIcon";

/** A scenario verdict with its routers already named. */
interface NamedVerdict {
  readonly leader: { readonly name: string; readonly rps: number };
  readonly runnerUp?: { readonly name: string; readonly rps: number };
  readonly lead: number;
  readonly tied: boolean;
}

/** Which router, if any, is fastest across every scenario. */
type Overall =
  | { readonly kind: "leads"; readonly name: string }
  | { readonly kind: "leadsOrTies"; readonly name: string }
  | { readonly kind: "tied"; readonly name: string }
  | { readonly kind: "none" }
  | { readonly kind: "empty" };

const formatPercent = (value: number) =>
  `${(value * 100).toFixed(1).replace(/\.0$/, "")}%`;

/** Returns the sentence for one scenario, or for a scenario without any result free of failed requests. */
function scenarioSentence(verdict: NamedVerdict | undefined) {
  if (!verdict) {
    return "No router finished this scenario without failed requests.";
  }
  const { leader, runnerUp, lead, tied } = verdict;
  if (!runnerUp) {
    return `${leader.name} is the only router without failed requests, at ${formatNumber(leader.rps)} requests per second.`;
  }
  if (tied) {
    return `${leader.name} and ${runnerUp.name} are tied within the run variance, at ${formatNumber(leader.rps)} and ${formatNumber(runnerUp.rps)} requests per second.`;
  }
  return `${leader.name} is the fastest at ${formatNumber(leader.rps)} requests per second, ${formatPercent(lead)} ahead of ${runnerUp.name} at ${formatNumber(runnerUp.rps)}.`;
}

/** Returns the sentence that summarizes every scenario of a run. */
function summarySentence(overall: Overall, run: number) {
  switch (overall.kind) {
    case "leads":
      return `${overall.name} is the fastest router in every scenario of run ${run}.`;
    case "leadsOrTies":
      return `${overall.name} is the fastest router, or tied for fastest, in every scenario of run ${run}.`;
    case "tied":
      return `${overall.name} are tied for fastest in every scenario of run ${run}.`;
    case "none":
      return `No single router is the fastest in every scenario of run ${run}.`;
    case "empty":
      return `No scenario of run ${run} has results without failed requests.`;
  }
}

/** Returns the name and version of a router, with its runtime when another router shares both, and its composition when `other` also shares the runtime. */
function routerName(
  row: BenchmarkVariantResult,
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
  other?: BenchmarkVariantResult,
) {
  const entry = variants.get(row.variant)!;
  const label = (item: BenchmarkCatalogEntry) =>
    [variantName(item), item.variant.version].filter(Boolean).join(" ");
  const runtime = (item: BenchmarkCatalogEntry) =>
    formatRuntimeTag(item.variant.platform);
  const shared = [...variants.values()].some(
    (item) => item !== entry && label(item) === label(entry),
  );
  if (!shared) {
    return label(entry);
  }
  const details = [runtime(entry)];
  const rival = other && variants.get(other.variant);
  if (
    rival &&
    rival !== entry &&
    label(rival) === label(entry) &&
    runtime(rival) === runtime(entry)
  ) {
    details.push(
      COMPOSITION_GROUPS.find(
        (group) => group.id === entry.variant.composition,
      )!.name,
    );
  }
  return `${label(entry)} (${details.join(", ")})`;
}

function nameVerdict(
  verdict: ScenarioVerdict | undefined,
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
): NamedVerdict | undefined {
  if (!verdict) {
    return undefined;
  }
  const named = (
    row: BenchmarkCompletedVariantResult,
    other: BenchmarkVariantResult | undefined,
  ) => ({
    name: routerName(row, variants, other),
    rps: row.throughput.medianRps,
  });
  return {
    leader: named(verdict.leader, verdict.runnerUp),
    runnerUp: verdict.runnerUp && named(verdict.runnerUp, verdict.leader),
    lead: verdict.lead,
    tied: verdict.tied,
  };
}

function overallOf(
  verdicts: readonly (ScenarioVerdict | undefined)[],
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
): Overall {
  const present = verdicts.filter((verdict) => verdict !== undefined);
  if (present.length === 0) {
    return { kind: "empty" };
  }
  const fastest = present.map((verdict) =>
    verdict.tied && verdict.runnerUp
      ? [verdict.leader, verdict.runnerUp]
      : [verdict.leader],
  );
  const gatewayId = (row: BenchmarkVariantResult) =>
    variants.get(row.variant)!.gateway.id;
  const shared = [...new Set(fastest[0].map(gatewayId))].filter((id) =>
    fastest.every((rows) => rows.some((row) => gatewayId(row) === id)),
  );
  if (shared.length > 1) {
    const names = shared.map(
      (id) =>
        [...variants.values()].find((entry) => entry.gateway.id === id)!.gateway
          .name,
    );
    return {
      kind: "tied",
      name: new Intl.ListFormat("en", { type: "conjunction" }).format(names),
    };
  }
  if (shared.length !== 1) {
    return { kind: "none" };
  }
  const name = [...variants.values()].find(
    (entry) => entry.gateway.id === shared[0],
  )!.gateway.name;
  return present.every((verdict) => !verdict.tied)
    ? { kind: "leads", name }
    : { kind: "leadsOrTies", name };
}

function linkFusion(text: string) {
  return text.split(/\b(Fusion)\b/).map((part, index) =>
    part === "Fusion" ? (
      <Link key={index} href={BENCHMARK_FUSION_DOCS_URL}>
        {part}
      </Link>
    ) : (
      part
    ),
  );
}

interface BenchmarkVerdictSummaryProps {
  readonly id: string;
  readonly report: BenchmarkReport;
  readonly stableOnly: boolean;
}

/** A written summary of every scenario: one overall sentence and one sentence per scenario. */
export function BenchmarkVerdictSummary({
  id,
  report,
  stableOnly,
}: BenchmarkVerdictSummaryProps) {
  const variants = benchmarkVariants(report);
  const scenarios = orderedScenarios(report).map((scenario) => ({
    scenario,
    verdict: scenarioVerdict(scenario, variants, stableOnly),
  }));
  if (scenarios.length === 0) {
    return null;
  }
  const overall = overallOf(
    scenarios.map(({ verdict }) => verdict),
    variants,
  );

  const incomplete = scenarios.some((item) => item.verdict === undefined);
  const summary = incomplete
    ? summarySentence(overall, report.run.number).replace(
        "in every scenario",
        "in every scenario with successful results",
      )
    : summarySentence(overall, report.run.number);
  const lines = scenarios.map(({ scenario, verdict }) => ({
    id: scenario.id,
    name: SCENARIO_INFO[scenario.id].name,
    text: scenarioSentence(nameVerdict(verdict, variants)),
    verdict,
    gateway: verdict && variants.get(verdict.leader.variant)!.gateway,
    rival: verdict?.runnerUp && variants.get(verdict.runnerUp.variant)!.gateway,
  }));
  const commonWinner =
    lines.length > 0 &&
    lines.every(
      (line) =>
        line.gateway === undefined ||
        line.gateway.id === lines.find((item) => item.gateway)?.gateway?.id,
    ) &&
    (overall.kind === "leads" || overall.kind === "leadsOrTies");
  const body = (
    <>
      <p className="text-cc-heading text-body mt-2">
        {commonWinner && (
          <CrownIcon className="text-cc-accent mr-1.5 inline size-4 align-[-2px]" />
        )}
        {linkFusion(summary)}
      </p>
      <dl className="mt-3 space-y-2 text-sm">
        {lines.map((line) => (
          <div
            key={line.id}
            title={line.text}
            className={`grid grid-cols-[8rem_1fr] gap-x-4 gap-y-1 ${commonWinner ? "sm:grid-cols-[9rem_9rem_1fr]" : "sm:grid-cols-[9rem_10rem_9rem_1fr]"}`}
          >
            <dt className="text-cc-heading font-semibold">{line.name}</dt>
            {!commonWinner && (
              <dd className="text-cc-heading flex items-center gap-1.5 font-medium">
                {line.gateway && (
                  <>
                    <CrownIcon className="text-cc-accent size-3.5 shrink-0" />
                    <span className="sr-only">
                      {line.verdict?.tied ? "Joint fastest: " : "Fastest: "}
                    </span>
                  </>
                )}
                {line.gateway ? (
                  gatewayLink(line.gateway) ? (
                    <Link href={gatewayLink(line.gateway)!}>
                      {line.gateway.name}
                    </Link>
                  ) : (
                    line.gateway.name
                  )
                ) : (
                  "No successful result"
                )}
              </dd>
            )}
            {line.verdict ? (
              <>
                <dd className="text-cc-heading col-start-2 tabular-nums sm:col-start-auto">
                  {formatNumber(line.verdict.leader.throughput.medianRps)}{" "}
                  <span className="text-cc-ink-dim">requests/s</span>
                </dd>
                <dd className="text-cc-ink-dim col-start-2 sm:col-start-auto">
                  {line.verdict.tied
                    ? "Tied within run variance"
                    : line.rival
                      ? `${formatPercent(line.verdict.lead)} faster`
                      : "Only successful router"}
                  {line.rival && (
                    <>
                      {` ${line.verdict.tied ? "with" : "than"} `}
                      {line.rival.name}
                    </>
                  )}
                </dd>
              </>
            ) : commonWinner ? (
              <dd className="text-cc-ink-dim col-start-2">
                No successful result
              </dd>
            ) : null}
          </div>
        ))}
      </dl>
    </>
  );
  return <section aria-labelledby={`${id}-heading`}>{body}</section>;
}
