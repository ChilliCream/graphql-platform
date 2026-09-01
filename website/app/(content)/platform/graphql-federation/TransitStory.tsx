"use client";

import { Fragment } from "react";
import type { CSSProperties, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { SectionHeading } from "@/src/components/SectionHeading";

import { CANON, GatewayChip, GlowNode, INK_DIM } from "./visuals/stage";

const W = 1024;
const H = 4900;

const MARKERS = [
  { s: 0, x: 150, y: 100 },
  { s: 1, x: 320, y: 140 },
  { s: 2, x: 512, y: 180 },
  { s: 3, x: 704, y: 220 },
  { s: 4, x: 874, y: 260 },
] as const;

const BEND_START = 3050;
const HUB = { x: 512, y: 3480 } as const;

const HORIZON_Y = 4300;
const CHIP = { x: 512, y: 4380 } as const;

function streamPath(x: number, y0: number): string {
  const c1y = BEND_START + (HUB.y - BEND_START) * 0.5;
  const c2y = HUB.y - (HUB.y - BEND_START) * 0.35;
  return `M${x} ${y0} L${x} ${BEND_START} C ${x} ${c1y}, ${HUB.x} ${c2y}, ${HUB.x} ${HUB.y}`;
}

const GAPS = [
  { x: 470, w: 460, y: 400, h: 320 },
  { x: 100, w: 460, y: 900, h: 320 },
  { x: 220, w: 584, y: 1390, h: 300 },
  { x: 470, w: 460, y: 1860, h: 320 },
  { x: 220, w: 584, y: 2350, h: 300 },
  { x: 220, w: 584, y: 3560, h: 360 },
  { x: 220, w: 584, y: 4450, h: 300 },
] as const;

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

/** Desktop placement: the block's center in the 1024 x H map, as CSS variables. */
function placement(top: number, left: number): CSSProperties {
  return { "--top": pct(top, H), "--left": `${left}%` } as CSSProperties;
}

const SCRIM =
  "radial-gradient(ellipse 62% 58% at 50% 50%, rgba(11,15,26,0.98) 0%, rgba(11,15,26,0.94) 50%, rgba(11,15,26,0.6) 76%, rgba(11,15,26,0) 93%)";

interface CopyBlockProps {
  readonly top: number;
  readonly left: number;
  readonly side?: boolean;
  readonly title: string;
  readonly children: ReactNode;
}

function CopyBlock({ top, left, side, title, children }: CopyBlockProps) {
  return (
    <div
      className={`relative w-full text-center sm:absolute sm:top-(--top) sm:left-(--left) sm:z-20 sm:-translate-x-1/2 sm:-translate-y-1/2 ${
        side ? "sm:w-[min(44%,26rem)] sm:text-left" : "sm:w-[min(92%,34rem)]"
      }`}
      style={placement(top, left)}
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -inset-x-32 -inset-y-20 hidden sm:block"
        style={{ background: SCRIM }}
      />
      <div className="relative">
        <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
          {title}
        </h3>
        <div className="text-cc-ink mt-4 space-y-3 text-sm sm:text-base">
          {children}
        </div>
      </div>
    </div>
  );
}

interface CodeLine {
  readonly text: string;
  readonly dots?: readonly string[];
  readonly accent?: string;
}

function CodeText({ line }: { readonly line: CodeLine }) {
  if (!line.accent || !line.text.includes(line.accent)) {
    return <span className="whitespace-pre text-[#c9d4e8]">{line.text}</span>;
  }
  const [before, after] = line.text.split(line.accent);
  return (
    <span className="whitespace-pre text-[#c9d4e8]">
      {before}
      <span className="text-[#5eead4]">{line.accent}</span>
      {after}
    </span>
  );
}

interface CodeBoxProps {
  readonly top: number;
  readonly left: number;
  readonly paired?: boolean;
  readonly label: string;
  readonly color?: string;
  readonly lines: readonly CodeLine[];
}

function CodeBox({ top, left, paired, label, color, lines }: CodeBoxProps) {
  return (
    <div
      className={`mx-auto w-[min(100%,21rem)] sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:mx-0 sm:-translate-x-1/2 sm:-translate-y-1/2 ${
        paired ? "sm:w-[min(43%,21rem)]" : "sm:w-[min(88%,21rem)]"
      }`}
      style={placement(top, left)}
    >
      <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
        <div className="flex items-center gap-2">
          {color && (
            <span
              className="inline-block h-2.5 w-2.5 rounded-[3px]"
              style={{ background: color }}
            />
          )}
          <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
            {label}
          </span>
        </div>
        <div className="border-cc-card-border mt-2 border-t pt-2 font-mono text-[12px] leading-6">
          {lines.map((l, i) => (
            <div key={i} className="flex items-center gap-2">
              <CodeText line={l} />
              {l.dots && l.dots.length > 0 && (
                <span className="ml-auto flex items-center gap-1">
                  {l.dots.map((d, k) => (
                    <span
                      key={k}
                      className="inline-block h-2 w-2 rounded-full"
                      style={{ background: d }}
                    />
                  ))}
                </span>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

interface Chapter {
  readonly copy: Omit<CopyBlockProps, "children">;
  readonly body: ReactNode;
  readonly boxes: readonly CodeBoxProps[];
}

const KEY = '@key(fields: "id")';

const STORY: readonly Chapter[] = [
  {
    copy: {
      top: 560,
      left: 68,
      side: true,
      title: "Clients want one API. Teams want to ship alone.",
    },
    body: (
      <>
        <p>
          A product page shows a name, a price, past orders, a delivery
          estimate, and who is signed in. Inside the company those five fields
          come from five services, each owned by a different team. The split is
          deliberate: a team that owns its service can change it and deploy it
          without asking anyone.
        </p>
        <p>
          The screen does not care about the split. It wants one place to ask
          for everything it shows. That is the tension every growing system
          meets: clients want one API, teams want independence, and the usual
          answers give up one to get the other.
        </p>
      </>
    ),
    boxes: [
      {
        top: 560,
        left: 30,
        label: "Product page · five teams",
        lines: [
          { text: "name", dots: [CANON[0].color] },
          { text: "price", dots: [CANON[1].color] },
          { text: "orders", dots: [CANON[2].color] },
          { text: "delivery", dots: [CANON[3].color] },
          { text: "account", dots: [CANON[4].color] },
        ],
      },
    ],
  },
  {
    copy: {
      top: 1060,
      left: 32,
      side: true,
      title: "Answer one: every app merges the data itself.",
    },
    body: (
      <>
        <p>
          The app calls each service, one request per service, and merges the
          answers itself. Five services, five calls, five formats, five ways to
          fail. Every app that shows this screen writes the same merging code,
          and when one service renames a field, every copy of that code breaks.
        </p>
        <p>
          The teams keep their independence. The clients pay for it, in every
          app, on every change.
        </p>
      </>
    ),
    boxes: [
      {
        top: 1060,
        left: 70,
        label: "One screen · five calls",
        lines: [
          { text: "GET /products/P-42", dots: [CANON[0].color] },
          { text: "GET /prices/P-42", dots: [CANON[1].color] },
          { text: "GET /orders?product=P-42", dots: [CANON[2].color] },
          { text: "GET /shipping/P-42", dots: [CANON[3].color] },
          { text: "GET /account", dots: [CANON[4].color] },
        ],
      },
    ],
  },
  {
    copy: {
      top: 1540,
      left: 50,
      title: "Answer two: one big API, one team, one queue.",
    },
    body: (
      <>
        <p>
          So the company builds one API in front of everything and gives it to
          one team. Now every field any client needs passes through that team:
          their review, their deploy, their backlog. Catalog wants to add a
          field. Billing wants to rename one. Both wait.
        </p>
        <p>
          The clients get one API. The teams give up their independence, and the
          queue in front of that API becomes the bottleneck the services were
          split to avoid.
        </p>
      </>
    ),
    boxes: [],
  },
  {
    copy: {
      top: 2020,
      left: 68,
      side: true,
      title: "GraphQL gives clients one schema and one query.",
    },
    body: (
      <>
        <p>
          GraphQL is a query language for APIs. A GraphQL API publishes a
          schema: a typed document that lists every field a client can ask for.
          A client sends one query naming exactly the fields it needs and gets
          exactly those fields back, in one response.
        </p>
        <p>
          One query can ask for any of the page&apos;s fields. This one names
          three: name, price, delivery. The response contains those three fields
          and nothing else.
        </p>
      </>
    ),
    boxes: [
      {
        top: 2020,
        left: 30,
        label: "One query",
        lines: [
          { text: "{" },
          { text: '  productById(id: "P-42") {' },
          { text: "    name", dots: [CANON[0].color] },
          { text: "    price", dots: [CANON[1].color] },
          { text: "    delivery", dots: [CANON[3].color] },
          { text: "  }" },
          { text: "}" },
        ],
      },
    ],
  },
  {
    copy: {
      top: 2500,
      left: 50,
      title: "One schema for clients. No single team to write it.",
    },
    body: (
      <>
        <p>
          That makes the problem sharper, not easier. One GraphQL API needs one
          schema, and no single team can write it, because each team knows only
          its own part. Catalog can describe a product&apos;s name and weight.
          Billing can describe its price. Neither can describe the other&apos;s
          fields.
        </p>
        <p>
          Write the whole schema in one place and one team owns it again, with
          every other team in its queue. The schema is the thing that has to be
          shared. The services do not.
        </p>
      </>
    ),
    boxes: [
      {
        top: 2900,
        left: 27,
        paired: true,
        label: "Catalog · schema.graphql",
        color: CANON[0].color,
        lines: [
          { text: `type Product ${KEY} {`, accent: KEY },
          { text: "  id: ID!" },
          { text: "  name: String!" },
          { text: "  weight: Float!" },
          { text: "}" },
        ],
      },
      {
        top: 2900,
        left: 73,
        paired: true,
        label: "Billing · schema.graphql",
        color: CANON[1].color,
        lines: [
          { text: `type Product ${KEY} {`, accent: KEY },
          { text: "  id: ID!" },
          { text: "  price: Money!" },
          { text: "}" },
        ],
      },
    ],
  },
  {
    copy: {
      top: 3740,
      left: 50,
      title: "Federation merges the schemas, not the services.",
    },
    body: (
      <>
        <p>
          Each team keeps its service and gives it a GraphQL API: a GraphQL
          server in any language, or one placed in front of the REST service it
          already runs. The service is now called a subgraph, and the schema it
          publishes is called a source schema. A build step called composition
          reads the source schemas and merges them into one composite schema.
          Types with the same name, like Product, merge into one. A key, a field
          such as id marked @key, identifies the same product in every schema,
          so the executor can fetch it from any subgraph. Composition records
          which subgraph answers each field.
        </p>
        <p>
          Composition never sees the services, only their schemas. When two
          source schemas disagree, the build fails and names the field. Nothing
          else about the services changes: separate code, separate databases,
          separate deploys.
        </p>
      </>
    ),
    boxes: [
      {
        top: 4100,
        left: 50,
        label: "Composite schema",
        lines: [
          { text: "type Product {" },
          {
            text: "  id: ID!",
            dots: [CANON[0].color, CANON[1].color, CANON[3].color],
          },
          { text: "  name: String!", dots: [CANON[0].color] },
          { text: "  weight: Float!", dots: [CANON[0].color] },
          { text: "  price: Money!", dots: [CANON[1].color] },
          { text: "  delivery: String!", dots: [CANON[3].color] },
          { text: "}" },
        ],
      },
    ],
  },
  {
    copy: {
      top: 4600,
      left: 50,
      title: "A gateway serves the composite schema.",
    },
    body: (
      <>
        <p>
          Everything so far happened at build time. At runtime a gateway serves
          the composite schema: one endpoint and one schema for every client.
          Inside it, a distributed executor reads each query, works out which
          subgraph answers each field, calls only those with ordinary GraphQL
          queries, and assembles one response. The merging code every app used
          to write now runs in one place.
        </p>
        <p>
          Clients get one API. Teams keep their own services and release
          schedules, and each changes its part of the schema alone, with
          composition checking every change against the whole. You run one
          gateway and own one build step instead of merging code in every app.
        </p>
      </>
    ),
    boxes: [],
  },
];

const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

function TransitMap() {
  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      aria-hidden="true"
      className="absolute inset-0 z-0 hidden h-full w-full sm:block"
    >
      <defs>
        <linearGradient
          id="fw-out"
          x1="0"
          y1={HUB.y}
          x2="0"
          y2={H}
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#f27765" />
          <stop offset="0.55" stopColor="#eabd21" />
          <stop offset="0.8" stopColor="#66be77" />
          <stop offset="1" stopColor="#66be77" stopOpacity="0" />
        </linearGradient>
        <linearGradient id="fw-gap" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#fff" />
          <stop offset="0.18" stopColor="#333" />
          <stop offset="0.82" stopColor="#333" />
          <stop offset="1" stopColor="#fff" />
        </linearGradient>
        <mask
          id="fw-mask"
          maskUnits="userSpaceOnUse"
          x="0"
          y="0"
          width={W}
          height={H}
        >
          <rect x="0" y="0" width={W} height={H} fill="#fff" />
          {GAPS.map((g, i) => (
            <rect
              key={i}
              x={g.x}
              y={g.y}
              width={g.w}
              height={g.h}
              fill="url(#fw-gap)"
            />
          ))}
        </mask>
      </defs>

      <g mask="url(#fw-mask)">
        {MARKERS.map((m) => (
          <path
            key={m.s}
            d={streamPath(m.x, m.y + 12)}
            fill="none"
            stroke={CANON[m.s].color}
            strokeWidth={2.5}
            strokeOpacity={0.9}
            strokeLinecap="round"
          />
        ))}

        <rect
          x={HUB.x - 1.25}
          y={HUB.y + 12}
          width={2.5}
          height={H - HUB.y - 12}
          fill="url(#fw-out)"
        />
      </g>

      {MARKERS.map((m) => (
        <g key={m.s}>
          <rect
            x={m.x - 8}
            y={m.y - 8}
            width={16}
            height={16}
            rx={4}
            fill={CANON[m.s].color}
          />
          <text
            x={m.x + 20}
            y={m.y + 5}
            textAnchor="start"
            fontFamily={MONO}
            fontSize={13}
            letterSpacing="0.18em"
            fill={INK_DIM}
          >
            {CANON[m.s].name.toUpperCase()}
          </text>
        </g>
      ))}

      <GlowNode x={HUB.x} y={HUB.y} id="fw-hub" r={10} />
      <text
        x={HUB.x - 122}
        y={HUB.y + 4}
        textAnchor="end"
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        SCHEMA COMPOSITION
      </text>
      <line
        x1={HUB.x - 112}
        x2={HUB.x - 38}
        y1={HUB.y}
        y2={HUB.y}
        stroke="rgba(245,241,234,0.3)"
        strokeDasharray="4 5"
      />

      <line
        x1={120}
        x2={904}
        y1={HORIZON_Y}
        y2={HORIZON_Y}
        stroke="rgba(245,241,234,0.22)"
        strokeDasharray="5 7"
      />
      <text
        x={140}
        y={HORIZON_Y - 16}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        BUILD TIME
      </text>
      <text
        x={140}
        y={HORIZON_Y + 26}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
        opacity={0.7}
      >
        RUNTIME
      </text>

      <GatewayChip x={CHIP.x} y={CHIP.y} />
    </svg>
  );
}

export function TransitStory() {
  return (
    <section
      id="problem"
      className="border-cc-card-border scroll-mt-24 overflow-hidden border-t"
    >
      <PageSection maxWidth="6xl" className="pt-16 sm:pt-24">
        <SectionHeading
          align="center"
          title="What problem does GraphQL Federation solve?"
        />
      </PageSection>

      <div className="relative mx-auto w-full max-w-5xl sm:aspect-[1024/4900]">
        <TransitMap />
        <div className="flex flex-col gap-10 px-5 py-16 sm:contents">
          {STORY.map((chapter, i) => (
            <Fragment key={i}>
              <CopyBlock {...chapter.copy}>{chapter.body}</CopyBlock>
              {chapter.boxes.map((box, j) => (
                <CodeBox key={j} {...box} />
              ))}
            </Fragment>
          ))}
        </div>
      </div>
    </section>
  );
}
