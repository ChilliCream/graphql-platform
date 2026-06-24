import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { NitroCompose } from "@/src/nitro";

export const metadata: Metadata = {
  title: "Nitro IDE | Feature Library for GraphQL Workspaces",
  description:
    "A dense feature library for the Nitro GraphQL IDE: OAuth 2 auth, organization workspaces, synced documents, PWA install, themes, multipart file upload.",
  keywords: [
    "Nitro GraphQL IDE",
    "Banana Cake Pop",
    "GraphQL workspaces",
    "GraphQL OAuth 2",
    "GraphQL multipart upload",
    "document synchronization",
    "PWA GraphQL IDE",
    "cross platform GraphQL client",
    "GraphQL collaboration",
    "ChilliCream Nitro",
  ],
  openGraph: {
    title: "Nitro IDE, Feature by Feature",
    description:
      "A scannable feature library for the Nitro GraphQL IDE: auth flows, workspaces, sync, PWA, themes, and multipart upload across browser, Mac, Windows, and Linux.",
  },
  robots: { index: false, follow: false },
};

/* ----------------------------------------------------------------------------
   Scene palette. Teal is the signature, violet anchors the supporting cells,
   coral is rationed exclusively to the OAuth 2 callout and the multipart
   marker so color carries meaning, not decoration.
---------------------------------------------------------------------------- */
const TEAL = "#5eead4";
const VIOLET = "#7c92c6";
const CORAL = "#f0786a";
const GREEN = "#34d399";

/* ============================================================================
   Primitives
============================================================================ */

interface EyebrowProps {
  readonly tag: string;
  readonly children: ReactNode;
  readonly color?: string;
}

function Eyebrow({ tag, children, color = TEAL }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label flex items-center gap-2 font-mono text-[0.7rem] tracking-[0.18em] uppercase">
      <span style={{ color }}>{tag}</span>
      <span className="bg-cc-card-border h-px w-6" aria-hidden />
      {children}
    </p>
  );
}

interface TileProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly glow?: string;
}

function Tile({ children, className = "", glow }: TileProps) {
  return (
    <div
      className={`border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border backdrop-blur ${className}`}
    >
      {glow && (
        <div
          className="pointer-events-none absolute -top-20 -right-20 h-44 w-44 rounded-full opacity-40 blur-3xl"
          style={{ backgroundColor: `${glow}55` }}
          aria-hidden
        />
      )}
      {children}
    </div>
  );
}

interface ChromeBarProps {
  readonly route: string;
  readonly right?: ReactNode;
}

function ChromeBar({ route, right }: ChromeBarProps) {
  return (
    <div className="border-cc-card-border bg-cc-code-header flex items-center gap-3 border-b px-4 py-2.5">
      <div className="flex gap-1.5" aria-hidden>
        <span className="bg-cc-danger/70 h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-warning/70 h-2.5 w-2.5 rounded-full" />
        <span className="bg-cc-success/70 h-2.5 w-2.5 rounded-full" />
      </div>
      <span className="text-cc-ink-dim truncate font-mono text-[0.66rem]">
        {route}
      </span>
      {right && <div className="ml-auto flex items-center gap-2">{right}</div>}
    </div>
  );
}

interface FeatureTagProps {
  readonly children: ReactNode;
  readonly color?: string;
}

function FeatureTag({ children, color = TEAL }: FeatureTagProps) {
  return (
    <span
      className="inline-flex items-center gap-1 rounded-full border px-2 py-0.5 font-mono text-[0.55rem] tracking-[0.1em] uppercase"
      style={{
        color,
        borderColor: `${color}55`,
        backgroundColor: `${color}14`,
      }}
    >
      {children}
    </span>
  );
}

/* ============================================================================
   HERO
============================================================================ */

