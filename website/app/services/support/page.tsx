import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import SupportPage from "@/page-components/services/support";

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <SupportPage recentPosts={recentPosts} />;
}
