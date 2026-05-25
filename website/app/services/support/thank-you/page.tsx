import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import ThankYouPage from "@/page-components/services/support/thank-you";

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <ThankYouPage recentPosts={recentPosts} />;
}