function Hero() {
  return (
    <header className="border-cc-card-border bg-cc-surface/40 relative overflow-hidden rounded-3xl border px-6 py-12 backdrop-blur sm:px-12 sm:py-16">
      <div
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(70% 80% at 12% -10%, rgba(94,234,212,0.18), transparent 60%), radial-gradient(55% 60% at 105% 110%, rgba(124,146,198,0.14), transparent 60%)",
        }}
        aria-hidden
      />
      <div
        className="pointer-events-none absolute inset-0 -z-10 opacity-[0.05]"
        style={{
          backgroundImage:
            "linear-gradient(rgba(245,241,234,1) 1px, transparent 1px), linear-gradient(90deg, rgba(245,241,234,1) 1px, transparent 1px)",
          backgroundSize: "46px 46px",
          maskImage:
            "radial-gradient(75% 70% at 30% 30%, #000 30%, transparent 80%)",
        }}
        aria-hidden
      />

      <div className="grid items-center gap-12 lg:grid-cols-[1fr_0.9fr]">
        <div>
          <Eyebrow tag="nitro ide">the GraphQL workspace</Eyebrow>
          <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 mt-5 tracking-tight">
            One IDE, every <span style={{ color: TEAL }}>feature</span> your
            team needs.
          </h1>
          <p className="lead text-cc-prose !font-body !text-lead mt-6 max-w-xl !font-normal">
            Auth flows, shared workspaces, synced documents, PWA install,
            themes, and multipart file upload. A dense, opinionated GraphQL IDE
            that runs in the browser and on every desktop your team uses.
          </p>

          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href="https://nitro.chillicream.com">
              Launch Nitro
            </SolidButton>
            <OutlineButton href="/docs/nitro">Read the Docs</OutlineButton>
          </div>

          <div className="mt-8 flex flex-wrap items-center gap-x-5 gap-y-2">
            <PlatformChip label="Web" />
            <PlatformChip label="macOS" />
            <PlatformChip label="Windows" />
            <PlatformChip label="Linux" />
            <PlatformChip label="PWA" accent />
          </div>
        </div>

        <HeroStack />
      </div>
    </header>
  );
}

interface PlatformChipProps {
  readonly label: string;
  readonly accent?: boolean;
}

function PlatformChip({ label, accent = false }: PlatformChipProps) {
  return (
    <span
      className="text-cc-ink-dim border-cc-card-border inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.62rem] tracking-[0.1em] uppercase"
      style={accent ? { color: TEAL, borderColor: `${TEAL}55` } : undefined}
    >
      <span
        className="h-1.5 w-1.5 rounded-full"
        style={{ backgroundColor: accent ? TEAL : VIOLET }}
        aria-hidden
      />
      {label}
    </span>
  );
}

function HeroStack() {
  return (
    <div className="relative">
      <div
        className="pointer-events-none absolute -inset-6 -z-10 rounded-[2.5rem] opacity-70 blur-3xl"
        style={{
          background:
            "radial-gradient(55% 55% at 60% 25%, rgba(94,234,212,0.22), transparent 70%), radial-gradient(50% 50% at 30% 90%, rgba(124,146,198,0.16), transparent 70%)",
        }}
        aria-hidden
      />
      <div className="border-cc-card-border bg-cc-surface/95 relative z-10 rounded-2xl border shadow-[0_30px_70px_-30px_rgba(0,0,0,0.8)] backdrop-blur">
        <ChromeBar
          route="nitro › workspace › checkout-api"
          right={<FeatureTag>connected</FeatureTag>}
        />
        <div className="grid grid-cols-[8.5rem_1fr] text-[0.66rem]">
          <aside className="border-cc-card-border bg-cc-surface/60 space-y-1 border-r px-3 py-3 font-mono">
            <SidebarItem label="documents" />
            <SidebarSub label="checkout.graphql" active />
            <SidebarSub label="ship.graphql" />
            <SidebarSub label="receipt.graphql" />
            <SidebarItem label="schemas" />
            <SidebarSub label="orders" />
            <SidebarSub label="billing" />
          </aside>
          <div className="space-y-3 px-4 py-3 font-mono">
            <div className="flex items-center justify-between">
              <span className="text-cc-ink-dim">POST</span>
              <FeatureTag color={VIOLET}>OAuth 2</FeatureTag>
            </div>
            <pre className="text-cc-ink text-[0.66rem] leading-relaxed">
              <span className="text-cc-ink-dim">mutation</span>{" "}
              <span style={{ color: TEAL }}>uploadReceipt</span>({"\n"}
              {"  "}$file: <span style={{ color: VIOLET }}>Upload!</span>
              {"\n"}) {"{"}
              {"\n"}
              {"  "}
              <span style={{ color: TEAL }}>uploadReceipt</span>(file: $file){" "}
              {"{"}
              {"\n"}
              {"    "}id
              {"\n"}
              {"    "}url
              {"\n"}
              {"  "}
              {"}"}
              {"\n"}
              {"}"}
            </pre>
            <div className="border-cc-card-border/60 flex items-center justify-between border-t pt-2">
              <span className="text-cc-ink-dim text-[0.6rem]">
                receipt.png · 412 KB
              </span>
              <span style={{ color: GREEN }} className="text-[0.6rem]">
                synced
              </span>
            </div>
          </div>
        </div>
        <div className="border-cc-card-border bg-cc-surface/40 flex items-center justify-between border-t px-4 py-2 font-mono text-[0.58rem]">
          <span className="text-cc-ink-dim">acme · production</span>
          <span className="text-cc-ink-dim flex items-center gap-2">
            <span
              className="h-1.5 w-1.5 rounded-full"
              style={{ backgroundColor: GREEN }}
            />
            3 teammates here
          </span>
        </div>
      </div>
    </div>
  );
}

