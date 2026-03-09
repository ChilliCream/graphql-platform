import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import IndexPage from "@/page-components/index";

export default function HomePage() {
  const recentPosts = getRecentBlogPostTeasers();
  return <IndexPage recentPosts={recentPosts} />;
}
