"use client";

import type { CSSProperties, ReactNode } from "react";
import { motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Scene accent                                                              */
/*  Page accent: cyan #16b9e4 (the airline livery).                           */
/*  Brand spectrum (cyan -> violet -> coral) appears exactly once, as a thin  */
/*  2px gradient strip along the BOARDING PASS hero wordmark.                 */
/* -------------------------------------------------------------------------- */

const CYAN = "#16b9e4";
const SPECTRUM_FROM = "#16b9e4";
const SPECTRUM_MID = "#7c92c6";
const SPECTRUM_TO = "#f0786a";

/* -------------------------------------------------------------------------- */
/*  Local CSS                                                                 */
/*  - perforated tear-line that turns cyan on hover                           */
/*  - tiny stub shift on hover                                                */
/*  - scanline ticket texture behind the hero                                 */
/*  - now-boarding dot pulse, paused under reduced motion                     */
/* -------------------------------------------------------------------------- */

const PASS_STYLE = `
.cc-v8-pass {
  position: relative;
  display: grid;
  grid-template-columns: 1fr;
  border-radius: 14px;
  overflow: hidden;
  background: var(--color-cc-card-bg);
  border: 1px solid var(--color-cc-card-border);
  transition: border-color 180ms ease;
}
@media (min-width: 640px) {
  .cc-v8-pass {
    grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
  }
}
.cc-v8-pass__body {
  padding: 1.25rem 1.5rem;
  min-width: 0;
}
.cc-v8-pass__stub {
  position: relative;
  padding: 1.25rem 1.25rem 1.25rem 1.5rem;
  background:
    repeating-linear-gradient(
      0deg,
      rgba(22, 185, 228, 0.04) 0px,
      rgba(22, 185, 228, 0.04) 1px,
      transparent 1px,
      transparent 6px
    ),
    rgba(12, 19, 34, 0.55);
  transition: transform 180ms ease;
  min-width: 0;
}
@media (min-width: 640px) {
  .cc-v8-pass__stub {
    border-left: 1px dashed var(--color-cc-card-border);
    transition: transform 180ms ease, border-left-color 180ms ease;
  }
}
@media (max-width: 639.98px) {
  .cc-v8-pass__stub {
    border-top: 1px dashed var(--color-cc-card-border);
    transition: transform 180ms ease, border-top-color 180ms ease;
  }
}
.cc-v8-pass:hover {
  border-color: var(--color-cc-card-border-hover);
}
.cc-v8-pass:hover .cc-v8-pass__stub {
  border-left-color: ${CYAN};
  border-top-color: ${CYAN};
  transform: translateX(2px);
}
@media (max-width: 639.98px) {
  .cc-v8-pass:hover .cc-v8-pass__stub {
    transform: translateY(2px);
  }
}

@keyframes cc-v8-now-boarding {
  0%, 100% { opacity: 0.35; transform: scale(0.85); }
  50%      { opacity: 1;    transform: scale(1.1); }
}
.cc-v8-now-boarding-dot {
  display: inline-block;
  width: 0.55rem;
  height: 0.55rem;
  border-radius: 999px;
  background: ${CYAN};
  box-shadow: 0 0 0 0 rgba(22, 185, 228, 0.6);
  animation: cc-v8-now-boarding 1s infinite ease-in-out;
}

.cc-v8-hero-texture {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background-image: repeating-linear-gradient(
    0deg,
    rgba(94, 234, 212, 0.02) 0px,
    rgba(94, 234, 212, 0.02) 1px,
    transparent 1px,
    transparent 4px
  );
  border-radius: inherit;
}

.cc-v8-barcode {
  display: flex;
  align-items: stretch;
  gap: 2px;
  height: 32px;
}
.cc-v8-barcode > span {
  display: block;
  background: var(--color-cc-ink-dim);
}

.cc-v8-stamp {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.35rem 0.7rem;
  border: 2px solid ${CYAN};
  color: ${CYAN};
  border-radius: 4px;
  font-family: var(--font-mono);
  font-size: 0.66rem;
  letter-spacing: 0.22em;
  text-transform: uppercase;
  transform: rotate(-3deg);
  box-shadow: inset 0 0 0 1px rgba(22, 185, 228, 0.25);
}

@media (prefers-reduced-motion: reduce) {
  .cc-v8-now-boarding-dot { animation: none; opacity: 0.8; }
  .cc-v8-pass, .cc-v8-pass__stub { transition: none; }
  .cc-v8-pass:hover .cc-v8-pass__stub { transform: none; }
}
`;

/* -------------------------------------------------------------------------- */
/*  Primitives                                                                */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface PassHeaderProps {
  readonly code: string;
  readonly flight: string;
  readonly status?: string;
}

function PassHeader({ code, flight, status = "ON TIME" }: PassHeaderProps) {
  return (
    <div className="border-cc-card-border flex items-center justify-between gap-3 border-b border-dashed px-5 py-2.5">
      <span
        className="font-mono text-[0.66rem] tracking-[0.22em] uppercase"
        style={{ color: CYAN }}
      >
        {code}
      </span>
      <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-tight">
        {flight}
      </span>
      <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-[0.18em] uppercase">
        {status}
      </span>
    </div>
  );
}

interface RouteRowProps {
  readonly from: string;
  readonly to: string;
  readonly fromLabel?: string;
  readonly toLabel?: string;
}

function RouteRow({
  from,
  to,
  fromLabel = "DEPARTS",
  toLabel = "ARRIVES",
}: RouteRowProps) {
  return (
    <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-3">
      <div>
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.2em] uppercase">
          {fromLabel}
        </p>
        <p className="text-cc-heading mt-1 font-mono text-[0.95rem] tracking-tight">
          {from}
        </p>
      </div>
      <div
        className="flex items-center gap-1.5 font-mono text-[0.7rem]"
        style={{ color: CYAN }}
        aria-hidden
      >
        <span className="h-px w-5" style={{ background: CYAN }} />
        <svg viewBox="0 0 16 16" className="h-3.5 w-3.5">
          <path
            d="M1 8h11M9 4l4 4-4 4"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </div>
      <div className="text-right">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.2em] uppercase">
          {toLabel}
        </p>
        <p className="text-cc-heading mt-1 font-mono text-[0.95rem] tracking-tight">
          {to}
        </p>
      </div>
    </div>
  );
}

