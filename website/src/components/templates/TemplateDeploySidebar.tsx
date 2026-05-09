"use client";

import Link from "next/link";
import React, { FC } from "react";

import {
  clientLabel,
  languageLabel,
  productLabel,
  topologyLabel,
  useCaseLabel,
} from "@/data/templates/filters";
import type { Template } from "@/data/templates/templates";

import { CliTabs } from "./CliTabs";

interface TemplateDeploySidebarProps {
  readonly template: Template;
}

// Sticky deploy sidebar — the equivalent of Vercel's "Deploy" sidebar but
// with a copy-to-clipboard CLI snippet as the primary action and an
// "Open in Nitro" upsell as the secondary. The Nitro button only shows for
// templates that have Nitro in the product mix; the rest get a simpler
// surface. Tertiary links are GitHub and (when available) live demo. Tag
// chips are clickable filters back into /templates so the detail page is
// a hub, not a dead end.
export const TemplateDeploySidebar: FC<TemplateDeploySidebarProps> = ({
  template,
}) => {
  const hasNitro = template.products.some((p) => p === "nitro");

  return (
    <aside className="cc-tpd-sidebar" aria-label="Deploy">
      <h2 className="cc-tpd-sidebar-section-title">Get started</h2>
      <CliTabs commands={template.cli} />

      {hasNitro && (
        <a
          href={`https://nitro.chillicream.com/init?template=${template.slug}`}
          className="cc-tpd-sidebar-cta is-primary"
          rel="noopener"
        >
          Open in Nitro →
        </a>
      )}
      <a
        href={template.githubUrl}
        className="cc-tpd-sidebar-cta"
        rel="noopener"
        target="_blank"
      >
        View on GitHub →
      </a>
      {template.demoUrl ? (
        <a
          href={template.demoUrl}
          className="cc-tpd-sidebar-cta"
          rel="noopener"
          target="_blank"
        >
          Live demo →
        </a>
      ) : null}

      <div className="cc-tpd-sidebar-divider" aria-hidden />

      <div className="cc-tpd-sidebar-row">
        <span className="label">Topology</span>
        <Link
          className="cc-tpd-sidebar-tagchip"
          href={`/templates?topology=${template.topology}`}
        >
          {topologyLabel(template.topology)}
        </Link>
      </div>

      <div className="cc-tpd-sidebar-row">
        <span className="label">Stack</span>
        <div className="cc-tpd-sidebar-tagchips">
          {template.products.map((p) => (
            <Link
              key={p}
              href={`/templates?product=${p}`}
              className="cc-tpd-sidebar-tagchip"
            >
              {productLabel(p)}
            </Link>
          ))}
        </div>
      </div>

      <div className="cc-tpd-sidebar-row">
        <span className="label">Language</span>
        <Link
          className="cc-tpd-sidebar-tagchip"
          href={`/templates?language=${template.language}`}
        >
          {languageLabel(template.language)}
        </Link>
      </div>

      {template.useCases.length > 0 && (
        <div className="cc-tpd-sidebar-row">
          <span className="label">Use case</span>
          <div className="cc-tpd-sidebar-tagchips">
            {template.useCases.map((u) => (
              <Link
                key={u}
                href={`/templates?use=${u}`}
                className="cc-tpd-sidebar-tagchip"
              >
                {useCaseLabel(u)}
              </Link>
            ))}
          </div>
        </div>
      )}

      {template.clients.length > 0 && template.clients[0] !== "none" && (
        <div className="cc-tpd-sidebar-row">
          <span className="label">Client</span>
          <div className="cc-tpd-sidebar-tagchips">
            {template.clients.map((c) => (
              <Link
                key={c}
                href={`/templates?client=${c}`}
                className="cc-tpd-sidebar-tagchip"
              >
                {clientLabel(c)}
              </Link>
            ))}
          </div>
        </div>
      )}

      <div className="cc-tpd-sidebar-row">
        <span className="label">Agent-ready</span>
        <Link
          className="cc-tpd-sidebar-tagchip"
          href={template.agentReady ? "/templates?agent=yes" : "/templates"}
        >
          {template.agentReady ? "Yes" : "No"}
        </Link>
      </div>

      <div className="cc-tpd-sidebar-divider" aria-hidden />

      <div className="cc-tpd-sidebar-row">
        <span className="label">License</span>
        <span className="value">
          <a
            href="https://opensource.org/licenses/MIT"
            rel="noopener"
            target="_blank"
          >
            {template.license}
          </a>
        </span>
      </div>
      <div className="cc-tpd-sidebar-row">
        <span className="label">Updated</span>
        <span className="value">{template.updatedRelative}</span>
      </div>
    </aside>
  );
};
