import {
  COMPOSITION_GROUPS,
  compositionReport,
  formatNumber,
  orderedScenarios,
  releasedGateways,
  SCENARIO_INFO,
  scenarioVerdict,
  type ScenarioVerdict,
} from "@/src/components/benchmarks/benchmarkView";
import { Link } from "@/src/design-system/Link";
import {
  BENCHMARK_FUSION_DOCS_URL,
  benchmarkVariants,
  decisiveFeatures,
  formatPlatformTag,
  gatewayLink,
  variantName,
  type BenchmarkCatalogEntry,
  type BenchmarkCompletedVariantResult,
  type BenchmarkReport,
  type BenchmarkVariantResult,
  type ScenarioId,
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
  | {
      readonly kind: "leads";
      readonly name: string;
      readonly gatewayId: string;
    }
  | {
      readonly kind: "leadsOrTies";
      readonly name: string;
      readonly gatewayId: string;
    }
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

/** Returns the sentence that summarizes every scenario. */
function summarySentence(overall: Overall) {
  switch (overall.kind) {
    case "leads":
      return `${overall.name} is the fastest router in every scenario.`;
    case "leadsOrTies":
      return `${overall.name} is the fastest router, or tied for fastest, in every scenario.`;
    case "tied":
      return `${overall.name} are tied for fastest in every scenario.`;
    case "none":
      return "No single router is the fastest in every scenario.";
    case "empty":
      return "No scenario has results without failed requests.";
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
  const features = decisiveFeatures(variants);
  const runtime = (item: BenchmarkCatalogEntry) =>
    formatPlatformTag(item.variant.platform, features.get(item.variant.id)!);
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

/** How a summary sentence refers to a runner-up whose gateway has never had a stable release. */
const UNRELEASED_RUNNER_UP = "the next fastest router";

/** How a summary row refers to a runner-up whose gateway has never had a stable release. */
const UNRELEASED_RUNNER_UP_SHORT = "the runner\u2011up";

/** Names the routers of a verdict, referring to a runner-up without a stable release by {@link UNRELEASED_RUNNER_UP}. */
function nameVerdict(
  verdict: ScenarioVerdict | undefined,
  variants: ReadonlyMap<string, BenchmarkCatalogEntry>,
  released: ReadonlySet<string>,
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
    runnerUp:
      verdict.runnerUp &&
      (released.has(variants.get(verdict.runnerUp.variant)!.gateway.id)
        ? named(verdict.runnerUp, verdict.leader)
        : {
            name: UNRELEASED_RUNNER_UP,
            rps: verdict.runnerUp.throughput.medianRps,
          }),
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
    ? { kind: "leads", name, gatewayId: shared[0] }
    : { kind: "leadsOrTies", name, gatewayId: shared[0] };
}

const formatList = (items: readonly string[]) =>
  new Intl.ListFormat("en", { type: "conjunction" }).format(items);

/** Returns the sentence that names the fastest router of each composition across every scenario, or undefined when no composition has a single fastest router with a stable release. */
export function fastestSentence(report: BenchmarkReport): string | undefined {
  const variants = benchmarkVariants(report);
  const released = releasedGateways(variants);
  const winners = COMPOSITION_GROUPS.flatMap((group) => {
    const scenarios = orderedScenarios(
      compositionReport(report, variants, group.id),
    );
    const verdicts = scenarios.map((scenario) =>
      scenarioVerdict(scenario, variants),
    );
    const overall = overallOf(verdicts, variants);
    if (
      (overall.kind !== "leads" && overall.kind !== "leadsOrTies") ||
      !released.has(overall.gatewayId)
    ) {
      return [];
    }
    const tied = scenarios
      .filter((_, index) => verdicts[index]?.tied)
      .map((scenario) => SCENARIO_INFO[scenario.id].name.toLowerCase());
    return {
      gatewayId: overall.gatewayId,
      name: overall.name,
      detail:
        tied.length === 0
          ? group.name
          : `${group.name}, tied in the ${formatList(tied)} scenario${tied.length > 1 ? "s" : ""}`,
    };
  });
  if (winners.length === 0) {
    return undefined;
  }
  const routers = Map.groupBy(winners, (winner) => winner.gatewayId);
  const parts = [...routers.values()].map((items) => {
    const details = items.map((item) => item.detail);
    return `${items[0].name} (${details.some((detail) => detail.includes(",")) ? details.join("; ") : formatList(details)})`;
  });
  return parts.length === 1
    ? `The fastest GraphQL federation router in the latest run is ${parts[0]}.`
    : `The fastest GraphQL federation routers in the latest run are ${formatList(parts)}.`;
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
  /** The variants shown in the tables of each scenario. A comparison is shown only when its runner-up is among them. */
  readonly visibleRows: ReadonlyMap<ScenarioId, ReadonlySet<string>>;
}

/** A written summary of every scenario: one overall sentence and one sentence per scenario. */
export function BenchmarkVerdictSummary({
  id,
  report,
  visibleRows,
}: BenchmarkVerdictSummaryProps) {
  const variants = benchmarkVariants(report);
  const released = releasedGateways(variants);
  const scenarios = orderedScenarios(report).map((scenario) => ({
    scenario,
    verdict: scenarioVerdict(scenario, variants),
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
    ? summarySentence(overall).replace(
        "in every scenario",
        "in every scenario with successful results",
      )
    : summarySentence(overall);
  const lines = scenarios.map(({ scenario, verdict }) => {
    const rival =
      verdict?.runnerUp &&
      visibleRows.get(scenario.id)?.has(verdict.runnerUp.variant)
        ? variants.get(verdict.runnerUp.variant)!.gateway
        : undefined;
    return {
      id: scenario.id,
      name: SCENARIO_INFO[scenario.id].name,
      text: scenarioSentence(nameVerdict(verdict, variants, released)),
      verdict,
      gateway: verdict && variants.get(verdict.leader.variant)!.gateway,
      rival,
      rivalName:
        rival &&
        (released.has(rival.id) ? rival.name : UNRELEASED_RUNNER_UP_SHORT),
    };
  });
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
      <dl
        className={`mt-3 space-y-2 text-sm sm:grid sm:space-y-0 sm:gap-x-4 sm:gap-y-2 ${commonWinner ? "sm:grid-cols-[auto_auto_minmax(0,1fr)]" : "sm:grid-cols-[auto_auto_auto_minmax(0,1fr)]"}`}
      >
        {lines.map((line) => (
          <div
            key={line.id}
            title={line.text}
            className="grid grid-cols-[7rem_1fr] gap-x-3 gap-y-1 sm:col-span-full sm:grid-cols-subgrid sm:whitespace-nowrap"
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
                {line.rival && (
                  <dd className="text-cc-ink-dim col-start-2 sm:col-start-auto sm:truncate">
                    {line.verdict.tied
                      ? `Tied with ${line.rivalName}`
                      : `${formatPercent(line.verdict.lead)} faster than ${line.rivalName}`}
                  </dd>
                )}
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
