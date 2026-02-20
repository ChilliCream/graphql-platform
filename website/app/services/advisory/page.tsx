import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import AdvisoryPage from "@/page-components/services/advisory";

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <AdvisoryPage recentPosts={recentPosts} />;
}
