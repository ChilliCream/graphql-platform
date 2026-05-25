import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import EcosystemPage from "@/page-components/platform/ecosystem";

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <EcosystemPage recentPosts={recentPosts} />;
}
