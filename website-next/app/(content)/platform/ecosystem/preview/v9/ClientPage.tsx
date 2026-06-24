"use client";

import type { CSSProperties, ReactNode } from "react";
import { motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Scene accent                                                              */
/*  Page accent: cc-accent teal (#5eead4), playing the printer LED and the    */
/*  single highlighted PAID stamp. The brand spectrum (cyan -> violet ->      */
/*  coral) appears exactly once, on the torn perforation edge at the top of   */
/*  the receipt, like the tape was just pulled off the roll.                  */
/* -------------------------------------------------------------------------- */

const SPECTRUM_FROM = "#16b9e4";
const SPECTRUM_MID = "#7c92c6";
const SPECTRUM_TO = "#f0786a";

/* -------------------------------------------------------------------------- */
/*  Local CSS                                                                 */
/*  - paper grain: a faint, static horizontal scanline behind the receipt     */
/*  - the receipt "paper" body, with a serrated bottom edge via mask          */
/*  - edge-of-paper feed guides on either side at desktop only                */
/* -------------------------------------------------------------------------- */

const RECEIPT_STYLE = `
.cc-v9-grain {
  background-image: linear-gradient(
    180deg,
    transparent 0 3px,
    rgba(245, 241, 234, 0.015) 3px 4px
  );
}
.cc-v9-paper {
  /* serrated bottom edge, like the tape was torn off below the footer */
  -webkit-mask-image:
    linear-gradient(#000 0 0),
    repeating-linear-gradient(
      90deg,
      #000 0 8px,
      transparent 8px 9px
    );
  mask-image:
    linear-gradient(#000 0 0),
    repeating-linear-gradient(
      90deg,
      #000 0 8px,
      transparent 8px 9px
    );
  -webkit-mask-size:
    100% calc(100% - 7px),
    100% 7px;
  mask-size:
    100% calc(100% - 7px),
    100% 7px;
  -webkit-mask-position:
    top left,
    bottom left;
  mask-position:
    top left,
    bottom left;
  -webkit-mask-repeat: no-repeat;
  mask-repeat: no-repeat;
}
`;

/* -------------------------------------------------------------------------- */
/*  Shared receipt primitives                                                 */
/* -------------------------------------------------------------------------- */

/** A dashed divider carrying a centered BEGIN/END pill that floats on the rule. */
interface RuleProps {
  readonly label?: string;
}

function Rule({ label }: RuleProps) {
  return (
    <div className="relative my-1 flex items-center justify-center">
      <div className="border-cc-card-border w-full border-t border-dashed" />
      {label ? (
        <span className="bg-cc-surface text-cc-nav-label absolute px-2 font-mono text-[0.58rem] tracking-[0.22em] uppercase">
          {label}
        </span>
      ) : null}
    </div>
  );
}

/** A double dashed rule for the subtotal break. */
function DoubleRule() {
  return (
    <div className="my-2 flex flex-col gap-1" aria-hidden>
      <div className="border-cc-card-border w-full border-t border-dashed" />
      <div className="border-cc-card-border w-full border-t border-dashed" />
    </div>
  );
}

/** Centered eyebrow text used for store-style metadata lines. */
interface MetaLineProps {
  readonly label: string;
  readonly value: ReactNode;
}

function MetaLine({ label, value }: MetaLineProps) {
  return (
    <div className="flex items-baseline justify-between font-mono text-[0.7rem]">
      <span className="text-cc-nav-label tracking-[0.14em] uppercase">
        {label}
      </span>
      <span className="text-cc-ink tabular-nums">{value}</span>
    </div>
  );
}

/**
 * One receipt line item: a SKU + name + description on the left, and a small
 * right-aligned status column (the qty/price stand-in). `indent` nests the row
 * like a receipt item modifier.
 */
interface ItemProps {
  readonly sku?: string;
  readonly name: ReactNode;
  readonly desc?: ReactNode;
  readonly right: ReactNode;
  readonly indent?: boolean;
}

function Item({ sku, name, desc, right, indent = false }: ItemProps) {
  return (
    <div
      className={[
        "flex items-start justify-between gap-3 font-mono",
        indent ? "pl-5" : "",
      ].join(" ")}
    >
      <div className="min-w-0">
        <p className="flex items-baseline gap-2">
          {sku ? (
            <span className="text-cc-nav-label shrink-0 text-[0.62rem] tracking-[0.1em] tabular-nums">
              {sku}
            </span>
          ) : indent ? (
            <span className="text-cc-nav-label shrink-0 text-[0.7rem]">
              {">"}
            </span>
          ) : null}
          <span className="text-cc-heading text-[0.78rem]">{name}</span>
        </p>
        {desc ? (
          <p className="text-cc-ink-dim mt-0.5 text-[0.64rem] leading-snug">
            {desc}
          </p>
        ) : null}
      </div>
      <span className="text-cc-ink shrink-0 text-right text-[0.66rem] tabular-nums">
        {right}
      </span>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Section block: a BEGIN marker, item rows, then an END marker.             */
/* -------------------------------------------------------------------------- */

interface BlockProps {
  readonly name: string;
  readonly children: ReactNode;
}

function Block({ name, children }: BlockProps) {
  const reduce = useReducedMotion();
  return (
    <motion.section
      className="flex flex-col gap-3 py-3"
      initial={reduce ? false : { opacity: 0, y: 10 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.3 }}
      transition={{ duration: 0.45, ease: "easeOut" as const }}
    >
      <Rule label={`begin ${name}`} />
      <div className="flex flex-col gap-3 px-1">{children}</div>
      <Rule label={`end ${name}`} />
    </motion.section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Decorative SVGs: torn perforation edge + barcode                          */
/* -------------------------------------------------------------------------- */

/**
 * The one allowed brand-spectrum element on the page: a zigzag stroke along the
 * top of the receipt, colored cyan -> violet -> coral, like a fresh tear off a
 * roll of tape.
 */
function PerforationEdge() {
  return (
    <svg
      viewBox="0 0 440 12"
      preserveAspectRatio="none"
      className="block h-3 w-full"
      aria-hidden
    >
      <defs>
        <linearGradient id="cc-v9-tear" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor={SPECTRUM_FROM} />
          <stop offset="50%" stopColor={SPECTRUM_MID} />
          <stop offset="100%" stopColor={SPECTRUM_TO} />
        </linearGradient>
      </defs>
      <path
        d="M0 11 L10 3 L20 11 L30 3 L40 11 L50 3 L60 11 L70 3 L80 11 L90 3 L100 11 L110 3 L120 11 L130 3 L140 11 L150 3 L160 11 L170 3 L180 11 L190 3 L200 11 L210 3 L220 11 L230 3 L240 11 L250 3 L260 11 L270 3 L280 11 L290 3 L300 11 L310 3 L320 11 L330 3 L340 11 L350 3 L360 11 L370 3 L380 11 L390 3 L400 11 L410 3 L420 11 L430 3 L440 11"
        fill="none"
        stroke="url(#cc-v9-tear)"
        strokeWidth="2"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** A deterministic vertical-bar barcode rendered inline. */
function Barcode() {
  // A fixed, repeatable bar pattern so it renders identically on the server.
  const widths = [
    3, 1, 2, 1, 1, 3, 2, 1, 1, 2, 3, 1, 2, 1, 3, 1, 1, 2, 1, 3, 2, 1, 1, 2, 1,
    3, 1, 2, 3, 1, 1, 2, 1, 1, 3, 2, 1, 3, 1, 2, 1, 1, 2, 3, 1, 2, 1, 1,
  ];
  let x = 0;
  const bars: ReactNode[] = [];
  widths.forEach((w, i) => {
    if (i % 2 === 0) {
      bars.push(
        <rect
          key={i}
          x={x}
          y={0}
          width={w}
          height={40}
          fill="var(--color-cc-heading)"
        />,
      );
    }
    x += w;
  });
  return (
    <svg
      viewBox={`0 0 ${x} 40`}
      preserveAspectRatio="none"
      className="block h-10 w-full"
      role="img"
      aria-label="Barcode reading NITRO-CHILLICREAM-COM"
    >
      {bars}
    </svg>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export function ClientPage() {
  const reduce = useReducedMotion();

  // Time-driven, non-scroll-coupled: the printer LED and the blinking caret.
  const ledAnim = reduce
    ? undefined
    : {
        animate: { opacity: [0.4, 1, 0.4] },
        transition: {
          duration: 1.6,
          repeat: Infinity,
          ease: "easeInOut" as const,
        },
      };
  const caretAnim = reduce
    ? undefined
    : {
        animate: { opacity: [1, 1, 0, 0] },
        transition: {
          duration: 1.1,
          repeat: Infinity,
          times: [0, 0.5, 0.5, 1],
        },
      };

  // Edge-of-paper feed guides, desktop only, very faint.
  const guideStyle: CSSProperties = {
    background:
      "linear-gradient(180deg, transparent, var(--color-cc-card-border) 12%, var(--color-cc-card-border) 88%, transparent)",
  };

  return (
    <div className="relative">
      <style>{RECEIPT_STYLE}</style>

      {/* printer LED glow at the very top, the cc-accent light source */}
      <div
        className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-72"
        style={{
          background:
            "radial-gradient(60% 40% at 50% 0%, rgba(94,234,212,0.10), transparent 70%)",
        }}
        aria-hidden
      />

      {/* edge-of-paper feed guides, desktop only */}
      <div
        className="pointer-events-none absolute top-10 bottom-10 left-1/2 -z-10 hidden w-px -translate-x-[252px] opacity-40 lg:block"
        style={guideStyle}
        aria-hidden
      />
      <div
        className="pointer-events-none absolute top-10 bottom-10 left-1/2 -z-10 hidden w-px translate-x-[251px] opacity-40 lg:block"
        style={guideStyle}
        aria-hidden
      />

      <div className="mx-auto max-w-[440px] py-10">
        {/* the torn perforation: one spectrum event */}
        <PerforationEdge />

        {/* the paper tape itself */}
        <div className="cc-v9-paper border-cc-card-border bg-cc-surface relative border-x border-b px-6 pt-7 pb-9 shadow-2xl shadow-black/40">
          {/* faint paper grain behind the content */}
          <div
            className="cc-v9-grain pointer-events-none absolute inset-0"
            aria-hidden
          />

          <div className="relative">
            {/* --------------------------- HEADER --------------------------- */}
            <header className="flex flex-col gap-3 text-center">
              <pre
                className="text-cc-heading font-mono text-[0.62rem] leading-[1.15] tracking-tight"
                aria-hidden
              >
                {`  ___  _  _  ___  _    _    ___  ___  ___  ___  __  __
 / __|| || ||_ _|| |  | |  |_ _|/ __|| _ \\| __||  \\/  |
| (__ | __ | | | | |__| |__ | || (__ |   /| _| | |\\/| |
 \\___||_||_||___||____|____||___|\\___||_|_\\|___||_|  |_|`}
              </pre>
              <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.28em] uppercase">
                Nitro GraphQL IDE
              </p>

              <div className="border-cc-card-border my-1 border-t border-dashed" />

              <div className="flex flex-col gap-1 text-left">
                <MetaLine label="Terminal" value="NITRO-IDE / v15.0.0" />
                <MetaLine label="Cashier" value="your team" />
                <MetaLine label="Date" value="2026-06-23" />
                <MetaLine
                  label="Order"
                  value={<span className="text-cc-accent">#NITRO-001</span>}
                />
                <MetaLine label="Register" value="nitro.chillicream.com" />
              </div>

              <div className="border-cc-card-border my-1 border-t border-dashed" />

              {/* the receipt headline: the only h1 */}
              <h1 className="font-heading text-cc-heading text-h4 font-semibold tracking-tight">
                The IDE your team actually opens.
              </h1>
              <p className="text-cc-ink-dim font-mono text-[0.7rem] leading-relaxed">
                Sign in with OAuth, organize APIs in shared workspaces, sync
                documents across every device, install as a PWA. Itemized below,
                totaled at the bottom.
                <motion.span
                  className="bg-cc-accent ml-0.5 inline-block h-[0.85em] w-[0.5ch] translate-y-[0.12em] align-baseline"
                  aria-hidden
                  {...caretAnim}
                />
              </p>
            </header>

            {/* --------------------------- AUTH ----------------------------- */}
            <Block name="auth">
              <Item
                sku="AUTH-01"
                name="OAuth 2"
                desc="Browser sign in, the way human accounts log into your API."
                right={
                  <span>
                    scope: openid
                    <br />
                    x1 flow
                  </span>
                }
              />
              <Item
                sku="AUTH-01"
                name="Bearer token"
                desc="Static header for service accounts and CI runners."
                right={
                  <span>
                    scope: api
                    <br />
                    header
                  </span>
                }
                indent
              />
              <Item
                sku="AUTH-01"
                name="Basic"
                desc="Username and password to keep legacy endpoints reachable."
                right={
                  <span>
                    scope: legacy
                    <br />
                    user:pass
                  </span>
                }
                indent
              />
              <Rule />
              <p className="text-cc-nav-label px-1 font-mono text-[0.6rem] tracking-tight">
                per-connection auth, scoped to a single workspace
              </p>
            </Block>

            {/* ------------------------- WORKSPACES ------------------------- */}
            <Block name="workspaces">
              <Item
                sku="WSP-02"
                name="ChilliCream"
                desc="One organization holds every workspace your team owns."
                right="org"
              />
              <Item
                name="Checkout x 4 APIs"
                right={
                  <span>
                    5 members
                    <br />
                    Editor
                  </span>
                }
                indent
              />
              <Item
                name="Identity x 2 APIs"
                right={
                  <span>
                    3 members
                    <br />
                    Viewer
                  </span>
                }
                indent
              />
              <Item
                name="Catalog x 3 APIs"
                right={
                  <span>
                    4 members
                    <br />
                    Editor
                  </span>
                }
                indent
              />
              <Rule />
              <p className="text-cc-nav-label px-1 font-mono text-[0.6rem] tracking-tight">
                members, roles, and connection settings live with the workspace
              </p>
            </Block>

            {/* ---------------------------- SYNC ---------------------------- */}
            <Block name="sync">
              <Item
                sku="SYNC-03"
                name="MacBook Pro"
                desc="macOS 14.2, native window"
                right={<span className="text-cc-accent">last sync: now</span>}
              />
              <Item
                sku="SYNC-03"
                name="Chrome PWA"
                desc="Linux, installed app"
                right="last sync: 2s"
                indent
              />
              <Item
                sku="SYNC-03"
                name="Edge PWA"
                desc="Windows, installed app"
                right="last sync: 2s"
                indent
              />
              <Item
                sku="SYNC-03"
                name="iPad Pro"
                desc="PWA 17.2, offline, queued"
                right="last sync: queued"
                indent
              />
              <Rule />
              <div className="flex items-center justify-between px-1 font-mono text-[0.6rem]">
                <span className="text-cc-nav-label tracking-tight">
                  sync charge
                </span>
                <span className="text-cc-ink tabular-nums">$0.00 included</span>
              </div>
            </Block>

            {/* --------------------------- INSTALL -------------------------- */}
            <Block name="install">
              <Item
                sku="PWA-04"
                name="Browser"
                desc="Any modern browser promotes Nitro to a standalone window."
                right="incl."
              />
              <Item sku="PWA-04" name="macOS" right="incl." indent />
              <Item sku="PWA-04" name="Windows" right="incl." indent />
              <Item sku="PWA-04" name="Linux" right="incl." indent />
              <Rule />
              <div className="border-cc-accent/40 bg-cc-accent/[0.06] mx-1 flex items-center justify-between rounded-sm border border-dashed px-3 py-2">
                <span className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.16em] uppercase">
                  coupon
                </span>
                <span className="text-cc-accent font-mono text-[0.66rem] tracking-[0.1em] uppercase">
                  ADD-TO-DOCK
                </span>
              </div>
              <p className="text-cc-nav-label px-1 font-mono text-[0.6rem] tracking-tight">
                no installer, no admin prompt, no IT ticket
              </p>
            </Block>

            {/* ----------------------- THEMES + UPLOAD ---------------------- */}
            <Block name="themes + upload">
              <Item
                sku="UI-05"
                name="Themes"
                desc="Dark, light, or follow system across every surface."
                right={
                  <span className="inline-flex gap-1 align-middle">
                    <span
                      className="border-cc-card-border inline-block h-3 w-3 rounded-[2px] border"
                      style={{ background: "#0c1322" }}
                      aria-hidden
                    />
                    <span
                      className="border-cc-card-border inline-block h-3 w-3 rounded-[2px] border"
                      style={{ background: "#f5f1ea" }}
                      aria-hidden
                    />
                    <span
                      className="border-cc-card-border inline-block h-3 w-3 rounded-[2px] border"
                      style={{
                        background:
                          "linear-gradient(135deg,#0c1322 0 50%,#f5f1ea 50% 100%)",
                      }}
                      aria-hidden
                    />
                  </span>
                }
              />
              <Item
                sku="NET-06"
                name="Multipart upload"
                desc="Sends files per the GraphQL multipart request spec."
                right="3 parts"
              />
              <div className="mx-1 flex flex-col gap-1">
                <Item name="operations" right="412 B" indent />
                <Item name="map" right="63 B" indent />
                <Item name="0 (image/png)" right="184 KB" indent />
              </div>
              <Rule />
              <p className="text-cc-nav-label px-1 font-mono text-[0.6rem] tracking-tight">
                works with any spec-compliant GraphQL server
              </p>
            </Block>

            {/* ------------------------- SUBTOTAL --------------------------- */}
            <section className="flex flex-col gap-2 py-3 font-mono">
              <DoubleRule />
              <div className="flex flex-col gap-1.5 px-1">
                <MetaLine label="Items" value="6" />
                <MetaLine label="Devices synced" value="4" />
                <MetaLine label="Platforms" value="4" />
                <MetaLine label="Offline cache" value="12 MB" />
              </div>
              <DoubleRule />
              <div className="relative flex items-baseline justify-between px-1">
                <span className="text-cc-accent text-[0.82rem] font-semibold tracking-[0.18em] uppercase">
                  Total
                </span>
                <span className="text-cc-accent text-[0.9rem] tabular-nums">
                  $0.00 / seat
                </span>
                {/* PAID stamp, rotated, cc-accent outline */}
                <span
                  className="border-cc-accent text-cc-accent pointer-events-none absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 rounded-md border-2 px-3 py-1 font-mono text-[0.72rem] font-bold tracking-[0.24em] uppercase opacity-90"
                  style={{ rotate: "-12deg" }}
                  aria-hidden
                >
                  Paid
                </span>
              </div>
              <p className="text-cc-nav-label px-1 font-mono text-[0.58rem] tracking-tight">
                <span className="text-cc-accent">
                  <CheckIcon size={10} />
                </span>{" "}
                no signup gate, your workspace travels with you
              </p>
            </section>

            {/* -------------------------- FOOTER ---------------------------- */}
            <section className="flex flex-col items-center gap-4 py-3 text-center">
              <Rule />
              <div className="w-full px-2">
                <Barcode />
                <p className="text-cc-nav-label mt-2 font-mono text-[0.6rem] tracking-[0.3em] uppercase">
                  NITRO-CHILLICREAM-COM
                </p>
              </div>

              <div className="flex w-full flex-col gap-2.5 px-2 pt-1">
                <SolidButton
                  href="https://nitro.chillicream.com"
                  className="w-full"
                >
                  Launch Nitro
                </SolidButton>
                <OutlineButton href="/docs/nitro" className="w-full">
                  Read the Docs
                </OutlineButton>
              </div>

              <div className="flex items-center gap-2 pt-1">
                <motion.span
                  className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full"
                  aria-hidden
                  {...ledAnim}
                />
                <p className="text-cc-ink-dim font-mono text-[0.66rem] tracking-tight">
                  Thank you. Come back with a query.
                </p>
              </div>

              <p className="text-cc-nav-label max-w-[300px] font-mono text-[0.56rem] leading-relaxed tracking-tight">
                telemetry sold separately. The IDE serves the GraphQL endpoint
                UI wherever you point it. dashboards and traces require
                configuring Nitro on the server.
              </p>
            </section>
          </div>
        </div>
      </div>
    </div>
  );
}
