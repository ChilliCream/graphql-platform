import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import PricingPage from "@/page-components/pricing";

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <PricingPage recentPosts={recentPosts} />;
}
