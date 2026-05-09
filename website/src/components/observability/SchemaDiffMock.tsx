"use client";

import React, { FC, ReactNode } from "react";

// PR check + schema-diff hunk + audit feed composite. The diff hunk shows a
// real-looking GraphQL field removal in red + a replacement in green, with
// gutter line numbers in --cc-ink-faint and field types syntax-highlighted in
// our cream/navy palette (we are NOT pretending to be GitHub).

interface DiffLine {
  readonly n: string;
  readonly kind: "ctx" | "add" | "del";
  readonly content: ReactNode;
}

const DIFF: readonly DiffLine[] = [
  {
    n: "12",
    kind: "ctx",
    content: <span className="kw">type</span>,
  },
  {
    n: "12",
    kind: "ctx",
    content: (
      <>
        <span className="kw">type</span>{" "}
        <span className="ty">BillingAddress</span> {"{"}
      </>
    ),
  },
  {
    n: "13",
    kind: "ctx",
    content: <>{"  "}street: String!</>,
  },
  {
    n: "14",
    kind: "ctx",
    content: <>{"  "}city: String!</>,
  },
  {
    n: "15",
    kind: "del",
    content: <>{"  "}zip: String!</>,
  },
  {
    n: "15",
    kind: "add",
    content: <>{"  "}postalCode: String!</>,
  },
  {
    n: "16",
    kind: "ctx",
    content: <>{"  "}country: String!</>,
  },
  {
    n: "17",
    kind: "ctx",
    content: <>{"}"}</>,
  },
  {
    n: "18",
    kind: "ctx",
    content: <></>,
  },
  {
    n: "19",
    kind: "ctx",
    content: (
      <>
        <span className="kw">extend type</span> <span className="ty">Cart</span>{" "}
        {"{"}
      </>
    ),
  },
  {
    n: "20",
    kind: "del",
    content: <>{"  "}billingAddress: BillingAddress!</>,
  },
  {
    n: "20",
    kind: "add",
    content: <>{"  "}billingAddress: BillingAddress @deprecated</>,
  },
  {
    n: "21",
    kind: "ctx",
    content: <>{"}"}</>,
  },
];

interface AuditEntry {
  readonly id: string;
  readonly title: string;
  readonly who: string;
  readonly when: string;
}

const AUDIT: readonly AuditEntry[] = [
  {
    id: "a1",
    title: "Renamed Cart.billingAddress.zip → postalCode",
    who: "billing-team",
    when: "today",
  },
  {
    id: "a2",
    title: "Added Shipping.quote(currency) argument",
    who: "shipping-team",
    when: "yesterday",
  },
  {
    id: "a3",
    title: "Promoted Catalog v2.4.1 to production",
    who: "catalog-team",
    when: "3 days ago",
  },
  {
    id: "a4",
    title: "Removed Users.legacyId (after deprecation window)",
    who: "users-team",
    when: "1 week ago",
  },
];

export const SchemaDiffMock: FC = () => {
  return (
    <div className="cc-schema-mock">
      <div className="cc-schema-stack">
        <div className="cc-schema-pr">
          <div className="cc-schema-pr-row">
            <span className="check is-ok" aria-hidden>
              <svg viewBox="0 0 12 12" width="10" height="10">
                <path
                  d="M2 6.5 L5 9 L10 3"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.6"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </span>
            <span className="name">composition / fusion</span>
            <span className="kind">passed</span>
          </div>
          <div className="cc-schema-pr-row">
            <span className="check is-fail" aria-hidden>
              <svg viewBox="0 0 12 12" width="9" height="9">
                <path
                  d="M3 3 L9 9 M9 3 L3 9"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.6"
                  strokeLinecap="round"
                />
              </svg>
            </span>
            <span className="name">breaking-change-detection</span>
            <span className="kind">1 breaking</span>
          </div>
          <div className="cc-schema-pr-row">
            <span className="check is-ok" aria-hidden>
              <svg viewBox="0 0 12 12" width="10" height="10">
                <path
                  d="M2 6.5 L5 9 L10 3"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.6"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </span>
            <span className="name">subgraph-tests / billing</span>
            <span className="kind">passed</span>
          </div>
        </div>

        <div className="cc-schema-diff" aria-label="Schema diff">
          <div className="cc-schema-diff-header">
            <span>billing.graphql</span>
            <span>+2 −2</span>
          </div>
          <pre>
            {DIFF.map((line, i) => {
              const cls =
                line.kind === "add"
                  ? "ln is-add"
                  : line.kind === "del"
                  ? "ln is-del"
                  : "ln";
              const sign =
                line.kind === "add" ? "+" : line.kind === "del" ? "−" : " ";
              return (
                <div key={i} className={cls}>
                  <span className="gutter">{line.n}</span>
                  <span className="sign">{sign}</span>
                  <span>{line.content}</span>
                </div>
              );
            })}
          </pre>
        </div>
      </div>

      <div className="cc-schema-audit">
        <div className="cc-schema-audit-header">Schema audit log</div>
        <ul>
          {AUDIT.map((entry) => (
            <li key={entry.id}>
              <div>
                {entry.title}
                <div className="who">@{entry.who}</div>
              </div>
              <div className="when">{entry.when}</div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
};
