import Link from "next/link";

import { CheckIcon } from "./CheckIcon";

const BULLETS = [
  "Dedicated solution architect",
  "24x7 oncall rotation",
  "Custom uptime SLA (99.99%+)",
  "Federation governance + policies",
  "SOC 2 Type II + ISO 27001",
  "DPA, subprocessor list, security review",
];

// Enterprise banner: page accent washes the surface so the band itself
// carries the differentiation.
export function EnterpriseBanner() {
  return (
    <section className="py-16">
      <div className="overflow-hidden rounded-2xl border border-fuchsia-400/25 bg-[radial-gradient(60%_80%_at_100%_50%,rgba(217,70,239,0.12),transparent_70%),rgba(217,70,239,0.05)] p-8 backdrop-blur-sm sm:p-12">
        <div className="grid items-start gap-10 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,1fr)]">
          <div>
            <div className="mb-3 font-mono text-xs font-semibold uppercase tracking-widest text-[var(--cc-ink-dim)]">
              Enterprise + Support
            </div>
            <h2 className="text-3xl font-semibold leading-tight tracking-tight text-[var(--cc-ink)] sm:text-4xl">
              Running Fusion in production? Let&apos;s talk.
            </h2>
            <p className="mt-5 max-w-[50ch] text-base leading-relaxed text-[var(--cc-ink-dim)]">
              Enterprise wraps Nitro Self-Hosted with a dedicated solution
              architect, 24x7 oncall, custom SLA, and procurement-ready
              compliance evidence. We sign your DPA, answer your questionnaire,
              and stay on the call when something breaks.
            </p>
            <Link
              href="mailto:contact@chillicream.com?subject=Enterprise"
              className="mt-7 inline-flex items-center justify-center rounded-full bg-[var(--cc-ink)] px-6 py-3 text-sm font-medium text-[#0c1322] no-underline transition-colors hover:bg-white"
            >
              Talk to sales →
            </Link>
          </div>
          <ul className="grid gap-x-6 gap-y-4 sm:grid-cols-2">
            {BULLETS.map((bullet) => (
              <li
                key={bullet}
                className="flex items-start gap-2.5 text-sm leading-snug text-[var(--cc-ink)]"
              >
                <span aria-hidden className="mt-0.5 shrink-0 text-fuchsia-400">
                  <CheckIcon />
                </span>
                <span>{bullet}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}
