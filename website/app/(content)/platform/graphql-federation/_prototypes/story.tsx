import type { ReactNode } from "react";

import { CANON } from "../visuals/stage";

export const SECTION_TITLE = "What problem does GraphQL Federation solve?";

interface CodeLine {
  readonly text: string;
  readonly dots?: readonly string[];
  readonly accent?: string;
}

interface Box {
  readonly label: string;
  readonly color?: string;
  readonly lines: readonly CodeLine[];
}

export interface Chapter {
  readonly title: string;
  readonly body: ReactNode;
  readonly boxes: readonly Box[];
}

const KEY = '@key(fields: "id")';

export const CHAPTERS: readonly Chapter[] = [
  {
    title: "Clients want one API. Teams want to ship alone.",
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
    title: "Answer one: every app merges the data itself.",
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
    title: "Answer two: one big API, one team, one queue.",
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
    title: "GraphQL gives clients one schema and one query.",
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
    title: "One schema for clients. No single team to write it.",
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
    title: "Federation merges the schemas, not the services.",
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
    title: "A gateway serves the composite schema.",
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

interface ProtoCodeBoxProps {
  readonly label: string;
  readonly color?: string;
  readonly lines: readonly CodeLine[];
}

/**
 * Chapter code-sample chrome cloned from TransitStory's `CodeBox`, minus the
 * absolute-positioning props: a static block a prototype can drop anywhere in
 * normal flow.
 */
export function ProtoCodeBox({ label, color, lines }: ProtoCodeBoxProps) {
  return (
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
  );
}