interface StubRowProps {
  readonly label: string;
  readonly value: string;
}

function StubRow({ label, value }: StubRowProps) {
  return (
    <div>
      <p className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.2em] uppercase">
        {label}
      </p>
      <p className="text-cc-heading mt-0.5 font-mono text-[0.82rem] tracking-tight">
        {value}
      </p>
    </div>
  );
}

function Barcode({ seed }: { readonly seed: string }) {
  // Deterministic bar pattern derived from the seed so SSR matches CSR.
  const bars: number[] = [];
  let h = 0;
  for (let i = 0; i < seed.length; i++) {
    h = (h * 31 + seed.charCodeAt(i)) | 0;
  }
  for (let i = 0; i < 28; i++) {
    h = (h * 1103515245 + 12345) | 0;
    const v = ((h >>> 16) & 0xff) % 6;
    bars.push(v < 2 ? 1 : v < 4 ? 2 : 3);
  }
  return (
    <div className="cc-v8-barcode" aria-hidden>
      {bars.map((w, i) => (
        <span key={i} style={{ width: `${w}px`, opacity: 0.55 }} />
      ))}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Boarding pass card                                                        */
/* -------------------------------------------------------------------------- */

interface BoardingPassProps {
  readonly code: string;
  readonly flight: string;
  readonly status?: string;
  readonly title: string;
  readonly description: string;
  readonly bullets: readonly string[];
  readonly from: string;
  readonly to: string;
  readonly fromLabel?: string;
  readonly toLabel?: string;
  readonly stub: ReadonlyArray<{
    readonly label: string;
    readonly value: string;
  }>;
  readonly extra?: ReactNode;
}

function BoardingPass({
  code,
  flight,
  status,
  title,
  description,
  bullets,
  from,
  to,
  fromLabel,
  toLabel,
  stub,
  extra,
}: BoardingPassProps) {
  return (
    <article className="cc-v8-pass">
      <div className="cc-v8-pass__body flex flex-col gap-5">
        <PassHeader code={code} flight={flight} status={status} />
        <div>
          <h3 className="font-heading text-h5 text-cc-heading font-semibold tracking-tight">
            {title}
          </h3>
          <p className="text-cc-ink-dim mt-3 text-[0.96rem] leading-relaxed">
            {description}
          </p>
        </div>
        <RouteRow from={from} to={to} fromLabel={fromLabel} toLabel={toLabel} />
        <ul className="flex flex-col gap-2">
          {bullets.map((b) => (
            <li
              key={b}
              className="text-cc-ink-dim flex items-start gap-2.5 text-[0.88rem] leading-relaxed"
            >
              <span className="mt-1 shrink-0" style={{ color: CYAN }}>
                <CheckIcon size={12} />
              </span>
              <span>{b}</span>
            </li>
          ))}
        </ul>
        {extra ? <div className="pt-1">{extra}</div> : null}
      </div>
      <aside className="cc-v8-pass__stub flex flex-col gap-3">
        <p
          className="font-mono text-[0.58rem] tracking-[0.22em] uppercase"
          style={{ color: CYAN }}
        >
          STUB
        </p>
        {stub.map((s) => (
          <StubRow key={s.label} label={s.label} value={s.value} />
        ))}
        <div className="mt-auto pt-3">
          <Barcode seed={`${code}-${flight}`} />
          <p className="text-cc-nav-label mt-2 font-mono text-[0.55rem] tracking-[0.18em] uppercase">
            {code}
          </p>
        </div>
      </aside>
    </article>
  );
}

/* -------------------------------------------------------------------------- */
/*  Departures board (mono table)                                             */
/* -------------------------------------------------------------------------- */

interface FlightRow {
  readonly gate: string;
  readonly flight: string;
  readonly destination: string;
  readonly etd: string;
  readonly status: "BOARDING" | "ON TIME" | "READY";
}

const FLIGHTS: readonly FlightRow[] = [
  {
    gate: "OAUTH-2",
    flight: "NTRO 101",
    destination: "Signed-in workspace",
    etd: "00:00",
    status: "BOARDING",
  },
  {
    gate: "WKSPC",
    flight: "NTRO 202",
    destination: "Shared APIs, shared people",
    etd: "00:01",
    status: "ON TIME",
  },
  {
    gate: "SYNC",
    flight: "NTRO 303",
    destination: "Every device, same documents",
    etd: "00:02",
    status: "ON TIME",
  },
  {
    gate: "PWA",
    flight: "NTRO 404",
    destination: "macOS, Windows, Linux",
    etd: "00:03",
    status: "READY",
  },
  {
    gate: "THEME",
    flight: "NTRO 505",
    destination: "Dark, light, follow system",
    etd: "00:04",
    status: "ON TIME",
  },
  {
    gate: "UPLD",
    flight: "NTRO 606",
    destination: "Multipart, exactly to spec",
    etd: "00:05",
    status: "ON TIME",
  },
];

function DeparturesBoard() {
  const reduceMotion = useReducedMotion();
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <div className="border-cc-card-border flex items-center justify-between border-b px-5 py-3">
        <div className="flex items-center gap-3">
          <span className="cc-v8-now-boarding-dot" aria-hidden />
          <span
            className="font-mono text-[0.66rem] tracking-[0.22em] uppercase"
            style={{ color: CYAN }}
          >
            Departures
          </span>
        </div>
        <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-tight">
          6 flights · gate NITRO
        </span>
      </div>

      <div className="border-cc-card-border grid grid-cols-[60px_80px_1fr_60px_90px] gap-3 border-b px-5 py-2 font-mono text-[0.58rem] tracking-[0.18em] uppercase">
        <span className="text-cc-nav-label">Gate</span>
        <span className="text-cc-nav-label">Flight</span>
        <span className="text-cc-nav-label">Destination</span>
        <span className="text-cc-nav-label">ETD</span>
        <span className="text-cc-nav-label text-right">Status</span>
      </div>

      <ul className="divide-cc-card-border divide-y">
        {FLIGHTS.map((f, i) => {
          const animProps = reduceMotion
            ? {}
            : {
                initial: { opacity: 0, y: 8 },
                whileInView: { opacity: 1, y: 0 },
                viewport: { once: true, margin: "-10% 0px" },
                transition: { duration: 0.35, delay: i * 0.06 },
              };
          return (
            <motion.li
              key={f.flight}
              {...animProps}
              className="grid grid-cols-[60px_80px_1fr_60px_90px] items-center gap-3 px-5 py-3"
            >
              <span
                className="font-mono text-[0.78rem]"
                style={{ color: CYAN }}
              >
                {f.gate}
              </span>
              <span className="text-cc-heading font-mono text-[0.82rem]">
                {f.flight}
              </span>
              <span className="text-cc-ink font-mono text-[0.82rem] tracking-tight">
                {f.destination}
              </span>
              <span className="text-cc-ink-dim font-mono text-[0.78rem] tabular-nums">
                {f.etd}
              </span>
              <span
                className={[
                  "text-right font-mono text-[0.62rem] tracking-[0.2em] uppercase",
                  f.status === "BOARDING"
                    ? "text-cc-accent"
                    : f.status === "READY"
                      ? "text-cc-success"
                      : "text-cc-ink",
                ].join(" ")}
                style={f.status === "BOARDING" ? { color: CYAN } : undefined}
              >
                {f.status}
              </span>
            </motion.li>
          );
        })}
      </ul>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Destinations (city codes)                                                 */
/* -------------------------------------------------------------------------- */

interface DestinationProps {
  readonly code: string;
  readonly name: string;
  readonly detail: string;
}

const DESTINATIONS: readonly DestinationProps[] = [
  { code: "MAC", name: "macOS", detail: "install as a PWA" },
  { code: "WIN", name: "Windows", detail: "install as a PWA" },
  { code: "LNX", name: "Linux", detail: "install as a PWA" },
  { code: "WEB", name: "Browser", detail: "any modern browser" },
];

function DestinationsServed() {
  return (
    <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      {DESTINATIONS.map((d) => (
        <div
          key={d.code}
          className="border-cc-card-border bg-cc-card-bg flex items-center gap-4 rounded-xl border p-4 backdrop-blur-sm"
        >
          <div
            className="flex h-14 w-14 shrink-0 flex-col items-center justify-center rounded-md border"
            style={{
              borderColor: "rgba(22, 185, 228, 0.45)",
              background: "rgba(22, 185, 228, 0.06)",
            }}
          >
            <span
              className="font-heading text-[1.1rem] font-semibold tracking-[0.08em]"
              style={{ color: CYAN }}
            >
              {d.code}
            </span>
          </div>
          <div>
            <p className="text-cc-heading font-mono text-[0.82rem]">{d.name}</p>
            <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-tight">
              {d.detail}
            </p>
          </div>
        </div>
      ))}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero pass                                                                 */
/* -------------------------------------------------------------------------- */

function HeroPass() {
  return (
    <div className="relative">
      <div
        className="pointer-events-none absolute -inset-8 -z-10 rounded-[28px] opacity-60 blur-2xl"
        style={{
          background:
            "radial-gradient(60% 60% at 50% 30%, rgba(22,185,228,0.22), transparent 70%)",
        }}
        aria-hidden
      />
      <article
        className="cc-v8-pass shadow-2xl shadow-black/40"
        style={{ borderRadius: "18px" }}
      >
        <div className="cc-v8-pass__body relative">
          <div className="cc-v8-hero-texture" aria-hidden />
          <div className="relative flex flex-col gap-7 sm:gap-9">
            {/* Pass header strip */}
            <div className="border-cc-card-border flex items-center justify-between gap-3 border-b border-dashed pb-3">
              <div className="flex items-center gap-3">
                <span
                  className="font-mono text-[0.66rem] tracking-[0.22em] uppercase"
                  style={{ color: CYAN }}
                >
                  CC AIRLINES
                </span>
                <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-tight">
                  · gate NITRO
                </span>
              </div>
              <div className="flex items-center gap-2">
                <span className="cc-v8-now-boarding-dot" aria-hidden />
                <span
                  className="font-mono text-[0.62rem] tracking-[0.2em] uppercase"
                  style={{ color: CYAN }}
                >
                  Now Boarding
                </span>
              </div>
            </div>

            {/* Headline with single 2px spectrum strip */}
            <div>
              <Eyebrow>Nitro · GraphQL IDE</Eyebrow>
              <h1 className="font-heading text-hero text-cc-heading mt-4 font-semibold tracking-tight">
                Boarding Pass
              </h1>
              <div
                className="mt-3 h-[2px] w-40 rounded-full"
                style={{
                  background: `linear-gradient(90deg, ${SPECTRUM_FROM}, ${SPECTRUM_MID} 55%, ${SPECTRUM_TO})`,
                }}
                aria-hidden
              />
              <p className="text-cc-heading font-heading text-h4 mt-5 font-semibold tracking-tight">
                The IDE your team actually opens.
              </p>
            </div>

            {/* Passenger + route */}
            <div className="grid gap-5 sm:grid-cols-[1.2fr_1fr]">
              <div>
                <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.2em] uppercase">
                  Passenger
                </p>
                <p className="text-cc-heading mt-1 font-mono text-[1.05rem] tracking-tight">
                  YOUR TEAM
                </p>
                <p className="text-cc-ink-dim mt-1 font-mono text-[0.66rem] tracking-tight">
                  class · workspace · shared
                </p>
              </div>
              <RouteRow from="BROWSER" to="ANY DEVICE" />
            </div>

            <p className="text-cc-ink-dim text-[1.02rem] leading-relaxed">
              Nitro is a focused, fast GraphQL workspace. Sign in with OAuth,
              organize APIs in shared workspaces, sync your documents across
              every device, and install it as a PWA on macOS, Windows, or Linux.
              Pick your gate, the IDE lands the same on every platform.
            </p>

            <div className="flex flex-wrap items-center gap-3">
              <SolidButton href="https://nitro.chillicream.com">
                Launch Nitro
              </SolidButton>
              <OutlineButton href="/docs/nitro">Read the Docs</OutlineButton>
            </div>

            <ul className="text-cc-ink-dim flex flex-wrap gap-x-6 gap-y-2 text-[0.88rem]">
              {[
                "Browser plus PWA on macOS, Windows, Linux",
                "OAuth 2, bearer, basic auth",
                "Workspaces sync across devices",
              ].map((item) => (
                <li key={item} className="flex items-center gap-2">
                  <span style={{ color: CYAN }}>
                    <CheckIcon size={13} />
                  </span>
                  {item}
                </li>
              ))}
            </ul>
          </div>
        </div>

        <aside className="cc-v8-pass__stub flex flex-col gap-4">
          <p
            className="font-mono text-[0.58rem] tracking-[0.22em] uppercase"
            style={{ color: CYAN }}
          >
            STUB · KEEP
          </p>
          <StubRow label="Seat" value="NTRO-001" />
          <StubRow label="Gate" value="OAUTH-2" />
          <StubRow label="Class" value="WORKSPACE" />
          <StubRow label="Flight" value="NTRO 101" />
          <StubRow label="Sync" value="ALL DEVICES" />
          <div className="mt-auto pt-3">
            <Barcode seed="hero-NTRO-001" />
            <p className="text-cc-nav-label mt-2 font-mono text-[0.55rem] tracking-[0.18em] uppercase">
              NTRO-001 · BOARD AT GATE
            </p>
          </div>
        </aside>
      </article>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  QR-style square for the CTA pass                                          */
/* -------------------------------------------------------------------------- */

function QrSquare() {
  // Deterministic 11x11 pseudo-QR pattern.
  const size = 11;
  const cells: boolean[] = [];
  let h = 0xdeadbeef;
  for (let i = 0; i < size * size; i++) {
    h = (h * 1664525 + 1013904223) | 0;
    cells.push(((h >>> 16) & 1) === 1);
  }
  return (
    <div
      className="grid h-28 w-28 shrink-0 rounded-md border p-1.5"
      style={{
        gridTemplateColumns: `repeat(${size}, 1fr)`,
        gridTemplateRows: `repeat(${size}, 1fr)`,
        gap: "1px",
        borderColor: "rgba(22, 185, 228, 0.45)",
        background: "rgba(22, 185, 228, 0.04)",
      }}
      aria-hidden
    >
      {cells.map((on, i) => {
        // Mark three finder squares (top-left, top-right, bottom-left).
        const row = Math.floor(i / size);
        const col = i % size;
        const isFinder =
          (row < 3 && col < 3) ||
          (row < 3 && col > size - 4) ||
          (row > size - 4 && col < 3);
        const cellOn = isFinder
          ? row === 0 ||
            row === 2 ||
            col === 0 ||
            col === 2 ||
            (row === 1 && col === 1) ||
            row === size - 1 ||
            col === size - 1
            ? true
            : (row + col) % 2 === 0
          : on;
        const style: CSSProperties = {
          background: cellOn ? CYAN : "transparent",
          opacity: cellOn ? 0.85 : 1,
        };
        return <span key={i} style={style} />;
      })}
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export function ClientPage() {
  return (
    <>
      <style>{PASS_STYLE}</style>
      <div className="flex flex-col gap-24 py-6 sm:gap-28">
        {/* ------------------------------ HERO ----------------------------- */}
        <section>
          <HeroPass />
        </section>

        {/* ------------------------ DEPARTURES BOARD ----------------------- */}
        <section>
          <div className="max-w-2xl">
            <Eyebrow>Departures · live schedule</Eyebrow>
            <h2 className="font-heading text-h2 text-cc-heading mt-4 font-semibold tracking-tight">
              Six flights, one gate.
            </h2>
            <p className="text-cc-ink-dim mt-5 text-[1.05rem] leading-relaxed">
              The Nitro capabilities on the board today. Authentication is
              boarding, sync rolls next, and a PWA install is already at the
              gate. Pick the one your team needs and read its boarding pass
              below.
            </p>
          </div>
          <div className="mt-10">
            <DeparturesBoard />
          </div>
        </section>

        {/* ----------------------- BOARDING PASS GRID ---------------------- */}
        <section>
          <div className="max-w-2xl">
            <Eyebrow>Boarding passes</Eyebrow>
            <h2 className="font-heading text-h3 text-cc-heading mt-3 font-semibold tracking-tight">
              Every capability, on a single pass.
            </h2>
            <p className="text-cc-ink-dim mt-4 text-[1rem] leading-relaxed">
              Each pass carries its own route, status, and stub. Tear the stub,
              keep the seat code, and board.
            </p>
          </div>

          <div className="mt-10 grid gap-6 lg:grid-cols-2">
            <BoardingPass
              code="01 · AUTH"
              flight="NTRO 101"
              status="BOARDING"
              title="Sign in the way your API expects."
              description="Nitro speaks the auth flows your services already use. OAuth 2 handles human sign-in, bearer tokens cover service accounts, and basic auth keeps legacy endpoints reachable, all configured per connection."
              bullets={[
                "OAuth 2 for browser-based sign in",
                "Bearer headers and basic auth for legacy endpoints",
                "Per-connection auth, scoped to a single workspace",
              ]}
              from="USER"
              to="API"
              stub={[
                { label: "Gate", value: "OAUTH-2" },
                { label: "Seat", value: "NTRO 12A" },
                { label: "Class", value: "PER-CONN" },
                { label: "Scope", value: "WORKSPACE" },
              ]}
            />
            <BoardingPass
              code="02 · WORKSPACES"
              flight="NTRO 202"
              title="One workspace per team, not per laptop."
              description="Group your APIs into workspaces and invite the people who own them. Operations, environments, and connection settings live with the workspace, so a new teammate is one invite away from running production queries the same way you do."
              bullets={[
                "Organize APIs under a shared organization",
                "Per-workspace members, roles, and connection settings",
                "Switching workspaces is one click, no re-config",
              ]}
              from="ORG"
              to="WORKSPACE"
              stub={[
                { label: "Terminal", value: "CHILLI/CHK" },
                { label: "Seat", value: "NTRO 14B" },
                { label: "APIs", value: "4 SHIPMTS" },
                { label: "Role", value: "EDITOR" },
              ]}
            />
            <BoardingPass
              code="03 · DOC SYNC"
              flight="NTRO 303"
              title="Your documents follow you, every device."
              description="Open Nitro on your laptop, your desktop, or the browser on a borrowed machine, your queries are already there. Documents stay in sync across your devices and across the teams you share a workspace with."
              bullets={[
                "Documents synced across all your signed-in devices",
                "Shared with the teams in your workspace",
                "Pick up where you left off on any machine",
              ]}
              from="LOCAL"
              to="SYNCED"
              stub={[
                { label: "Luggage", value: "TAGGED" },
                { label: "Seat", value: "NTRO 16C" },
                { label: "Bags", value: "18 DOCS" },
                { label: "Status", value: "UP TO DATE" },
              ]}
            />
            <BoardingPass
              code="04 · PWA INSTALL"
              flight="NTRO 404"
              status="READY"
              title="A real app, without a real installer."
              description="Nitro runs as a Progressive Web App, so any modern browser can promote it to a standalone window with its own icon and offline cache. No installer, no admin privileges, no IT ticket. The same Nitro on the web becomes the Nitro on your desktop."
              bullets={[
                "Install from the browser, no admin rights required",
                "Standalone window, dock icon, offline cache",
                "Updates roll out the moment they ship",
              ]}
              from="BROWSER"
              to="PWA"
              stub={[
                { label: "Carry-on", value: "ONLY" },
                { label: "Seat", value: "NTRO 18D" },
                { label: "Checked", value: "NONE" },
                { label: "Cabin", value: "STANDALONE" },
              ]}
            />
            <BoardingPass
              code="05 · THEMES"
              flight="NTRO 505"
              title="Light, dark, or whatever your OS decides."
              description="Pick a theme that does not fight your environment. Dark for the pairing session, light for the projector, or system to track your OS automatically. Every surface, from the editor to the response pane, is tuned for both modes."
              bullets={[
                "Hand-tuned dark and light themes",
                "Follow system to switch with your OS",
                "All surfaces, including charts and diagnostics",
              ]}
              from="DAY"
              to="NIGHT"
              fromLabel="LIGHT"
              toLabel="DARK"
              stub={[
                { label: "Cabin", value: "AUTO" },
                { label: "Seat", value: "NTRO 20E" },
                { label: "Lights", value: "FOLLOW OS" },
                { label: "Surfaces", value: "ALL" },
              ]}
            />
            <BoardingPass
              code="06 · MULTIPART"
              flight="NTRO 606"
              title="Multipart uploads, exactly to spec."
              description="Send files alongside your variables without leaving the editor. Nitro builds the multipart request per the GraphQL multipart spec, so the same payload your Hot Chocolate server accepts in production is the one the IDE sends from your machine."
              bullets={[
                "Implements the GraphQL multipart request spec",
                "Drag-and-drop into variables, previewed inline",
                "Works with any spec-compliant GraphQL server",
              ]}
              from="VARIABLES"
              to="/graphql"
              stub={[
                { label: "Cargo", value: "3 PARTS" },
                { label: "Seat", value: "NTRO 22F" },
                { label: "Flight", value: "MULTIPART" },
                { label: "Manifest", value: "TO SPEC" },
              ]}
              extra={
                <div className="border-cc-card-border bg-cc-surface/40 rounded-md border p-3 font-mono text-[0.66rem]">
                  <div className="text-cc-nav-label tracking-[0.18em] uppercase">
                    Cargo manifest
                  </div>
                  <ul className="mt-2 flex flex-col gap-1">
                    <li className="grid grid-cols-[1fr_auto_auto] gap-3">
                      <span className="text-cc-heading">operations</span>
                      <span className="text-cc-ink-dim">application/json</span>
                      <span className="text-cc-nav-label tabular-nums">
                        412 B
                      </span>
                    </li>
                    <li className="grid grid-cols-[1fr_auto_auto] gap-3">
                      <span className="text-cc-heading">map</span>
                      <span className="text-cc-ink-dim">application/json</span>
                      <span className="text-cc-nav-label tabular-nums">
                        63 B
                      </span>
                    </li>
                    <li className="grid grid-cols-[1fr_auto_auto] gap-3">
                      <span className="text-cc-heading">0</span>
                      <span className="text-cc-ink-dim">image/png</span>
                      <span className="text-cc-nav-label tabular-nums">
                        184 KB
                      </span>
                    </li>
                  </ul>
                </div>
              }
            />
          </div>
        </section>

        {/* --------------------- DESTINATIONS SERVED ----------------------- */}
        <section>
          <div className="max-w-2xl">
            <Eyebrow>Destinations served</Eyebrow>
            <h2 className="font-heading text-h3 text-cc-heading mt-3 font-semibold tracking-tight">
              Same Nitro, every gate.
            </h2>
            <p className="text-cc-ink-dim mt-4 text-[1rem] leading-relaxed">
              Pick a destination, board the same plane. Workspaces, history, and
              connections travel with you.
            </p>
          </div>
          <div className="mt-10">
            <DestinationsServed />
          </div>
        </section>

        {/* ----------------------- HONESTY (FINE PRINT) -------------------- */}
        <section>
          <article
            className="cc-v8-pass"
            style={{ maxWidth: "880px", margin: "0 auto" }}
          >
            <div className="cc-v8-pass__body flex flex-col gap-4">
              <div className="flex items-center gap-3">
                <Eyebrow>Fine print</Eyebrow>
                <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-tight">
                  · half-pass
                </span>
              </div>
              <h2 className="font-heading text-h4 text-cc-heading font-semibold tracking-tight">
                Nitro is the IDE. Telemetry is a separate decision.
              </h2>
              <p className="text-cc-ink-dim text-[0.98rem] leading-relaxed">
                The IDE ships ready to fly: authoring, workspaces, document
                sync, cross-platform install, and a built-in GraphQL endpoint UI
                that runs wherever you point it. That is enough to make a team
                productive on day one.
              </p>
              <p className="text-cc-ink-dim text-[0.98rem] leading-relaxed">
                Operational telemetry, the dashboards and traces, requires
                configuring Nitro telemetry on the server side. It is opt-in,
                not magic. The IDE is useful with or without it.
              </p>
            </div>
            <aside className="cc-v8-pass__stub flex flex-col gap-3">
              <p
                className="font-mono text-[0.58rem] tracking-[0.22em] uppercase"
                style={{ color: CYAN }}
              >
                FP · KEEP
              </p>
              <StubRow label="Ships" value="IDE READY" />
              <StubRow label="Setup" value="TELEMETRY" />
              <StubRow label="Mode" value="OPT-IN" />
              <div className="mt-auto pt-3">
                <Barcode seed="fine-print-half" />
              </div>
            </aside>
          </article>
        </section>

        {/* ----------------------------- CTA ------------------------------- */}
        <section>
          <article className="cc-v8-pass">
            <div className="cc-v8-pass__body flex flex-col gap-6">
              <div className="flex flex-wrap items-center justify-between gap-4">
                <Eyebrow>Final call · gate NITRO</Eyebrow>
                <span className="cc-v8-stamp">
                  <span className="cc-v8-now-boarding-dot" aria-hidden />
                  Now Boarding
                </span>
              </div>
              <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
                Open Nitro. Pick a workspace. Run a query.
              </h2>
              <p className="text-cc-ink-dim max-w-xl text-[1.05rem] leading-relaxed">
                The fastest way to feel the difference is to launch the IDE
                against your own GraphQL endpoint. No install, no signup gate,
                and your workspace travels with you.
              </p>
              <div className="flex flex-wrap items-center gap-3">
                <SolidButton href="https://nitro.chillicream.com">
                  Launch Nitro
                </SolidButton>
                <OutlineButton href="/docs/nitro">Read the Docs</OutlineButton>
              </div>
            </div>
            <aside className="cc-v8-pass__stub flex items-start gap-4">
              <QrSquare />
              <div className="flex flex-1 flex-col gap-3">
                <p
                  className="font-mono text-[0.58rem] tracking-[0.22em] uppercase"
                  style={{ color: CYAN }}
                >
                  BOARDING
                </p>
                <StubRow label="Seat" value="NTRO 01A" />
                <StubRow label="Gate" value="NITRO" />
                <StubRow label="ETD" value="00:00" />
              </div>
            </aside>
          </article>
        </section>
      </div>
    </>
  );
}
