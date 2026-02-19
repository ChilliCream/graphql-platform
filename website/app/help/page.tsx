import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import HelpPage from "@/page-components/help";

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <HelpPage recentPosts={recentPosts} />;
}
