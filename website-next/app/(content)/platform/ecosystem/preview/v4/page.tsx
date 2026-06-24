import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Nitro IDE: The Workspace for Your GraphQL APIs",
  description:
    "GraphQL IDE Nitro, read as a literate notebook. Authentication, workspaces, document sync, PWA install, themes, and multipart upload, in code-led cells.",
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
    title: "Nitro IDE: The Workspace for Your GraphQL APIs",
    description:
      "GraphQL IDE Nitro, read as a literate notebook. Authentication, workspaces, document sync, PWA install, themes, and multipart upload, in code-led cells.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Shared chrome primitives                                                  */
/*  The page is a literate notebook: a left accent rail, numbered cells, and  */
/*  one cc-code-bg panel per cell with a cc-code-header file path chrome.     */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.625rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface PanelHeaderProps {
  readonly path: string;
  readonly meta?: string;
}

function PanelHeader({ path, meta }: PanelHeaderProps) {
  return (
    <div className="border-cc-card-border bg-cc-code-header flex items-center gap-2.5 border-b px-4 py-2.5">
      <span className="bg-cc-accent h-1.5 w-1.5 rounded-full" aria-hidden />
      <span className="text-cc-ink-dim font-mono text-[0.7rem] tracking-tight">
        {path}
      </span>
      {meta ? (
        <span className="text-cc-nav-label ml-auto font-mono text-[0.62rem] tracking-tight">
          {meta}
        </span>
      ) : null}
    </div>
  );
}

interface CodePanelProps {
  readonly path: string;
  readonly meta?: string;
  readonly children: ReactNode;
}

function CodePanel({ path, meta, children }: CodePanelProps) {
  return (
    <div className="border-cc-card-border bg-cc-code-bg overflow-hidden rounded-xl border shadow-2xl shadow-black/30">
      <PanelHeader path={path} meta={meta} />
      <div className="overflow-x-auto p-5 font-mono text-[0.74rem] leading-6">
        {children}
      </div>
    </div>
  );
}

interface CellProps {
  readonly index: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly paragraphs: readonly string[];
  readonly panel: ReactNode;
}

/**
 * One notebook cell: a left rail with the cell index, a prose commentary
 * block, then the centerpiece code panel directly below it. The rail is the
 * only accent event in the cell.
 */
