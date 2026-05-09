"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, Suspense, useEffect, useMemo } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { VariantSwitcher } from "@/components/redesign-system/cinematic";
import { SolutionPageRenderer } from "@/components/solutions/SolutionPageRenderer";
import { SolutionsRoot } from "@/components/solutions/SolutionsRoot";
import { SolutionPageRendererCinematic } from "@/components/solutions/cinematic/SolutionPageRendererCinematic";
import { SolutionsCinematicRoot } from "@/components/solutions/cinematic/SolutionsCinematicRoot";
import { SolutionPageRendererGrid } from "@/components/solutions/grid/SolutionPageRendererGrid";
import type { SolutionRecord } from "@/data/solutions/types";

interface SolutionPageProps {
  readonly record: SolutionRecord;
}

// SolutionPage dispatches on `?v=` to render one of three sibling templates:
//   default    -> band-stacked composition (`SolutionPageRenderer`)
//   cinematic  -> default composition + per-slug astronomical chart
//                 (`SolutionPageRendererCinematic`)
//   grid       -> hairline-bordered square grid composition
//                 (`SolutionPageRendererGrid`)
//
// All three variants reuse the same data file (`solutions.ts`), the same
// accent thread, and the same shared illustration vocabulary; only the
// rendering differs.
//
// The variant switcher is rendered at the top level so visitors can hop
// between variants with one click; useSearchParams requires a Suspense
// boundary under the App Router.
const SolutionPage: FC<SolutionPageProps> = ({ record }) => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <SiteLayout disableStars>
      <SEO title={record.title} description={record.metaDescription} />
      <LandingGlobalStyle />
      <Suspense fallback={<DefaultBody record={record} />}>
        <SolutionPageBody record={record} />
      </Suspense>
    </SiteLayout>
  );
};

const SolutionPageBody: FC<SolutionPageProps> = ({ record }) => {
  const searchParams = useSearchParams();
  const requested = searchParams.get("v");
  const variant: "default" | "cinematic" | "grid" =
    requested === "cinematic"
      ? "cinematic"
      : requested === "grid"
      ? "grid"
      : "default";

  const switcherOptions = useMemo(
    () => [
      {
        id: "default",
        label: "Default",
        href: `/solutions/${record.slug}/`,
      },
      {
        id: "cinematic",
        label: "Cinematic",
        href: `/solutions/${record.slug}/?v=cinematic`,
      },
      {
        id: "grid",
        label: "Grid",
        href: `/solutions/${record.slug}/?v=grid`,
      },
    ],
    [record.slug]
  );

  if (variant === "cinematic") {
    return (
      <>
        <SolutionsCinematicRoot slug={record.slug}>
          <SolutionPageRendererCinematic record={record} />
        </SolutionsCinematicRoot>
        <VariantSwitcher options={switcherOptions} currentId="cinematic" />
      </>
    );
  }

  if (variant === "grid") {
    return (
      <>
        <SolutionPageRendererGrid record={record} slug={record.slug} />
        <VariantSwitcher options={switcherOptions} currentId="grid" />
      </>
    );
  }

  return (
    <>
      <DefaultBody record={record} />
      <VariantSwitcher options={switcherOptions} currentId="default" />
    </>
  );
};

const DefaultBody: FC<SolutionPageProps> = ({ record }) => (
  <SolutionsRoot>
    <SolutionPageRenderer record={record} />
  </SolutionsRoot>
);

export default SolutionPage;
