import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

const REGULATED_POINTS: readonly string[] = [
  "Procurement, MSA, and security review",
  "BYOC or fully on-prem deployments",
  "Dedicated onboarding & runbooks",
];

/**
 * The "regulated industry or air-gapped?" band: a short pitch for teams with
 * compliance constraints, with a checklist of what we handle and CTAs.
 */
export function RegulatedBand() {
  return (
    <section
      aria-labelledby="regulated-heading"
      className="border-cc-card-border bg-cc-card-bg/60 mt-24 rounded-3xl border p-8 sm:mt-28 sm:p-12"
    >
      <div className="grid items-center gap-8 md:grid-cols-[1.4fr_1fr]">
        <div>
          <p className="text-cc-ink-dim font-mono text-xs tracking-[0.18em] uppercase">
            Regulated &amp; on-prem
          </p>
          <h2
            id="regulated-heading"
            className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold text-balance"
          >
            Regulated industry or air-gapped?
          </h2>
          <p className="text-cc-ink mt-4 max-w-xl text-base text-pretty">
            We work directly with platform teams on procurement, data residency
            review, and dedicated onboarding. Bring us a constraint, we&rsquo;ll
            come back with an architecture.
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            <SolidButton href="/services/support/contact?subject=Sales">
              Talk to Sales
            </SolidButton>
            <OutlineButton href="/platform">See the platform</OutlineButton>
          </div>
        </div>
        <ul className="grid gap-3">
          {REGULATED_POINTS.map((item) => (
            <li
              key={item}
              className="border-cc-card-border bg-cc-bg/40 flex items-start gap-3 rounded-xl border px-4 py-3"
            >
              <span className="text-cc-accent mt-1 flex-none">
                <CheckIcon />
              </span>
              <span className="text-cc-ink text-sm">{item}</span>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
