import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Nitro IDE, Built for Teams",
  description:
    "Nitro is the collaborative GraphQL IDE for teams: shared workspaces, document sync, OAuth 2 auth, multipart upload, PWA install, light or dark themes.",
  keywords: [
    "Nitro IDE",
    "Banana Cake Pop",
    "GraphQL IDE",
    "collaborative GraphQL",
    "GraphQL workspaces",
    "GraphQL OAuth 2",
    "GraphQL file upload",
    "PWA GraphQL client",
    "ChilliCream",
    "Hot Chocolate",
  ],
  openGraph: {
    title: "Nitro IDE, Built for Teams",
    description:
      "Collaborative GraphQL IDE: shared workspaces, document sync, OAuth 2, multipart file upload, PWA install, light and dark themes, in your browser or on Mac, Windows, Linux.",
  },
  robots: { index: false, follow: false },
};

/* ---------------------------------------------------------------------------
   Stance: Connected Workspace. The page reads like a team room: a confident
   hero, a "Built for teams" trio, a cross-platform availability strip, then the
   remaining capability surfaces (auth flows, PWA, themes, file upload), with a
   single embedded NitroCompose as proof of the IDE. Teal is the signature ink,
   the brand cyan/violet/coral spectrum appears at most once (the team band).
--------------------------------------------------------------------------- */

export default function EcosystemPreviewV2Page() {
  return (
    <article className="text-cc-ink">
      <Hero />
      <BuiltForTeams />
      <AnywhereStrip />
      <ProofOfIde />
      <AuthFlows />
      <PwaAndThemes />
      <FileUpload />
      <ClosingCta />
    </article>
  );
}

/* ===========================================================================
   HERO. Team-first headline, dual CTA, a small "presence" widget showing
   teammates in the same workspace as inline SVG.
=========================================================================== */
function Hero() {
  return (
    <header className="relative pt-6 sm:pt-10">
      <div className="text-cc-nav-label flex items-center gap-4 font-mono text-[0.7rem] tracking-[0.22em] uppercase">
        <span>Connected Workspace</span>
        <span className="bg-cc-card-border h-px flex-1" />
        <span>Nitro</span>
      </div>

      <div className="mt-10 grid items-end gap-12 lg:grid-cols-[1.15fr_0.85fr]">
        <div>
          <h1 className="font-heading text-cc-heading max-w-3xl text-[clamp(2.75rem,7.5vw,6rem)] leading-[0.98] font-bold tracking-[-0.02em] text-balance">
            One IDE.
            <br />
            Your whole team.
          </h1>

          <p className="lead text-cc-prose mt-8 max-w-2xl">
            Nitro is the GraphQL IDE the rest of ChilliCream is named after. It
            is built for the way teams actually work: shared workspaces, the
            same documents on every device, authentication that survives a
            staging swap, and a tab that follows you from the browser to a
            native window.
          </p>

          <div className="mt-10 flex flex-wrap items-center gap-3">
            <SolidButton href="https://nitro.chillicream.com">
              Launch Nitro
            </SolidButton>
            <OutlineButton href="/docs/nitro">Read the docs</OutlineButton>
          </div>

          <ul className="text-cc-ink-dim mt-8 flex flex-wrap items-center gap-x-5 gap-y-2 font-mono text-xs">
            <KeyFact>Workspaces with roles</KeyFact>
            <KeyFact>Sync across devices</KeyFact>
            <KeyFact>Browser, Mac, Windows, Linux</KeyFact>
          </ul>
        </div>

        <PresenceCard />
      </div>
    </header>
  );
}

function KeyFact({ children }: { readonly children: ReactNode }) {
  return (
    <li className="inline-flex items-center gap-2">
      <span className="text-cc-accent">
        <CheckIcon />
      </span>
      <span className="tracking-wide uppercase">{children}</span>
    </li>
  );
}

/* PresenceCard. Inline SVG and divs illustrating "your team, in this workspace"
   without inventing a screenshot. Pure tokens, no PNGs. */
