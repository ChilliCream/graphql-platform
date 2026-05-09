"use client";

import React, { FC, useEffect } from "react";

import { AtAGlance } from "@/components/customers/AtAGlance";
import { CaseStudyCard } from "@/components/customers/CaseStudyCard";
import { CustomersRoot } from "@/components/customers/CustomersRoot";
import { ReferenceCallCta } from "@/components/customers/ReferenceCallCta";
import { StoryBody } from "@/components/customers/StoryBody";
import { StoryHeader } from "@/components/customers/StoryHeader";
import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { findRelated, type Story } from "@/data/customers/stories";

interface CustomerStoryPageProps {
  readonly story: Story;
}

const CustomerStoryPage: FC<CustomerStoryPageProps> = ({ story }) => {
  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  const related = findRelated(story);

  return (
    <SiteLayout disableStars>
      <SEO
        title={`${story.displayName} customer story`}
        description={story.subhead}
      />
      <LandingGlobalStyle />
      <CustomersRoot>
        <StoryHeader story={story} />

        <section className="cc-csd-section cc-csd-body-section">
          <div className="cc-csd-body-inner">
            <StoryBody sections={story.sections} />
            <AtAGlance data={story.atAGlance} keyMetrics={story.keyMetrics} />
          </div>
        </section>

        <ReferenceCallCta />

        <section className="cc-csd-section cc-csd-related">
          <div className="cc-csd-related-inner">
            <div className="cc-cu-heading">
              <div className="eyebrow">More stories</div>
              <h2 className="display">Stories like this one.</h2>
            </div>
            <div className="cc-cu-cards-grid">
              {related.map((s) => (
                <CaseStudyCard key={s.slug} story={s} />
              ))}
            </div>
          </div>
        </section>
      </CustomersRoot>
    </SiteLayout>
  );
};

export default CustomerStoryPage;
