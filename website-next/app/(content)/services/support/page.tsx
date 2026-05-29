import { type Plan, PlanGrid } from "@/src/components/PlanCard";
import { PageHero, Section } from "@/src/components/SectionTitle";

const SUPPORT_PLANS: Plan[] = [
  {
    title: "Community",
    price: 0,
    period: "month",
    description: "For personal or non-commercial projects, to start hacking.",
    features: ["Public Slack Channel"],
    ctaText: "Join Slack",
    ctaLink: "https://slack.chillicream.com/",
  },
  {
    title: "Startup",
    price: 450,
    period: "month",
    description:
      "For small teams with moderate bandwidth and projects of low to medium complexity.",
    features: ["Private Slack Channel", "2 critical incidents"],
    ctaText: "Contact Us",
    ctaLink: "/services/support/contact?plan=Startup",
  },
  {
    title: "Business",
    price: 1300,
    period: "month",
    description: "For larger teams with business-critical projects.",
    features: [
      "Private Slack Channel",
      "5 critical incidents",
      "2 non-critical incidents",
      "Email support",
    ],
    ctaText: "Contact Us",
    ctaLink: "/services/support/contact?plan=Business",
  },
  {
    title: "Enterprise",
    price: "custom",
    description:
      "For the whole organization, all your teams and business units, and with tailor made SLAs.",
    features: [
      "Private Slack Channel",
      "Unlimited critical incidents",
      "10 non-critical incidents",
      "Phone support",
      "Dedicated account manager",
      "Status reviews",
    ],
    ctaText: "Contact Us",
    ctaLink: "/services/support/contact?plan=Enterprise",
  },
];

interface FeatureValue {
  title: string;
  values: (boolean | string)[];
}

const COMPARISON: FeatureValue[] = [
  {
    title: "Critical Incidents",
    values: [
      false,
      "2 (next business day)",
      "5 (next business day)",
      "∞ (24 hours)",
    ],
  },
  {
    title: "Non-critical Incidents",
    values: [false, false, "5 (3 business days)", "10 (next business day)"],
  },
  { title: "Public Slack Channel", values: [true, true, true, true] },
  { title: "Private Slack Channel", values: [false, true, true, true] },
  { title: "Private Issue Tracking Board", values: [false, false, true, true] },
  { title: "Email Support", values: [false, false, true, true] },
  { title: "Phone Support", values: [false, false, false, true] },
  { title: "Dedicated Account Manager", values: [false, false, false, true] },
  { title: "Status Reviews", values: [false, false, false, true] },
];

const PLAN_NAMES = ["Community", "Startup", "Business", "Enterprise"];

export default function SupportPage() {
  return (
    <>
      <PageHero
        title="Expert Help When You Need It"
        teaser="At ChilliCream, we want you to be successful. Our Support plans are designed to give you peace of mind on every project."
      />

      <Section title="Support Plans">
        <PlanGrid plans={SUPPORT_PLANS} />
      </Section>

      <Section title="Compare our Support Plans">
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b border-[var(--cc-card-border)]">
                <th className="px-4 py-3 text-left font-semibold text-[var(--cc-ink)]">
                  Support
                </th>
                {PLAN_NAMES.map((name) => (
                  <th
                    key={name}
                    className="px-4 py-3 text-center font-semibold text-[var(--cc-ink)]"
                  >
                    {name}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {COMPARISON.map((row) => (
                <tr key={row.title} className="border-b border-[var(--cc-card-border)]">
                  <td className="px-4 py-3 text-[var(--cc-ink)]">{row.title}</td>
                  {row.values.map((v, i) => (
                    <td
                      key={i}
                      className="px-4 py-3 text-center text-[var(--cc-ink)]"
                    >
                      {v === true ? (
                        <span className="text-fuchsia-400">✓</span>
                      ) : v === false ? (
                        <span className="text-[var(--cc-ink-faint)]">—</span>
                      ) : (
                        v
                      )}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Section>
    </>
  );
}
