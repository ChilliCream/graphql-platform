"use client";

import React, { FC, useEffect } from "react";

import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { SolutionPageRenderer } from "@/components/solutions/SolutionPageRenderer";
import { SolutionsRoot } from "@/components/solutions/SolutionsRoot";
import type { SolutionRecord } from "@/data/solutions/types";

interface SolutionPageProps {
  readonly record: SolutionRecord;
}

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
      <SolutionsRoot>
        <SolutionPageRenderer record={record} />
      </SolutionsRoot>
    </SiteLayout>
  );
};

export default SolutionPage;
