"use client";

import React, { FC } from "react";

// Error feed → field-path breadcrumb → resolver source. Three-pane composite
// in the shape of the Vercel Logs mock. The resolver source is monospace
// with a faint red strip on the offending line, in our cream/navy palette
// (not GitHub).

interface FeedRow {
  readonly id: string;
  readonly msg: string;
  readonly time: string;
  readonly active?: boolean;
  readonly color?: string;
}

const FEED: readonly FeedRow[] = [
  {
    id: "e-401",
    msg: "TIMEOUT @ Billing.charge",
    time: "12:04",
    active: true,
    color: "var(--cc-col-cat)",
  },
  {
    id: "e-402",
    msg: "ECONNRESET @ Shipping.quote",
    time: "11:58",
    color: "var(--cc-col-shi)",
  },
  {
    id: "e-403",
    msg: "VALIDATION @ Catalog.products",
    time: "11:51",
    color: "var(--cc-col-cat)",
  },
  {
    id: "e-404",
    msg: "AUTH @ Users.session",
    time: "11:42",
    color: "var(--cc-col-usr)",
  },
  {
    id: "e-405",
    msg: "TIMEOUT @ Billing.charge",
    time: "11:33",
    color: "var(--cc-col-cat)",
  },
  {
    id: "e-406",
    msg: "VALIDATION @ Ordering.create",
    time: "11:30",
    color: "var(--cc-col-ord)",
  },
];

export const ErrorFeedMock: FC = () => {
  return (
    <div className="cc-error-mock">
      <div className="cc-error-feed">
        <div className="cc-error-feed-header">
          <span>Error feed</span>
          <span>last hour</span>
        </div>
        <ul>
          {FEED.map((row) => (
            <li key={row.id} className={row.active ? "is-active" : ""}>
              <span
                className="cc-error-dot"
                style={{ background: row.color ?? "var(--cc-col-cat)" }}
              />
              <span className="cc-error-msg">{row.msg}</span>
              <span className="cc-error-time">{row.time}</span>
            </li>
          ))}
        </ul>
      </div>
      <div className="cc-error-detail">
        <div className="cc-error-detail-header">
          <span>Field path</span>
          <span>Billing service</span>
        </div>
        <div className="cc-error-breadcrumb" aria-label="Field path">
          <span className="seg">Query</span>
          <span className="sep">.</span>
          <span className="seg">cart</span>
          <span className="sep">.</span>
          <span className="seg">items[2]</span>
          <span className="sep">.</span>
          <span className="seg">billingAddress</span>
          <span className="sep">.</span>
          <span className="hot">zip</span>
        </div>
        <div className="cc-error-source">
          <div>
            <span className="gutter">12</span>
            <span className="kw">async</span> <span className="kw">Task</span>
            &lt;
            <span className="ty">string</span>&gt;{" "}
            <span className="ty">ResolveZipAsync</span>(
          </div>
          <div>
            <span className="gutter">13</span>
            {"  "}
            <span className="ty">BillingAddress</span> address)
          </div>
          <div>
            <span className="gutter">14</span>
            {"{"}
          </div>
          <div>
            <span className="gutter">15</span>
            {"  "}
            <span className="kw">var</span> result ={" "}
            <span className="kw">await</span> _zipApi
          </div>
          <div className="err">
            <span className="gutter">16</span>
            {"    "}.<span className="ty">LookupAsync</span>
            (address.<span className="ty">PostalCode</span>);
          </div>
          <div>
            <span className="gutter">17</span>
            {"  "}
            <span className="kw">return</span> result.
            <span className="ty">Zip</span>;
          </div>
          <div>
            <span className="gutter">18</span>
            {"}"} <span className="com">// Billing.Resolvers/Address.cs</span>
          </div>
        </div>
      </div>
    </div>
  );
};