function PresenceCard() {
  const teammates: ReadonlyArray<{
    readonly initials: string;
    readonly name: string;
    readonly status: string;
    readonly tint: string;
  }> = [
    {
      initials: "RS",
      name: "Rafael",
      status: "Editing Checkout.gql",
      tint: "#5eead4",
    },
    {
      initials: "AM",
      name: "Aiyana",
      status: "Viewing schema",
      tint: "#2dd4bf",
    },
    { initials: "JK", name: "Joon", status: "Idle", tint: "#0d9488" },
  ];

  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-5 backdrop-blur-sm">
      <div className="text-cc-nav-label flex items-center justify-between font-mono text-[0.65rem] tracking-[0.22em] uppercase">
        <span>Workspace</span>
        <span>acme / payments</span>
      </div>

      <div className="mt-5 flex items-center gap-2">
        {teammates.map((t) => (
          <span
            key={t.initials}
            className="text-cc-surface ring-cc-surface inline-flex h-9 w-9 items-center justify-center rounded-full text-xs font-semibold ring-2"
            style={{ background: t.tint }}
          >
            {t.initials}
          </span>
        ))}
        <span className="text-cc-ink-dim ml-2 font-mono text-xs">
          +4 online
        </span>
      </div>

      <ul className="mt-6 space-y-3">
        {teammates.map((t) => (
          <li
            key={t.name}
            className="border-cc-card-border/60 flex items-center gap-3 border-b pb-3 last:border-b-0 last:pb-0"
          >
            <span
              className="h-2 w-2 rounded-full"
              style={{ background: t.tint }}
              aria-hidden
            />
            <span className="text-cc-ink text-sm font-medium">{t.name}</span>
            <span className="text-cc-ink-dim ml-auto font-mono text-[0.7rem]">
              {t.status}
            </span>
          </li>
        ))}
      </ul>

      <div className="text-cc-ink-dim mt-5 flex items-center justify-between font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        <span>Document sync</span>
        <span className="text-cc-accent inline-flex items-center gap-1">
          <span className="bg-cc-accent inline-block h-1.5 w-1.5 animate-pulse rounded-full" />
          Live
        </span>
      </div>
    </div>
  );
}

/* ===========================================================================
   BUILT FOR TEAMS. The 3-card band, the spine of the Connected Workspace
   stance. The brand spectrum (cyan / violet / coral) shows up here, once.
=========================================================================== */
function BuiltForTeams() {
  return (
    <section className="mt-28 sm:mt-36">
      <SectionEyebrow label="Built for teams" />
      <h2 className="font-heading text-cc-heading text-h2 mt-4 max-w-3xl text-balance">
        The IDE remembers the team, not just the tab.
      </h2>
      <p className="text-cc-prose text-lead mt-5 max-w-2xl">
        Nitro keeps your GraphQL work in a place you share with the people you
        ship with. The same documents, the same auth, the same conventions, on
        whichever machine you opened today.
      </p>

      <div className="mt-12 grid gap-6 md:grid-cols-3">
        <TeamCard
          accent="#16b9e4"
          title="Organization workspaces"
          body="Group your APIs by team, project or environment. Invite colleagues into a workspace and they see exactly the documents, variables and connections you set up, with their own login."
        />
        <TeamCard
          accent="#7c92c6"
          title="Document sync across devices"
          body="Sign in on your laptop, your desktop, a browser at a conference. The same query collection, the same drafts, the same history follow you. Edits land on the other devices without a manual export."
        />
        <TeamCard
          accent="#f0786a"
          title="Roles and access"
          body="Decide who can edit a workspace, who can only read it, and who can manage members. Sensitive connections stay where they belong; review-only seats stay review-only."
        />
      </div>
    </section>
  );
}

function TeamCard({
  accent,
  title,
  body,
}: {
  readonly accent: string;
  readonly title: string;
  readonly body: string;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative overflow-hidden rounded-2xl border p-6 transition-colors">
      <span
        className="absolute inset-x-0 top-0 h-px"
        style={{
          background: `linear-gradient(90deg, transparent, ${accent}, transparent)`,
        }}
        aria-hidden
      />
      <span
        className="inline-flex h-9 w-9 items-center justify-center rounded-lg"
        style={{ background: `${accent}1a`, color: accent }}
      >
        <TeamGlyph accent={accent} />
      </span>
      <h3 className="text-cc-heading font-heading text-h5 mt-5">{title}</h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed">{body}</p>
    </div>
  );
}

