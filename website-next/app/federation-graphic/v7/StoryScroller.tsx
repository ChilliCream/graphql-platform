"use client";

/**
 * The story scroller: one sticky canvas, seven beats. A single service; a
 * second team joins; their Product definitions drift; three more services
 * pile on and the client-side wiring becomes the mess it always becomes; then
 * the turn — it was always one Product (entities) — composition pulls the
 * streams into the glow node, and the benefits land on the settled scene.
 * The canvas is a pure function of the active step; CSS transitions carry
 * every change. Steps drive the stage via an IntersectionObserver.
 */

import { useEffect, useRef, useState } from "react";

import { MONO_FONT } from "./palette";
import { CANON, GlowNode, INK_DIM, stream } from "./visuals/stage";

const TR = "opacity 0.7s ease, transform 0.7s ease";

interface Step {
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
}

const STEPS: readonly Step[] = [
  {
    eyebrow: "The story",
    title: "It starts with one service.",
    body: "One team, one schema, one endpoint. Catalog owns products, clients ask for them, everyone goes home on time. There is rarely a company where it stays this way.",
  },
  {
    eyebrow: "The story",
    title: "Then there are two.",
    body: "Billing splits out: its own team, its own deploys, its own database. Reasonable — payment logic should not live next to product descriptions. But now there are two schemas, and clients talk to both.",
  },
  {
    eyebrow: "The friction",
    title: "Two teams. Two truths.",
    body: "Both services need Product. Catalog says price is Money. Billing says price is Float. Nobody decided this; it just happened in different sprints. Every client now stitches two APIs together and quietly papers over the difference.",
  },
  {
    eyebrow: "The friction",
    title: "Then five. The problems compound.",
    body: "Ordering, Shipping, and User join. Five endpoints, five drifting definitions, and every client rebuilds the same joins. Each new client multiplies the wiring; each schema change breaks someone you have never met.",
  },
  {
    eyebrow: "The turn",
    title: "It was always one Product.",
    body: "Step back and the mess has a shape. No service ever owned the whole Product: Catalog knows its name, Billing its price, Shipping its delivery. The same id means the same thing everywhere. That shared identity is an entity, and it is the key to everything that follows.",
  },
  {
    eyebrow: "The turn",
    title: "So compose, before it runs.",
    body: "Each team publishes its schema as a contract. Fusion composes them into one graph and validates every type against every other, at build time. The Float! that drifted in? It never ships; composition fails the build with the exact conflict. Clients get one endpoint.",
  },
  {
    eyebrow: "The payoff",
    title: "Teams ship apart. Clients query together.",
    body: "Every team keeps its own service, cadence, and deploys. Every client sees one coherent graph with one contract, guaranteed consistent before it went live. The wiring is gone; the graph does the traveling.",
  },
];

/* ── Canvas geometry ─────────────────────────────────────────────────── */

const CARD_W = 142;
const CARD_H = 64;

// Service card positions per stage-band: alone, pair, and the arc of five.
const P_ALONE: readonly (readonly [number, number])[] = [[309, 40]];
const P_PAIR: readonly (readonly [number, number])[] = [
  [120, 40],
  [460, 110],
];
const P_ARC: readonly (readonly [number, number])[] = [
  [10, 30],
  [160, 130],
  [310, 40],
  [462, 140],
  [612, 50],
];

const SERVICES = [
  { s: 0, facet: "name: String!" },
  { s: 1, facet: "price: Float!" },
  { s: 2, facet: "status: Status!" },
  { s: 3, facet: "delivery: Date!" },
  { s: 4, facet: "email: String!" },
] as const;

const CLIENTS = [
  { label: "web", x: 120, y: 540 },
  { label: "mobile", x: 330, y: 556 },
  { label: "partner", x: 540, y: 540 },
] as const;

const NODE: readonly [number, number] = [380, 330];

function cardPos(i: number, stage: number): readonly [number, number] {
  if (stage <= 0) {
    return i === 0 ? P_ALONE[0] : [P_ARC[i][0], -140];
  }
  if (stage <= 2) {
    if (i === 0) {
      return P_PAIR[0];
    }
    if (i === 1) {
      return P_PAIR[1];
    }
    return [P_ARC[i][0], -140];
  }
  return P_ARC[i];
}

const STREAMS = SERVICES.map((_, i) => {
  const [bx, by] = [P_ARC[i][0] + CARD_W / 2, P_ARC[i][1] + CARD_H];
  return stream(bx, by, NODE, 0.25);
});

