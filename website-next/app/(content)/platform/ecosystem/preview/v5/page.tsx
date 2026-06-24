import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "The Nitro Dispatch: A GraphQL IDE Nitro Long Read",
  description:
    "A long-form dispatch on the GraphQL IDE Nitro. Workspaces, document sync, OAuth 2, PWA install on macOS, Windows, Linux, and multipart upload, set as an article.",
  keywords: [
    "GraphQL IDE Nitro",
    "Nitro GraphQL IDE",
    "GraphQL workspace",
    "GraphQL OAuth 2",
    "GraphQL document sync",
    "GraphQL multipart upload",
    "GraphQL PWA",
    "cross platform GraphQL IDE",
    "GraphQL collaboration",
    "Banana Cake Pop",
  ],
  openGraph: {
    title: "The Nitro Dispatch",
    description:
      "A long-form dispatch on the GraphQL IDE Nitro. Workspaces, document sync, OAuth 2, PWA install on macOS, Windows, Linux.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Editorial primitives                                                      */
/*                                                                            */
/*  Single narrow reading column on cc-bg. Thin cc-card-border rules and a    */
/*  single cc-accent vertical hairline along the left edge of the column do   */
/*  all the heavy lifting. Cards are borderless or hairline-only.             */
/* -------------------------------------------------------------------------- */

interface ChapterLabelProps {
  readonly children: ReactNode;
}

/** Tiny mono uppercase chapter label hanging in the left margin. */
function ChapterLabel({ children }: ChapterLabelProps) {
  return (
    <p className="text-cc-accent font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface FigureCaptionProps {
  readonly children: ReactNode;
}

function FigureCaption({ children }: FigureCaptionProps) {
  return (
    <p className="text-cc-nav-label mt-3 font-mono text-[0.6rem] tracking-[0.18em] uppercase">
      {children}
    </p>
  );
}

interface FigureFrameProps {
  readonly children: ReactNode;
}

/** Hairline-only frame for inline article figures. No bg fill, no shadow. */
function FigureFrame({ children }: FigureFrameProps) {
  return (
    <div className="border-cc-card-border overflow-hidden rounded-sm border">
      {children}
    </div>
  );
}

interface ChapterProps {
  readonly label: string;
  readonly title: string;
  readonly children: ReactNode;
}

/**
 * One numbered chapter. Opens with a horizontal rule, the chapter label
 * in the margin, an h3 title, then long-form body content.
 */
function Chapter({ label, title, children }: ChapterProps) {
  return (
    <section className="py-20">
      <hr className="border-cc-card-border mb-12 border-t" aria-hidden />
      <ChapterLabel>{label}</ChapterLabel>
      <h2 className="font-heading text-h3 text-cc-heading mt-2 font-semibold tracking-tight">
        {title}
      </h2>
      <div className="mt-8 flex flex-col gap-6">{children}</div>
    </section>
  );
}

interface ParagraphProps {
  readonly children: ReactNode;
}

function Paragraph({ children }: ParagraphProps) {
  return <p className="text-cc-prose text-body leading-relaxed">{children}</p>;
}

interface PullQuoteProps {
  readonly children: ReactNode;
}

/** Centered italic pull quote with a thin cc-accent vertical hairline. */
function PullQuote({ children }: PullQuoteProps) {
  return (
    <blockquote className="border-cc-accent text-cc-heading my-4 border-l py-2 pl-6 italic">
      <p className="font-heading text-h4 leading-snug font-light">{children}</p>
    </blockquote>
  );
}

/* -------------------------------------------------------------------------- */
/*  Inline figures                                                            */
/* -------------------------------------------------------------------------- */

/** Fig. 02: three auth methods as a stacked hairline list, no card fill. */
function AuthFigure() {
  const methods = [
    {
      name: "OAuth 2",
      detail: "browser sign in",
      active: true,
    },
    {
      name: "Bearer",
      detail: "static token header",
      active: false,
    },
    {
      name: "Basic",
      detail: "username and password",
      active: false,
    },
  ] as const;

  return (
    <FigureFrame>
      <ul className="divide-cc-card-border flex flex-col divide-y">
        {methods.map((m) => (
          <li key={m.name} className="flex items-center gap-4 px-5 py-3.5">
            <span
              className={[
                "font-mono text-[0.6rem] tracking-[0.18em] uppercase",
                m.active ? "text-cc-accent" : "text-cc-nav-label",
              ].join(" ")}
            >
              {m.active ? "active" : "available"}
            </span>
            <div className="flex-1">
              <p className="text-cc-heading font-mono text-[0.82rem]">
                {m.name}
              </p>
              <p className="text-cc-ink-dim mt-0.5 font-mono text-[0.66rem] tracking-tight">
                {m.detail}
              </p>
            </div>
            {m.active ? (
              <span className="text-cc-accent">
                <CheckIcon size={12} />
              </span>
            ) : null}
          </li>
        ))}
      </ul>
    </FigureFrame>
  );
}

/** Fig. 03: org -> workspace -> API tree as text and tiny SVGs over hairlines. */
function WorkspaceFigure() {
  const tree = [
    {
      label: "ChilliCream",
      depth: 0,
      kind: "org" as const,
      meta: "organization",
    },
    { label: "Checkout", depth: 1, kind: "ws" as const, meta: "4 APIs" },
    { label: "orders-api", depth: 2, kind: "api" as const, meta: "" },
    { label: "payments-api", depth: 2, kind: "api" as const, meta: "" },
    { label: "shipping-api", depth: 2, kind: "api" as const, meta: "open" },
    { label: "Identity", depth: 1, kind: "ws" as const, meta: "2 APIs" },
    { label: "Catalog", depth: 1, kind: "ws" as const, meta: "3 APIs" },
  ];
  const initials = ["AK", "MR", "JS", "TP"];

  return (
    <FigureFrame>
      <div className="grid grid-cols-1 sm:grid-cols-[1fr_auto]">
        <ul className="divide-cc-card-border flex flex-col divide-y">
          {tree.map((t, i) => (
            <li
              key={`${t.label}-${i}`}
              className="flex items-center gap-3 px-5 py-2.5"
              style={{ paddingLeft: `${1.25 + t.depth * 1.1}rem` }}
            >
              <span
                className={
                  t.kind === "org"
                    ? "text-cc-accent"
                    : t.kind === "ws"
                      ? "text-cc-ink"
                      : t.meta === "open"
                        ? "text-cc-accent"
                        : "text-cc-ink-dim"
                }
                aria-hidden
              >
                {t.kind === "org" ? (
                  <svg viewBox="0 0 16 16" className="h-3.5 w-3.5">
                    <path
                      d="M2 13V5l6-3 6 3v8H2z M6 13V8h4v5"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.3"
                    />
                  </svg>
                ) : t.kind === "ws" ? (
                  <svg viewBox="0 0 16 16" className="h-3.5 w-3.5">
                    <path
                      d="M2 5a1 1 0 0 1 1-1h3l1.5 1.5H13a1 1 0 0 1 1 1V12a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V5z"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.3"
                    />
                  </svg>
                ) : (
                  <svg viewBox="0 0 16 16" className="h-2.5 w-2.5">
                    <circle cx="8" cy="8" r="3" fill="currentColor" />
                  </svg>
                )}
              </span>
              <span
                className={[
                  "font-mono text-[0.78rem]",
                  t.kind === "org"
                    ? "text-cc-heading font-semibold"
                    : t.kind === "ws"
                      ? "text-cc-ink"
                      : t.meta === "open"
                        ? "text-cc-heading"
                        : "text-cc-ink-dim",
                ].join(" ")}
              >
                {t.label}
              </span>
              {t.meta ? (
                <span className="text-cc-nav-label ml-auto font-mono text-[0.6rem] tracking-[0.16em] uppercase">
                  {t.meta}
                </span>
              ) : null}
            </li>
          ))}
        </ul>
        <div className="border-cc-card-border flex flex-col gap-3 border-t p-5 sm:min-w-44 sm:border-t-0 sm:border-l">
          <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            members
          </p>
          <div className="flex flex-wrap gap-1.5">
            {initials.map((i) => (
              <span
                key={i}
                className="border-cc-card-border text-cc-ink flex h-7 w-7 items-center justify-center rounded-full border font-mono text-[0.6rem]"
              >
                {i}
              </span>
            ))}
          </div>
          <p className="text-cc-nav-label mt-2 font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            role
          </p>
          <p className="text-cc-ink font-mono text-[0.74rem]">Editor</p>
        </div>
      </div>
    </FigureFrame>
  );
}

/** Fig. 04: device list anchored to a single thin cc-accent vertical rail. */
function SyncFigure() {
  const devices = [
    { name: "MacBook Pro", os: "macOS", state: "active", at: "now" },
    { name: "Chrome on Linux", os: "PWA", state: "synced", at: "2s ago" },
    { name: "Edge on Windows", os: "PWA", state: "synced", at: "2s ago" },
    { name: "iPad Pro", os: "PWA", state: "queued", at: "offline" },
  ] as const;

  return (
    <FigureFrame>
      <div className="relative px-5 py-6">
        <span
          className="bg-cc-accent pointer-events-none absolute top-7 bottom-7 left-7 w-px opacity-60"
          aria-hidden
        />
        <ul className="flex flex-col gap-4">
          {devices.map((d) => (
            <li key={d.name} className="flex items-center gap-4">
              <span
                className={[
                  "bg-cc-bg z-10 flex h-4 w-4 shrink-0 items-center justify-center rounded-full border",
                  d.state === "active"
                    ? "border-cc-accent text-cc-accent"
                    : d.state === "synced"
                      ? "border-cc-accent/60 text-cc-accent"
                      : "border-cc-card-border text-cc-ink-dim",
                ].join(" ")}
              >
                {d.state === "queued" ? (
                  <svg viewBox="0 0 8 8" className="h-2 w-2" aria-hidden>
                    <circle cx="4" cy="4" r="1.6" fill="currentColor" />
                  </svg>
                ) : (
                  <CheckIcon size={9} />
                )}
              </span>
              <div className="flex-1">
                <p className="text-cc-heading font-mono text-[0.78rem]">
                  {d.name}
                </p>
                <p className="text-cc-ink-dim font-mono text-[0.62rem] tracking-tight">
                  {d.os}
                </p>
              </div>
              <span
                className={[
                  "font-mono text-[0.6rem] tracking-[0.18em] uppercase",
                  d.state === "active"
                    ? "text-cc-accent"
                    : d.state === "queued"
                      ? "text-cc-ink-dim"
                      : "text-cc-success",
                ].join(" ")}
              >
                {d.at}
              </span>
            </li>
          ))}
        </ul>
      </div>
    </FigureFrame>
  );
}

/** Fig. 05a: install affordance line + platform name pills. */
function InstallFigure() {
  const platforms = ["macOS", "Windows", "Linux", "ChromeOS"];
  return (
    <FigureFrame>
      <div className="px-5 py-5">
        <div className="border-cc-card-border flex items-center gap-3 border-b pb-4">
          <span className="text-cc-accent" aria-hidden>
            <svg viewBox="0 0 16 16" className="h-3.5 w-3.5">
              <path
                d="M3 13h10M8 3v7m0 0l-3-3m3 3l3-3"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.3"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </span>
          <span className="text-cc-ink-dim font-mono text-[0.74rem]">
            nitro.chillicream.com
          </span>
          <span className="text-cc-accent ml-auto font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            install app
          </span>
        </div>
        <div className="mt-4 flex flex-wrap gap-2">
          {platforms.map((p) => (
            <span
              key={p}
              className="border-cc-card-border text-cc-ink rounded-full border px-3 py-1 font-mono text-[0.62rem] tracking-tight"
            >
              {p}
            </span>
          ))}
        </div>
      </div>
    </FigureFrame>
  );
}

/** Fig. 05b: slim three-swatch theme triptych. */
function ThemesFigure() {
  const swatches = [
    {
      label: "Dark",
      bg: "#0c1322",
      bar: "rgba(94,234,212,0.8)",
      faint: "rgba(245,241,234,0.45)",
      active: true,
    },
    {
      label: "Light",
      bg: "#f5f1ea",
      bar: "rgba(16,160,142,0.9)",
      faint: "rgba(12,19,34,0.45)",
      active: false,
    },
    {
      label: "System",
      bg: "linear-gradient(135deg, #0c1322 0 50%, #f5f1ea 50% 100%)",
      bar: "rgba(94,234,212,0.7)",
      faint: "rgba(245,241,234,0.45)",
      active: false,
    },
  ] as const;

  return (
    <FigureFrame>
      <div className="grid grid-cols-3">
        {swatches.map((s, i) => (
          <div
            key={s.label}
            className={
              i < swatches.length - 1 ? "border-cc-card-border border-r" : ""
            }
          >
            <div className="relative h-16" style={{ background: s.bg }}>
              <span
                className="absolute top-3 left-3 h-1 w-8 rounded-full"
                style={{ background: s.faint }}
              />
              <span
                className="absolute top-6 left-3 h-1.5 w-10 rounded"
                style={{ background: s.bar }}
              />
              <span
                className="absolute top-10 left-3 h-1 w-6 rounded-full"
                style={{ background: s.faint }}
              />
            </div>
            <div className="border-cc-card-border flex items-center justify-between border-t px-3 py-2">
              <span className="text-cc-ink font-mono text-[0.66rem]">
                {s.label}
              </span>
              {s.active ? (
                <span className="text-cc-accent">
                  <CheckIcon size={10} />
                </span>
              ) : null}
            </div>
          </div>
        ))}
      </div>
    </FigureFrame>
  );
}

/** Fig. 06: multipart payload preview + parts table as a hairline grid. */
function UploadFigure() {
  const parts = [
    { name: "operations", type: "application/json", size: "412 B" },
    { name: "map", type: "application/json", size: "63 B" },
    { name: "0", type: "image/png", size: "184 KB" },
  ];
  return (
    <FigureFrame>
      <div className="px-5 py-5">
        <pre className="text-cc-prose overflow-x-auto font-mono text-[0.72rem] leading-6">
          <code>
            <span className="text-cc-nav-label"># multipart request</span>
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
        <div className="border-cc-card-border mt-5 border-t">
          <div className="text-cc-nav-label grid grid-cols-[2rem_1fr_8rem_5rem] gap-3 py-2 font-mono text-[0.58rem] tracking-[0.18em] uppercase">
            <span>#</span>
            <span>part</span>
            <span>type</span>
            <span className="text-right">size</span>
          </div>
          <div className="divide-cc-card-border flex flex-col divide-y">
            {parts.map((p, i) => (
              <div
                key={p.name}
                className="grid grid-cols-[2rem_1fr_8rem_5rem] gap-3 py-2.5"
              >
                <span className="text-cc-nav-label font-mono text-[0.7rem] tabular-nums">
                  {i}
                </span>
                <span className="text-cc-heading font-mono text-[0.74rem]">
                  {p.name}
                </span>
                <span className="text-cc-ink-dim font-mono text-[0.66rem]">
                  {p.type}
                </span>
                <span className="text-cc-ink-dim text-right font-mono text-[0.66rem] tabular-nums">
                  {p.size}
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </FigureFrame>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function EcosystemV5Page() {
  return (
    <article className="relative mx-auto max-w-3xl px-4 py-10 sm:py-16">
      {/* Margin hairline: a single thin cc-accent vertical rule running the
          length of the article. Sits flush-left of the reading column on
          larger screens, where the chapter labels hang against it. */}
      <span
        className="bg-cc-accent pointer-events-none absolute top-10 bottom-10 left-0 hidden w-px opacity-40 sm:block"
        aria-hidden
      />

      {/* -------------------- Masthead and Lede -------------------- */}
      <header className="max-w-2xl">
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          The Nitro Dispatch / Vol. 01 / The GraphQL IDE
        </p>
        <h1 className="font-heading text-hero text-cc-heading mt-8 font-semibold tracking-tight">
          <span className="text-cc-accent float-left mr-3 leading-[0.85] font-semibold">
            N
          </span>
          itro, the IDE your team actually opens.
        </h1>
        <p className="text-cc-prose text-lead mt-10 leading-snug">
          A focused, fast GraphQL workspace. Sign in with OAuth, organize APIs
          in shared workspaces, sync your documents across every device, and
          install it as a PWA on macOS, Windows, or Linux. The IDE knows your
          schema, so authoring an operation feels native.
        </p>
        <p className="text-cc-ink-dim mt-8 font-mono text-[0.72rem] tracking-tight">
          <a
            href="https://nitro.chillicream.com"
            className="text-cc-accent hover:text-cc-accent-hover underline-offset-4 hover:underline"
          >
            Launch Nitro
          </a>
          <span className="mx-3 opacity-50">·</span>
          <Link
            href="/docs/nitro"
            className="text-cc-accent hover:text-cc-accent-hover underline-offset-4 hover:underline"
          >
            Read the Docs
          </Link>
        </p>
      </header>

      {/* -------------------- Chapter I: authoring loop -------------------- */}
      <Chapter
        label="I. The authoring loop"
        title="Author, run, and inspect, in one place."
      >
        <Paragraph>
          The point of an IDE is the loop between thought and result. You think
          of a field, the editor knows it. You hold a variable, the editor knows
          its shape. You run an operation, the response settles inline, right
          next to the query that produced it. Nitro keeps the schema close
          enough that the path from idea to verified result is short enough to
          feel native, not negotiated.
        </Paragraph>
        <Paragraph>
          Schema-aware completion is the foundation, but the rest of the
          surface, the variables editor, the response pane, the connection
          chooser, all live in the same window, on the same workspace. Nothing
          context switches. Below, an animated reading of the loop itself.
        </Paragraph>
        <figure>
          <FigureFrame>
            <NitroCompose />
          </FigureFrame>
          <FigureCaption>Fig. 01 / the authoring loop</FigureCaption>
        </figure>
      </Chapter>

      {/* -------------------- Chapter II: auth -------------------- */}
      <Chapter
        label="II. Sign in the way your API expects"
        title="OAuth 2, bearer, basic, per connection."
      >
        <Paragraph>
          Nitro speaks the auth flows your services already use. OAuth 2 handles
          human sign-in. Bearer tokens cover service accounts and
          machine-to-machine connections. Basic auth keeps the legacy endpoint
          reachable for the afternoon when you have to debug it. Each is
          configured per connection, scoped to a single workspace, never smeared
          across the whole IDE.
        </Paragraph>
        <Paragraph>
          The chooser is not a setting buried in a preference pane, it is a
          per-connection field. You bring up a connection, you pick the method
          the API on the other end expects, you save it next to the environment.
          The next time anyone in the workspace opens that connection, the same
          choice is already there.
        </Paragraph>
        <figure>
          <AuthFigure />
          <FigureCaption>Fig. 02 / per-connection auth</FigureCaption>
        </figure>
      </Chapter>

      {/* -------------------- Chapter III: workspaces -------------------- */}
      <Chapter
        label="III. One workspace per team, not per laptop"
        title="Organizations, workspaces, and the people inside them."
      >
        <Paragraph>
          A workspace is the unit Nitro takes seriously. Operations,
          environments, connection settings, and members all live with the
          workspace, not the device. Group your APIs under a shared
          organization, invite the people who own them, and a new teammate is
          one invite away from running production queries the same way you do.
        </Paragraph>
        <Paragraph>
          Switching between workspaces is one click, no re-config, no re-export,
          no carrying credentials around in a Notion page. The Checkout
          workspace looks like the Checkout workspace whether you open it on the
          laptop at the desk or the browser on the borrowed machine in the
          conference room.
        </Paragraph>
        <figure>
          <WorkspaceFigure />
          <FigureCaption>Fig. 03 / org, workspace, API</FigureCaption>
        </figure>
      </Chapter>

      {/* -------------------- Chapter IV: document sync -------------------- */}
      <Chapter
        label="IV. Your documents follow you, every device"
        title="Cross-device document sync, signed in once."
      >
        <Paragraph>
          Open Nitro on your laptop. Open Nitro on your desktop. Open Nitro in
          the browser on a borrowed machine. Your queries are already there.
          Documents stay in sync across every signed-in device and across the
          teammates who share the workspace with you, so the operation you
          half-finished at lunch is the one you finish in the afternoon.
        </Paragraph>
        <Paragraph>
          The figure below shows a single signed-in account across four devices,
          anchored to a single thin rail, the same rail this article itself
          hangs from in the margin. The shape is intentional. Sync is a quiet
          feature, the kind you only notice when it is missing.
        </Paragraph>
        <figure>
          <SyncFigure />
          <FigureCaption>Fig. 04 / one account, every device</FigureCaption>
        </figure>
        <PullQuote>
          Open Nitro on a borrowed machine, your queries are already there.
        </PullQuote>
      </Chapter>

      {/* -------------------- Chapter V: PWA + themes -------------------- */}
      <Chapter
        label="V. A real app, without a real installer"
        title="Install from the browser, run like a desktop app."
      >
        <Paragraph>
          Nitro runs as a Progressive Web App, so any modern browser can promote
          it to a standalone window with its own icon and offline cache. No
          installer, no admin privileges, no IT ticket. The same Nitro on the
          web becomes the Nitro on your desktop. Updates roll out the moment
          they ship, because there is nothing to re-download.
        </Paragraph>
        <figure>
          <InstallFigure />
          <FigureCaption>
            Fig. 05 / installs anywhere a modern browser runs
          </FigureCaption>
        </figure>
        <Paragraph>
          The window dresses itself for the room. Hand-tuned dark for the
          pairing session, light for the projector, or system to follow the OS
          automatically. Every surface, from the editor to the response pane, is
          tuned for both modes, so nothing about the IDE fights the screen it is
          on.
        </Paragraph>
        <figure>
          <ThemesFigure />
          <FigureCaption>Fig. 05b / dark, light, system</FigureCaption>
        </figure>
      </Chapter>

      {/* -------------------- Chapter VI: multipart upload -------------------- */}
      <Chapter
        label="VI. Multipart uploads, exactly to spec"
        title="Files travel alongside your variables."
      >
        <Paragraph>
          Send files alongside your variables without leaving the editor. Nitro
          builds the multipart request per the GraphQL multipart spec, so the
          same payload your Hot Chocolate server accepts in production is the
          one the IDE sends from your machine. Drag a file into a variable, the
          preview shows up inline, the request goes out as a well-formed
          multipart body, with operations, map, and the file parts in the places
          the spec asks for them.
        </Paragraph>
        <figure>
          <UploadFigure />
          <FigureCaption>
            Fig. 06 / jaydenseric/graphql-multipart-request-spec compatible
          </FigureCaption>
        </figure>
      </Chapter>

      {/* -------------------- Editor's note: where the line is -------------------- */}
      <section className="py-20">
        <hr className="border-cc-card-border mb-12 border-t" aria-hidden />
        <ChapterLabel>Editor&apos;s note</ChapterLabel>
        <h2 className="font-heading text-h2 text-cc-heading mt-2 max-w-2xl font-semibold tracking-tight">
          Where the line is.
        </h2>
        <div className="mt-8 flex flex-col gap-6">
          <Paragraph>
            What ships in the IDE is authoring, workspaces, document sync,
            cross-platform install, and a built-in GraphQL endpoint UI that runs
            wherever you point it. That is enough to make a team productive on
            day one, with or without anything else.
          </Paragraph>
          <Paragraph>
            Operational telemetry, the dashboards and the traces, requires
            configuring Nitro telemetry on the server side. It is not magic, it
            is opt-in. The IDE is useful with or without it. We would rather
            state the line than blur it.
          </Paragraph>
        </div>
      </section>

      {/* -------------------- Colophon -------------------- */}
      <footer className="py-16">
        <hr className="border-cc-card-border mb-10 border-t" aria-hidden />
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.18em] uppercase">
          Filed under: Nitro, GraphQL IDE, Workspaces / ChilliCream
        </p>
        <p className="text-cc-ink-dim mt-6 font-mono text-[0.72rem] tracking-tight">
          <a
            href="https://nitro.chillicream.com"
            className="text-cc-accent hover:text-cc-accent-hover underline-offset-4 hover:underline"
          >
            Launch Nitro
          </a>
          <span className="mx-3 opacity-50">·</span>
          <Link
            href="/docs/nitro"
            className="text-cc-accent hover:text-cc-accent-hover underline-offset-4 hover:underline"
          >
            Read the Docs
          </Link>
        </p>
      </footer>
    </article>
  );
}
