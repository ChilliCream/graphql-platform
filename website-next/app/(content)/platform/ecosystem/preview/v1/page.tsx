import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Nitro IDE: The Workspace for Your GraphQL APIs",
  description:
    "Nitro is the GraphQL IDE for teams. Author operations, organize APIs in workspaces, sync documents, sign in with OAuth, install as a PWA on any platform.",
  keywords: [
    "Nitro GraphQL IDE",
    "Banana Cake Pop",
    "GraphQL workspace",
    "GraphQL OAuth 2",
    "GraphQL document sync",
    "GraphQL file upload multipart",
    "GraphQL PWA",
    "GraphQL IDE PWA",
    "cross platform GraphQL IDE",
    "GraphQL collaboration",
  ],
  openGraph: {
    title: "Nitro: The IDE for Your GraphQL APIs",
    description:
      "Author operations, share workspaces, and sync documents in the browser or as a PWA on macOS, Windows, and Linux. The GraphQL IDE your team actually opens.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Scene accent                                                              */
/*  Single color event: a clipped cyan (#16b9e4) -> violet (#7c92c6) gradient */
/*  for the hero wordmark + a teal cc-accent rail throughout the rest.        */
/* -------------------------------------------------------------------------- */

const SCENE_FROM = "#16b9e4";
const SCENE_MID = "#7c92c6";
const SCENE_TO = "#f0786a";

/* -------------------------------------------------------------------------- */
/*  Small shared chrome primitives                                            */
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

interface WindowDotsProps {
  readonly title: string;
  readonly meta?: string;
}

function WindowDots({ title, meta }: WindowDotsProps) {
  return (
    <div className="border-cc-card-border flex items-center gap-2 border-b bg-black/30 px-3.5 py-2.5">
      <span className="flex gap-1.5" aria-hidden>
        <span className="h-2.5 w-2.5 rounded-full bg-[#ff5f57]/80" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#febc2e]/80" />
        <span className="h-2.5 w-2.5 rounded-full bg-[#28c840]/80" />
      </span>
      <span className="text-cc-ink-dim ml-1.5 font-mono text-[0.7rem] tracking-tight">
        {title}
      </span>
      {meta ? (
        <span className="text-cc-nav-label ml-auto font-mono text-[0.65rem] tracking-tight">
          {meta}
        </span>
      ) : null}
    </div>
  );
}

interface SectionHeadProps {
  readonly eyebrow: string;
  readonly title: ReactNode;
  readonly children?: ReactNode;
}

function SectionHead({ eyebrow, title, children }: SectionHeadProps) {
  return (
    <div className="max-w-2xl">
      <Eyebrow>{eyebrow}</Eyebrow>
      <h2 className="font-heading text-h3 text-cc-heading mt-3 font-semibold tracking-tight">
        {title}
      </h2>
      {children ? (
        <p className="text-cc-ink-dim mt-4 text-[1.05rem] leading-relaxed">
          {children}
        </p>
      ) : null}
    </div>
  );
}

interface FeatureFrameProps {
  readonly eyebrow: string;
  readonly title: string;
  readonly description: string;
  readonly bullets: readonly string[];
  readonly visual: ReactNode;
  readonly reverse?: boolean;
}

/**
 * One feature row: prose on one side, a small inline visual mock on the other.
 * Alternates direction to keep the rhythm interesting across six sections.
 */
function FeatureRow({
  eyebrow,
  title,
  description,
  bullets,
  visual,
  reverse = false,
}: FeatureFrameProps) {
  return (
    <section className="grid items-center gap-10 lg:grid-cols-[1fr_1.05fr] lg:gap-14">
      <div className={reverse ? "lg:order-2" : ""}>
        <Eyebrow>{eyebrow}</Eyebrow>
        <h3 className="font-heading text-h4 text-cc-heading mt-3 font-semibold tracking-tight">
          {title}
        </h3>
        <p className="text-cc-ink-dim mt-4 text-[1.02rem] leading-relaxed">
          {description}
        </p>
        <ul className="mt-5 flex flex-col gap-2.5">
          {bullets.map((b) => (
            <li
              key={b}
              className="text-cc-ink-dim flex items-start gap-2.5 text-[0.92rem] leading-relaxed"
            >
              <span className="text-cc-accent mt-1 shrink-0">
                <CheckIcon size={13} />
              </span>
              <span>{b}</span>
            </li>
          ))}
        </ul>
      </div>
      <div className={reverse ? "lg:order-1" : ""}>{visual}</div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Visual mocks, one per feature row                                         */
/* -------------------------------------------------------------------------- */

/** Authentication flows: a stylized sign-in chooser with three methods. */
function AuthVisual() {
  const methods = [
    {
      key: "OAuth 2",
      detail: "browser sign in",
      glyph: (
        <svg viewBox="0 0 16 16" className="h-3.5 w-3.5" aria-hidden>
          <circle
            cx="8"
            cy="8"
            r="5.5"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
          />
          <path
            d="M5.5 8h5M8 5.5v5"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
          />
        </svg>
      ),
      active: true,
    },
    {
      key: "Bearer",
      detail: "static token header",
      glyph: (
        <svg viewBox="0 0 16 16" className="h-3.5 w-3.5" aria-hidden>
          <rect
            x="3"
            y="6"
            width="10"
            height="6"
            rx="1.5"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
          />
          <path
            d="M5.5 6V4.5a2.5 2.5 0 0 1 5 0V6"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
          />
        </svg>
      ),
      active: false,
    },
    {
      key: "Basic",
      detail: "username + password",
      glyph: (
        <svg viewBox="0 0 16 16" className="h-3.5 w-3.5" aria-hidden>
          <circle
            cx="8"
            cy="6"
            r="2.5"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
          />
          <path
            d="M3 13c1-2.5 3-3.5 5-3.5s4 1 5 3.5"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
          />
        </svg>
      ),
      active: false,
    },
  ] as const;

  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <WindowDots title="connection · auth" meta="staging-api" />
      <div className="flex flex-col gap-2 p-4">
        {methods.map((m) => (
          <div
            key={m.key}
            className={[
              "flex items-center gap-3 rounded-md border px-3 py-2.5 transition-colors",
              m.active
                ? "border-cc-accent/40 bg-cc-accent/[0.06]"
                : "border-cc-card-border bg-cc-surface/40",
            ].join(" ")}
          >
            <span
              className={
                m.active
                  ? "text-cc-accent bg-cc-accent/10 flex h-7 w-7 items-center justify-center rounded-md"
                  : "text-cc-ink-dim bg-cc-surface/70 flex h-7 w-7 items-center justify-center rounded-md"
              }
            >
              {m.glyph}
            </span>
            <div className="flex-1">
              <p
                className={
                  m.active
                    ? "text-cc-heading font-mono text-[0.78rem]"
                    : "text-cc-ink font-mono text-[0.78rem]"
                }
              >
                {m.key}
              </p>
              <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-tight">
                {m.detail}
              </p>
            </div>
            {m.active ? (
              <span className="text-cc-accent font-mono text-[0.6rem] tracking-[0.18em] uppercase">
                ready
              </span>
            ) : null}
          </div>
        ))}
        <div className="border-cc-card-border mt-2 flex items-center justify-between border-t pt-3">
          <span className="text-cc-nav-label font-mono text-[0.62rem] tracking-tight">
            per-connection auth, scoped to a workspace
          </span>
          <span className="text-cc-success font-mono text-[0.62rem] tracking-tight">
            connected
          </span>
        </div>
      </div>
    </div>
  );
}

/** Organization workspaces: tree of org -> workspaces -> APIs + members row. */
function WorkspaceVisual() {
  const tree = [
    { label: "ChilliCream", depth: 0, kind: "org" },
    { label: "Checkout", depth: 1, kind: "ws", count: "4 APIs" },
    { label: "orders-api", depth: 2, kind: "api" },
    { label: "payments-api", depth: 2, kind: "api" },
    { label: "shipping-api", depth: 2, kind: "api", active: true },
    { label: "warehouse-api", depth: 2, kind: "api" },
    { label: "Identity", depth: 1, kind: "ws", count: "2 APIs" },
    { label: "Catalog", depth: 1, kind: "ws", count: "3 APIs" },
  ] as const;

  const initials = ["AK", "MR", "JS", "TP", "+3"];

  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <WindowDots title="workspaces" meta="3 workspaces · 9 APIs" />
      <div className="grid grid-cols-[1fr_auto] gap-3 p-4">
        <div className="flex flex-col gap-0.5">
          {tree.map((t, i) => (
            <div
              key={`${t.label}-${i}`}
              className={[
                "flex items-center gap-2 rounded-md px-2 py-1.5",
                "active" in t && t.active ? "bg-cc-accent/[0.08]" : "",
              ].join(" ")}
              style={{ paddingLeft: `${0.5 + t.depth * 0.9}rem` }}
            >
              <span
                className={
                  t.kind === "org"
                    ? "text-cc-heading"
                    : t.kind === "ws"
                      ? "text-cc-ink"
                      : "active" in t && t.active
                        ? "text-cc-accent"
                        : "text-cc-ink-dim"
                }
              >
                {t.kind === "org" ? (
                  <svg viewBox="0 0 16 16" className="h-3.5 w-3.5" aria-hidden>
                    <path
                      d="M2 13V5l6-3 6 3v8H2z M6 13V8h4v5"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.4"
                    />
                  </svg>
                ) : t.kind === "ws" ? (
                  <svg viewBox="0 0 16 16" className="h-3.5 w-3.5" aria-hidden>
                    <path
                      d="M2 5a1 1 0 0 1 1-1h3l1.5 1.5H13a1 1 0 0 1 1 1V12a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V5z"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.4"
                    />
                  </svg>
                ) : (
                  <svg viewBox="0 0 16 16" className="h-3 w-3" aria-hidden>
                    <circle cx="8" cy="8" r="3" fill="currentColor" />
                  </svg>
                )}
              </span>
              <span
                className={[
                  "font-mono text-[0.74rem]",
                  t.kind === "org"
                    ? "text-cc-heading font-semibold"
                    : t.kind === "ws"
                      ? "text-cc-ink"
                      : "active" in t && t.active
                        ? "text-cc-heading"
                        : "text-cc-ink-dim",
                ].join(" ")}
              >
                {t.label}
              </span>
              {"count" in t && t.count ? (
                <span className="text-cc-nav-label ml-auto font-mono text-[0.6rem] tracking-tight">
                  {t.count}
                </span>
              ) : null}
            </div>
          ))}
        </div>
        <div className="border-cc-card-border flex w-32 flex-col gap-2 border-l pl-4">
          <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            members
          </p>
          <div className="flex flex-wrap gap-1.5">
            {initials.map((i, idx) => (
              <span
                key={i}
                className={[
                  "flex h-6 w-6 items-center justify-center rounded-full font-mono text-[0.55rem]",
                  idx === initials.length - 1
                    ? "text-cc-ink-dim bg-cc-surface/70 border-cc-card-border border"
                    : "text-cc-surface",
                ].join(" ")}
                style={
                  idx < initials.length - 1
                    ? {
                        background: `linear-gradient(135deg, var(--color-cc-accent), #99f6e4)`,
                      }
                    : undefined
                }
              >
                {i}
              </span>
            ))}
          </div>
          <p className="text-cc-nav-label mt-3 font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            role
          </p>
          <span className="text-cc-ink font-mono text-[0.7rem]">Editor</span>
        </div>
      </div>
    </div>
  );
}

/** Document sync: device list with sync rail, last-synced timestamps. */
function SyncVisual() {
  const devices = [
    { name: "MacBook Pro", os: "macOS · 14.2", state: "active", at: "now" },
    { name: "Chrome (Linux)", os: "PWA · 121", state: "synced", at: "2s" },
    { name: "Edge (Windows)", os: "PWA · 121", state: "synced", at: "2s" },
    { name: "iPad Pro", os: "PWA · 17.2", state: "queued", at: "offline" },
  ] as const;

  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <WindowDots title="documents.sync" meta="across your devices" />
      <div className="relative p-4">
        <div
          className="pointer-events-none absolute top-6 bottom-6 left-7 w-px"
          style={{
            background:
              "linear-gradient(180deg, rgba(94,234,212,0.5), rgba(94,234,212,0.06))",
          }}
          aria-hidden
        />
        <ul className="flex flex-col gap-3">
          {devices.map((d) => (
            <li key={d.name} className="flex items-center gap-3">
              <span
                className={[
                  "z-10 flex h-5 w-5 shrink-0 items-center justify-center rounded-full border",
                  d.state === "active"
                    ? "border-cc-accent bg-cc-accent text-cc-surface"
                    : d.state === "synced"
                      ? "border-cc-accent/60 bg-cc-surface text-cc-accent"
                      : "border-cc-card-border bg-cc-surface text-cc-ink-dim",
                ].join(" ")}
              >
                {d.state === "queued" ? (
                  <svg viewBox="0 0 16 16" className="h-2.5 w-2.5" aria-hidden>
                    <circle cx="8" cy="8" r="3" fill="currentColor" />
                  </svg>
                ) : (
                  <CheckIcon size={10} />
                )}
              </span>
              <div className="flex-1">
                <p className="text-cc-heading font-mono text-[0.74rem]">
                  {d.name}
                </p>
                <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-tight">
                  {d.os}
                </p>
              </div>
              <span
                className={[
                  "font-mono text-[0.6rem] tracking-[0.16em] uppercase",
                  d.state === "active"
                    ? "text-cc-accent"
                    : d.state === "synced"
                      ? "text-cc-success"
                      : "text-cc-warning",
                ].join(" ")}
              >
                {d.at}
              </span>
            </li>
          ))}
        </ul>
        <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3">
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-tight">
            18 documents · 4 collections
          </span>
          <span className="text-cc-success font-mono text-[0.6rem] tracking-tight">
            up to date
          </span>
        </div>
      </div>
    </div>
  );
}