function TeamGlyph({ accent }: { readonly accent: string }) {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 20 20"
      fill="none"
      aria-hidden
      stroke={accent}
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <circle cx="7" cy="7.5" r="2.5" />
      <circle cx="13.5" cy="8.5" r="2" />
      <path d="M2.5 16c0.6-2.4 2.5-3.8 4.5-3.8s3.9 1.4 4.5 3.8" />
      <path d="M12 14.5c0.7-1.4 2-2.2 3.3-2.2 1 0 1.8 0.4 2.2 0.9" />
    </svg>
  );
}

/* ===========================================================================
   ANYWHERE STRIP. "Your IDE, anywhere": browser plus native chips, with a
   monochrome chrome bar visual.
=========================================================================== */
function AnywhereStrip() {
  const platforms: ReadonlyArray<{
    readonly label: string;
    readonly hint: string;
    readonly glyph: ReactNode;
  }> = [
    {
      label: "Browser",
      hint: "Runs at nitro.chillicream.com",
      glyph: <BrowserGlyph />,
    },
    { label: "macOS", hint: "DMG installer", glyph: <AppleGlyph /> },
    {
      label: "Windows",
      hint: "x64 installer",
      glyph: <WindowsGlyph />,
    },
    { label: "Linux", hint: "AppImage and Snap", glyph: <LinuxGlyph /> },
  ];

  return (
    <section className="mt-28 sm:mt-36">
      <SectionEyebrow label="Your IDE, anywhere" />
      <div className="mt-4 flex flex-wrap items-end justify-between gap-6">
        <h2 className="font-heading text-cc-heading text-h2 max-w-2xl text-balance">
          Same workspace, four front doors.
        </h2>
        <p className="text-cc-ink-dim max-w-md text-sm">
          Use the web build for a five-second start. Use the desktop builds when
          you want a window that owns the dock, native shortcuts and an offline
          cache.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg mt-10 overflow-hidden rounded-2xl border">
        <div className="border-cc-card-border/70 flex items-center gap-1.5 border-b px-4 py-3">
          <span className="bg-cc-ink-dim/40 h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-ink-dim/40 h-2.5 w-2.5 rounded-full" />
          <span className="bg-cc-ink-dim/40 h-2.5 w-2.5 rounded-full" />
          <span className="text-cc-ink-dim ml-4 font-mono text-[0.7rem]">
            nitro.chillicream.com
          </span>
        </div>
        <ul className="grid gap-px sm:grid-cols-2 lg:grid-cols-4">
          {platforms.map((p) => (
            <li
              key={p.label}
              className="bg-cc-surface flex items-center gap-4 px-5 py-6"
            >
              <span className="text-cc-accent border-cc-card-border bg-cc-card-bg inline-flex h-10 w-10 items-center justify-center rounded-lg border">
                {p.glyph}
              </span>
              <div>
                <p className="text-cc-heading text-sm font-semibold">
                  {p.label}
                </p>
                <p className="text-cc-ink-dim mt-0.5 font-mono text-[0.7rem]">
                  {p.hint}
                </p>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

function BrowserGlyph() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 20 20"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <circle cx="10" cy="10" r="7.5" />
      <path d="M2.5 10h15" />
      <path d="M10 2.5c2.2 2.4 3.3 4.9 3.3 7.5s-1.1 5.1-3.3 7.5c-2.2-2.4-3.3-4.9-3.3-7.5S7.8 4.9 10 2.5z" />
    </svg>
  );
}

function AppleGlyph() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 20 20"
      fill="currentColor"
      aria-hidden
    >
      <path d="M14.4 10.7c0-2 1.6-2.9 1.7-3-0.9-1.4-2.4-1.6-2.9-1.6-1.2-0.1-2.4 0.7-3 0.7-0.6 0-1.6-0.7-2.6-0.7-1.3 0-2.6 0.8-3.2 2-1.4 2.4-0.4 6 1 8 0.7 0.9 1.5 2 2.6 2 1 0 1.4-0.7 2.6-0.7s1.5 0.7 2.6 0.7c1.1 0 1.8-1 2.5-1.9 0.8-1.1 1.1-2.2 1.1-2.3-0.1 0-2.4-0.9-2.4-3.2zM12.6 5c0.5-0.6 0.9-1.5 0.8-2.4-0.8 0-1.7 0.5-2.2 1.2-0.5 0.6-0.9 1.5-0.8 2.3 0.9 0.1 1.7-0.5 2.2-1.1z" />
    </svg>
  );
}

function WindowsGlyph() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 20 20"
      fill="currentColor"
      aria-hidden
    >
      <path d="M2 4.2 9.3 3.1V9.5H2V4.2z" />
      <path d="M10.2 3 18 1.8V9.5h-7.8V3z" />
      <path d="M2 10.5h7.3V17L2 15.8v-5.3z" />
      <path d="M10.2 10.5H18V18.2L10.2 17v-6.5z" />
    </svg>
  );
}

