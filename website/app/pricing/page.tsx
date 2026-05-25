import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import { createMetadata } from "@/lib/metadata";
import { siteMetadata } from "@/lib/site-config";
import PricingPage from "@/page-components/pricing";

export const metadata = createMetadata({
  title: "Pricing",
  pageUrl: `${siteMetadata.siteUrl}/pricing/`,
  canonicalUrl: `${siteMetadata.siteUrl}/pricing/`,
});

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <PricingPage recentPosts={recentPosts} />;
}
