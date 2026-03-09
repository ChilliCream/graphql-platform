import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import TrainingPage from "@/page-components/services/training";

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <TrainingPage recentPosts={recentPosts} />;
}