interface SidebarItemProps {
  readonly label: string;
}

function SidebarItem({ label }: SidebarItemProps) {
  return (
    <p className="text-cc-nav-label pt-1 text-[0.56rem] tracking-[0.14em] uppercase">
      {label}
    </p>
  );
}

interface SidebarSubProps {
  readonly label: string;
  readonly active?: boolean;
}

function SidebarSub({ label, active = false }: SidebarSubProps) {
  return (
    <p
      className={`truncate rounded px-1.5 py-0.5 text-[0.64rem] ${
        active
          ? "bg-cc-hover text-cc-heading"
          : "text-cc-ink-dim hover:bg-cc-hover"
      }`}
    >
      {label}
    </p>
  );
}

/* ============================================================================
   FEATURE LIBRARY, a dense 6 cell bento grid
============================================================================ */

function FeatureLibrary() {
  return (
    <section className="mt-8">
      <div className="mb-7 max-w-3xl">
        <Eyebrow tag="feature library">scannable, opinionated</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-4">
          Every cell is a feature that ships today.
        </h2>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          The bento below covers what Nitro actually does. No invented surface,
          no roadmap promises. Read it like a spec sheet and pick the cell that
          answers your question.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
        <div className="sm:col-span-3">
          <AuthTile />
        </div>
        <div className="sm:col-span-3">
          <OAuthDetailTile />
        </div>
        <div className="sm:col-span-2">
          <WorkspacesTile />
        </div>
        <div className="sm:col-span-2">
          <SyncTile />
        </div>
        <div className="sm:col-span-2">
          <PwaTile />
        </div>
        <div className="sm:col-span-3">
          <ThemesTile />
        </div>
        <div className="sm:col-span-3">
          <CrossPlatformTile />
        </div>
        <div className="sm:col-span-6">
          <UploadTile />
        </div>
      </div>
    </section>
  );
}

/* ---- Auth flows ---------------------------------------------------------- */

function AuthTile() {
  const flows: readonly { name: string; detail: string; color: string }[] = [
    { name: "basic", detail: "username + password", color: VIOLET },
    { name: "bearer", detail: "static token header", color: VIOLET },
    { name: "OAuth 2", detail: "authorization code flow", color: TEAL },
  ];
  return (
    <Tile className="flex h-full flex-col" glow={TEAL}>
      <ChromeBar route="nitro › auth" />
      <div className="flex flex-1 flex-col gap-4 p-5">
        <div>
          <Eyebrow tag="auth flows">three, ready to use</Eyebrow>
          <h3 className="font-heading text-h6 text-cc-heading mt-3">
            Authenticate the way the API expects.
          </h3>
        </div>
        <ul className="divide-cc-card-border/60 divide-y">
          {flows.map((f) => (
            <li
              key={f.name}
              className="grid grid-cols-[6rem_1fr_auto] items-center gap-3 py-2.5"
            >
              <FeatureTag color={f.color}>{f.name}</FeatureTag>
              <span className="text-cc-ink-dim font-mono text-[0.7rem]">
                {f.detail}
              </span>
              <span style={{ color: GREEN }} className="shrink-0">
                <CheckIcon size={14} />
              </span>
            </li>
          ))}
        </ul>
        <p className="text-caption text-cc-ink-dim mt-auto">
          Per request, per environment, per workspace. Switch without leaving
          the tab.
        </p>
      </div>
    </Tile>
  );
}