interface CanvasProps {
  readonly stage: number;
}

function StoryCanvas({ stage }: CanvasProps) {
  const s = stage;
  const clientCount = s <= 0 ? 1 : s <= 2 ? 2 : 3;
  const visible = (i: number) => (i < 2 ? s >= i : s >= 3);
  const tangleOn = s === 3 ? 0.5 : s === 4 ? 0.1 : 0;
  const pairLinesOn = s >= 1 && s <= 2 ? 0.55 : 0;

  return (
    <svg
      viewBox="0 0 760 620"
      width="100%"
      className="block"
      aria-hidden="true"
    >
      {/* ── client wiring, per era ── */}
      {/* one clean line */}
      <path
        d={`M${P_ALONE[0][0] + CARD_W / 2} ${P_ALONE[0][1] + CARD_H} L385 540`}
        fill="none"
        stroke="rgba(139,160,188,0.5)"
        strokeWidth={1.5}
        style={{ opacity: s === 0 ? 0.7 : 0, transition: TR }}
      />
      {/* two services × two clients */}
      <g style={{ opacity: pairLinesOn, transition: TR }}>
        {[0, 1].map((i) =>
          CLIENTS.slice(0, 2).map((c) => (
            <path
              key={`${i}-${c.label}`}
              d={`M${P_PAIR[i][0] + CARD_W / 2} ${P_PAIR[i][1] + CARD_H} L${c.x + 55} ${c.y}`}
              fill="none"
              stroke="rgba(139,160,188,0.6)"
              strokeWidth={1.25}
            />
          )),
        )}
      </g>
      {/* the tangle: five × three */}
      <g style={{ opacity: tangleOn, transition: TR }}>
        {SERVICES.map((_, i) =>
          CLIENTS.map((c) => (
            <path
              key={`${i}-${c.label}`}
              d={`M${P_ARC[i][0] + CARD_W / 2} ${P_ARC[i][1] + CARD_H} L${c.x + 55} ${c.y}`}
              fill="none"
              stroke="rgba(139,160,188,0.55)"
              strokeWidth={1}
            />
          )),
        )}
      </g>

      {/* ── identity ties (the turn) ── */}
      <g style={{ opacity: s === 4 ? 1 : 0, transition: TR }}>
        {SERVICES.map((_, i) => {
          const [bx, by] = [P_ARC[i][0] + CARD_W / 2, P_ARC[i][1] + CARD_H];
          return (
            <path
              key={i}
              d={`M${bx} ${by} L${NODE[0]} ${NODE[1] - 26}`}
              fill="none"
              stroke={CANON[SERVICES[i].s].color}
              strokeWidth={1.25}
              strokeDasharray="3 6"
              strokeOpacity={0.65}
            />
          );
        })}
        <circle
          cx={NODE[0]}
          cy={NODE[1]}
          r={17}
          fill="none"
          stroke="#fff"
          strokeOpacity={0.8}
          strokeWidth={1.5}
        />
        <text
          x={NODE[0]}
          y={NODE[1] + 5}
          textAnchor="middle"
          fontFamily={MONO_FONT}
          fontSize={9.5}
          fill="#e8eef8"
        >
          P-401
        </text>
        <text
          x={NODE[0] + 30}
          y={NODE[1] - 18}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          fill={INK_DIM}
        >
          Product · same id everywhere
        </text>
      </g>

      {/* ── composition (the resolve) ── */}
      <g style={{ opacity: s >= 5 ? 1 : 0, transition: TR }}>
        {STREAMS.map((st, i) => (
          <path
            key={i}
            d={st.d}
            fill="none"
            stroke={CANON[SERVICES[i].s].color}
            strokeWidth={2}
            strokeOpacity={0.85}
            strokeLinecap="round"
          />
        ))}
        <rect
          x={NODE[0] - 0.75}
          y={NODE[1] + 10}
          width={1.5}
          height={165}
          fill="#f5f0ea"
          opacity={0.55}
        />
        {CLIENTS.map((c) => (
          <path
            key={c.label}
            d={`M${NODE[0]} 505 L${c.x + 55} ${c.y}`}
            fill="none"
            stroke="rgba(245,241,234,0.4)"
            strokeWidth={1.25}
          />
        ))}
        <GlowNode x={NODE[0]} y={NODE[1]} id="story-node" r={8} />
        <text
          x={NODE[0] - 96}
          y={NODE[1] + 4}
          textAnchor="end"
          fontFamily={MONO_FONT}
          fontSize={9.5}
          letterSpacing="0.2em"
          fill={INK_DIM}
        >
          FUSION COMPOSITION
        </text>
        <line
          x1={NODE[0] - 88}
          x2={NODE[0] - 30}
          y1={NODE[1]}
          y2={NODE[1]}
          stroke="rgba(245,241,234,0.3)"
          strokeDasharray="4 5"
        />
        <text
          x={NODE[0] + 30}
          y={NODE[1] + 34}
          fontFamily={MONO_FONT}
          fontSize={9.5}
          fill="#66be77"
          style={{
            opacity: s >= 5 ? 0.95 : 0,
            transition: TR,
            transitionDelay: "0.5s",
          }}
        >
          ✓ composed · conflicts fail the build
        </text>
      </g>

      {/* ── the services ── */}
      {SERVICES.map((svc, i) => {
        const [x, y] = cardPos(i, s);
        const isBilling = i === 1;
        const drift = isBilling && s >= 2 && s <= 4;
        return (
          <g
            key={i}
            style={{
              transform: `translate(${x}px, ${y}px)`,
              opacity: visible(i) ? 1 : 0,
              transition: TR,
            }}
          >
            <rect
              width={CARD_W}
              height={CARD_H}
              rx={10}
              fill="rgba(12,19,34,0.6)"
              stroke={
                drift && s === 2
                  ? "rgba(242,119,101,0.55)"
                  : "rgba(245,241,234,0.13)"
              }
              style={{ transition: "stroke 0.7s ease" }}
            />
            <rect
              x={12}
              y={11}
              width={8}
              height={8}
              rx={2.5}
              fill={CANON[svc.s].color}
            />
            <text
              x={27}
              y={19}
              fontFamily={MONO_FONT}
              fontSize={9}
              letterSpacing="0.14em"
              fill={INK_DIM}
            >
              {CANON[svc.s].name.toUpperCase()}
            </text>
            <text
              x={CARD_W - 10}
              y={19}
              textAnchor="end"
              fontFamily={MONO_FONT}
              fontSize={7.5}
              fill={INK_DIM}
              opacity={0.55}
            >
              team {String.fromCharCode(65 + i)}
            </text>
            <line
              x1={0}
              x2={CARD_W}
              y1={27}
              y2={27}
              stroke="rgba(245,241,234,0.1)"
            />
            <text
              x={12}
              y={42}
              fontFamily={MONO_FONT}
              fontSize={9.5}
              fill="#c9d4e8"
            >
              {"type Product {"}
            </text>
            {isBilling ? (
              <g>
                <text
                  x={20}
                  y={55}
                  fontFamily={MONO_FONT}
                  fontSize={9.5}
                  fill="#f27765"
                  style={{ opacity: s >= 5 ? 0 : 1, transition: TR }}
                >
                  price: Float!
                </text>
                <text
                  x={20}
                  y={55}
                  fontFamily={MONO_FONT}
                  fontSize={9.5}
                  fill="#66be77"
                  style={{ opacity: s >= 5 ? 1 : 0, transition: TR }}
                >
                  price: Money!
                </text>
              </g>
            ) : (
              <text
                x={20}
                y={55}
                fontFamily={MONO_FONT}
                fontSize={9.5}
                fill="#c9d4e8"
              >
                {svc.facet}
              </text>
            )}
          </g>
        );
      })}

      {/* Catalog's competing price line, alive during the friction era. */}
      <g
        style={{
          transform: `translate(${cardPos(0, s)[0]}px, ${cardPos(0, s)[1]}px)`,
          opacity: s >= 2 && s <= 4 ? 1 : 0,
          transition: TR,
        }}
      >
        <text
          x={92}
          y={55}
          fontFamily={MONO_FONT}
          fontSize={8}
          fill={s >= 4 ? INK_DIM : "#f27765"}
          style={{ transition: "fill 0.7s ease" }}
        >
          {s >= 4 ? "price ✕" : "price: Money!"}
        </text>
      </g>

      {/* ── problem badges ── */}
      {[
        "5 endpoints",
        "types drift apart",
        "every client re-stitches",
        "n × m integrations",
      ].map((label, k) => (
        <g
          key={label}
          style={{
            transform: `translate(596px, ${300 + k * 34}px)`,
            opacity: s === 3 ? 0.95 : 0,
            transition: TR,
            transitionDelay: s === 3 ? `${0.2 + k * 0.18}s` : "0s",
          }}
        >
          <rect
            width={156}
            height={24}
            rx={7}
            fill="rgba(12,19,34,0.7)"
            stroke="rgba(242,119,101,0.45)"
          />
          <text
            x={78}
            y={16}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill="#f9c2b8"
          >
            {label}
          </text>
        </g>
      ))}

      {/* ── benefit tags ── */}
      {[
        { label: "one endpoint", x: 420, y: 470, c: "#5eead4" },
        { label: "teams deploy independently", x: 96, y: 236, c: "#66be77" },
        { label: "validated before deploy ✓", x: 470, y: 236, c: "#66be77" },
      ].map((b, k) => (
        <g
          key={b.label}
          style={{
            transform: `translate(${b.x}px, ${b.y}px)`,
            opacity: s >= 6 ? 0.95 : 0,
            transition: TR,
            transitionDelay: s >= 6 ? `${0.15 + k * 0.2}s` : "0s",
          }}
        >
          <rect
            width={b.label.length * 6.2 + 24}
            height={24}
            rx={7}
            fill="rgba(12,19,34,0.7)"
            stroke={`${b.c}55`}
          />
          <text
            x={(b.label.length * 6.2 + 24) / 2}
            y={16}
            textAnchor="middle"
            fontFamily={MONO_FONT}
            fontSize={8.5}
            fill={b.c}
          >
            {b.label}
          </text>
        </g>
      ))}

      {/* ── clients ── */}
      {CLIENTS.map((c, i) => {
        const soloX = 330;
        const x = clientCount === 1 ? soloX : c.x;
        return (
          <g
            key={c.label}
            style={{
              transform: `translate(${x}px, ${c.y}px)`,
              opacity: i < clientCount ? 1 : 0,
              transition: TR,
            }}
          >
            <rect
              width={110}
              height={26}
              rx={7}
              fill="rgba(12,19,34,0.6)"
              stroke="rgba(245,241,234,0.13)"
            />
            <text
              x={55}
              y={17}
              textAnchor="middle"
              fontFamily={MONO_FONT}
              fontSize={9.5}
              fill={INK_DIM}
            >
              {c.label}
            </text>
          </g>
        );
      })}
    </svg>
  );
}

