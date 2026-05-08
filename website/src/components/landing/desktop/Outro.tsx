"use client";

import React from "react";

export const FinalCta: React.FC = () => {
  return (
    <section
      className="cc-act cc-act-final-cta"
      data-screen-label="07 Pour your platform"
    >
      <div className="cc-act-label">
        <span className="num">07</span> Fancy a drink?
      </div>
      <div className="cc-final-cta-inner-d">
        <div className="eyebrow">Ready when you are</div>
        <h2 className="display">Do you fancy a drink?</h2>
        <p>
          Start free. Scale when you need to. Talk to a human whenever you want
          one.
        </p>
        <div className="cc-cta-row">
          <button className="cc-btn cc-btn-primary">Start pouring →</button>
          <button className="cc-btn cc-btn-ghost">Talk to us</button>
        </div>
      </div>
    </section>
  );
};

const POSTS = [
  {
    tag: "Capability",
    date: "Apr 2026",
    title: "Semantic Introspection.",
    excerpt:
      "Schema introspection that understands meaning, not just shape — so agents and tools can navigate your graph the way humans do.",
    image:
      "https://chillicream.com/images/blog/2026-04-22-semantic-introspection/header.png",
    href: "https://chillicream.com/blog/2026/04/22/semantic-introspection/",
  },
  {
    tag: "Observability",
    date: "Mar 2025",
    title: "OpenTelemetry for all your services.",
    excerpt:
      "Trace requests end-to-end across every service in your federated graph — without bespoke instrumentation.",
    image:
      "https://chillicream.com/images/blog/2025-03-17-open-telemetry-for-everyone/header.png",
    href: "https://chillicream.com/blog/2025/03/17/telemetry/",
  },
  {
    tag: "Release",
    date: "Feb 2025",
    title: "What's new in Hot Chocolate 15.",
    excerpt:
      "Faster execution, sharper diagnostics, and the schema-first refinements teams have been asking for.",
    image:
      "https://chillicream.com/images/blog/2025-02-01-hot-chocolate-15/hot-chocolate-15.png",
    href: "https://chillicream.com/blog/2025/02/01/hot-chocolate-15/",
  },
];

export const Blog: React.FC = () => {
  return (
    <section
      className="cc-act cc-act-blog"
      data-screen-label="08 From the blog"
    >
      <div className="cc-act-label">
        <span className="num">08</span> From the blog
      </div>
      <div className="cc-blog-inner-d">
        <div className="cc-blog-heading-d">
          <div className="eyebrow">From the blog</div>
          <h2 className="display">What we're brewing.</h2>
        </div>
        <div className="cc-blog-grid-d">
          {POSTS.map((p) => (
            <a
              key={p.href}
              href={p.href}
              target="_blank"
              rel="noopener noreferrer"
              className="cc-blog-card-d"
            >
              <div
                className="cc-blog-image-d"
                style={{ backgroundImage: `url(${p.image})` }}
                role="img"
                aria-label={p.title}
              />
              <div className="cc-blog-body-d">
                <div className="cc-blog-meta-d">
                  <span className="cc-blog-tag-d">{p.tag}</span>
                  <span>{p.date}</span>
                </div>
                <h3 className="cc-blog-title-d">{p.title}</h3>
                <p className="cc-blog-excerpt-d">{p.excerpt}</p>
                <span className="cc-blog-cta-d">Read →</span>
              </div>
            </a>
          ))}
        </div>
      </div>
    </section>
  );
};
