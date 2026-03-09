"use client";

import { SiteLayout } from "@/components/layout";
import { ContentSection, SEO, SupportForm } from "@/components/misc";
import {
  MostRecentBlogPostsSection,
  NewsletterSection,
} from "@/components/widgets";
import { RecentBlogPost } from "@/components/widgets/most-recent-blog-posts-section";
import { SUPPORT_PLANS, SupportPlan } from "@/types/support";
import { getValidatedQueryParam } from "@/utils/url-helpers";
import React, { FC, useEffect, useState } from "react";

interface SupportContactPageProps {
  recentPosts?: RecentBlogPost[];
}

const SupportContactPage: FC<SupportContactPageProps> = ({ recentPosts }) => {
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
      <MostRecentBlogPostsSection posts={recentPosts} />
    </SiteLayout>
  );
};

export default SupportContactPage;