/* ---- OAuth 2 detail callout ---------------------------------------------- */

function OAuthDetailTile() {
  return (
    <Tile className="flex h-full flex-col" glow={TEAL}>
      <ChromeBar
        route="nitro › auth › oauth"
        right={<FeatureTag color={TEAL}>OAuth 2</FeatureTag>}
      />
      <div className="flex flex-1 flex-col gap-4 p-5">
        <div>
          <Eyebrow tag="oauth 2 in detail">authorization code</Eyebrow>
          <h3 className="font-heading text-h6 text-cc-heading mt-3">
            Real OAuth 2, in the IDE.
          </h3>
        </div>
        <ol className="space-y-2.5 font-mono text-[0.68rem]">
          <OAuthStep n={1} label="open authorize url in browser" />
          <OAuthStep n={2} label="receive code on redirect" />
          <OAuthStep n={3} label="exchange code for access + refresh token" />
          <OAuthStep n={4} label="attach bearer to GraphQL request" />
          <OAuthStep
            n={5}
            label="refresh the token with one click when it expires"
          />
        </ol>
        <p className="text-caption text-cc-ink-dim mt-auto">
          Client id, scopes, and endpoints live on the workspace. Tokens stay on
          your device.
        </p>
      </div>
    </Tile>
  );
}

interface OAuthStepProps {
  readonly n: number;
  readonly label: string;
}

function OAuthStep({ n, label }: OAuthStepProps) {
  return (
    <li className="flex items-center gap-3">
      <span
        className="border-cc-card-border text-cc-ink-dim inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full border font-mono text-[0.6rem] tabular-nums"
        style={{ color: TEAL, borderColor: `${TEAL}55` }}
      >
        {n}
      </span>
      <span className="text-cc-ink">{label}</span>
    </li>
  );
}

/* ---- Organization workspaces -------------------------------------------- */

function WorkspacesTile() {
  const members: readonly { initial: string; tone: string }[] = [
    { initial: "P", tone: TEAL },
    { initial: "M", tone: VIOLET },
    { initial: "J", tone: GREEN },
    { initial: "A", tone: VIOLET },
  ];
  return (
    <Tile className="flex h-full flex-col p-5" glow={VIOLET}>
      <Eyebrow tag="workspaces" color={VIOLET}>
        organize + share
      </Eyebrow>
      <h3 className="font-heading text-h6 text-cc-heading mt-3">
        Organization workspaces.
      </h3>
      <div className="mt-4 flex -space-x-2">
        {members.map((m) => (
          <span
            key={m.initial}
            className="border-cc-card-bg bg-cc-surface inline-flex h-7 w-7 items-center justify-center rounded-full border-2 font-mono text-[0.62rem]"
            style={{ color: m.tone }}
          >
            {m.initial}
          </span>
        ))}
        <span className="border-cc-card-bg bg-cc-surface text-cc-ink-dim inline-flex h-7 w-7 items-center justify-center rounded-full border-2 font-mono text-[0.6rem]">
          +6
        </span>
      </div>
      <ul className="mt-4 space-y-2 text-sm">
        <BulletRow text="Group GraphQL APIs by team" />
        <BulletRow text="Invite colleagues by email" />
        <BulletRow text="Share documents, schemas, environments" />
      </ul>
    </Tile>
  );
}

interface BulletRowProps {
  readonly text: string;
}

function BulletRow({ text }: BulletRowProps) {
  return (
    <li className="text-cc-ink flex items-start gap-2 text-[0.82rem] leading-relaxed">
      <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
        <CheckIcon size={13} />
      </span>
      <span>{text}</span>
    </li>
  );
}