function Cell({ index, eyebrow, title, paragraphs, panel }: CellProps) {
  return (
    <section className="relative pl-10 sm:pl-14">
      <span
        className="text-cc-accent absolute top-0 left-0 font-mono text-[0.66rem] tracking-[0.18em] uppercase sm:left-2"
        aria-hidden
      >
        {index}
      </span>
      <Eyebrow>{eyebrow}</Eyebrow>
      <h3 className="font-heading text-h4 text-cc-heading mt-3 font-semibold tracking-tight">
        {title}
      </h3>
      <div className="text-cc-ink-dim mt-4 flex flex-col gap-3 text-[1.02rem] leading-relaxed">
        {paragraphs.map((p) => (
          <p key={p.slice(0, 24)}>{p}</p>
        ))}
      </div>
      <div className="mt-7">{panel}</div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Syntax token colors used inline in font-mono code blocks. These are the   */
/*  GitHub Dark palette already in use across the site's code surfaces.       */
/* -------------------------------------------------------------------------- */

const SYN = {
  keyword: "#ff7b72",
  string: "#a5d6ff",
  number: "#79c0ff",
  type: "#79c0ff",
  fn: "#d2a8ff",
  var: "#ffa657",
  ok: "#7ee787",
  comment: "#62748e",
} as const;

interface LineProps {
  readonly n: number;
  readonly children: ReactNode;
}

function Line({ n, children }: LineProps) {
  return (
    <div className="grid grid-cols-[2.25rem_1fr] gap-0">
      <span className="text-cc-nav-label font-mono text-[0.6rem] tabular-nums select-none">
        {n.toString().padStart(2, "0")}
      </span>
      <span className="text-cc-ink whitespace-pre">{children}</span>
    </div>
  );
}

interface ColorProps {
  readonly color: string;
  readonly children: ReactNode;
}

function C({ color, children }: ColorProps) {
  return <span style={{ color }}>{children}</span>;
}

/* -------------------------------------------------------------------------- */
/*  Hero cover panel: a Nitro request/response pair.                          */
/* -------------------------------------------------------------------------- */

function HeroCoverPanel() {
  return (
    <div className="border-cc-card-border bg-cc-code-bg overflow-hidden rounded-2xl border shadow-2xl shadow-black/40">
      <PanelHeader
        path="nitro · Checkout / shipping-api · request.graphql"
        meta="POST /graphql · 200 OK · 12 ms"
      />
      <div className="grid divide-y divide-[var(--color-cc-card-border)] lg:grid-cols-2 lg:divide-x lg:divide-y-0">
        <div className="p-5 font-mono text-[0.76rem] leading-6">
          <p className="text-cc-nav-label mb-3 font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            request
          </p>
          <Line n={1}>
            <C color={SYN.comment}># POST https://api.example.com/graphql</C>
          </Line>
          <Line n={2}>
            <C color={SYN.keyword}>query</C> <C color={SYN.fn}>GetOrder</C>(
            <C color={SYN.var}>$id</C>: <C color={SYN.type}>ID!</C>) {"{"}
          </Line>
          <Line n={3}>
            {"  "}
            <C color={SYN.fn}>order</C>(id: <C color={SYN.var}>$id</C>) {"{"}
          </Line>
          <Line n={4}>{"    id"}</Line>
          <Line n={5}>{"    status"}</Line>
          <Line n={6}>{"    total"}</Line>
          <Line n={7}>{"    customer { name email }"}</Line>
          <Line n={8}>{"  }"}</Line>
          <Line n={9}>{"}"}</Line>
        </div>
        <div className="p-5 font-mono text-[0.76rem] leading-6">
          <p className="text-cc-nav-label mb-3 font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            response
          </p>
          <Line n={1}>{"{"}</Line>
          <Line n={2}>
            {"  "}
            <C color={SYN.string}>&quot;data&quot;</C>: {"{"}
          </Line>
          <Line n={3}>
            {"    "}
            <C color={SYN.string}>&quot;order&quot;</C>: {"{"}
          </Line>
          <Line n={4}>
            {"      "}
            <C color={SYN.string}>&quot;id&quot;</C>:{" "}
            <C color={SYN.string}>&quot;ord_8421&quot;</C>,
          </Line>
          <Line n={5}>
            {"      "}
            <C color={SYN.string}>&quot;status&quot;</C>:{" "}
            <C color={SYN.string}>&quot;SHIPPED&quot;</C>,
          </Line>
          <Line n={6}>
            {"      "}
            <C color={SYN.string}>&quot;total&quot;</C>:{" "}
            <C color={SYN.number}>148.20</C>,
          </Line>
          <Line n={7}>
            {"      "}
            <C color={SYN.string}>&quot;customer&quot;</C>: {"{ ... }"}
          </Line>
          <Line n={8}>{"    }"}</Line>
          <Line n={9}>{"  }"}</Line>
          <Line n={10}>{"}"}</Line>
        </div>
      </div>
      <div className="border-cc-card-border bg-cc-code-header flex items-center justify-between border-t px-4 py-2 font-mono text-[0.6rem]">
        <span className="text-cc-success">connected · OAuth 2</span>
        <span className="text-cc-nav-label">
          cached locally · synced to workspace
        </span>
      </div>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Cell panels                                                               */
/* -------------------------------------------------------------------------- */

function AuthPanel() {
  return (
    <CodePanel path="nitro/connection.auth.ts" meta="typescript">
      <Line n={1}>
        <C color={SYN.keyword}>import</C> {"{ "}
        <C color={SYN.type}>Connection</C>
        {" }"} <C color={SYN.keyword}>from</C>{" "}
        <C color={SYN.string}>&quot;@chillicream/nitro&quot;</C>;
      </Line>
      <Line n={2}> </Line>
      <Line n={3}>
        <C color={SYN.keyword}>export</C> <C color={SYN.keyword}>const</C>{" "}
        <C color={SYN.var}>shippingApi</C>: <C color={SYN.type}>Connection</C> ={" "}
        {"{"}
      </Line>
      <Line n={4}>
        {"  "}name: <C color={SYN.string}>&quot;shipping-api&quot;</C>,
      </Line>
      <Line n={5}>
        {"  "}endpoint:{" "}
        <C color={SYN.string}>&quot;https://api.example.com/graphql&quot;</C>,
      </Line>
      <Line n={6}>{"  auth: {"}</Line>
      <Line n={7}>
        {"    "}
        <C color={SYN.fn}>oauth2</C>: {"{ authority: "}
        <C color={SYN.string}>&quot;https://id.example.com&quot;</C>, scopes: [
        <C color={SYN.string}>&quot;graphql&quot;</C>] {"}"},{"  "}
        <C color={SYN.comment}>{"// ready"}</C>
      </Line>
      <Line n={8}>
        {"    "}
        <C color={SYN.fn}>bearer</C>: {"{ token: env."}
        <C color={SYN.var}>SERVICE_TOKEN</C> {"}"},
      </Line>
      <Line n={9}>
        {"    "}
        <C color={SYN.fn}>basic</C>: {"{ username: "}
        <C color={SYN.string}>&quot;legacy&quot;</C>, password: env.
        <C color={SYN.var}>LEGACY_PASS</C> {"}"},
      </Line>
      <Line n={10}>{"  },"}</Line>
      <Line n={11}>
        {"  "}scope: <C color={SYN.string}>&quot;workspace&quot;</C>,
      </Line>
      <Line n={12}>{"};"}</Line>
    </CodePanel>
  );
}

interface WorkspaceRow {
  readonly text: string;
  readonly note?: string;
  readonly active?: boolean;
}

function WorkspacesPanel() {
  const rows: readonly WorkspaceRow[] = [
    { text: "ChilliCream/", note: "org · 3 workspaces" },
    { text: "  Checkout/", note: "workspace · 4 APIs · 7 members" },
    { text: "    orders-api", note: "graphql · editor" },
    { text: "    payments-api", note: "graphql · editor" },
    {
      text: "    shipping-api",
      note: "graphql · editor · open",
      active: true,
    },
    { text: "    warehouse-api", note: "graphql · viewer" },
    { text: "  Identity/", note: "workspace · 2 APIs" },
    { text: "    users-api", note: "graphql · editor" },
    { text: "    sessions-api", note: "graphql · viewer" },
    { text: "  Catalog/", note: "workspace · 3 APIs" },
  ];
  return (
    <CodePanel path="nitro/workspaces.tree" meta="3 workspaces · 9 APIs">
      {rows.map((r, i) => (
        <div
          key={r.text}
          className={[
            "grid grid-cols-[2.25rem_1fr_auto] gap-0 rounded-md px-2",
            r.active ? "bg-cc-accent/[0.06] -mx-2" : "-mx-2",
          ].join(" ")}
        >
          <span className="text-cc-nav-label font-mono text-[0.6rem] tabular-nums select-none">
            {(i + 1).toString().padStart(2, "0")}
          </span>
          <span
            className={
              r.active
                ? "text-cc-accent font-mono whitespace-pre"
                : "text-cc-ink font-mono whitespace-pre"
            }
          >
            {r.text}
          </span>
          {r.note ? (
            <span className="text-cc-nav-label font-mono text-[0.66rem]">
              {r.note}
            </span>
          ) : null}
        </div>
      ))}
    </CodePanel>
  );
}

interface LogRow {
  readonly t: string;
  readonly device: string;
  readonly msg: string;
  readonly tone: "ok" | "warn" | "info";
}

function SyncPanel() {
  const log: readonly LogRow[] = [
    {
      t: "12:04:11",
      device: "macbook-pro",
      msg: "synced 18 documents in 2s",
      tone: "ok",
    },
    {
      t: "12:04:11",
      device: "chrome-linux",
      msg: "synced 18 documents in 2s",
      tone: "ok",
    },
    {
      t: "12:04:11",
      device: "edge-windows",
      msg: "synced 18 documents in 2s",
      tone: "ok",
    },
    {
      t: "12:04:09",
      device: "ipad-pro",
      msg: "queued · offline",
      tone: "warn",
    },
    {
      t: "12:04:02",
      device: "macbook-pro",
      msg: "open Checkout/shipping-api · GetOrder",
      tone: "info",
    },
    {
      t: "12:03:58",
      device: "macbook-pro",
      msg: "join workspace Checkout (editor)",
      tone: "info",
    },
  ];
  return (
    <CodePanel path="nitro/sync/documents.log" meta="tail -f · across devices">
      {log.map((row, i) => (
        <div
          key={`${row.t}-${row.device}-${i}`}
          className="grid grid-cols-[2.25rem_5rem_8rem_1fr] gap-0"
        >
          <span className="text-cc-nav-label font-mono text-[0.6rem] tabular-nums select-none">
            {(i + 1).toString().padStart(2, "0")}
          </span>
          <span className="text-cc-nav-label font-mono tabular-nums">
            {row.t}
          </span>
          <span className="text-cc-ink font-mono">{row.device}</span>
          <span
            className={
              row.tone === "ok"
                ? "text-cc-success font-mono"
                : row.tone === "warn"
                  ? "text-cc-warning font-mono"
                  : "text-cc-ink-dim font-mono"
            }
          >
            {row.msg}
          </span>
        </div>
      ))}
      <div className="border-cc-card-border mt-3 flex items-center justify-between border-t pt-3">
        <span className="text-cc-nav-label font-mono text-[0.62rem]">
          18 documents · 4 collections · scoped to workspace Checkout
        </span>
        <span className="text-cc-success font-mono text-[0.62rem]">
          up to date
        </span>
      </div>
    </CodePanel>
  );
}

function ManifestPanel() {
  return (
    <CodePanel path="nitro/manifest.webmanifest" meta="PWA · install profile">
      <Line n={1}>{"{"}</Line>
      <Line n={2}>
        {"  "}
        <C color={SYN.string}>&quot;name&quot;</C>:{" "}
        <C color={SYN.string}>&quot;Nitro&quot;</C>,
      </Line>
      <Line n={3}>
        {"  "}
        <C color={SYN.string}>&quot;short_name&quot;</C>:{" "}
        <C color={SYN.string}>&quot;Nitro&quot;</C>,
      </Line>
      <Line n={4}>
        {"  "}
        <C color={SYN.string}>&quot;start_url&quot;</C>:{" "}
        <C color={SYN.string}>&quot;/&quot;</C>,
      </Line>
      <Line n={5}>
        {"  "}
        <C color={SYN.string}>&quot;display&quot;</C>:{" "}
        <C color={SYN.string}>&quot;standalone&quot;</C>,
      </Line>
      <Line n={6}>
        {"  "}
        <C color={SYN.string}>&quot;background_color&quot;</C>:{" "}
        <C color={SYN.string}>&quot;#0b0f1a&quot;</C>,
      </Line>
      <Line n={7}>
        {"  "}
        <C color={SYN.string}>&quot;theme_color&quot;</C>:{" "}
        <C color={SYN.string}>&quot;#5eead4&quot;</C>,
      </Line>
      <Line n={8}>
        {"  "}
        <C color={SYN.string}>&quot;scope&quot;</C>:{" "}
        <C color={SYN.string}>&quot;/&quot;</C>,
      </Line>
      <Line n={9}>
        {"  "}
        <C color={SYN.string}>&quot;icons&quot;</C>: [ {"{ src: "}
        <C color={SYN.string}>&quot;/icon-512.png&quot;</C>, sizes:{" "}
        <C color={SYN.string}>&quot;512x512&quot;</C> {"}"} ]
      </Line>
      <Line n={10}>{"}"}</Line>
      <Line n={11}> </Line>
      <Line n={12}>
        <C color={SYN.comment}># Install app: from any modern browser</C>
      </Line>
      <Line n={13}>
        <C color={SYN.ok}>$</C> open https://nitro.chillicream.com{" "}
        <C color={SYN.comment}>{"// then click Install app"}</C>
      </Line>
    </CodePanel>
  );
}

function ThemesPanel() {
  return (
    <CodePanel path="nitro/themes.tokens.css" meta="dark · light · system">
      <div className="grid gap-6 lg:grid-cols-2">
        <div>
          <p className="text-cc-nav-label mb-2 font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            :root (dark)
          </p>
          <Line n={1}>
            <C color={SYN.keyword}>:root</C> {"{"}
          </Line>
          <Line n={2}>
            {"  "}--cc-bg: <C color={SYN.number}>#0b0f1a</C>;
          </Line>
          <Line n={3}>
            {"  "}--cc-surface: <C color={SYN.number}>#0c1322</C>;
          </Line>
          <Line n={4}>
            {"  "}--cc-heading: <C color={SYN.number}>#f5f0ea</C>;
          </Line>
          <Line n={5}>
            {"  "}--cc-ink: <C color={SYN.number}>#e6e1d6</C>;
          </Line>
          <Line n={6}>
            {"  "}--cc-accent: <C color={SYN.number}>#5eead4</C>;
          </Line>
          <Line n={7}>{"}"}</Line>
        </div>
        <div>
          <p className="text-cc-nav-label mb-2 font-mono text-[0.6rem] tracking-[0.18em] uppercase">
            [data-theme=&quot;light&quot;]
          </p>
          <Line n={1}>
            [<C color={SYN.var}>data-theme</C>=
            <C color={SYN.string}>&quot;light&quot;</C>] {"{"}
          </Line>
          <Line n={2}>
            {"  "}--cc-bg: <C color={SYN.number}>#f5f1ea</C>;
          </Line>
          <Line n={3}>
            {"  "}--cc-surface: <C color={SYN.number}>#ffffff</C>;
          </Line>
          <Line n={4}>
            {"  "}--cc-heading: <C color={SYN.number}>#0b0f1a</C>;
          </Line>
          <Line n={5}>
            {"  "}--cc-ink: <C color={SYN.number}>#1f2937</C>;
          </Line>
          <Line n={6}>
            {"  "}--cc-accent: <C color={SYN.number}>#0f766e</C>;
          </Line>
          <Line n={7}>{"}"}</Line>
        </div>
      </div>
      <div className="border-cc-card-border mt-5 flex items-center justify-between border-t pt-3 font-mono text-[0.62rem]">
        <span className="text-cc-nav-label">
          system theme follows prefers-color-scheme
        </span>
        <span className="text-cc-accent">active · dark</span>
      </div>
    </CodePanel>
  );
}

function UploadPanel() {
  return (
    <CodePanel
      path="POST /graphql"
      meta="multipart/form-data · boundary=----nitro-83a"
    >
      <Line n={1}>
        <C color={SYN.keyword}>POST</C> /graphql HTTP/1.1
      </Line>
      <Line n={2}>Host: api.example.com</Line>
      <Line n={3}>Authorization: Bearer eyJhbGciOi...</Line>
      <Line n={4}>
        Content-Type: multipart/form-data; boundary=----nitro-83a
      </Line>
      <Line n={5}> </Line>
      <Line n={6}>------nitro-83a</Line>
      <Line n={7}>
        Content-Disposition: form-data; name=
        <C color={SYN.string}>&quot;operations&quot;</C>
      </Line>
      <Line n={8}> </Line>
      <Line n={9}>
        {"{ "}
        <C color={SYN.string}>&quot;query&quot;</C>:{" "}
        <C color={SYN.string}>
          {'"mutation($file: Upload!) { uploadAvatar(file: $file) { url } }"'}
        </C>
        , <C color={SYN.string}>&quot;variables&quot;</C>: {"{ "}
        <C color={SYN.string}>&quot;file&quot;</C>:{" "}
        <C color={SYN.keyword}>null</C> {"} }"}
      </Line>
      <Line n={10}>------nitro-83a</Line>
      <Line n={11}>
        Content-Disposition: form-data; name=
        <C color={SYN.string}>&quot;map&quot;</C>
      </Line>
      <Line n={12}> </Line>
      <Line n={13}>
        {"{ "}
        <C color={SYN.string}>&quot;0&quot;</C>: [
        <C color={SYN.string}>&quot;variables.file&quot;</C>] {"}"}
      </Line>
      <Line n={14}>------nitro-83a</Line>
      <Line n={15}>
        Content-Disposition: form-data; name=
        <C color={SYN.string}>&quot;0&quot;</C>; filename=
        <C color={SYN.string}>&quot;avatar.png&quot;</C>
      </Line>
      <Line n={16}>Content-Type: image/png</Line>
      <Line n={17}> </Line>
      <Line n={18}>
        <C color={SYN.comment}>&lt;binary png payload, 184 KB&gt;</C>
      </Line>
      <Line n={19}>------nitro-83a--</Line>
      <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3 font-mono text-[0.62rem]">
        <span className="text-cc-nav-label">
          jaydenseric/graphql-multipart-request-spec compatible
        </span>
        <span className="text-cc-success">200 OK · 42 ms</span>
      </div>
    </CodePanel>
  );
}

interface PlatformRow {
  readonly name: string;
  readonly note: string;
}

function PlatformsPanel() {
  const rows: readonly PlatformRow[] = [
    { name: "Browser", note: "any modern browser, runs as a tab" },
    { name: "macOS", note: "install as a PWA from the browser" },
    { name: "Windows", note: "install as a PWA from the browser" },
    { name: "Linux", note: "install as a PWA from the browser" },
    { name: "ChromeOS", note: "install as a PWA from the browser" },
  ];
  return (
    <CodePanel path="nitro/platforms.txt" meta="one Nitro, every surface">
      {rows.map((r, i) => (
        <div key={r.name} className="grid grid-cols-[2.25rem_8rem_1fr] gap-0">
          <span className="text-cc-nav-label font-mono text-[0.6rem] tabular-nums select-none">
            {(i + 1).toString().padStart(2, "0")}
          </span>
          <span className="text-cc-heading font-mono">{r.name}</span>
          <span className="text-cc-ink-dim font-mono">{r.note}</span>
        </div>
      ))}
    </CodePanel>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function EcosystemV4Page() {
  return (
    <div className="relative mx-auto flex max-w-5xl flex-col gap-20 py-6">
      {/* The notebook left rail: a thin cc-accent vertical line tying all */}
      {/* cells together. It anchors the whole reading column. */}
      <div
        aria-hidden
        className="bg-cc-accent/30 pointer-events-none absolute top-2 bottom-2 left-1 w-px sm:left-3"
      />

      {/* ------------------------------ HERO ----------------------------- */}
      <section className="relative pl-10 sm:pl-14">
        <Eyebrow>nitro · graphql ide manual</Eyebrow>
        <h1 className="font-heading text-hero text-cc-heading mt-5 font-semibold tracking-tight">
          The IDE your team{" "}
          <span className="text-cc-accent">actually opens.</span>
        </h1>
        <p className="lead text-cc-ink-dim mt-6 max-w-3xl">
          Nitro is a focused GraphQL workspace, read here as a literate
          notebook. Each cell pairs a short note with the real shape of the
          thing it describes (a connection config, a workspace tree, a sync log,
          a manifest, a CSS token block, a multipart request). Skim it like a
          manual.
        </p>
        <div className="mt-9 flex flex-wrap items-center gap-3">
          <SolidButton href="https://nitro.chillicream.com">
            Launch Nitro
          </SolidButton>
          <OutlineButton href="/docs/nitro">Read the Docs</OutlineButton>
        </div>
        <div className="mt-10">
          <HeroCoverPanel />
        </div>
      </section>

      {/* -------------------------- CELL 00: PREFACE --------------------- */}
      <section className="relative pl-10 sm:pl-14">
        <span
          className="text-cc-accent absolute top-0 left-0 font-mono text-[0.66rem] tracking-[0.18em] uppercase sm:left-2"
          aria-hidden
        >
          00
        </span>
        <Eyebrow>preface</Eyebrow>
        <h2 className="font-heading text-h5 text-cc-heading mt-3 font-semibold tracking-tight">
          How to read this page.
        </h2>
        <p className="text-cc-ink-dim mt-4 max-w-3xl text-[1.02rem] leading-relaxed">
          Six cells, top to bottom. Each one names a real artifact in the IDE
          and shows it. The prose is the commentary above the panel, the panel
          is the centerpiece. If you only have a minute, read the panels.
        </p>
        <div className="mt-5 flex flex-wrap gap-1.5">
          {["Browser", "PWA", "OAuth"].map((tag) => (
            <span
              key={tag}
              className="border-cc-card-border bg-cc-surface/60 text-cc-ink-dim rounded-full border px-2.5 py-1 font-mono text-[0.62rem] tracking-tight"
            >
              {tag}
            </span>
          ))}
        </div>
      </section>

      {/* ---------------------------- CELLS 01..06 ----------------------- */}
      <Cell
        index="01"
        eyebrow="cell 01 · authentication"
        title="Sign in the way your API expects."
        paragraphs={[
          "Nitro speaks the auth flows your services already use. OAuth 2 covers human sign in, bearer tokens cover service accounts, and basic auth keeps legacy endpoints reachable.",
          "Auth is configured per connection and scoped to a workspace, so the credentials your team needs travel with the workspace, not the laptop.",
        ]}
        panel={<AuthPanel />}
      />

      <Cell
        index="02"
        eyebrow="cell 02 · workspaces"
        title="One workspace per team, not per laptop."
        paragraphs={[
          "Group APIs into workspaces and invite the people who own them. Operations, environments, and connection settings live with the workspace.",
          "A new teammate is one invite away from running production queries the same way you do. Switching workspaces is one click, no reconfiguration.",
        ]}
        panel={<WorkspacesPanel />}
      />

      <Cell
        index="03"
        eyebrow="cell 03 · document sync"
        title="Your documents follow you, every device."
        paragraphs={[
          "Open Nitro on your laptop, your desktop, or a borrowed browser, your queries are already there. Documents stay in sync across your devices and across the teams you share a workspace with.",
          "Offline edits queue locally and replay when the device reconnects. The log below is what that looks like in practice.",
        ]}
        panel={<SyncPanel />}
      />

      <Cell
        index="04"
        eyebrow="cell 04 · install anywhere"
        title="A real app, without a real installer."
        paragraphs={[
          "Nitro runs as a Progressive Web App, so any modern browser can promote it to a standalone window with its own icon and offline cache.",
          "No installer, no admin prompt, no IT ticket. The same Nitro on the web becomes the Nitro on your desktop, and updates roll out the moment they ship.",
        ]}
        panel={<ManifestPanel />}
      />

      <Cell
        index="05"
        eyebrow="cell 05 · themes"
        title="Light, dark, or whatever your OS decides."
        paragraphs={[
          "Pick a theme that does not fight your environment. Dark for the pairing session, light for the projector, or system to track your OS automatically.",
          "Every surface is tuned for both modes, from the editor to the response pane. The tokens are CSS custom properties, shown here side by side.",
        ]}
        panel={<ThemesPanel />}
      />

      <Cell
        index="06"
        eyebrow="cell 06 · multipart upload"
        title="Multipart uploads, exactly to spec."
        paragraphs={[
          "Send files alongside your variables without leaving the editor. Nitro builds the multipart request per the GraphQL multipart spec.",
          "The same payload your Hot Chocolate server accepts in production is the one the IDE sends from your machine. The raw request is below.",
        ]}
        panel={<UploadPanel />}
      />

      {/* ---------------------- APPENDIX: PLATFORMS ---------------------- */}
      <section className="relative pl-10 sm:pl-14">
        <span
          className="text-cc-accent absolute top-0 left-0 font-mono text-[0.66rem] tracking-[0.18em] uppercase sm:left-2"
          aria-hidden
        >
          A
        </span>
        <Eyebrow>appendix · cross platform</Eyebrow>
        <h2 className="font-heading text-h5 text-cc-heading mt-3 font-semibold tracking-tight">
          The same Nitro, on whatever you sit in front of.
        </h2>
        <p className="text-cc-ink-dim mt-4 max-w-3xl text-[1.02rem] leading-relaxed">
          Use Nitro in any modern browser, or install the PWA on macOS, Windows,
          or Linux. Your workspaces, history, and connections come with you.
        </p>
        <div className="mt-7">
          <PlatformsPanel />
        </div>
      </section>

      {/* ----------------------- HONESTY FOOTNOTE ------------------------ */}
      <section className="relative pl-10 sm:pl-14">
        <span
          className="text-cc-accent absolute top-0 left-0 font-mono text-[0.66rem] tracking-[0.18em] uppercase sm:left-2"
          aria-hidden
        >
          fn
        </span>
        <Eyebrow>footnote</Eyebrow>
        <h2 className="font-heading text-h5 text-cc-heading mt-3 font-semibold tracking-tight">
          Where the line is.
        </h2>
        <div className="bg-cc-surface/40 border-cc-card-border mt-5 rounded-xl border p-6 sm:p-7">
          <div className="border-cc-accent border-l-2 pl-5">
            <p className="text-cc-ink-dim text-[1rem] leading-relaxed">
              What ships in the IDE is authoring, workspaces, document sync,
              cross platform install, and a built in GraphQL endpoint UI. That
              is enough to make a team productive on day one.
            </p>
            <p className="text-cc-ink-dim mt-3 text-[1rem] leading-relaxed">
              Operational telemetry, the dashboards and traces, is opt in and
              configured server side via Nitro telemetry. It is a separate
              decision, and the IDE is useful with or without it.
            </p>
          </div>
        </div>
      </section>

      {/* ------------------------ CTA: SIGN OFF -------------------------- */}
      <section className="relative flex flex-col items-center gap-7 py-8 text-center">
        <Eyebrow>end of notebook</Eyebrow>
        <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
          Open Nitro. Pick a workspace. Run a query.
        </h2>
        <p className="text-cc-ink-dim max-w-xl text-[1.05rem] leading-relaxed">
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
        <ul className="text-cc-ink-dim mt-2 flex flex-wrap justify-center gap-x-6 gap-y-2 text-[0.88rem]">
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
      </section>
    </div>
  );
}