function LinuxGlyph() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 20 20"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden
    >
      <path d="M10 2.5c-2.2 0-3.5 1.8-3.5 4.5 0 1.5 0.6 2.7 1.4 3.7-1.3 0.8-3 2.4-3 4.5 0 2 1.7 2.3 5.1 2.3 3.4 0 5.1-0.3 5.1-2.3 0-2.1-1.7-3.7-3-4.5 0.8-1 1.4-2.2 1.4-3.7 0-2.7-1.3-4.5-3.5-4.5z" />
      <circle cx="8.6" cy="6.8" r="0.6" fill="currentColor" />
      <circle cx="11.4" cy="6.8" r="0.6" fill="currentColor" />
    </svg>
  );
}

/* ===========================================================================
   PROOF OF IDE. The single embedded Nitro animation, framed exactly as
   instructed: max-w-5xl, rounded-xl, bordered card, overflow-hidden.
=========================================================================== */
function ProofOfIde() {
  return (
    <section className="mt-28 sm:mt-36">
      <SectionEyebrow label="In the editor" />
      <div className="mt-4 flex flex-wrap items-end justify-between gap-6">
        <h2 className="font-heading text-cc-heading text-h2 max-w-2xl text-balance">
          A schema-aware editor your team will actually open.
        </h2>
        <p className="text-cc-ink-dim max-w-md text-sm">
          Autocomplete grounded in your live schema, inline validation, variable
          templates, history that survives a reload. Shared by the workspace,
          not your local disk.
        </p>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg mx-auto mt-10 max-w-5xl overflow-hidden rounded-xl border">
        <NitroCompose />
      </div>

      <p className="text-cc-ink-dim mx-auto mt-4 max-w-5xl font-mono text-[0.7rem] tracking-[0.18em] uppercase">
        Looping product capture, no audio
      </p>
    </section>
  );
}

/* ===========================================================================
   AUTH FLOWS. Basic, bearer, OAuth 2 as three honest entries, with a small
   inline diagram of an OAuth round trip in monochrome.
=========================================================================== */
function AuthFlows() {
  return (
    <section className="mt-28 sm:mt-36">
      <SectionEyebrow label="Authentication" />
      <div className="mt-4 grid gap-12 lg:grid-cols-[1fr_1fr] lg:gap-16">
        <div>
          <h2 className="font-heading text-cc-heading text-h2 max-w-xl text-balance">
            Authentication that matches your real endpoint.
          </h2>
          <p className="text-cc-prose text-lead mt-5 max-w-xl">
            Pick the flow the API actually uses. Nitro stores credentials per
            connection in the workspace, so a teammate who joins a workspace
            inherits the connection shape without inheriting your tokens.
          </p>

          <ul className="mt-8 space-y-4">
            <AuthRow
              name="Basic"
              detail="Username and password header. Useful for internal services and quick local probes."
            />
            <AuthRow
              name="Bearer"
              detail="Static or pasted token in the Authorization header. The token stays in your account, not in the document."
            />
            <AuthRow
              name="OAuth 2"
              detail="Authorization Code, Client Credentials, Implicit and Password flows. Nitro completes the dance and refreshes when the token expires."
            />
          </ul>
        </div>

        <OAuthDiagram />
      </div>
    </section>
  );
}

