import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import AnalyticsPage from "@/page-components/platform/analytics";

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <AnalyticsPage recentPosts={recentPosts} />;
}