/* ---- Document sync ------------------------------------------------------- */

function SyncTile() {
  return (
    <Tile className="flex h-full flex-col p-5" glow={TEAL}>
      <Eyebrow tag="document sync">device + team</Eyebrow>
      <h3 className="font-heading text-h6 text-cc-heading mt-3">
        Documents follow you.
      </h3>
      <div className="border-cc-card-border/60 mt-4 space-y-2 rounded-lg border bg-black/20 p-3 font-mono text-[0.66rem]">
        <SyncRow label="laptop" status="up to date" tone={GREEN} />
        <SyncRow label="desktop" status="up to date" tone={GREEN} />
        <SyncRow label="phone (PWA)" status="syncing" tone={TEAL} />
        <SyncRow label="teammate · maya" status="up to date" tone={GREEN} />
      </div>
      <p className="text-caption text-cc-ink-dim mt-auto pt-4">
        Save once. Open anywhere your account is signed in.
      </p>
    </Tile>
  );
}

interface SyncRowProps {
  readonly label: string;
  readonly status: string;
  readonly tone: string;
}

function SyncRow({ label, status, tone }: SyncRowProps) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-cc-ink-dim">{label}</span>
      <span className="flex items-center gap-2">
        <span
          className="h-1.5 w-1.5 rounded-full"
          style={{ backgroundColor: tone, boxShadow: `0 0 6px ${tone}aa` }}
        />
        <span style={{ color: tone }}>{status}</span>
      </span>
    </div>
  );
}

/* ---- PWA support --------------------------------------------------------- */

function PwaTile() {
  return (
    <Tile className="flex h-full flex-col p-5" glow={VIOLET}>
      <Eyebrow tag="pwa" color={VIOLET}>
        install from any browser
      </Eyebrow>
      <h3 className="font-heading text-h6 text-cc-heading mt-3">
        No admin password required.
      </h3>
      <div className="border-cc-card-border/60 mt-4 flex items-center gap-3 rounded-lg border bg-black/20 p-3">
        <div
          className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg"
          style={{
            background:
              "linear-gradient(135deg, rgba(94,234,212,0.25), rgba(124,146,198,0.25))",
          }}
        >
          <span className="font-heading text-cc-heading text-lg" aria-hidden>
            N
          </span>
        </div>
        <div className="min-w-0">
          <p className="text-cc-heading truncate font-mono text-[0.74rem]">
            Nitro
          </p>
          <p className="text-cc-ink-dim truncate font-mono text-[0.6rem]">
            nitro.chillicream.com
          </p>
        </div>
        <span
          className="ml-auto rounded-full border px-2.5 py-1 font-mono text-[0.58rem] tracking-[0.1em] uppercase"
          style={{ color: TEAL, borderColor: `${TEAL}55` }}
        >
          Install
        </span>
      </div>
      <p className="text-caption text-cc-ink-dim mt-auto pt-4">
        Use your favorite browser to install Nitro on any device, no
        administrative privileges needed.
      </p>
    </Tile>
  );
}

/* ---- Themes -------------------------------------------------------------- */

function ThemesTile() {
  return (
    <Tile className="flex h-full flex-col p-5" glow={VIOLET}>
      <Eyebrow tag="themes" color={VIOLET}>
        dark, light, or system
      </Eyebrow>
      <h3 className="font-heading text-h6 text-cc-heading mt-3">
        Beautiful themes, your call.
      </h3>
      <div className="mt-4 grid grid-cols-3 gap-3">
        <ThemeSwatch
          label="Dark"
          bg="linear-gradient(160deg, #0b0f1a, #14213a)"
          dot={TEAL}
        />
        <ThemeSwatch
          label="Light"
          bg="linear-gradient(160deg, #f5f1ea, #e2dccf)"
          dot="#14213a"
          dark
        />
        <ThemeSwatch
          label="System"
          bg="linear-gradient(160deg, #0b0f1a 0%, #0b0f1a 50%, #f5f1ea 50%, #f5f1ea 100%)"
          dot={VIOLET}
        />
      </div>
      <p className="text-caption text-cc-ink-dim mt-auto pt-4">
        Set one theme or let the OS flip Nitro at sundown.
      </p>
    </Tile>
  );
}