/** PWA install: address bar with install affordance + platform pills. */
function PwaVisual() {
  const platforms = ["macOS", "Windows", "Linux", "ChromeOS"];
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <WindowDots title="install · nitro.chillicream.com" />
      <div className="p-4">
        <div className="border-cc-card-border bg-cc-surface/60 flex items-center gap-2 rounded-lg border px-3 py-2">
          <span className="text-cc-success">
            <svg viewBox="0 0 16 16" className="h-3.5 w-3.5" aria-hidden>
              <path
                d="M5 8.5a3 3 0 1 1 1.2-5.8M11 8.5a3 3 0 1 0-1.2-5.8M3.5 13h9"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.4"
                strokeLinecap="round"
              />
            </svg>
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.7rem]">
            nitro.chillicream.com
          </span>
          <span className="text-cc-accent border-cc-accent/40 bg-cc-accent/10 ml-auto rounded-md border px-2 py-1 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
            install app
          </span>
        </div>
        <p className="text-cc-ink-dim mt-4 text-[0.82rem] leading-relaxed">
          Click install in your browser. Nitro becomes a real app with its own
          window, dock icon, and offline cache. No installer, no admin prompt.
        </p>
        <div className="mt-4 flex flex-wrap gap-1.5">
          {platforms.map((p) => (
            <span
              key={p}
              className="border-cc-card-border bg-cc-surface/60 text-cc-ink-dim rounded-full border px-2.5 py-1 font-mono text-[0.6rem] tracking-tight"
            >
              {p}
            </span>
          ))}
        </div>
      </div>
    </div>
  );
}

