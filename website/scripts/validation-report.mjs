const DEFAULT_WARNING_LIMIT = 10;

export function createReport() {
  const findings = [];

  return {
    findings,
    error(rule, message) {
      findings.push({ severity: "error", rule, message });
    },
    warn(rule, message) {
      findings.push({ severity: "warning", rule, message });
    },
    count(severity) {
      return findings.filter((finding) => finding.severity === severity).length;
    },
    hasErrors() {
      return findings.some((finding) => finding.severity === "error");
    },
  };
}

export function groupByRule(findings, severity) {
  const groups = new Map();
  for (const finding of findings) {
    if (finding.severity !== severity) {
      continue;
    }
    const messages = groups.get(finding.rule) ?? [];
    messages.push(finding.message);
    groups.set(finding.rule, messages);
  }
  return [...groups.entries()].sort(([a], [b]) => a.localeCompare(b));
}

export function formatGroups(
  findings,
  severity,
  limit = Number.POSITIVE_INFINITY,
) {
  const lines = [];
  for (const [rule, messages] of groupByRule(findings, severity)) {
    lines.push(`[${rule}] ${messages.length}`);
    for (const message of messages.slice(0, limit)) {
      lines.push(`  - ${message}`);
    }
    if (messages.length > limit) {
      lines.push(`  ... and ${messages.length - limit} more`);
    }
  }
  return lines;
}

export function formatSummary(findings) {
  const errors = findings.filter((finding) => finding.severity === "error");
  const warnings = findings.filter((finding) => finding.severity === "warning");
  const errorRules = new Set(errors.map((finding) => finding.rule)).size;
  const warningRules = new Set(warnings.map((finding) => finding.rule)).size;
  return `Summary: ${errors.length} errors in ${errorRules} rules, ${warnings.length} warnings in ${warningRules} rules.`;
}

export function printReport(
  report,
  { warningLimit = DEFAULT_WARNING_LIMIT, log = console.log } = {},
) {
  const { findings } = report;
  if (report.count("warning") > 0) {
    log(`Warnings (${report.count("warning")}):`);
    for (const line of formatGroups(findings, "warning", warningLimit)) {
      log(line);
    }
  }
  if (report.hasErrors()) {
    log(`Errors (${report.count("error")}):`);
    for (const line of formatGroups(findings, "error")) {
      log(line);
    }
  }
  log(formatSummary(findings));
}
