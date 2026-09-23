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
        title: "Critical Incidents",
        values: [
          false,
          "2 (next business day)",
          "5 (next business day)",
          "Unlimited (24 hours)",
        ],
      },
      {
        title: "Non-critical Incidents",
        values: [false, false, "5 (3 business days)", "10 (next business day)"],
      },
    ],
  },
  {
    title: "Channels",
    rows: [
      {
        title: "Public Slack Channel",
        values: [true, true, true, true],
      },
      {
        title: "Private Slack Channel",
        values: [false, true, true, true],
      },
      {
        title: "Private Issue Tracking Board",
        values: [false, false, true, true],
      },
      {
        title: "Email Support",
        values: [false, false, true, true],
      },
      {
        title: "Phone Support",
        values: [false, false, false, true],
      },
    ],
  },
  {
    title: "Strategic",
    rows: [
      {
        title: "Dedicated Account Manager",
        values: [false, false, false, true],
      },
      {
        title: "Status Reviews",
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
      heading="Feature comparison"
      columns={PLAN_NAMES}
      groups={groups}
    />
  );
}