/** Themes: side-by-side dark / light / system preview swatches. */
function ThemesVisual() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <WindowDots title="appearance" meta="auto · follow system" />
      <div className="grid grid-cols-3 gap-3 p-4">
        {[
          {
            label: "Dark",
            ring: "border-cc-accent/60",
            bg: "#0c1322",
            barA: "rgba(245,241,234,0.85)",
            barB: "rgba(94,234,212,0.7)",
            barC: "rgba(245,241,234,0.4)",
            active: true,
          },
          {
            label: "Light",
            ring: "border-cc-card-border",
            bg: "#f5f1ea",
            barA: "rgba(12,19,34,0.85)",
            barB: "rgba(16,160,142,0.85)",
            barC: "rgba(12,19,34,0.45)",
            active: false,
          },
          {
            label: "System",
            ring: "border-cc-card-border",
            bg: "linear-gradient(135deg, #0c1322 0 50%, #f5f1ea 50% 100%)",
            barA: "rgba(245,241,234,0.7)",
            barB: "rgba(94,234,212,0.7)",
            barC: "rgba(12,19,34,0.5)",
            active: false,
          },
        ].map((s) => (
          <div
            key={s.label}
            className={[
              "flex flex-col overflow-hidden rounded-lg border",
              s.ring,
            ].join(" ")}
          >
            <div className="relative h-20" style={{ background: s.bg }}>
              <span
                className="absolute top-2 left-2 h-1.5 w-7 rounded-full"
                style={{ background: s.barA }}
              />
              <span
                className="absolute top-5 left-2 h-1.5 w-10 rounded-full"
                style={{ background: s.barC }}
              />
              <span
                className="absolute top-10 left-2 h-2 w-12 rounded-md"
                style={{ background: s.barB }}
              />
              <span
                className="absolute top-14 left-2 h-1.5 w-6 rounded-full"
                style={{ background: s.barC }}
              />
            </div>
            <div className="border-cc-card-border bg-cc-surface/60 flex items-center justify-between border-t px-2 py-2">
              <span className="text-cc-ink font-mono text-[0.66rem]">
                {s.label}
              </span>
              {s.active ? (
                <span className="text-cc-accent">
                  <CheckIcon size={11} />
                </span>
              ) : null}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

/** File upload: a multipart payload preview with parts table. */
function UploadVisual() {
  const parts = [
    { name: "operations", type: "application/json", size: "412 B" },
    { name: "map", type: "application/json", size: "63 B" },
    { name: "0", type: "image/png", size: "184 KB" },
  ];
  return (
    <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-xl border backdrop-blur-sm">
      <WindowDots title="POST /graphql" meta="multipart/form-data" />
      <div className="p-4">
        <pre className="bg-cc-surface/60 text-cc-ink-dim overflow-x-auto rounded-md px-3 py-2 font-mono text-[0.7rem] leading-5">
          <code>
            <span className="text-cc-nav-label"># multipart request spec</span>
            {"\n"}
            <span style={{ color: "#7ee787" }}>mutation</span>{" "}
            <span style={{ color: "#d2a8ff" }}>UploadAvatar</span>(
            <span style={{ color: "#ffa657" }}>$file</span>:{" "}
            <span style={{ color: "#79c0ff" }}>Upload!</span>) {"{"}
            {"\n  "}
            <span style={{ color: "#d2a8ff" }}>uploadAvatar</span>(file:{" "}
            <span style={{ color: "#ffa657" }}>$file</span>) {"{"} url {"}"}
            {"\n"}
            {"}"}
          </code>
        </pre>
        <div className="mt-3 flex flex-col gap-1.5">
          {parts.map((p, i) => (
            <div
              key={p.name}
              className="border-cc-card-border bg-cc-surface/40 grid grid-cols-[auto_1fr_auto_auto] items-center gap-2 rounded-md border px-2.5 py-1.5"
            >
              <span className="text-cc-nav-label font-mono text-[0.6rem] tabular-nums">
                {i}
              </span>
              <span className="text-cc-heading font-mono text-[0.7rem]">
                {p.name}
              </span>
              <span className="text-cc-ink-dim font-mono text-[0.6rem]">
                {p.type}
              </span>
              <span className="text-cc-nav-label font-mono text-[0.6rem] tabular-nums">
                {p.size}
              </span>
            </div>
          ))}
        </div>
        <p className="text-cc-nav-label border-cc-card-border mt-3 border-t pt-3 font-mono text-[0.6rem] tracking-tight">
          jaydenseric/graphql-multipart-request-spec compatible
        </p>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Platform availability strip                                               */
/* -------------------------------------------------------------------------- */

interface PlatformCardProps {
  readonly name: string;
  readonly detail: string;
  readonly icon: ReactNode;
}

function PlatformCard({ name, detail, icon }: PlatformCardProps) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex items-center gap-3 rounded-xl border p-4 backdrop-blur-sm">
      <span className="text-cc-accent bg-cc-accent/[0.08] flex h-9 w-9 shrink-0 items-center justify-center rounded-md">
        {icon}
      </span>
      <div>
        <p className="text-cc-heading font-mono text-[0.78rem]">{name}</p>
        <p className="text-cc-nav-label font-mono text-[0.62rem] tracking-tight">
          {detail}
        </p>
      </div>
    </div>
  );
}

function PlatformStrip() {
  return (
    <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <PlatformCard
        name="Browser"
        detail="any modern browser · PWA"
        icon={
          <svg viewBox="0 0 20 20" className="h-4 w-4" aria-hidden>
            <circle
              cx="10"
              cy="10"
              r="7.5"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.5"
            />
            <path
              d="M2.5 10h15M10 2.5c3 2.5 3 12.5 0 15M10 2.5c-3 2.5-3 12.5 0 15"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.5"
            />
          </svg>
        }
      />
      <PlatformCard
        name="macOS"
        detail="install as a PWA"
        icon={
          <svg viewBox="0 0 20 20" className="h-4 w-4" aria-hidden>
            <path
              d="M13.5 6.2c-1 0-1.8.5-2.4.5s-1.4-.5-2.3-.5c-1.4.1-2.8 1-3.4 2.4-.7 1.7-.2 4.4 1 6.2.6.9 1.3 1.9 2.2 1.9.9 0 1.2-.6 2.3-.6s1.4.6 2.3.6c1 0 1.6-.9 2.2-1.8.5-.8.8-1.7 1-2.6-1.8-.7-2.4-3.1-.9-4.6-.5-.7-1.3-1.4-2-1.5zm-2-2c.7-.8.6-2 .5-2.4-1.1.1-2 .8-2.5 1.6-.4.8-.7 2 .4 2.3.7 0 1.2-.6 1.6-1.5z"
              fill="currentColor"
            />
          </svg>
        }
      />
      <PlatformCard
        name="Windows"
        detail="install as a PWA"
        icon={
          <svg viewBox="0 0 20 20" className="h-4 w-4" aria-hidden>
            <path
              d="M2.5 5l6.5-.9v6.4H2.5V5zm0 6.6h6.5V18l-6.5-.9v-5.5zm7.7-7.6L18 3v8H10.2V4zm0 8.6H18V17l-7.8-1.1v-3.3z"
              fill="currentColor"
            />
          </svg>
        }
      />
      <PlatformCard
        name="Linux"
        detail="install as a PWA"
        icon={
          <svg viewBox="0 0 20 20" className="h-4 w-4" aria-hidden>
            <path
              d="M10 2.5c-2.2 0-3.6 1.8-3.6 4.2 0 1.7.6 2.5 1.1 3.4.5.9-.4 1.5-1.4 2-1.4.6-2.4 1.8-2.4 3 0 .8.5 1.4 1.4 1.7l.2.7c.2.6.8.9 1.5.9.5 0 1-.2 1.6-.5.5-.3.9-.5 1.6-.5s1.1.2 1.6.5c.6.3 1.1.5 1.6.5.7 0 1.3-.3 1.5-.9l.2-.7c.9-.3 1.4-.9 1.4-1.7 0-1.2-1-2.4-2.4-3-1-.5-1.9-1.1-1.4-2 .5-.9 1.1-1.7 1.1-3.4 0-2.4-1.4-4.2-3.6-4.2zm-1.3 3.3c.3 0 .6.4.6.9s-.3.9-.6.9-.6-.4-.6-.9.3-.9.6-.9zm2.6 0c.3 0 .6.4.6.9s-.3.9-.6.9-.6-.4-.6-.9.3-.9.6-.9z"
              fill="currentColor"
            />
          </svg>
        }
      />
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function EcosystemV1Page() {
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-32">
      {/* ------------------------------ HERO ----------------------------- */}
      <section className="grid items-start gap-10 lg:grid-cols-[1fr_0.55fr] lg:gap-14">
        <div>
          <Eyebrow>Nitro · GraphQL IDE</Eyebrow>
          <h1 className="font-heading text-hero text-cc-heading mt-5 font-semibold tracking-tight">
            The IDE your team{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{
                backgroundImage: `linear-gradient(100deg, ${SCENE_FROM}, ${SCENE_MID} 55%, ${SCENE_TO})`,
              }}
            >
              actually opens.
            </span>
          </h1>
          <p className="lead text-cc-ink-dim mt-6 max-w-2xl">
            Nitro is a focused, fast GraphQL workspace. Sign in with OAuth,
            organize APIs in shared workspaces, sync your documents across every
            device, and install it as a PWA on macOS, Windows, or Linux. The IDE
            knows your schema, so authoring an operation feels native.
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href="https://nitro.chillicream.com">
              Launch Nitro
            </SolidButton>
            <OutlineButton href="/docs/nitro">Read the Docs</OutlineButton>
          </div>
          <ul className="text-cc-ink-dim mt-9 flex flex-wrap gap-x-6 gap-y-2 text-[0.88rem]">
            {[
              "Browser plus PWA on macOS, Windows, Linux",
              "OAuth 2, bearer, basic auth",
              "Workspaces sync across devices",
            ].map((item) => (
              <li key={item} className="flex items-center gap-2">
                <span className="text-cc-accent">
                  <CheckIcon size={13} />
                </span>
                {item}
              </li>
            ))}
          </ul>
        </div>

        {/* Compact hero side mock: a stylized Nitro window header + tab row. */}
        <div className="relative">
          <div
            className="pointer-events-none absolute -inset-6 -z-10 rounded-3xl opacity-50 blur-2xl"
            style={{
              background: `radial-gradient(60% 60% at 70% 20%, rgba(94,234,212,0.22), transparent 70%)`,
            }}
            aria-hidden
          />
          <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border shadow-2xl shadow-black/40 backdrop-blur-md">
            <WindowDots title="nitro · Checkout / shipping-api" meta="OAuth" />
            <div className="border-cc-card-border flex items-center gap-1 border-b bg-black/20 px-3 py-1.5">
              {["GetOrder", "RateLimits", "UploadAvatar"].map((t, i) => (
                <span
                  key={t}
                  className={[
                    "rounded-md px-2.5 py-1 font-mono text-[0.66rem]",
                    i === 0
                      ? "bg-cc-surface text-cc-heading border-cc-card-border border"
                      : "text-cc-ink-dim",
                  ].join(" ")}
                >
                  {t}
                </span>
              ))}
              <span className="text-cc-nav-label ml-auto font-mono text-[0.6rem]">
                3 tabs
              </span>
            </div>
            <div className="divide-cc-card-border grid grid-cols-[88px_1fr] divide-x">
              <div className="flex flex-col gap-2 p-3">
                {["Checkout", "Identity", "Catalog"].map((w, i) => (
                  <span
                    key={w}
                    className={[
                      "rounded-md px-2 py-1 font-mono text-[0.62rem]",
                      i === 0
                        ? "bg-cc-accent/[0.08] text-cc-accent"
                        : "text-cc-ink-dim",
                    ].join(" ")}
                  >
                    {w}
                  </span>
                ))}
              </div>
              <div className="p-3 font-mono text-[0.7rem] leading-5">
                <span style={{ color: "#7ee787" }}>query</span>{" "}
                <span style={{ color: "#d2a8ff" }}>GetOrder</span>(
                <span style={{ color: "#ffa657" }}>$id</span>:{" "}
                <span style={{ color: "#79c0ff" }}>ID!</span>) {"{"}
                <br />
                <span className="text-cc-ink-dim">
                  {"  "}
                  order(id: $id) {"{"}
                </span>
                <br />
                <span className="text-cc-ink-dim">{"    id status total"}</span>
                <br />
                <span className="text-cc-ink-dim">{"  }"}</span>
                <br />
                {"}"}
              </div>
            </div>
            <div className="border-cc-card-border flex items-center justify-between border-t bg-black/20 px-3 py-1.5 font-mono text-[0.6rem]">
              <span className="text-cc-success">connected</span>
              <span className="text-cc-nav-label">12 ms · 200 OK</span>
            </div>
          </div>
        </div>
      </section>

      {/* ----------------------- NITRO IN ACTION ------------------------- */}
      <section>
        <div className="mx-auto max-w-3xl text-center">
          <Eyebrow>Nitro · the authoring loop</Eyebrow>
          <h2 className="font-heading text-h2 text-cc-heading mt-4 font-semibold tracking-tight">
            Author, run, and inspect, in one place.
          </h2>
          <p className="text-cc-ink-dim mt-5 text-[1.05rem] leading-relaxed">
            An animated illustration of the authoring loop: schema-aware
            completion, an operation moving through the pipeline, and a typed
            response settling inline.
          </p>
        </div>
        <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-5xl overflow-hidden rounded-xl border backdrop-blur-sm">
          <NitroCompose />
        </div>
        <p className="text-cc-nav-label mx-auto mt-3 max-w-5xl text-center font-mono text-[0.6rem] tracking-tight">
          animated illustration of the IDE authoring loop
        </p>
      </section>

      {/* ------------------------- FEATURE ROWS -------------------------- */}
      <section className="flex flex-col gap-24">
        <FeatureRow
          eyebrow="01 · authentication"
          title="Sign in the way your API expects."
          description="Nitro speaks the auth flows your services already use. OAuth 2 handles human sign-in, bearer tokens cover service accounts, and basic auth keeps legacy endpoints reachable, all configured per connection."
          bullets={[
            "OAuth 2 for browser-based sign in",
            "Bearer headers and basic auth for legacy endpoints",
            "Per-connection auth, scoped to a single workspace",
          ]}
          visual={<AuthVisual />}
        />
        <FeatureRow
          eyebrow="02 · workspaces"
          title="One workspace per team, not per laptop."
          description="Group your APIs into workspaces and invite the people who own them. Operations, environments, and connection settings live with the workspace, so a new teammate is one invite away from running production queries the same way you do."
          bullets={[
            "Organize APIs under a shared organization",
            "Per-workspace members, roles, and connection settings",
            "Switching workspaces is one click, no re-config",
          ]}
          visual={<WorkspaceVisual />}
          reverse
        />
        <FeatureRow
          eyebrow="03 · document sync"
          title="Your documents follow you, every device."
          description="Open Nitro on your laptop, your desktop, or the browser on a borrowed machine, your queries are already there. Documents stay in sync across your devices and across the teams you share a workspace with."
          bullets={[
            "Documents synced across all your signed-in devices",
            "Shared with the teams in your workspace",
            "Pick up where you left off on any machine",
          ]}
          visual={<SyncVisual />}
        />
        <FeatureRow
          eyebrow="04 · install anywhere"
          title="A real app, without a real installer."
          description="Nitro runs as a Progressive Web App, so any modern browser can promote it to a standalone window with its own icon and offline cache. No installer, no admin privileges, no IT ticket. The same Nitro on the web becomes the Nitro on your desktop."
          bullets={[
            "Install from the browser, no admin rights required",
            "Standalone window, dock icon, offline cache",
            "Updates roll out the moment they ship",
          ]}
          visual={<PwaVisual />}
          reverse
        />
        <FeatureRow
          eyebrow="05 · themes"
          title="Light, dark, or whatever your OS decides."
          description="Pick a theme that does not fight your environment. Dark for the pairing session, light for the projector, or system to track your OS automatically. Every surface, from the editor to the response pane, is tuned for both modes."
          bullets={[
            "Hand-tuned dark and light themes",
            "Follow system to switch with your OS",
            "All surfaces, including charts and diagnostics",
          ]}
          visual={<ThemesVisual />}
        />
        <FeatureRow
          eyebrow="06 · file upload"
          title="Multipart uploads, exactly to spec."
          description="Send files alongside your variables without leaving the editor. Nitro builds the multipart request per the GraphQL multipart spec, so the same payload your Hot Chocolate server accepts in production is the one the IDE sends from your machine."
          bullets={[
            "Implements the GraphQL multipart request spec",
            "Drag-and-drop into variables, previewed inline",
            "Works with any spec-compliant GraphQL server",
          ]}
          visual={<UploadVisual />}
          reverse
        />
      </section>

      {/* --------------------- CROSS-PLATFORM STRIP ---------------------- */}
      <section>
        <SectionHead
          eyebrow="Cross-platform · everywhere your team is"
          title="The same Nitro, on whatever you sit in front of."
        >
          Nitro is the same product on every platform. Use it in any modern
          browser, or install the PWA on macOS, Windows, or Linux. Your
          workspaces, history, and connections come with you.
        </SectionHead>
        <div className="mt-10">
          <PlatformStrip />
        </div>
      </section>

      {/* ------------------------ HONESTY BAND --------------------------- */}
      <section className="border-cc-card-border bg-cc-surface/50 rounded-2xl border p-8 backdrop-blur-sm sm:p-10">
        <Eyebrow>Where the line is</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading mt-3 max-w-3xl font-semibold tracking-tight">
          Nitro is the IDE. Telemetry is a separate decision.
        </h2>
        <div className="mt-7 grid gap-6 sm:grid-cols-2">
          <p className="text-cc-ink-dim text-[1rem] leading-relaxed">
            What ships in the IDE is authoring, workspaces, document sync,
            cross-platform install, and a built-in GraphQL endpoint UI that runs
            wherever you point it. That is enough to make a team productive on
            day one.
          </p>
          <p className="text-cc-ink-dim text-[1rem] leading-relaxed">
            Operational telemetry, the dashboards and traces, requires
            configuring Nitro telemetry on the server side. It is not magic, it
            is opt-in. The IDE is useful with or without it.
          </p>
        </div>
      </section>

      {/* ----------------------------- CTA ------------------------------- */}
      <section className="flex flex-col items-center gap-7 py-6 text-center">
        <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
          Open Nitro. Pick a workspace. Run a query.
        </h2>
        <p className="text-cc-ink-dim max-w-xl text-[1.1rem] leading-relaxed">
          The fastest way to feel the difference is to launch the IDE against
          your own GraphQL endpoint. No install, no signup gate, and your
          workspace travels with you.
        </p>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="https://nitro.chillicream.com">
            Launch Nitro
          </SolidButton>
          <OutlineButton href="/docs/nitro">Read the Docs</OutlineButton>
        </div>
      </section>
    </div>
  );
}
