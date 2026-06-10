import { type Plan, PlanGrid } from "@/src/components/PlanGrid";
import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
} from "@/src/design-system/Table";
import { CheckIcon } from "@/src/components/CheckIcon";

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
        <Table>
          <TableHead>
            <TableRow>
              <TableHeaderCell>Support</TableHeaderCell>
              {PLAN_NAMES.map((name) => (
                <TableHeaderCell key={name} align="center">
                  {name}
                </TableHeaderCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {COMPARISON.map((row) => (
              <TableRow key={row.title}>
                <TableCell>{row.title}</TableCell>
                {row.values.map((v, i) => (
                  <TableCell key={i} align="center">
                    {v === true ? (
                      <span className="inline-flex items-center justify-center text-cc-accent">
                        <CheckIcon />
                      </span>
                    ) : v === false ? (
                      <span className="text-cc-prose">—</span>
                    ) : (
                      v
                    )}
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Section>
    </>
  );
}
