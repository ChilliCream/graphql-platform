"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, Suspense, useEffect } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { AccentThread } from "@/components/redesign-system/AccentThread";
import { VariantSwitcher } from "@/components/redesign-system/cinematic/VariantSwitcher";
import {
  TemplatesCinematicGrid,
  TemplatesCinematicHero,
  TemplatesCinematicRoot,
} from "@/components/templates/cinematic";
import { TemplatesGridVariant } from "@/components/templates/grid";
import { TemplatesCtaStrip } from "@/components/templates/TemplatesCtaStrip";
import { TemplatesGrid } from "@/components/templates/TemplatesGrid";
import { TemplatesHero } from "@/components/templates/TemplatesHero";
import { TemplatesRoot } from "@/components/templates/TemplatesRoot";

// Dispatch shell: ?v=cinematic swaps in the cinematic variant of the
// gallery (chapter markers, prism chip filter row, featured-as-exhibit hero
// via InsetWindow). ?v=grid swaps in the Grid variant (Vercel-style square
// hairline-bordered card grid). The default variant is unchanged. The 8
// detail pages (/templates/[slug]) only have one variant, so they don't
// dispatch here.
const TemplatesPage: FC = () => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <SiteLayout disableStars>
      <SEO
        title="Templates"
        description="Production-ready GraphQL services, federations, and clients. Clone, customize, ship. Filter by topology, language, product mix, and agent-readiness."
      />
      <LandingGlobalStyle />
      <Suspense fallback={null}>
        <TemplatesPageBody />
      </Suspense>
    </SiteLayout>
  );
};

// Inner body reads ?v=cinematic from useSearchParams (which requires a
// Suspense boundary in the App Router) and dispatches to the right variant.
// The variant switcher lives at the top level of both branches so the user
// can hop between variants without losing filter state in the URL.
const TemplatesPageBody: FC = () => {
  const searchParams = useSearchParams();
  const variant = searchParams?.get("v");

  if (variant === "cinematic") {
    return <CinematicTemplates searchParams={searchParams} />;
  }
  if (variant === "grid") {
    return <GridTemplates searchParams={searchParams} />;
  }
  return <DefaultTemplates searchParams={searchParams} />;
};

interface VariantProps {
  readonly searchParams: ReturnType<typeof useSearchParams>;
}

interface VariantHrefs {
  readonly defaultHref: string;
  readonly cinematicHref: string;
  readonly gridHref: string;
}

const buildVariantHrefs = (
  searchParams: ReturnType<typeof useSearchParams>
): VariantHrefs => {
  const params = new URLSearchParams(searchParams?.toString() ?? "");
  params.delete("v");
  const baseQuery = params.toString();
  const defaultHref = baseQuery ? `/templates?${baseQuery}` : "/templates";
  const cinematicHref = baseQuery
    ? `/templates?${baseQuery}&v=cinematic`
    : "/templates?v=cinematic";
  const gridHref = baseQuery
    ? `/templates?${baseQuery}&v=grid`
    : "/templates?v=grid";
  return { defaultHref, cinematicHref, gridHref };
};

const variantOptions = (
  hrefs: VariantHrefs
): { id: string; label: string; href: string }[] => [
  { id: "default", label: "Default", href: hrefs.defaultHref },
  { id: "cinematic", label: "Cinematic", href: hrefs.cinematicHref },
  { id: "grid", label: "Grid", href: hrefs.gridHref },
];

const DefaultTemplates: FC<VariantProps> = ({ searchParams }) => {
  const hrefs = buildVariantHrefs(searchParams);
  return (
    <AccentThread page="templates">
      <TemplatesRoot>
        <TemplatesHero />
        <Suspense fallback={null}>
          <TemplatesGrid />
        </Suspense>
        <TemplatesCtaStrip />
      </TemplatesRoot>
      <VariantSwitcher currentId="default" options={variantOptions(hrefs)} />
    </AccentThread>
  );
};

const CinematicTemplates: FC<VariantProps> = ({ searchParams }) => {
  const hrefs = buildVariantHrefs(searchParams);
  return (
    <AccentThread page="templates">
      <TemplatesCinematicRoot>
        <TemplatesCinematicHero />
        <Suspense fallback={null}>
          <TemplatesCinematicGrid />
        </Suspense>
        <TemplatesCtaStrip />
      </TemplatesCinematicRoot>
      <VariantSwitcher currentId="cinematic" options={variantOptions(hrefs)} />
    </AccentThread>
  );
};

const GridTemplates: FC<VariantProps> = ({ searchParams }) => {
  const hrefs = buildVariantHrefs(searchParams);
  return (
    <AccentThread page="templates">
      <TemplatesGridVariant />
      <VariantSwitcher currentId="grid" options={variantOptions(hrefs)} />
    </AccentThread>
  );
};

export default TemplatesPage;