interface ThemeSwatchProps {
  readonly label: string;
  readonly bg: string;
  readonly dot: string;
  readonly dark?: boolean;
}

function ThemeSwatch({ label, bg, dot, dark = false }: ThemeSwatchProps) {
  return (
    <div className="border-cc-card-border/60 overflow-hidden rounded-lg border">
      <div className="relative h-16" style={{ background: bg }}>
        <span
          className="absolute right-2 bottom-2 h-2 w-2 rounded-full"
          style={{ backgroundColor: dot, boxShadow: `0 0 6px ${dot}aa` }}
        />
      </div>
      <p
        className={`px-2 py-1.5 font-mono text-[0.62rem] ${dark ? "text-cc-ink" : "text-cc-ink"}`}
      >
        {label}
      </p>
    </div>
  );
}

/* ---- Cross-platform availability ----------------------------------------- */

interface PlatformRow {
  readonly name: string;
  readonly detail: string;
  readonly icon: ReactNode;
}

function CrossPlatformTile() {
  const platforms: readonly PlatformRow[] = [
    {
      name: "Web",
      detail: "any modern browser",
      icon: <WebGlyph />,
    },
    {
      name: "macOS",
      detail: "Apple Silicon + Intel",
      icon: <AppleGlyph />,
    },
    {
      name: "Windows",
      detail: "x64 installer",
      icon: <WindowsGlyph />,
    },
    {
      name: "Linux",
      detail: "AppImage, Snap (Ubuntu)",
      icon: <LinuxGlyph />,
    },
  ];
  return (
    <Tile className="flex h-full flex-col p-5" glow={TEAL}>
      <Eyebrow tag="cross platform">browser + desktop</Eyebrow>
      <h3 className="font-heading text-h6 text-cc-heading mt-3">
        Wherever your team writes queries.
      </h3>
      <div className="mt-4 grid grid-cols-2 gap-3">
        {platforms.map((p) => (
          <div
            key={p.name}
            className="border-cc-card-border/60 flex items-center gap-3 rounded-lg border bg-black/20 p-3"
          >
            <span
              className="text-cc-heading flex h-7 w-7 shrink-0 items-center justify-center"
              aria-hidden
            >
              {p.icon}
            </span>
            <div className="min-w-0">
              <p className="text-cc-heading font-mono text-[0.72rem]">
                {p.name}
              </p>
              <p className="text-cc-ink-dim truncate font-mono text-[0.6rem]">
                {p.detail}
              </p>
            </div>
          </div>
        ))}
      </div>
      <p className="text-caption text-cc-ink-dim mt-auto pt-4">
        Same workspace, same documents, same theme, signed in everywhere.
      </p>
    </Tile>
  );
}

function WebGlyph() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <circle
        cx="12"
        cy="12"
        r="9"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.4"
      />
      <ellipse
        cx="12"
        cy="12"
        rx="4"
        ry="9"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.4"
      />
      <path
        d="M3 12h18"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
    </svg>
  );
}

function AppleGlyph() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <path
        d="M16.5 12.6c0-2 1.6-3 1.7-3.1-.9-1.4-2.4-1.6-2.9-1.6-1.2-.1-2.4.7-3 .7-.6 0-1.6-.7-2.6-.7-1.3 0-2.6.8-3.3 2-1.4 2.5-.4 6.1 1 8.1.7 1 1.5 2 2.5 2 1 0 1.4-.7 2.6-.7 1.2 0 1.5.7 2.6.7 1.1 0 1.8-1 2.4-2 .8-1.1 1.1-2.2 1.1-2.3-.1 0-2.1-.8-2.1-3.1zM14.7 6.6c.5-.6.9-1.5.8-2.4-.8 0-1.7.5-2.2 1.1-.5.5-.9 1.4-.8 2.3.9.1 1.7-.4 2.2-1z"
        fill="currentColor"
      />
    </svg>
  );
}

