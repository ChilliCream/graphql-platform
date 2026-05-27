"use client";

import React from "react";

interface Post {
  tag: string;
  date: string;
  title: string;
  excerpt: string;
  image: string;
  href: string;
}

const POSTS: Post[] = [
  {
    tag: "Capability",
    date: "Apr 2026",
    title: "Semantic Introspection.",
    excerpt:
      "Schema introspection that understands meaning — so agents and tools can navigate your graph the way humans do.",
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

export const FinalCta: React.FC = () => (
  <section className="act final-cta" data-screen-label="07 Fancy a drink?">
    <div className="act-label">
      <span className="num">07</span> Fancy a drink?
    </div>
    <div className="final-cta-inner">
      <div className="eyebrow">Ready when you are</div>
      <h2 className="display">Do you fancy a drink?</h2>
      <p>
        Start free. Scale when you need to. Talk to a human whenever you want
        one.
      </p>
      <div className="cta-stack">
        <button className="btn btn-primary">Start pouring →</button>
        <button className="btn btn-ghost">Talk to us</button>
      </div>
    </div>
  </section>
);

export const Blog: React.FC = () => (
  <section className="act blog" data-screen-label="08 From the blog">
    <div className="act-label">
      <span className="num">08</span> From the blog
    </div>
    <div className="act-heading section-headline-fade">
      <div className="eyebrow">From the blog</div>
      <h2 className="display">What we're brewing.</h2>
    </div>
    <div className="blog-grid">
      {POSTS.map((p) => (
        <a
          key={p.href}
          href={p.href}
          target="_blank"
          rel="noopener noreferrer"
          className="blog-card"
        >
          <img
            className="blog-image"
            src={p.image}
            alt={p.title}
            loading="lazy"
          />
          <div className="blog-body">
            <div className="blog-meta">
              <span className="blog-tag">{p.tag}</span>
              <span className="blog-date">{p.date}</span>
            </div>
            <h3 className="blog-title">{p.title}</h3>
            <p className="blog-excerpt">{p.excerpt}</p>
            <span className="blog-cta">Read →</span>
          </div>
        </a>
      ))}
    </div>
  </section>
);

export const LandingFooter: React.FC = () => (
  <footer className="foot">
    <div className="col">
      <h4>ChilliCream</h4>
      <span>The API platform for humans and agents.</span>
    </div>
    <div className="col">
      <h4>Products</h4>
      <a href="#">Hot Chocolate</a>
      <a href="#">Nitro</a>
      <a href="#">Mocha</a>
      <a href="#">Strawberry Shake</a>
    </div>
    <div className="col">
      <h4>Platform</h4>
      <a href="#">Fusion</a>
      <a href="#">Bananacakes</a>
      <a href="#">Adapters</a>
    </div>
    <div className="col">
      <h4>Resources</h4>
      <a href="#">Docs</a>
      <a href="#">Examples</a>
      <a href="#">Community</a>
    </div>
  </footer>
);