function AuthRow({
  name,
  detail,
}: {
  readonly name: string;
  readonly detail: string;
}) {
  return (
    <li className="border-cc-card-border/70 flex gap-5 border-t pt-4">
      <span className="text-cc-accent w-20 shrink-0 pt-1 font-mono text-xs tracking-[0.18em] uppercase">
        {name}
      </span>
      <p className="text-cc-ink-dim text-sm leading-relaxed">{detail}</p>
    </li>
  );
}

function OAuthDiagram() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-6">
      <div className="text-cc-nav-label flex items-center justify-between font-mono text-[0.65rem] tracking-[0.22em] uppercase">
        <span>OAuth 2 / Authorization Code</span>
        <span className="text-cc-accent">Stored in workspace</span>
      </div>

      <svg
        viewBox="0 0 360 220"
        className="mt-4 w-full"
        role="img"
        aria-label="Nitro completes the OAuth authorization code round trip and refreshes the token when it expires."
      >
        <defs>
          <marker
            id="cc-oauth-arrow"
            viewBox="0 0 10 10"
            refX="9"
            refY="5"
            markerWidth="6"
            markerHeight="6"
            orient="auto-start-reverse"
          >
            <path d="M0 0 L10 5 L0 10 z" fill="#5eead4" />
          </marker>
        </defs>

        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="11"
        >
          <Node x={20} y={20} label="Nitro" />
          <Node x={220} y={20} label="Authorize URL" />
          <Node x={20} y={140} label="Token endpoint" />
          <Node x={220} y={140} label="Your API" />

          <Edge x1={92} y1={40} x2={220} y2={40} label="1. open" />
          <Edge x1={250} y1={68} x2={92} y2={140} label="2. code" />
          <Edge x1={92} y1={160} x2={220} y2={160} label="3. token" />
          <Edge
            x1={250}
            y1={140}
            x2={250}
            y2={68}
            label="4. refresh"
            vertical
          />
        </g>
      </svg>

      <p className="text-cc-ink-dim mt-2 text-xs">
        Nitro completes steps two through four. The workspace remembers the
        client config, not the token.
      </p>
    </div>
  );
}

function Node({
  x,
  y,
  label,
}: {
  readonly x: number;
  readonly y: number;
  readonly label: string;
}) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={92}
        height={40}
        rx={8}
        fill="rgba(94,234,212,0.06)"
        stroke="rgba(94,234,212,0.5)"
      />
      <text
        x={x + 46}
        y={y + 24}
        textAnchor="middle"
        fill="#5eead4"
        fontWeight={600}
      >
        {label}
      </text>
    </g>
  );
}

function Edge({
  x1,
  y1,
  x2,
  y2,
  label,
  vertical,
}: {
  readonly x1: number;
  readonly y1: number;
  readonly x2: number;
  readonly y2: number;
  readonly label: string;
  readonly vertical?: boolean;
}) {
  const midX = (x1 + x2) / 2;
  const midY = (y1 + y2) / 2;
  return (
    <g>
      <line
        x1={x1}
        y1={y1}
        x2={x2}
        y2={y2}
        stroke="#5eead4"
        strokeWidth={1.2}
        strokeDasharray="4 4"
        markerEnd="url(#cc-oauth-arrow)"
      />
      <text
        x={vertical ? midX + 8 : midX}
        y={vertical ? midY : midY - 6}
        textAnchor={vertical ? "start" : "middle"}
        fill="#9aa3b2"
      >
        {label}
      </text>
    </g>
  );
}