function WindowsGlyph() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <path
        d="M3 5.5l8-1.1v7.1H3V5.5zM12 4.3L21 3v9H12V4.3zM3 12.5h8v7.1l-8-1.1v-6zM12 12.5h9V21l-9-1.3v-7.2z"
        fill="currentColor"
      />
    </svg>
  );
}

function LinuxGlyph() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <path
        d="M12 3c-2.4 0-3.6 2-3.6 4.4 0 1.5.6 2.6 1 3.5.3.6.5 1 .5 1.4 0 .7-.5 1.2-1 1.8-.9 1-2 2-2 3.6 0 1 .5 1.7 1.4 1.9.8.2 1.5-.2 2.1-.6.5-.4.9-.8 1.6-.8s1.1.4 1.6.8c.6.4 1.3.8 2.1.6.9-.2 1.4-.9 1.4-1.9 0-1.6-1.1-2.6-2-3.6-.5-.6-1-1.1-1-1.8 0-.4.2-.8.5-1.4.4-.9 1-2 1-3.5C15.6 5 14.4 3 12 3zm-1.2 4.6a.6.7 0 110 1.4.6.7 0 010-1.4zm2.4 0a.6.7 0 110 1.4.6.7 0 010-1.4zM12 10.6c.7 0 1.3.4 1.3.8 0 .3-.6.7-1.3.7-.7 0-1.3-.4-1.3-.7 0-.4.6-.8 1.3-.8z"
        fill="currentColor"
      />
    </svg>
  );
}

/* ---- Multipart file upload (full-width spec strip) ----------------------- */

function UploadTile() {
  return (
    <Tile className="flex h-full flex-col" glow={CORAL}>
      <ChromeBar
        route="nitro › request › uploadReceipt"
        right={<FeatureTag color={CORAL}>multipart spec</FeatureTag>}
      />
      <div className="grid gap-0 sm:grid-cols-[1.1fr_1fr]">
        <div className="border-cc-card-border/60 space-y-4 p-6 sm:border-r">
          <Eyebrow tag="graphql file upload" color={CORAL}>
            multipart request spec
          </Eyebrow>
          <h3 className="font-heading text-h6 text-cc-heading">
            Send files with the query they belong to.
          </h3>
          <p className="text-cc-ink-dim text-sm leading-relaxed">
            Nitro implements the latest GraphQL multipart request specification.
            Pick a file from the request panel, bind it to an Upload variable,
            send. The server gets the file in the same request as the mutation.
          </p>
          <ul className="space-y-2 text-sm">
            <BulletRow text="Latest version of the multipart spec" />
            <BulletRow text="Multiple files per request" />
            <BulletRow text="Variables bound by JSON map" />
          </ul>
        </div>
        <div className="bg-cc-code-header/60 p-5">
          <p className="text-cc-nav-label mb-3 font-mono text-[0.6rem] tracking-[0.12em] uppercase">
            request body
          </p>
          <pre className="text-cc-ink overflow-x-auto font-mono text-[0.64rem] leading-relaxed">
            <span className="text-cc-ink-dim">--boundary</span>
            {"\n"}Content-Disposition: form-data; name=
            <span style={{ color: TEAL }}>&quot;operations&quot;</span>
            {"\n\n"}
            {"{"} &quot;query&quot;:{" "}
            <span style={{ color: TEAL }}>
              &quot;mutation($f: Upload!) {"{"} uploadReceipt(file: $f) {"{"} id{" "}
              {"}"} {"}"}&quot;
            </span>
            ,{"\n"}
            {"  "}&quot;variables&quot;: {"{"} &quot;f&quot;: null {"}"} {"}"}
            {"\n"}
            <span className="text-cc-ink-dim">--boundary</span>
            {"\n"}Content-Disposition: form-data; name=
            <span style={{ color: TEAL }}>&quot;map&quot;</span>
            {"\n\n"}
            {"{"} &quot;0&quot;:{" "}
            <span style={{ color: VIOLET }}>[&quot;variables.f&quot;]</span>{" "}
            {"}"}
            {"\n"}
            <span className="text-cc-ink-dim">--boundary</span>
            {"\n"}Content-Disposition: form-data; name=
            <span style={{ color: TEAL }}>&quot;0&quot;</span>; filename=
            <span style={{ color: CORAL }}>&quot;receipt.png&quot;</span>
            {"\n"}Content-Type: image/png
            {"\n\n"}
            <span className="text-cc-ink-dim">
              {"<"}binary content{">"}
            </span>
            {"\n"}
            <span className="text-cc-ink-dim">--boundary--</span>
          </pre>
        </div>
      </div>
    </Tile>
  );
}

