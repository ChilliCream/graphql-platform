"use client";

import Link from "next/link";
import React, { FC } from "react";

import { categoryLabel } from "@/data/integrations/categories";
import type { Integration } from "@/data/integrations/integrations";
import { productLabel } from "@/data/templates/filters";

interface IntegrationSidebarProps {
  readonly integration: Integration;
}

interface SidebarLinkProps {
  readonly label: string;
  readonly href: string;
}

const SidebarLink: FC<SidebarLinkProps> = ({ label, href }) => (
  <a href={href} className="cc-ind-sidebar-link" rel="noopener" target="_blank">
    <span>{label}</span>
    <span className="arrow">↗</span>
  </a>
);

// Sticky sidebar metadata block. Predictable shape across every listing,
// same intent as Vercel's per-listing sidebar but with our axes: Type pill
// (Native = cream background, Community = ghost), Categories, Products,
// Min version, Maintained by, then the external links rail. Categories and
// Products are clickable, jumping back to the index with the correct
// pre-filter so the page is a hub, not a dead end.
export const IntegrationSidebar: FC<IntegrationSidebarProps> = ({
  integration,
}) => {
  return (
    <aside className="cc-ind-sidebar" aria-label="Integration metadata">
      <div className="cc-ind-sidebar-row">
        <span className="label">Type</span>
        <span className={`cc-ind-sidebar-typebadge is-${integration.type}`}>
          {integration.type === "native" ? "Native" : "Community"}
        </span>
      </div>

      <div className="cc-ind-sidebar-row">
        <span className="label">Categories</span>
        <div className="cc-ind-sidebar-tagchips">
          <Link
            href={`/integrations?category=${integration.category}`}
            className="cc-ind-sidebar-tagchip"
          >
            {categoryLabel(integration.category)}
          </Link>
        </div>
      </div>

      {integration.products.length > 0 && (
        <div className="cc-ind-sidebar-row">
          <span className="label">Products</span>
          <div className="cc-ind-sidebar-tagchips">
            {integration.products.map((p) => (
              <span key={p} className="cc-ind-sidebar-tagchip">
                {productLabel(p)}
              </span>
            ))}
          </div>
        </div>
      )}

      <div className="cc-ind-sidebar-row">
        <span className="label">Min version</span>
        <span className="value">{integration.minVersion}</span>
      </div>

      <div className="cc-ind-sidebar-row">
        <span className="label">Maintained by</span>
        <span className="value">{integration.maintainer}</span>
      </div>

      <div className="cc-ind-sidebar-divider" aria-hidden />

      <div className="cc-ind-sidebar-row">
        <span className="label">Links</span>
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          {integration.links.docs && (
            <SidebarLink label="Documentation" href={integration.links.docs} />
          )}
          {integration.links.nuget && (
            <SidebarLink label="NuGet package" href={integration.links.nuget} />
          )}
          {integration.links.github && (
            <SidebarLink label="GitHub repo" href={integration.links.github} />
          )}
          {integration.links.website && (
            <SidebarLink
              label="Provider site"
              href={integration.links.website}
            />
          )}
        </div>
      </div>
    </aside>
  );
};
