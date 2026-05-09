"use client";

import React, { FC } from "react";

import { DEFAULT_TRACE, TraceWaterfall } from "./TraceWaterfall";

// Section 01: composite hero. Copy on the left, composite mock on the right.
// The mock is a single bordered card containing the trace waterfall
// (gateway + 4 services), an inline error feed snippet, and a tiny
// schema-diff card. Stacked vertically inside one frame so it reads as a
// single "operator's window" surface, not three floating cards.

const HERO_FEED: readonly { msg: string; time: string; color: string }[] = [
  {
    msg: "TIMEOUT @ Billing.charge",
    time: "12:04",
    color: "var(--cc-col-cat)",
  },
  { msg: "OK @ Catalog.products", time: "12:04", color: "var(--cc-col-ord)" },
  {
    msg: "VALIDATION @ Shipping.quote",
    time: "12:03",
    color: "var(--cc-col-shi)",
  },
];

export const ObservabilityHero: FC = () => {
  return (
    <section className="cc-obs-section cc-obs-hero">
      <div className="cc-section-label">
        <span className="num">01</span> Observability
      </div>
      <div className="cc-obs-hero-inner">
        <div className="cc-obs-hero-copy">
          <div className="eyebrow">Nitro · Observability</div>
          <h1 className="display">
            Operator's <span className="accent">Window.</span>
          </h1>
          <p>
            One trace spans the gateway and every owning service. One control
            surface for staging and prod. The federation, finally legible.
          </p>
          <div className="cc-obs-hero-cta">
            <a href="/pricing" className="cc-btn cc-btn-primary">
              Start free →
            </a>
            <a
              href="mailto:contact@chillicream.com?subject=Nitro%20demo"
              className="cc-btn cc-btn-ghost"
            >
              Book a demo
            </a>
          </div>
        </div>

        <div className="cc-obs-hero-collage" aria-hidden>
          <div className="cc-obs-hero-collage-inner">
            <div className="cc-obs-hero-collage-header">
              <span>nitro · cart-checkout · 3a4f</span>
              <span className="dots">
                <span />
                <span />
                <span />
              </span>
            </div>
            <div
              style={{
                border: "1px solid var(--cc-ink-faint)",
                borderRadius: 12,
                background: "rgba(8,14,26,0.6)",
                padding: 16,
              }}
            >
              <TraceWaterfall
                spans={DEFAULT_TRACE}
                totalLabel="0ms · 600ms"
                axisMs={[0, 150, 300, 450, 600]}
              />
            </div>
            <div
              style={{
                display: "grid",
                gridTemplateColumns: "minmax(0, 1.1fr) minmax(0, 1fr)",
                gap: 12,
              }}
            >
              <div
                style={{
                  border: "1px solid var(--cc-ink-faint)",
                  borderRadius: 12,
                  background: "rgba(8,14,26,0.6)",
                  overflow: "hidden",
                }}
              >
                <div
                  style={{
                    padding: "10px 14px",
                    borderBottom: "1px solid var(--cc-ink-faint)",
                    fontFamily: "var(--cc-font-mono), monospace",
                    fontSize: 10,
                    letterSpacing: "0.16em",
                    textTransform: "uppercase",
                    color: "var(--cc-ink-dim)",
                  }}
                >
                  Errors · last hour
                </div>
                <ul
                  style={{
                    listStyle: "none",
                    margin: 0,
                    padding: 0,
                  }}
                >
                  {HERO_FEED.map((row, i) => (
                    <li
                      key={i}
                      style={{
                        display: "grid",
                        gridTemplateColumns: "10px minmax(0,1fr) auto",
                        gap: 10,
                        padding: "10px 14px",
                        borderBottom:
                          i < HERO_FEED.length - 1
                            ? "1px solid rgba(245,241,234,0.06)"
                            : "none",
                        fontSize: 12,
                        alignItems: "center",
                      }}
                    >
                      <span
                        style={{
                          width: 8,
                          height: 8,
                          borderRadius: 999,
                          background: row.color,
                        }}
                      />
                      <span
                        style={{
                          color: "var(--cc-ink)",
                          fontFamily: "var(--cc-font-mono), monospace",
                          overflow: "hidden",
                          textOverflow: "ellipsis",
                          whiteSpace: "nowrap",
                        }}
                      >
                        {row.msg}
                      </span>
                      <span
                        style={{
                          fontFamily: "var(--cc-font-mono), monospace",
                          fontSize: 10,
                          color: "var(--cc-ink-dim)",
                          letterSpacing: "0.08em",
                        }}
                      >
                        {row.time}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
              <div
                style={{
                  border: "1px solid var(--cc-ink-faint)",
                  borderRadius: 12,
                  background: "rgba(8,14,26,0.85)",
                  padding: "12px 14px",
                  fontFamily: "var(--cc-font-mono), monospace",
                  fontSize: 11.5,
                  lineHeight: 1.55,
                  color: "var(--cc-ink)",
                }}
              >
                <div
                  style={{
                    fontSize: 10,
                    letterSpacing: "0.16em",
                    textTransform: "uppercase",
                    color: "var(--cc-ink-dim)",
                    marginBottom: 8,
                  }}
                >
                  Schema diff · billing.graphql
                </div>
                <div
                  style={{
                    background: "rgba(220,110,80,0.1)",
                    color: "var(--cc-col-cat)",
                    padding: "2px 6px",
                    borderRadius: 4,
                    margin: "2px 0",
                  }}
                >
                  − zip: String!
                </div>
                <div
                  style={{
                    background: "rgba(110,200,140,0.1)",
                    color: "var(--cc-col-ord)",
                    padding: "2px 6px",
                    borderRadius: 4,
                    margin: "2px 0",
                  }}
                >
                  + postalCode: String!
                </div>
                <div
                  style={{
                    color: "var(--cc-ink-dim)",
                    marginTop: 8,
                    fontSize: 10,
                    letterSpacing: "0.06em",
                  }}
                >
                  1 breaking · 4 audited
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
