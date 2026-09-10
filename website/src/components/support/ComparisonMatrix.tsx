import { FeatureComparison } from "@/src/components/FeatureComparison";

const PLAN_NAMES: readonly string[] = [
  "Community",
  "Startup",
  "Business",
  "Enterprise",
];

const COMPARISON = [
  {
    title: "Response & incidents",
    rows: [
      {
        title: "Critical incidents",
        values: [
          false,
          "2 (next business day)",
          "5 (next business day)",
          "Unlimited (24 hours)",
        ],
      },
      {
        title: "Non-critical incidents",
        values: [
          false,
          false,
          "Included (3 business days)",
          "10 (next business day)",
        ],
      },
    ],
  },
  {
    title: "Channels",
    rows: [
      {
        title: "Public Slack channel",
        values: [true, true, true, true],
      },
      {
        title: "Private Slack channel",
        values: [false, true, true, true],
      },
      {
        title: "Private issue tracking board",
        values: [false, false, true, true],
      },
      {
        title: "Email support",
        values: [false, false, true, true],
      },
      {
        title: "Phone support",
        values: [false, false, false, true],
      },
    ],
  },
  {
    title: "Strategic",
    rows: [
      {
        title: "Dedicated account manager",
        values: [false, false, false, true],
      },
      {
        title: "Status reviews",
        hint: "Recurring check-ins on roadmap, upgrades, and posture.",
        values: [false, false, false, true],
      },
    ],
  },
];

/**
 * The support plan comparison: maps the positional plan comparison data onto the
 * shared `FeatureComparison` table.
 */
export function ComparisonMatrix() {
  const groups = COMPARISON.map((group) => ({
    title: group.title,
    rows: group.rows.map((row) => ({
      label: row.title,
      cells: row.values,
    })),
  }));

  return (
    <FeatureComparison
      id="compare"
      className="py-16"
      eyebrow="Compare plans"
      heading="Compare GraphQL support coverage"
      columns={PLAN_NAMES}
      groups={groups}
    />
  );
}
