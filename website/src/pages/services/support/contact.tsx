import { SiteLayout } from "@/components/layout";
import { ContentSection, SEO, SupportForm } from "@/components/misc";
import {
  MostRecentBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";
import { SUPPORT_PLANS, SupportPlan } from "@/types/support";
import { getValidatedQueryParam } from "@/utils/url-helpers";
import React, { FC, useEffect, useState } from "react";

const SupportContactPage: FC = () => {
  const [selectedPlan, setSelectedPlan] = useState<SupportPlan>("Startup");

  useEffect(() => {
    const planFromUrl = getValidatedQueryParam("plan", SUPPORT_PLANS);
    if (planFromUrl) {
      setSelectedPlan(planFromUrl);
    }
  }, []);

  return (
    <SiteLayout>
      <SEO title="Contact Support" />
      <ContentSection noBackground titleSpace="small">
        <SupportForm initialPlan={selectedPlan} />
      </ContentSection>
      <NewsletterSection />
      <MostRecentBlogPostsSection />
    </SiteLayout>
  );
};

export default SupportContactPage;
