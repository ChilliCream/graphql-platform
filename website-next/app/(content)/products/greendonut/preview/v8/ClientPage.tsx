"use client";

import type { CSSProperties, ReactNode } from "react";
import { motion, useReducedMotion } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { GreenDonut } from "@/src/icons/GreenDonut";

/* ------------------------------------------------------------------ */
/* Dewey Decimal DataLoader                                            */
/* A reference library does what DataLoader does: take a stack of      */
/* slips with keys, walk to the stacks once, return the books in       */
/* order. The page is staged as a card catalog. Each section is a      */
/* wooden drawer face with a brass label plate (mono uppercase title), */
/* a Dewey classification number, and alphabetical tab dividers.       */
/* Content sits on ruled index cards with a punched hole and a teal    */
/* DUE BACK stamp. Mono is the catalog voice; font-heading anchors the */
/* page H1, the drawer plate H2s, and the closing CTA.                 */
/* ------------------------------------------------------------------ */

// Brand spectrum, used at most once on this page (the reference plaque seam).
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// Ruled index-card paper: hairline baselines every 24px.
const RULED_PAPER: CSSProperties = {
  backgroundImage:
    "repeating-linear-gradient(to bottom, transparent 0, transparent 23px, var(--color-cc-card-border) 23px, var(--color-cc-card-border) 24px)",
};

/* ------------------------------------------------------------------ */
/* Motion helpers (enter-view-once or time-driven, never scroll)       */
/* ------------------------------------------------------------------ */

function usePlateRise() {
  const reduce = useReducedMotion();
  if (reduce) {
    return {
      initial: { opacity: 1, y: 0 },
      whileInView: { opacity: 1, y: 0 },
      viewport: { once: true, amount: 0.5 },
    } as const;
  }
  return {
    initial: { opacity: 0, y: 10 },
    whileInView: { opacity: 1, y: 0 },
    viewport: { once: true, amount: 0.5 },
    transition: { duration: 0.6, ease: "easeOut" },
  } as const;
}

/* ------------------------------------------------------------------ */
/* Catalog primitives                                                  */
/* ------------------------------------------------------------------ */

interface BrassPlateProps {
  readonly title: string;
  readonly classification: string;
  readonly seam?: boolean;
}

// Brass label plate that crowns every drawer: mono uppercase title on the
// left, Dewey classification number on the right, two drawer-pull notches.
function BrassPlate({ title, classification, seam }: BrassPlateProps) {
  const rise = usePlateRise();
  return (
    <motion.div {...rise} className="relative">
      {seam ? (
        <div
          aria-hidden
          className="absolute inset-x-0 top-0 h-px"
          style={{ background: SPECTRUM }}
        />
      ) : null}
      <div className="border-cc-card-border bg-cc-surface flex items-center justify-between gap-4 rounded-t-xl border border-b-0 px-5 py-3">
        <div className="flex items-center gap-3">
          <span
            aria-hidden
            className="flex items-center gap-1.5 opacity-60"
            style={{ lineHeight: 0 }}
          >
            <span className="border-cc-card-border h-1.5 w-4 rounded-full border" />
            <span className="border-cc-card-border h-1.5 w-4 rounded-full border" />
          </span>
          <h2 className="text-cc-heading font-heading text-h6 tracking-[0.12em] uppercase">
            {title}
          </h2>
        </div>
        <span className="text-cc-accent font-mono text-[12px] tracking-[0.14em] tabular-nums">
          {classification}
        </span>
      </div>
    </motion.div>
  );
}

interface DrawerProps {
  readonly title: string;
  readonly classification: string;
  readonly tabs?: string;
  readonly seam?: boolean;
  readonly children: ReactNode;
}

// Drawer shell: brass plate on top, an inside well (ruled) holding cards.
function Drawer({ title, classification, tabs, seam, children }: DrawerProps) {
  return (
    <section className="mt-12">
      <BrassPlate title={title} classification={classification} seam={seam} />
      <div
        className="border-cc-card-border bg-cc-card-bg rounded-b-xl border px-5 pt-5 pb-6 sm:px-6"
        style={RULED_PAPER}
      >
        {tabs ? <TabDivider letters={tabs} /> : null}
        {children}
      </div>
    </section>
  );
}

interface TabDividerProps {
  readonly letters: string;
}

