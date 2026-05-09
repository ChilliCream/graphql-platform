"use client";

import Link from "next/link";
import React, { FC } from "react";

import { topologyLabel } from "@/data/templates/filters";
import type { Template } from "@/data/templates/templates";

interface TemplateDetailHeaderProps {
  readonly template: Template;
}

// Detail-page header: breadcrumb, H1, one-line tagline. We keep the hero
// understated — the README body and the sticky deploy sidebar are the
// page's real estate; the header just labels the surface and points back
// to the gallery.
export const TemplateDetailHeader: FC<TemplateDetailHeaderProps> = ({
  template,
}) => {
  return (
    <section className="cc-tpd-section cc-tpd-header">
      <div className="cc-section-label">
        <span className="num">01</span> Template
      </div>
      <div className="cc-tpd-header-inner">
        <nav className="cc-tpd-breadcrumb" aria-label="Breadcrumb">
          <Link href="/templates">Templates</Link>
          <span className="sep" aria-hidden>
            /
          </span>
          <Link href={`/templates?topology=${template.topology}`} className="">
            {topologyLabel(template.topology)}
          </Link>
          <span className="sep" aria-hidden>
            /
          </span>
          <span className="crumb-current">{template.title}</span>
        </nav>
        <h1 className="display">{template.title}</h1>
        <p className="cc-tpd-tagline">{template.tagline}</p>
      </div>
    </section>
  );
};
