import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import CIPage from "@/page-components/platform/continuous-integration";

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <CIPage recentPosts={recentPosts} />;
}