// Alphabetical tab divider that indexes the drawer's contents.
function TabDivider({ letters }: TabDividerProps) {
  return (
    <div className="mb-5 flex items-center gap-2">
      <span className="border-cc-card-border bg-cc-surface text-cc-nav-label inline-flex items-center rounded-t-md border border-b-0 px-3 py-1 font-mono text-[10.5px] tracking-[0.22em] uppercase">
        {letters}
      </span>
      <span className="border-cc-card-border flex-1 border-b" aria-hidden />
    </div>
  );
}

interface IndexCardProps {
  readonly className?: string;
  readonly children: ReactNode;
}

// Index card: cc-card-bg, a 2px ruled left margin, a punched hole top-left,
// and 1ch indented body text.
function IndexCard({ className, children }: IndexCardProps) {
  return (
    <div
      className={`border-cc-card-border bg-cc-bg/60 relative rounded-lg border border-l-2 p-5 pl-6 ${className ?? ""}`}
    >
      <span
        aria-hidden
        className="border-cc-card-border bg-cc-surface absolute top-3 left-2 h-2.5 w-2.5 rounded-full border"
      />
      <div className="pl-[1ch]">{children}</div>
    </div>
  );
}

interface StampProps {
  readonly label: string;
  readonly tone?: "teal" | "red";
}

// A rubber-stamp style chip. Teal DUE BACK / BATCHED, red RE-FETCH.
function Stamp({ label, tone = "teal" }: StampProps) {
  const cls =
    tone === "red"
      ? "border-cc-danger/60 text-cc-danger"
      : "border-cc-accent/60 text-cc-accent";
  return (
    <span
      className={`inline-flex items-center rounded-[3px] border-2 px-1.5 py-0.5 font-mono text-[9.5px] font-semibold tracking-[0.18em] uppercase ${cls}`}
      style={{ transform: "rotate(-3deg)" }}
    >
      {label}
    </span>
  );
}

/* ------------------------------------------------------------------ */
/* GitHub-dark code, scoped tokens so the page stays on cc-*           */
/* ------------------------------------------------------------------ */

const C = {
  kw: { color: "#ff7b72" },
  type: { color: "#ffa657" },
  comment: { color: "#8b949e", fontStyle: "italic" as const },
  attr: { color: "#d2a8ff" },
  fn: { color: "#d2a8ff" },
  param: { color: "#79c0ff" },
  plain: { color: "#c9d1d9" },
};

interface SlipLineProps {
  readonly n: number;
  readonly children: ReactNode;
}

// A typewritten requisition slip row, line numbers as slip row numbers.
function SlipLine({ n, children }: SlipLineProps) {
  return (
    <div className="flex gap-4 px-5">
      <span
        className="w-6 shrink-0 text-right font-mono text-[11px] text-[#484f58] tabular-nums select-none"
        aria-hidden
      >
        {n}
      </span>
      <span className="font-mono text-[12.5px] leading-6 whitespace-pre">
        {children}
      </span>
    </div>
  );
}

// Perforated top edge for requisition slips.
function Perforation() {
  return (
    <div
      aria-hidden
      className="h-2 w-full"
      style={{
        backgroundImage:
          "radial-gradient(circle at 6px 50%, var(--color-cc-bg) 2px, transparent 2.5px)",
        backgroundSize: "12px 8px",
        backgroundRepeat: "repeat-x",
      }}
    />
  );
}

/* ------------------------------------------------------------------ */
/* Hero before/after: the cart glides one cart of six keys (time loop) */
/* ------------------------------------------------------------------ */

const HERO_KEYS = ["7", "12", "3", "9", "21", "4"];

function CartGlide() {
  const reduce = useReducedMotion();
  const cart = (
    <svg
      viewBox="0 0 60 40"
      className="h-9 w-14"
      role="img"
      aria-label="A library cart carrying six requested keys"
    >
      <rect
        x="6"
        y="6"
        width="40"
        height="20"
        rx="2"
        fill="none"
        stroke="#5eead4"
        strokeOpacity="0.8"
        strokeWidth="1.5"
      />
      <line
        x1="6"
        y1="16"
        x2="46"
        y2="16"
        stroke="#5eead4"
        strokeOpacity="0.4"
        strokeWidth="1"
      />
      <circle cx="14" cy="32" r="3" fill="#5eead4" fillOpacity="0.85" />
      <circle cx="38" cy="32" r="3" fill="#5eead4" fillOpacity="0.85" />
      <line
        x1="11"
        y1="26"
        x2="41"
        y2="26"
        stroke="#5eead4"
        strokeOpacity="0.7"
        strokeWidth="1.5"
      />
    </svg>
  );

  return (
    <div className="relative h-10 overflow-hidden">
      {reduce ? (
        <div className="absolute top-0 left-0">{cart}</div>
      ) : (
        <motion.div
          className="absolute top-0 left-0"
          initial={{ x: "0%" }}
          animate={{ x: ["0%", "0%", "320%", "320%"] }}
          transition={{
            duration: 7,
            times: [0, 0.57, 0.85, 1],
            ease: "easeInOut",
            repeat: Infinity,
          }}
        >
          {cart}
        </motion.div>
      )}
    </div>
  );
}

