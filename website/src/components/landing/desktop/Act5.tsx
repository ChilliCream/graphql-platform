"use client";

import React from "react";

interface Model {
  key: string;
  tag: string;
  title: string;
  price: string;
  priceNote: string;
  copy: string;
  bullets: string[];
  cta: string;
  featured: boolean;
  badge?: string;
  icon: (color: string, sw: number) => React.ReactNode;
}

export const Act5: React.FC = () => {
  const MODELS: Model[] = [
    {
      key: "serverless",
      tag: "FRENCH PRESS",
      title: "Serverless",
      price: "Free",
      priceNote: "then pay per request",
      copy: "Push code, get a graph. Zero infrastructure to manage — we scale, secure, and observe it for you.",
      bullets: [
        "Zero-config deploys",
        "Pay per request",
        "Auto-scaling region pool",
        "Built-in observability",
      ],
      cta: "Start free →",
      featured: false,
      icon: (color, sw) => (
        <g
          stroke={color}
          strokeWidth={sw}
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <line x1="100" y1="20" x2="100" y2="34" />
          <circle cx="100" cy="20" r="4" />
          <rect x="50" y="34" width="100" height="14" rx="3" />
          <path d="M 56 48 L 56 192 L 144 192 L 144 48" />
          <path d="M 144 80 Q 168 80 168 110 Q 168 140 144 140" />
          <line x1="100" y1="48" x2="100" y2="120" />
          <line x1="68" y1="120" x2="132" y2="120" />
          <line x1="60" y1="160" x2="140" y2="160" opacity="0.4" />
        </g>
      ),
    },
    {
      key: "hosted",
      tag: "DRIP BREWER",
      title: "Hosted",
      price: "From $499",
      priceNote: "per month, single-tenant",
      copy: "Dedicated single-tenant clusters with reserved capacity. You own the SLA — we own the operations.",
      bullets: [
        "Reserved capacity",
        "Region pinning",
        "Custom retention windows",
        "99.99% SLA + 24/7 oncall",
      ],
      cta: "Talk to sales →",
      featured: true,
      badge: "MOST POPULAR",
      icon: (color, sw) => (
        <g
          stroke={color}
          strokeWidth={sw}
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M 36 28 L 164 28 L 164 70 L 132 70 L 132 90 L 68 90 L 68 70 L 36 70 Z" />
          <path d="M 76 90 L 124 90 L 110 132 L 90 132 Z" />
          <path d="M 70 138 L 130 138 L 138 198 L 62 198 Z" />
          <path d="M 130 150 Q 154 150 154 174 Q 154 196 138 196" />
          <line x1="68" y1="172" x2="132" y2="172" opacity="0.4" />
          <line x1="40" y1="200" x2="160" y2="200" />
        </g>
      ),
    },
    {
      key: "self-hosted",
      tag: "POUR OVER",
      title: "Self-Hosted",
      price: "BYO infra",
      priceNote: "Helm, Docker, or bare-metal",
      copy: "Run it on your own infrastructure. Full data sovereignty, air-gapped installs, your security review.",
      bullets: [
        "Helm + Docker",
        "Air-gapped install",
        "Full data sovereignty",
        "Bring-your-own observability",
      ],
      cta: "Get the binaries →",
      featured: false,
      icon: (color, sw) => (
        <g
          stroke={color}
          strokeWidth={sw}
          fill="none"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M 56 28 L 144 28 L 116 100 L 84 100 Z" />
          <line x1="76" y1="108" x2="124" y2="108" />
          <line x1="76" y1="118" x2="124" y2="118" />
          <path d="M 84 118 L 60 196 Q 60 204 70 204 L 130 204 Q 140 204 140 196 L 116 118" />
          <line x1="66" y1="178" x2="134" y2="178" opacity="0.4" />
          <path d="M 56 28 Q 64 22 72 28" />
        </g>
      ),
    },
  ];

  const COMMON_FEATURES = [
    "SSO + RBAC",
    "Audit logs",
    "Schema registry",
    "Federation",
    "OpenTelemetry",
    "GraphiQL",
  ];

  return (
    <section
      className="cc-act cc-act-brew"
      data-screen-label="06 Brew it your way"
    >
      <div className="cc-act-label">
        <span className="num">06</span> Brew it your way
      </div>

      <div className="cc-brew-inner-d">
        <div className="cc-brew-heading-d">
          <div className="eyebrow">Three ways to deploy</div>
          <h2 className="display">Brew it your way.</h2>
          <p>
            Same engine, same APIs, same DX — pick the operational shape that
            fits your team.
          </p>
        </div>

        <div className="cc-brew-grid-d">
          {MODELS.map((m) => (
            <article
              key={m.key}
              className={"cc-brew-card-d" + (m.featured ? " is-featured" : "")}
            >
              {m.badge && <div className="cc-brew-badge-d">{m.badge}</div>}

              <div className="cc-brew-icon-d">
                <svg
                  viewBox="0 0 200 220"
                  width="100%"
                  height="100%"
                  aria-hidden
                >
                  {m.icon("var(--cc-ink)", 1.6)}
                </svg>
              </div>

              <div className="cc-brew-tag-d">{m.tag}</div>
              <h3 className="cc-brew-title-d">{m.title}</h3>

              <div className="cc-brew-price-d">
                <span className="cc-brew-price-amount-d">{m.price}</span>
                <span className="cc-brew-price-note-d">{m.priceNote}</span>
              </div>

              <p className="cc-brew-copy-d">{m.copy}</p>

              <ul className="cc-brew-bullets-d">
                {m.bullets.map((b) => (
                  <li key={b}>
                    <svg viewBox="0 0 16 16" width="14" height="14" aria-hidden>
                      <path
                        d="M3 8.5 L6.5 12 L13 4.5"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                      />
                    </svg>
                    <span>{b}</span>
                  </li>
                ))}
              </ul>

              <button
                className={
                  "cc-btn " + (m.featured ? "cc-btn-primary" : "cc-btn-ghost")
                }
              >
                {m.cta}
              </button>
            </article>
          ))}
        </div>

        <div className="cc-brew-common-d">
          <span className="cc-brew-common-label-d">Every tier ships with</span>
          <ul className="cc-brew-common-list-d">
            {COMMON_FEATURES.map((f) => (
              <li key={f}>{f}</li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
};