/* ============================================================================
   PROOF, embed NitroCompose once as evidence of the IDE
============================================================================ */

function ComposeProof() {
  return (
    <section className="mt-8">
      <div className="mb-6 max-w-3xl">
        <Eyebrow tag="proof">the IDE, animated</Eyebrow>
        <h2 className="font-heading text-h4 text-cc-heading sm:text-h3 mt-4">
          Schema aware authoring, in motion.
        </h2>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          Nitro reads your schema and writes with you. Completions, type hints,
          and variable binding are first class, not bolted on. Below is the
          Author surface looping standalone.
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-card-bg mx-auto max-w-5xl overflow-hidden rounded-xl border">
        <NitroCompose />
      </div>
    </section>
  );
}

/* ============================================================================
   HONESTY BEAT, keep IDE and telemetry as two distinct facts
============================================================================ */

function HonestySection() {
  const points: readonly string[] = [
    "The GraphQL IDE can be served straight from your Hot Chocolate endpoint, no extra service.",
    "Sign in to sync documents and join organization workspaces across devices.",
    "Telemetry dashboards live separately in Nitro and require Nitro configuration.",
    "Tokens stay on the device. Documents stay with your signed-in account.",
  ];
  return (
    <section className="border-cc-card-border bg-cc-card-bg mt-8 grid gap-8 rounded-3xl border p-6 backdrop-blur sm:grid-cols-[0.8fr_1.2fr] sm:p-10">
      <div>
        <Eyebrow tag="scope" color={GREEN}>
          what is true
        </Eyebrow>
        <h2 className="font-heading text-h5 text-cc-heading sm:text-h4 mt-4">
          Built into Hot Chocolate, opinionated on the rest.
        </h2>
        <p className="text-cc-ink-dim mt-3 leading-relaxed">
          The IDE ships with the server. The workspace, sync, and dashboards are
          features of the hosted Nitro app, intentionally kept distinct.
        </p>
      </div>
      <ul className="space-y-4">
        {points.map((point) => (
          <li
            key={point}
            className="text-cc-ink flex items-start gap-3 text-sm leading-relaxed"
          >
            <span className="mt-0.5 shrink-0" style={{ color: TEAL }}>
              <CheckIcon size={15} />
            </span>
            <span>{point}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

/* ============================================================================
   CLOSING CTA
============================================================================ */

function ClosingCta() {
  return (
    <section className="border-cc-card-border bg-cc-surface/40 relative mt-8 overflow-hidden rounded-3xl border px-6 py-14 text-center backdrop-blur sm:px-12 sm:py-20">
      <div
        className="pointer-events-none absolute inset-0 -z-10"
        style={{
          background:
            "radial-gradient(55% 90% at 50% -10%, rgba(94,234,212,0.16), transparent 60%)",
        }}
        aria-hidden
      />
      <Eyebrow tag="get started">on every device</Eyebrow>
      <h2 className="font-heading text-h4 text-cc-heading sm:text-h2 mx-auto mt-5 max-w-2xl">
        One workspace. Every feature. Wherever you write GraphQL.
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-5 max-w-xl leading-relaxed">
        Launch Nitro in the browser, install the PWA, or download the desktop
        app for macOS, Windows, and Linux. Sign in once and your team is there.
      </p>
      <div className="mt-9 flex flex-wrap justify-center gap-3">
        <SolidButton href="https://nitro.chillicream.com">
          Launch Nitro
        </SolidButton>
        <OutlineButton href="/docs/nitro">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

/* ============================================================================
   PAGE
============================================================================ */

export default function EcosystemPreviewV3Page() {
  return (
    <main>
      <Hero />
      <FeatureLibrary />
      <ComposeProof />
      <HonestySection />
      <ClosingCta />
    </main>
  );
}
