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

// Enterprise banner lives on an accent band: page accent washes the surface so
// the band itself carries the differentiation. Layout is edge-to-edge
// content-on-band, no inner card chrome.
export function EnterpriseBanner() {
  return (
    <div className="cc-enterprise">
      <div className="cc-enterprise-inner">
        <div className="cc-enterprise-grid">
          <div className="cc-enterprise-copy">
            <div className="eyebrow">Enterprise + Support</div>
            <h2 className="display">
              Running Fusion in production? Let&apos;s talk.
            </h2>
            <p>
              Enterprise wraps Nitro Self-Hosted with a dedicated solution
              architect, 24x7 oncall, custom SLA, and procurement-ready
              compliance evidence. We sign your DPA, answer your questionnaire,
              and stay on the call when something breaks.
            </p>
            <Link
              href="mailto:contact@chillicream.com?subject=Enterprise"
              className="cc-btn cc-btn-primary"
            >
              Talk to sales →
            </Link>
          </div>
          <ul className="cc-enterprise-bullets">
            {BULLETS.map((bullet) => (
              <li key={bullet}>
                <CheckIcon />
                <span>{bullet}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}
