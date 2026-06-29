import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

/**
 * Locked cc-* palette for the inline diagram: dark surfaces, neutral ink, teal
 * accent, amber for the single in-flight reaction, green for the request that
 * already returned. Mirrors the values behind the cc-* tokens so the SVG matches
 * the surrounding cards.
 */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  inkDim: "rgba(245, 241, 234, 0.62)",
  heading: "#f5f0ea",
  navLabel: "#62748e",
  accent: "#5eead4",
  amber: "#fbbf24",
  healthy: "#34d399",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "mocha-v7-";

/** The shipping follow-up advances one step at a time; "packed" is current. */
const SHIP_STEPS: readonly {
  readonly name: string;
  readonly state: "done" | "active" | "next";
}[] = [
  { name: "picking", state: "done" },
  { name: "packed", state: "active" },
  { name: "dispatched", state: "next" },
];

/** One reaction card in the stacked (mobile / tablet) form of the fan-out. */
function MobileReaction({
  action,
  note,
  inFlight = false,
}: {
  readonly action: string;
  readonly note: string;
  readonly inFlight?: boolean;
}) {
  return (
    <div className="border-cc-card-border bg-cc-surface rounded-xl border px-4 py-3">
      <div className="flex items-center justify-between gap-3">
        <p className="text-cc-heading font-mono text-sm">{action}</p>
        {inFlight && (
          <span className="inline-flex shrink-0 items-center rounded-full border border-[#fbbf24]/40 bg-[#fbbf24]/10 px-2 py-0.5 font-mono text-[0.6rem] font-medium text-[#fbbf24]">
            in flight
          </span>
        )}
      </div>
      <p className="text-cc-ink-dim mt-1 text-xs">{note}</p>
    </div>
  );
}

/** The shipping reaction, stacked: a short labeled process that advances. */
function MobileShipping() {
  return (
    <div className="border-cc-card-border bg-cc-surface rounded-xl border px-4 py-3">
      <p className="text-cc-heading font-mono text-sm">start shipping</p>
      <p className="text-cc-ink-dim mt-1 text-xs">
        shipping moves a step at a time
      </p>
      <div className="mt-3 flex items-center">
        {SHIP_STEPS.map((step, index) => (
          <span key={step.name} className="flex items-center">
            {index > 0 && (
              <span
                aria-hidden="true"
                className="text-cc-ink-faint px-1 text-[0.7rem]"
              >
                &rarr;
              </span>
            )}
            <span
              className={[
                "rounded-md border px-2 py-0.5 font-mono text-[0.65rem]",
                step.state === "active"
                  ? "border-cc-accent/60 text-cc-accent bg-cc-accent/5"
                  : step.state === "done"
                    ? "border-cc-card-border text-cc-ink-dim"
                    : "border-cc-ink-faint text-cc-ink-dim border-dashed",
              ].join(" ")}
            >
              {step.name}
            </span>
          </span>
        ))}
      </div>
    </div>
  );
}

/**
 * Mocha messaging section, take v7. A consequence fan-out in plain business
 * language: one event ("OrderPlaced") on the left, the actions that follow on
 * the right. One reaction stays in the same service, three cross a single
 * boundary into other services, one reaction is a small multi-step process, and
 * one copy of the event is in flight (amber). The heading block sits beside the
 * diagram on large screens and above it on small ones.
 */
