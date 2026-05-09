"use client";

import React, { FC } from "react";

interface TrustItem {
  readonly key: string;
  readonly title: string;
  readonly body: string;
  readonly icon: React.ReactNode;
}

const stroke = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.6,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

const ITEMS: readonly TrustItem[] = [
  {
    key: "federation",
    title: "Federation-aware out of the box",
    body: "Traces, errors, and replays know about the gateway and every owning service. Nothing to wire up.",
    icon: (
      <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden {...stroke}>
        <circle cx="12" cy="12" r="3" />
        <circle cx="4" cy="6" r="2" />
        <circle cx="20" cy="6" r="2" />
        <circle cx="4" cy="18" r="2" />
        <circle cx="20" cy="18" r="2" />
        <line x1="6" y1="7" x2="10" y2="11" />
        <line x1="18" y1="7" x2="14" y2="11" />
        <line x1="6" y1="17" x2="10" y2="13" />
        <line x1="18" y1="17" x2="14" y2="13" />
      </svg>
    ),
  },
  {
    key: "envs",
    title: "Same surface for dev, staging, prod",
    body: "One control surface across every environment. Capture in prod, replay in staging, ship the fix the same day.",
    icon: (
      <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden {...stroke}>
        <rect x="3" y="5" width="18" height="11" rx="2" />
        <path d="M3 10 H21" />
        <path d="M9 19 H15" />
        <path d="M12 16 V19" />
      </svg>
    ),
  },
  {
    key: "otel",
    title: "OpenTelemetry. Bring your backend.",
    body: "Federation traces drop into Jaeger, Tempo, Datadog, Honeycomb, Grafana, New Relic. No glue code, no proxies.",
    icon: (
      <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden {...stroke}>
        <path d="M4 12 a8 8 0 0 1 16 0" />
        <path d="M7 12 a5 5 0 0 1 10 0" />
        <circle cx="12" cy="12" r="2" />
        <line x1="12" y1="14" x2="12" y2="20" />
        <line x1="9" y1="20" x2="15" y2="20" />
      </svg>
    ),
  },
];

export const TrustStrip: FC = () => {
  return (
    <section className="cc-obs-section cc-obs-trust">
      <div className="cc-section-label">
        <span className="num">06</span> Trust
      </div>
      <div className="cc-obs-trust-inner">
        {ITEMS.map((item) => (
          <div key={item.key} className="cc-obs-trust-tile">
            <div className="cc-obs-trust-icon">{item.icon}</div>
            <h3 className="cc-obs-trust-title">{item.title}</h3>
            <p className="cc-obs-trust-body">{item.body}</p>
          </div>
        ))}
      </div>
    </section>
  );
};