/* ── Scroller ────────────────────────────────────────────────────────── */

export function StoryScroller() {
  const [stage, setStage] = useState(0);
  const stepRefs = useRef<(HTMLDivElement | null)[]>([]);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            const idx = Number(
              (entry.target as HTMLElement).dataset.step ?? "0",
            );
            setStage(idx);
          }
        }
      },
      { rootMargin: "-45% 0px -45% 0px", threshold: 0 },
    );
    stepRefs.current.forEach((el) => {
      if (el) {
        observer.observe(el);
      }
    });
    return () => observer.disconnect();
  }, []);

  return (
    <section className="border-cc-card-border border-t">
      <div className="mx-auto max-w-6xl px-5 sm:px-12">
        <div className="grid grid-cols-1 gap-10 lg:grid-cols-12">
          {/* The story, step by step. */}
          <div className="lg:col-span-5">
            {STEPS.map((step, i) => (
              <div
                key={i}
                ref={(el) => {
                  stepRefs.current[i] = el;
                }}
                data-step={i}
                className="flex min-h-[70vh] items-center py-10 lg:min-h-[85vh]"
              >
                <div>
                  <div className="flex items-center gap-3">
                    <span className="text-cc-accent font-mono text-xs tracking-[0.2em]">
                      {String(i + 1).padStart(2, "0")}
                    </span>
                    <span
                      aria-hidden="true"
                      className="bg-cc-card-border h-px w-8"
                    />
                    <span className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
                      {step.eyebrow}
                    </span>
                  </div>
                  <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 text-balance">
                    {step.title}
                  </h2>
                  <p className="text-cc-ink mt-5 text-base">{step.body}</p>
                </div>
              </div>
            ))}
          </div>

          {/* The one canvas that lives through all of it. */}
          <div className="hidden lg:col-span-7 lg:block">
            <div className="sticky top-20 flex h-[calc(100vh-5rem)] items-center">
              <StoryCanvas stage={stage} />
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