export function MochaSectionV7() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="grid grid-cols-1 gap-10 lg:grid-cols-[minmax(0,24rem)_minmax(0,1fr)] lg:items-center lg:gap-12">
          {/* heading block */}
          <div>
            <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
              Messaging
            </p>
            <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
              Your app is mostly side effects.
            </h2>
            <p className="text-cc-ink mt-5 max-w-3xl text-base text-pretty sm:text-lg">
              Something happens in your app, and other things follow from it. An
              order is placed, so stock goes down, the customer is charged, a
              confirmation goes out, and shipping starts. You already write this
              work, usually tangled into the request that triggered it. Mocha
              lets you write it as what it is: one thing happening, and the rest
              reacting on its own.
            </p>
            <Link
              href="/platform/workflows"
              className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
            >
              Open workflows
              <span aria-hidden="true">&rarr;</span>
            </Link>
          </div>

          {/* diagram: one order, the reactions that follow */}
          <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover rounded-3xl border p-5 backdrop-blur-sm transition-colors sm:p-6">
            <div className="flex items-center justify-between gap-3">
              <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                from one order
              </span>
              <span className="border-cc-accent/35 text-cc-accent bg-cc-accent/10 inline-flex shrink-0 items-center rounded-full border px-2.5 py-0.5 font-mono text-[0.6rem] font-medium">
                4 things follow
              </span>
            </div>

            {/* large screens: inline fan-out diagram */}
            <svg
              viewBox="0 0 760 408"
              width="100%"
              aria-hidden="true"
              className="mt-4 hidden h-auto w-full lg:block"
              style={{ display: "block", fontFamily: MONO }}
            >
              <defs>
                <radialGradient id={`${ID}lit`} cx="28%" cy="50%" r="75%">
                  <stop offset="0" stopColor={C.accent} stopOpacity="0.16" />
                  <stop offset="0.7" stopColor={C.accent} stopOpacity="0" />
                </radialGradient>
                <linearGradient
                  id={`${ID}flow`}
                  gradientUnits="userSpaceOnUse"
                  x1="180"
                  y1="204"
                  x2="440"
                  y2="102"
                >
                  <stop offset="0" stopColor={C.amber} />
                  <stop offset="1" stopColor={C.accent} />
                </linearGradient>
              </defs>

              {/* faint teal wash behind the event: the one lit origin */}
              <rect
                x="10"
                y="150"
                width="190"
                height="110"
                fill={`url(#${ID}lit)`}
              />

              {/* a single thin service boundary the events cross */}
              <line
                x1="392"
                y1="44"
                x2="392"
                y2="392"
                stroke={C.inkFaint}
                strokeWidth="1"
                strokeDasharray="3 6"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="40"
                y="44"
                fontSize="7"
                letterSpacing="1.6"
                fill={C.navLabel}
              >
                SAME SERVICE
              </text>
              <text
                x="404"
                y="44"
                fontSize="7"
                letterSpacing="1.6"
                fill={C.navLabel}
              >
                OTHER SERVICES
              </text>

              {/* reduce stock stays in the same service: a solid near hop */}
              <path
                d="M180 204 C 195 168 202 120 210 99"
                fill="none"
                stroke={C.accent}
                strokeOpacity="0.55"
                strokeWidth="1.5"
                strokeLinecap="round"
                vectorEffect="non-scaling-stroke"
              />
              {/* charge payment: the active hop carrying the in-flight event */}
              <path
                d="M180 204 C 320 204 320 102 440 102"
                fill="none"
                stroke={`url(#${ID}flow)`}
                strokeOpacity="0.95"
                strokeWidth="1.75"
                strokeLinecap="round"
                vectorEffect="non-scaling-stroke"
              />
              {/* the two idle cross-boundary hops, waiting for their copy */}
              <path
                d="M180 204 C 320 204 320 206 440 206"
                fill="none"
                stroke={C.accent}
                strokeOpacity="0.3"
                strokeWidth="1.25"
                strokeDasharray="4 5"
                strokeLinecap="round"
                vectorEffect="non-scaling-stroke"
              />
              <path
                d="M180 204 C 320 204 320 331 440 331"
                fill="none"
                stroke={C.accent}
                strokeOpacity="0.3"
                strokeWidth="1.25"
                strokeDasharray="4 5"
                strokeLinecap="round"
                vectorEffect="non-scaling-stroke"
              />

              {/* in-flight event: one amber copy riding the active hop */}
              <circle cx="341" cy="135" r="3" fill={C.amber} />
              <rect
                x="276"
                y="141"
                width="84"
                height="24"
                rx="12"
                fill={C.surface}
                stroke={C.amber}
                strokeOpacity="0.9"
                strokeWidth="1.25"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="318"
                y="157"
                textAnchor="middle"
                fontSize="8.5"
                fontWeight="500"
                fill={C.amber}
              >
                OrderPlaced
              </text>

              {/* event node: the one thing that happened */}
              <rect
                x="24"
                y="164"
                width="156"
                height="80"
                rx="14"
                fill={C.surface}
                stroke={C.accent}
                strokeWidth="1.25"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="40"
                y="189"
                fontSize="7"
                letterSpacing="1.5"
                fill={C.navLabel}
              >
                EVENT
              </text>
              <text
                x="40"
                y="208"
                fontSize="14"
                fontWeight="600"
                fill={C.accent}
              >
                OrderPlaced
              </text>
              <text x="40" y="226" fontSize="8" fill={C.inkDim}>
                an order is placed
              </text>

              {/* reduce stock: a reaction in the same service */}
              <rect
                x="210"
                y="70"
                width="150"
                height="58"
                rx="12"
                fill={C.surface}
                fillOpacity="0.6"
                stroke={C.cardBorder}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="226"
                y="98"
                fontSize="12"
                fontWeight="600"
                fill={C.heading}
              >
                reduce stock
              </text>
              <text x="226" y="114" fontSize="8" fill={C.inkDim}>
                stock goes down
              </text>

              {/* charge payment: in flight, lit amber, in another service */}
              <rect
                x="440"
                y="70"
                width="296"
                height="64"
                rx="12"
                fill={C.surface}
                fillOpacity="0.6"
                stroke={C.amber}
                strokeOpacity="0.55"
                strokeWidth="1.25"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="456"
                y="92"
                fontSize="6.5"
                letterSpacing="1.5"
                fill={C.amber}
              >
                IN FLIGHT
              </text>
              <text
                x="456"
                y="109"
                fontSize="12"
                fontWeight="600"
                fill={C.heading}
              >
                charge payment
              </text>
              <text x="456" y="125" fontSize="8" fill={C.inkDim}>
                the customer is charged
              </text>

              {/* send confirmation: another service */}
              <rect
                x="440"
                y="176"
                width="296"
                height="60"
                rx="12"
                fill={C.surface}
                fillOpacity="0.6"
                stroke={C.cardBorder}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="456"
                y="202"
                fontSize="12"
                fontWeight="600"
                fill={C.heading}
              >
                send confirmation
              </text>
              <text x="456" y="219" fontSize="8" fill={C.inkDim}>
                a confirmation goes out
              </text>

              {/* start shipping: a small process that advances a step */}
              <rect
                x="440"
                y="276"
                width="296"
                height="110"
                rx="12"
                fill={C.surface}
                fillOpacity="0.6"
                stroke={C.cardBorder}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="456"
                y="302"
                fontSize="12"
                fontWeight="600"
                fill={C.heading}
              >
                start shipping
              </text>
              <text x="456" y="318" fontSize="7.5" fill={C.inkDim}>
                shipping moves a step at a time
              </text>
              {/* picking -> packed -> dispatched, "packed" current */}
              <rect
                x="456"
                y="338"
                width="74"
                height="22"
                rx="7"
                fill="none"
                stroke={C.cardBorder}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="493"
                y="353"
                textAnchor="middle"
                fontSize="8"
                fill={C.inkDim}
              >
                picking
              </text>
              <text
                x="536"
                y="353"
                textAnchor="middle"
                fontSize="9"
                fill={C.inkFaint}
              >
                &#8594;
              </text>
              <rect
                x="548"
                y="338"
                width="74"
                height="22"
                rx="7"
                fill="none"
                stroke={C.accent}
                strokeOpacity="0.6"
                strokeWidth="1.25"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="585"
                y="353"
                textAnchor="middle"
                fontSize="8"
                fontWeight="600"
                fill={C.accent}
              >
                packed
              </text>
              <text
                x="628"
                y="353"
                textAnchor="middle"
                fontSize="9"
                fill={C.inkFaint}
              >
                &#8594;
              </text>
              <rect
                x="640"
                y="338"
                width="80"
                height="22"
                rx="7"
                fill="none"
                stroke={C.inkFaint}
                strokeWidth="1"
                strokeDasharray="3 3"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="680"
                y="353"
                textAnchor="middle"
                fontSize="8"
                fill={C.inkDim}
              >
                dispatched
              </text>

              {/* connection dots where each hop meets a node */}
              <circle cx="180" cy="204" r="2.5" fill={C.accent} />
              <circle cx="210" cy="99" r="2" fill={C.accent} />
              <circle cx="440" cy="102" r="2.5" fill={C.amber} />
              <circle cx="440" cy="206" r="2" fill={C.accent} />
              <circle cx="440" cy="331" r="2" fill={C.accent} />

              {/* the order request already returned while the rest runs */}
              <rect
                x="100"
                y="152"
                width="80"
                height="18"
                rx="9"
                fill={C.surface}
                stroke={C.healthy}
                strokeOpacity="0.7"
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="140"
                y="164.5"
                textAnchor="middle"
                fontSize="7"
                fontWeight="500"
                fill={C.healthy}
              >
                order saved
              </text>
            </svg>

            {/* small / medium screens: the same fan-out, stacked */}
            <div className="mt-4 lg:hidden">
              <div className="border-cc-accent/40 bg-cc-surface rounded-2xl border p-4">
                <div className="flex items-center justify-between gap-3">
                  <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                    event
                  </span>
                  <span className="border-cc-status-healthy/50 bg-cc-status-healthy/10 text-cc-status-healthy inline-flex shrink-0 items-center rounded-full border px-2 py-0.5 font-mono text-[0.6rem] font-medium">
                    order saved
                  </span>
                </div>
                <p className="text-cc-accent mt-2 font-mono text-base font-semibold">
                  OrderPlaced
                </p>
                <p className="text-cc-ink-dim mt-0.5 text-xs">
                  an order is placed
                </p>
              </div>

              <div className="flex flex-col items-center py-3">
                <span className="bg-cc-card-border h-5 w-px" />
                <span className="text-cc-ink-dim py-1 text-center text-xs">
                  the rest follows on its own
                </span>
                <span className="bg-cc-card-border h-5 w-px" />
              </div>

              <p className="text-cc-nav-label mb-2 font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                same service
              </p>
              <MobileReaction action="reduce stock" note="stock goes down" />

              <div className="my-4 flex items-center gap-3">
                <span className="bg-cc-card-border h-px flex-1" />
                <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
                  other services
                </span>
                <span className="bg-cc-card-border h-px flex-1" />
              </div>

              <div className="space-y-3">
                <MobileReaction
                  action="charge payment"
                  note="the customer is charged"
                  inFlight
                />
                <MobileReaction
                  action="send confirmation"
                  note="a confirmation goes out"
                />
                <MobileShipping />
              </div>
            </div>
          </div>
        </div>
      </RevealOnScroll>
    </section>
  );
}