function HeroCatalogRoom() {
  return (
    <section className="mt-4">
      <BrassPlate
        title="Green Donut DataLoader for .NET"
        classification="005.741"
      />
      <div
        className="border-cc-card-border bg-cc-card-bg rounded-b-xl border px-5 pt-6 pb-8 sm:px-6"
        style={RULED_PAPER}
      >
        <div className="flex items-center gap-3">
          <GreenDonut className="h-9 w-9" />
          <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.22em] uppercase">
            Card Catalog / Reference Desk
          </span>
        </div>

        <h1 className="font-heading text-cc-heading text-h1 mt-6 max-w-3xl">
          Kill N+1 in your .NET resolvers.
        </h1>

        <IndexCard className="mt-6 max-w-2xl">
          <p className="lead text-cc-ink">
            Green Donut is the DataLoader for .NET. It collapses many key
            requests from the same tick into one batched fetch, caches each
            result inside the request, and deduplicates repeat keys. Source
            generated, AOT friendly, MIT licensed.
          </p>
        </IndexCard>

        <div className="mt-7 flex flex-wrap items-center gap-3">
          <SolidButton href="/docs/greendonut">Get Started</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
            View on GitHub
          </OutlineButton>
        </div>

        {/* Two index cards side by side: BEFORE 21 slips, AFTER one cart. */}
        <div className="mt-8 grid grid-cols-1 gap-5 md:grid-cols-2">
          {/* BEFORE: 21 hand-typed catalog slips with red RE-FETCH stamps. */}
          <IndexCard>
            <div className="flex items-center justify-between">
              <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.2em] uppercase">
                Before
              </span>
              <Stamp label="Re-fetch" tone="red" />
            </div>
            <ul className="mt-4 space-y-2">
              {HERO_KEYS.map((k) => (
                <li
                  key={k}
                  className="flex items-center justify-between font-mono text-[12px]"
                >
                  <span className="text-cc-ink">
                    SLIP &middot; GetUser(id: {k})
                  </span>
                  <span className="text-cc-danger/90 font-mono text-[10px] tracking-[0.16em] uppercase">
                    trip
                  </span>
                </li>
              ))}
              <li className="text-cc-ink-dim font-mono text-[12px]">
                . . . 15 more slips typed by hand
              </li>
            </ul>
            <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
              <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.16em] uppercase">
                trips to the stacks
              </span>
              <span className="text-cc-danger font-mono text-[13px] font-semibold tabular-nums">
                21
              </span>
            </div>
          </IndexCard>

          {/* AFTER: one cart pulling six keys with the teal BATCHED stamp. */}
          <IndexCard>
            <div className="flex items-center justify-between">
              <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.2em] uppercase">
                After
              </span>
              <Stamp label="Batched" tone="teal" />
            </div>
            <div className="mt-4 font-mono text-[12px]">
              <span className="text-cc-ink">LoadAsync(</span>
              <span className="text-cc-accent">[{HERO_KEYS.join(", ")}]</span>
              <span className="text-cc-ink">)</span>
            </div>
            <div className="mt-4">
              <CartGlide />
            </div>
            <div className="mt-2 flex flex-wrap gap-1.5">
              {HERO_KEYS.map((k) => (
                <span
                  key={k}
                  className="border-cc-accent/40 text-cc-accent inline-flex h-6 items-center rounded-full border px-2 font-mono text-[11px] tabular-nums"
                >
                  {k}
                </span>
              ))}
            </div>
            <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
              <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.16em] uppercase">
                trips to the stacks
              </span>
              <span className="text-cc-accent font-mono text-[13px] font-semibold tabular-nums">
                1
              </span>
            </div>
          </IndexCard>
        </div>

        <ul className="text-cc-ink-dim mt-7 grid grid-cols-1 gap-x-6 gap-y-2 text-sm sm:grid-cols-2">
          {[
            "Batching, caching, dedup",
            "[DataLoader] attribute",
            "Per request scoped cache",
            "Auto discovered by Hot Chocolate",
          ].map((f) => (
            <li key={f} className="flex items-center gap-2">
              <span className="text-cc-accent">
                <CheckIcon />
              </span>
              {f}
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

/* ------------------------------------------------------------------ */
/* S3 pillar card (hover-raised drawer card)                           */
/* ------------------------------------------------------------------ */

interface PillarCardProps {
  readonly index: string;
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

function PillarCard({ index, title, body, bullets }: PillarCardProps) {
  return (
    <article className="border-cc-card-border bg-cc-bg/60 hover:border-cc-card-border-hover group relative flex h-full flex-col rounded-lg border border-l-2 p-5 pl-6 transition-colors">
      <span
        aria-hidden
        className="border-cc-card-border bg-cc-surface absolute top-3 left-2 h-2.5 w-2.5 rounded-full border shadow-none transition-[box-shadow,transform] group-hover:translate-y-px group-hover:shadow-[1px_1px_0_var(--color-cc-card-border-hover)]"
      />
      <div className="pl-[1ch]">
        <span className="text-cc-accent font-mono text-[11px] tracking-[0.14em] tabular-nums">
          {index}
        </span>
        <h3 className="text-cc-heading mt-3 font-mono text-[13px] font-semibold tracking-[0.08em] uppercase">
          {title}
        </h3>
        <p className="text-cc-ink mt-2 text-sm leading-relaxed">{body}</p>
        <ul className="mt-4 space-y-2">
          {bullets.map((b) => (
            <li
              key={b}
              className="text-cc-ink-dim flex items-start gap-2 text-sm"
            >
              <span className="text-cc-accent mt-[3px]">
                <CheckIcon />
              </span>
              <span>{b}</span>
            </li>
          ))}
        </ul>
        <div className="mt-4">
          <Stamp label="Due back" tone="teal" />
        </div>
      </div>
    </article>
  );
}

/* ------------------------------------------------------------------ */
/* S5 shape divider tabs (KEYED, GROUPED, PAGED)                       */
/* ------------------------------------------------------------------ */

interface ShapeTabProps {
  readonly letter: string;
  readonly name: string;
  readonly shape: string;
  readonly use: string;
  readonly active?: boolean;
}

function ShapeTab({ letter, name, shape, use, active }: ShapeTabProps) {
  return (
    <div className={active ? "-mt-2 self-start" : "self-start"}>
      <span className="border-cc-card-border bg-cc-surface text-cc-accent inline-flex h-7 w-7 items-center justify-center rounded-t-md border border-b-0 font-mono text-[12px] font-semibold">
        {letter}
      </span>
      <div
        className={`border-cc-card-border bg-cc-bg/60 flex h-full flex-col rounded-tr-lg rounded-b-lg border p-5 ${active ? "border-cc-card-border-hover" : ""}`}
      >
        <div className="text-cc-heading font-mono text-[12.5px] font-semibold tracking-[0.08em] uppercase">
          {name}
        </div>
        <code className="text-cc-ink mt-3 block font-mono text-[12px] break-words">
          {shape}
        </code>
        <p className="text-cc-ink-dim mt-3 text-sm">{use}</p>
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Page                                                                */
/* ------------------------------------------------------------------ */

export function ClientPage() {
  return (
    <>
      {/* Page-wide paper-grain haze, fixed top-left, cc-accent at 4%. */}
      <div
        aria-hidden
        className="pointer-events-none fixed inset-0 -z-10"
        style={{
          background:
            "radial-gradient(600px circle at 0% 0%, rgba(94, 234, 212, 0.04), transparent 70%)",
        }}
      />

      <main className="mx-auto w-full max-w-6xl px-6 pt-16 pb-24 sm:px-8">
        {/* S1 CATALOG ROOM hero */}
        <HeroCatalogRoom />

        {/* S2 THE N+1 PROBLEM */}
        <Drawer title="Problem" classification="519.2" tabs="A to D">
          <p className="text-cc-ink mb-5 max-w-3xl pl-[1ch] text-sm leading-relaxed">
            Resolvers run per field, per node. A list of orders loads in one
            query. Then each order asks for its customer, its line items, its
            shipping address. The database does the same point lookup, again and
            again, for a single request. Latency climbs linearly with the page
            size and the connection pool starts to choke.
          </p>
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <IndexCard>
              <div className="flex items-center justify-between">
                <span className="text-cc-danger font-mono text-[11px] tracking-[0.18em] uppercase">
                  Without DataLoader
                </span>
                <Stamp label="Re-fetch" tone="red" />
              </div>
              <div className="mt-3 space-y-1.5 font-mono text-[12px]">
                <div className="text-cc-ink">SELECT * FROM orders LIMIT 20</div>
                <div className="text-cc-ink-dim">
                  SELECT * FROM customers WHERE id = 1
                </div>
                <div className="text-cc-ink-dim">
                  SELECT * FROM customers WHERE id = 2
                </div>
                <div className="text-cc-ink-dim">
                  SELECT * FROM customers WHERE id = 3
                </div>
                <div className="text-cc-ink-dim">. . . 17 more</div>
              </div>
              <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
                <span className="text-cc-ink-dim font-mono text-[11px]">
                  queries
                </span>
                <span className="text-cc-danger font-mono text-[13px] font-semibold tabular-nums">
                  21
                </span>
              </div>
            </IndexCard>

            <IndexCard>
              <div className="flex items-center justify-between">
                <span className="text-cc-accent font-mono text-[11px] tracking-[0.18em] uppercase">
                  With Green Donut
                </span>
                <Stamp label="Batched" tone="teal" />
              </div>
              <div className="mt-3 space-y-1.5 font-mono text-[12px]">
                <div className="text-cc-ink">SELECT * FROM orders LIMIT 20</div>
                <div className="text-cc-ink">SELECT * FROM customers</div>
                <div className="text-cc-ink">
                  {"    "}WHERE id IN (1, 2, ..., 20)
                </div>
              </div>
              <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
                <span className="text-cc-ink-dim font-mono text-[11px]">
                  queries
                </span>
                <span className="text-cc-accent font-mono text-[13px] font-semibold tabular-nums">
                  2
                </span>
              </div>
            </IndexCard>
          </div>

          {/* Ledger footer row: red 21 vs teal 2. */}
          <div className="border-cc-card-border mt-5 flex items-center justify-between rounded-lg border px-5 py-3 font-mono text-[12px]">
            <span className="text-cc-ink-dim tracking-[0.16em] uppercase">
              Ledger
            </span>
            <span className="flex items-center gap-4">
              <span className="text-cc-danger tabular-nums">21 trips</span>
              <span className="text-cc-ink-dim">vs</span>
              <span className="text-cc-accent tabular-nums">2 trips</span>
            </span>
          </div>
        </Drawer>

        {/* S3 SIX PILLARS */}
        <Drawer title="Catalogue" classification="005.741" tabs="E to J">
          <p className="text-cc-ink mb-6 max-w-3xl pl-[1ch] text-sm leading-relaxed">
            Green Donut is the DataLoader implementation that powers Hot
            Chocolate, and it runs standalone in any .NET service. Each entry
            below maps to a concrete capability, not a marketing promise.
          </p>
          <div className="grid grid-cols-1 gap-5 md:grid-cols-2 lg:grid-cols-3">
            <PillarCard
              index="005.741.01"
              title="The DataLoader pattern"
              body="Resolvers ask for keys. Green Donut collects the keys that arrive on the same tick, sends one batched fetch, and hands each resolver its result."
              bullets={[
                "Coalesces sibling resolver calls",
                "Returns results in the original key order",
                "Works with any async data source",
              ]}
            />
            <PillarCard
              index="005.741.02"
              title="Batching, caching, dedup"
              body="Three behaviors, one loader. Batch many keys into one fetch. Cache each key for the rest of the request. Skip repeat keys entirely."
              bullets={[
                "Configurable max batch size",
                "Same key in one request returns the same task",
                "Negative caching is opt in",
              ]}
            />
            <PillarCard
              index="005.741.03"
              title="[DataLoader] attribute"
              body="Author a loader as a plain static method. The source generator writes the interface, the registration, and the typed accessor for you."
              bullets={[
                "No base class, no ceremony",
                "Generated wiring at build time",
                "AOT friendly, zero reflection",
              ]}
            />
            <PillarCard
              index="005.741.04"
              title="Per request, pluggable cache"
              body="The default cache scope is the request, so concurrent requests never see each other's data. Swap in a global or shared cache when you want it."
              bullets={[
                "Request scoped by default",
                "Pluggable cache abstraction",
                "Hand keyed entries in or out",
              ]}
            />
            <PillarCard
              index="005.741.05"
              title="Keyed, grouped, pagination"
              body="Pick the loader shape that matches the data. One result per key, many results per key, or a paged window per key. Same attribute, different signature."
              bullets={[
                "Batch loaders for single results",
                "Grouped loaders for one to many",
                "Pagination loaders for cursor windows",
              ]}
            />
            <PillarCard
              index="005.741.06"
              title="Hot Chocolate or standalone"
              body="Drop the loader in a Hot Chocolate server and it gets auto discovered. Use it in a Worker, an MVC controller, or a console app with the same API."
              bullets={[
                "Auto discovered by Hot Chocolate",
                "Works in any .NET service",
                "MIT licensed, no per request cost",
              ]}
            />
          </div>
        </Drawer>

        {/* S4 [DataLoader] ATTRIBUTE */}
        <Drawer title="Attribute" classification="005.13" tabs="K to P">
          <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
            <IndexCard>
              <h3 className="text-cc-heading font-mono text-[13px] font-semibold tracking-[0.08em] uppercase">
                Write the method, skip the plumbing
              </h3>
              <p className="text-cc-ink mt-3 text-sm leading-relaxed">
                Mark a static method with{" "}
                <span className="text-cc-heading bg-cc-surface rounded px-1 font-mono text-[12px]">
                  [DataLoader]
                </span>
                . Take an{" "}
                <span className="text-cc-heading bg-cc-surface rounded px-1 font-mono text-[12px]">
                  IReadOnlyList&lt;TKey&gt;
                </span>{" "}
                and return an{" "}
                <span className="text-cc-heading bg-cc-surface rounded px-1 font-mono text-[12px]">
                  IReadOnlyDictionary&lt;TKey, TValue&gt;
                </span>
                . The source generator emits the loader type, the interface, and
                the DI registration. Inject the generated interface and call{" "}
                <span className="text-cc-heading bg-cc-surface rounded px-1 font-mono text-[12px]">
                  LoadAsync
                </span>{" "}
                from your resolver.
              </p>
              <ul className="mt-5 space-y-3">
                {[
                  "Dependencies after the keys are resolved from DI per batch.",
                  "Returns are looked up by key. Missing keys yield null.",
                  "Cancellation flows through to the database driver, not just the loader.",
                ].map((t) => (
                  <li
                    key={t}
                    className="text-cc-ink-dim flex items-start gap-3 text-sm"
                  >
                    <span className="text-cc-accent mt-[3px]">
                      <CheckIcon />
                    </span>
                    <span>{t}</span>
                  </li>
                ))}
              </ul>
            </IndexCard>

            {/* Requisition slip: typewritten code on perforated paper. */}
            <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
              <Perforation />
              <div className="bg-cc-code-header border-cc-card-border flex items-center justify-between border-y px-4 py-2.5">
                <span className="font-mono text-[11px] text-[#8b949e]">
                  UserDataLoader.cs
                </span>
                <span className="font-mono text-[10px] tracking-[0.18em] text-[#8b949e] uppercase">
                  Requisition slip
                </span>
              </div>
              <div className="py-4">
                <SlipLine n={1}>
                  <span style={C.comment}>
                    {"// One method. Wiring is generated."}
                  </span>
                </SlipLine>
                <SlipLine n={2}>
                  <span style={C.kw}>public static class</span>{" "}
                  <span style={C.type}>UserDataLoader</span>
                </SlipLine>
                <SlipLine n={3}>
                  <span style={C.plain}>{"{"}</span>
                </SlipLine>
                <SlipLine n={4}>
                  {"    "}
                  <span style={C.attr}>[DataLoader]</span>
                </SlipLine>
                <SlipLine n={5}>
                  {"    "}
                  <span style={C.kw}>public static async</span>{" "}
                  <span style={C.type}>
                    {"Task<IReadOnlyDictionary<int, User>>"}
                  </span>{" "}
                  <span style={C.fn}>GetUsersAsync</span>
                  <span style={C.plain}>(</span>
                </SlipLine>
                <SlipLine n={6}>
                  {"        "}
                  <span style={C.type}>{"IReadOnlyList<int>"}</span>{" "}
                  <span style={C.param}>ids</span>
                  <span style={C.plain}>,</span>
                </SlipLine>
                <SlipLine n={7}>
                  {"        "}
                  <span style={C.type}>AppDbContext</span>{" "}
                  <span style={C.param}>db</span>
                  <span style={C.plain}>,</span>
                </SlipLine>
                <SlipLine n={8}>
                  {"        "}
                  <span style={C.type}>CancellationToken</span>{" "}
                  <span style={C.param}>ct</span>
                  <span style={C.plain}>{")"}</span>
                </SlipLine>
                <SlipLine n={9}>
                  {"        "}
                  <span style={C.plain}>{"=>"}</span>{" "}
                  <span style={C.kw}>await</span>{" "}
                  <span style={C.param}>db</span>
                  <span style={C.plain}>.Users</span>
                </SlipLine>
                <SlipLine n={10}>
                  {"            "}
                  <span style={C.plain}>.</span>
                  <span style={C.fn}>Where</span>
                  <span style={C.plain}>(u {"=>"} </span>
                  <span style={C.param}>ids</span>
                  <span style={C.plain}>.</span>
                  <span style={C.fn}>Contains</span>
                  <span style={C.plain}>(u.Id))</span>
                </SlipLine>
                <SlipLine n={11}>
                  {"            "}
                  <span style={C.plain}>.</span>
                  <span style={C.fn}>ToDictionaryAsync</span>
                  <span style={C.plain}>(u {"=>"} u.Id, </span>
                  <span style={C.param}>ct</span>
                  <span style={C.plain}>);</span>
                </SlipLine>
                <SlipLine n={12}>
                  <span style={C.plain}>{"}"}</span>
                </SlipLine>
              </div>
            </div>
          </div>
        </Drawer>

        {/* S5 SHAPE INDEX */}
        <Drawer title="Shape Index" classification="005.741.5" tabs="Q to T">
          <p className="text-cc-ink mb-6 max-w-3xl pl-[1ch] text-sm leading-relaxed">
            The signature picks the loader. Same attribute, same generated
            wiring, the right shape for the relationship you are loading.
          </p>
          <div className="grid grid-cols-1 items-start gap-5 sm:grid-cols-3">
            <ShapeTab
              letter="K"
              name="Keyed"
              shape="IReadOnlyDictionary<TKey, TValue>"
              use="One result per key. The default for foreign key lookups."
              active
            />
            <ShapeTab
              letter="G"
              name="Grouped"
              shape="ILookup<TKey, TValue>"
              use="Many results per key. One to many relationships, like an order to its line items."
            />
            <ShapeTab
              letter="P"
              name="Paged"
              shape="Page<TKey, TValue>"
              use="A cursor window per key. Connections on a parent type that need paging per node."
            />
          </div>
        </Drawer>

        {/* S6 HOT CHOCOLATE INTEGRATION */}
        <Drawer
          title="Hot Chocolate Integration"
          classification="005.741.6"
          tabs="U to Z"
        >
          <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
            {/* Resolver snippet on perforated paper, AUTO-DISCOVERED stamp. */}
            <div className="bg-cc-code-bg border-cc-card-border overflow-hidden rounded-lg border">
              <Perforation />
              <div className="bg-cc-code-header border-cc-card-border flex items-center justify-between border-y px-4 py-2.5">
                <span className="font-mono text-[11px] text-[#8b949e]">
                  OrderType.cs
                </span>
                <Stamp label="Auto-discovered" tone="teal" />
              </div>
              <div className="py-4">
                <SlipLine n={1}>
                  <span style={C.kw}>public class</span>{" "}
                  <span style={C.type}>OrderType</span>{" "}
                  <span style={C.plain}>:</span>{" "}
                  <span style={C.type}>{"ObjectType<Order>"}</span>
                </SlipLine>
                <SlipLine n={2}>
                  <span style={C.plain}>{"{"}</span>
                </SlipLine>
                <SlipLine n={3}>
                  {"    "}
                  <span style={C.kw}>public static async</span>{" "}
                  <span style={C.type}>{"Task<User?>"}</span>{" "}
                  <span style={C.fn}>GetCustomerAsync</span>
                  <span style={C.plain}>(</span>
                </SlipLine>
                <SlipLine n={4}>
                  {"        "}
                  <span style={C.attr}>[Parent]</span>{" "}
                  <span style={C.type}>Order</span>{" "}
                  <span style={C.param}>order</span>
                  <span style={C.plain}>,</span>
                </SlipLine>
                <SlipLine n={5}>
                  {"        "}
                  <span style={C.type}>IUserDataLoader</span>{" "}
                  <span style={C.param}>users</span>
                  <span style={C.plain}>,</span>
                </SlipLine>
                <SlipLine n={6}>
                  {"        "}
                  <span style={C.type}>CancellationToken</span>{" "}
                  <span style={C.param}>ct</span>
                  <span style={C.plain}>{")"}</span>
                </SlipLine>
                <SlipLine n={7}>
                  {"        "}
                  <span style={C.plain}>{"=>"}</span>{" "}
                  <span style={C.kw}>await</span>{" "}
                  <span style={C.param}>users</span>
                  <span style={C.plain}>.</span>
                  <span style={C.fn}>LoadAsync</span>
                  <span style={C.plain}>(</span>
                  <span style={C.param}>order</span>
                  <span style={C.plain}>.CustomerId, </span>
                  <span style={C.param}>ct</span>
                  <span style={C.plain}>);</span>
                </SlipLine>
                <SlipLine n={8}>
                  <span style={C.plain}>{"}"}</span>
                </SlipLine>
              </div>
            </div>

            <IndexCard>
              <h3 className="text-cc-heading font-mono text-[13px] font-semibold tracking-[0.08em] uppercase">
                Same loader, different host
              </h3>
              <p className="text-cc-ink mt-3 text-sm leading-relaxed">
                Green Donut is the engine Hot Chocolate uses to batch resolver
                work and eliminate N+1, and you do not need a GraphQL server to
                use it. Drop a loader in a background worker, a REST controller,
                or a CLI. Same{" "}
                <span className="text-cc-heading bg-cc-surface rounded px-1 font-mono text-[12px]">
                  LoadAsync
                </span>{" "}
                entry point, same batching, same per request scope.
              </p>
              <ul className="text-cc-ink-dim mt-5 space-y-3 text-sm">
                {[
                  "Worker: scope a batch to the message being handled.",
                  "MVC: scope a batch to the HTTP request, opt in to the loader.",
                  "CLI: scope a batch to any custom unit of work you define.",
                ].map((t) => (
                  <li key={t} className="flex items-start gap-3">
                    <span className="text-cc-accent mt-[3px]">
                      <CheckIcon />
                    </span>
                    <span>{t}</span>
                  </li>
                ))}
              </ul>
            </IndexCard>
          </div>
        </Drawer>

        {/* S7 REFERENCE PLAQUE (MIT band, spectrum seam used once) */}
        <Drawer title="Reference Plaque" classification="005.741.0" seam>
          <div className="flex flex-col items-start justify-between gap-6 pl-[1ch] sm:flex-row sm:items-center">
            <div>
              <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.22em] uppercase">
                Open source
              </span>
              <div className="text-cc-heading mt-2 font-mono text-[15px] font-semibold tracking-[0.06em] uppercase">
                MIT licensed. Maintained in the open.
              </div>
              <p className="text-cc-ink-dim mt-2 max-w-2xl text-sm">
                Green Donut ships in the same repository as Hot Chocolate,
                Fusion, Strawberry Shake, and Cookie Crumble. No per request
                fee, no per seat fee, no commercial fork.
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                View on GitHub
              </OutlineButton>
              <SolidButton href="/docs/greendonut">Read the docs</SolidButton>
            </div>
          </div>
        </Drawer>

        {/* S8 CHECK-OUT DESK closing CTA */}
        <section className="mt-12">
          <BrassPlate title="Check-out Desk" classification="005.741.9" />
          <div
            className="border-cc-card-border bg-cc-card-bg rounded-b-xl border px-5 pt-8 pb-10 sm:px-6"
            style={RULED_PAPER}
          >
            <div className="mx-auto grid max-w-3xl grid-cols-1 items-start gap-6 md:grid-cols-[1fr_auto]">
              <div>
                <span className="text-cc-nav-label font-mono text-[11px] tracking-[0.22em] uppercase">
                  Stop paying for N+1
                </span>
                <h2 className="font-heading text-cc-heading text-h2 mt-3">
                  Six resolver round trips, one batched fetch.
                </h2>
                <p className="text-cc-ink mt-4 max-w-xl text-sm leading-relaxed">
                  Add Green Donut to your service, mark a method with{" "}
                  <span className="text-cc-heading bg-cc-surface rounded px-1 font-mono text-[12px]">
                    [DataLoader]
                  </span>
                  , and inject the generated interface. The N+1 disappears on
                  the next request.
                </p>
                <div className="mt-7 flex flex-wrap items-center gap-3">
                  <SolidButton href="/docs/greendonut">Get Started</SolidButton>
                  <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
                    View on GitHub
                  </OutlineButton>
                </div>
              </div>

              {/* DATE DUE card on the right. */}
              <div className="border-cc-card-border bg-cc-bg/60 w-full rounded-lg border p-4 md:w-44">
                <div className="text-cc-nav-label border-cc-card-border border-b pb-2 text-center font-mono text-[10px] tracking-[0.22em] uppercase">
                  Date Due
                </div>
                <ul className="mt-2 space-y-2">
                  {["Batch", "Cache", "Dedup", "Generate"].map((row) => (
                    <li
                      key={row}
                      className="border-cc-card-border/60 flex items-center justify-between border-b pb-1 font-mono text-[11px]"
                    >
                      <span className="text-cc-ink-dim">{row}</span>
                      <span className="text-cc-accent">
                        <CheckIcon size={12} />
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        </section>
      </main>
    </>
  );
}