/* ===========================================================================
   PWA AND THEMES. Paired columns: install-anywhere and the theme picker.
=========================================================================== */
function PwaAndThemes() {
  return (
    <section className="mt-28 sm:mt-36">
      <SectionEyebrow label="Make it yours" />
      <div className="mt-10 grid gap-6 md:grid-cols-2">
        <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-7">
          <h3 className="font-heading text-cc-heading text-h4">
            Install as a PWA, no admin rights
          </h3>
          <p className="text-cc-ink-dim mt-4 text-sm leading-relaxed">
            Open Nitro in any modern browser and add it as a Progressive Web
            App. It gets its own window, its own dock icon, and an offline cache
            for the documents you opened recently. No installer, no IT ticket.
          </p>

          <ul className="text-cc-ink mt-6 space-y-2 text-sm">
            <Bullet>Own window and shortcuts</Bullet>
            <Bullet>Offline access to recent documents</Bullet>
            <Bullet>Updates ship with the next browser visit</Bullet>
          </ul>

          <PwaIllustration />
        </div>

        <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-7">
          <h3 className="font-heading text-cc-heading text-h4">
            Themes that respect the room
          </h3>
          <p className="text-cc-ink-dim mt-4 text-sm leading-relaxed">
            Pick light, pick dark, or let Nitro follow the operating system so
            the IDE matches the rest of your screen at sunset. Themes apply per
            device, the workspace itself stays neutral.
          </p>

          <div className="mt-6 grid grid-cols-3 gap-3">
            <ThemeSwatch label="Light" tone="light" />
            <ThemeSwatch label="Dark" tone="dark" />
            <ThemeSwatch label="System" tone="system" />
          </div>

          <p className="text-cc-ink-dim mt-6 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
            Editor, charts and chrome all switch together
          </p>
        </div>
      </div>
    </section>
  );
}

function Bullet({ children }: { readonly children: ReactNode }) {
  return (
    <li className="flex items-start gap-2">
      <span className="text-cc-accent mt-0.5">
        <CheckIcon />
      </span>
      <span>{children}</span>
    </li>
  );
}

function PwaIllustration() {
  return (
    <svg
      viewBox="0 0 320 140"
      className="mt-6 w-full"
      role="img"
      aria-label="Nitro running as a standalone window next to the browser."
    >
      <rect
        x={10}
        y={20}
        width={170}
        height={108}
        rx={10}
        fill="rgba(255,255,255,0.02)"
        stroke="rgba(255,255,255,0.12)"
      />
      <circle cx={22} cy={32} r={2.5} fill="rgba(255,255,255,0.3)" />
      <circle cx={30} cy={32} r={2.5} fill="rgba(255,255,255,0.3)" />
      <circle cx={38} cy={32} r={2.5} fill="rgba(255,255,255,0.3)" />
      <rect
        x={20}
        y={48}
        width={60}
        height={6}
        rx={2}
        fill="rgba(94,234,212,0.4)"
      />
      <rect
        x={20}
        y={62}
        width={140}
        height={4}
        rx={2}
        fill="rgba(255,255,255,0.1)"
      />
      <rect
        x={20}
        y={72}
        width={120}
        height={4}
        rx={2}
        fill="rgba(255,255,255,0.1)"
      />
      <rect
        x={20}
        y={82}
        width={130}
        height={4}
        rx={2}
        fill="rgba(255,255,255,0.1)"
      />

      <rect
        x={190}
        y={40}
        width={120}
        height={88}
        rx={10}
        fill="rgba(94,234,212,0.06)"
        stroke="rgba(94,234,212,0.45)"
      />
      <rect
        x={200}
        y={54}
        width={50}
        height={5}
        rx={2}
        fill="rgba(94,234,212,0.7)"
      />
      <rect
        x={200}
        y={66}
        width={90}
        height={4}
        rx={2}
        fill="rgba(255,255,255,0.18)"
      />
      <rect
        x={200}
        y={76}
        width={80}
        height={4}
        rx={2}
        fill="rgba(255,255,255,0.18)"
      />
      <rect
        x={200}
        y={86}
        width={84}
        height={4}
        rx={2}
        fill="rgba(255,255,255,0.18)"
      />
      <text
        x={250}
        y={120}
        textAnchor="middle"
        fill="#5eead4"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize={9}
      >
        Installed PWA
      </text>
    </svg>
  );
}

function ThemeSwatch({
  label,
  tone,
}: {
  readonly label: string;
  readonly tone: "light" | "dark" | "system";
}) {
  const gradient =
    tone === "light"
      ? "linear-gradient(135deg, #f4f4f5, #d4d4d8)"
      : tone === "dark"
        ? "linear-gradient(135deg, #0c1322, #1f2937)"
        : "linear-gradient(135deg, #f4f4f5 0 50%, #0c1322 50% 100%)";
  return (
    <div className="border-cc-card-border/80 overflow-hidden rounded-xl border">
      <div
        className="h-16 w-full"
        style={{ background: gradient }}
        aria-hidden
      />
      <p className="text-cc-ink-dim px-3 py-2 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
        {label}
      </p>
    </div>
  );
}

/* ===========================================================================
   FILE UPLOAD. The GraphQL multipart spec, framed for what it actually is.
=========================================================================== */
function FileUpload() {
  return (
    <section className="mt-28 sm:mt-36">
      <SectionEyebrow label="Multipart upload" />
      <div className="mt-4 grid gap-12 lg:grid-cols-[1.05fr_0.95fr] lg:gap-16">
        <div>
          <h2 className="font-heading text-cc-heading text-h2 max-w-xl text-balance">
            GraphQL file upload, the way the spec wrote it.
          </h2>
          <p className="text-cc-prose text-lead mt-5 max-w-xl">
            Nitro speaks the GraphQL multipart request specification, so you can
            attach files to a mutation from the IDE without writing the curl by
            hand. Drop a PDF onto a variable, pick the field it maps to, send.
          </p>
          <ul className="text-cc-ink mt-6 space-y-2 text-sm">
            <Bullet>Drop attachments straight onto variables</Bullet>
            <Bullet>Multiple files per request, ordered</Bullet>
            <Bullet>Headers, auth and operation are preserved</Bullet>
          </ul>
        </div>

        <div className="border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border">
          <div className="border-cc-card-border/70 flex items-center justify-between border-b px-4 py-3">
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.22em] uppercase">
              POST /graphql
            </span>
            <span className="text-cc-accent font-mono text-[0.65rem]">
              multipart/form-data
            </span>
          </div>
          <pre className="text-cc-ink overflow-x-auto px-5 py-5 font-mono text-[0.78rem] leading-relaxed">
            {`--boundary
Content-Disposition: form-data; name="operations"

{ "query": "mutation($f: Upload!) { upload(file: $f) { id } }",
  "variables": { "f": null } }

--boundary
Content-Disposition: form-data; name="map"

{ "0": ["variables.f"] }

--boundary
Content-Disposition: form-data; name="0"; filename="invoice.pdf"
Content-Type: application/pdf

<binary>`}
          </pre>
        </div>
      </div>
    </section>
  );
}

/* ===========================================================================
   CLOSING CTA. Confident invite, dual link, no fluff.
=========================================================================== */
function ClosingCta() {
  return (
    <section className="mt-32 mb-16 sm:mt-40">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-3xl border px-8 py-14 sm:px-14">
        <span
          aria-hidden
          className="pointer-events-none absolute -top-24 right-[-10%] h-72 w-72 rounded-full opacity-20 blur-3xl"
          style={{ background: "#5eead4" }}
        />
        <div className="relative grid items-center gap-10 lg:grid-cols-[1.2fr_0.8fr]">
          <div>
            <p className="text-cc-nav-label font-mono text-[0.7rem] tracking-[0.22em] uppercase">
              Get the team in
            </p>
            <h2 className="font-heading text-cc-heading text-h2 mt-4 max-w-2xl text-balance">
              Open a workspace. Invite three teammates. Ship before lunch.
            </h2>
            <p className="text-cc-prose text-lead mt-5 max-w-xl">
              Sign in, open a workspace, invite the team.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-3 lg:justify-end">
            <SolidButton href="https://nitro.chillicream.com">
              Open Nitro in your browser
            </SolidButton>
            <OutlineButton href="/pricing">See pricing</OutlineButton>
          </div>
        </div>
      </div>
    </section>
  );
}

/* ===========================================================================
   Shared bits.
=========================================================================== */
function SectionEyebrow({ label }: { readonly label: string }) {
  return (
    <div className="text-cc-nav-label flex items-center gap-4 font-mono text-[0.7rem] tracking-[0.22em] uppercase">
      <span className="text-cc-accent">{label}</span>
      <span className="bg-cc-card-border h-px flex-1" />
    </div>
  );
}
