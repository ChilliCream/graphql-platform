import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import ContactPage from "@/page-components/services/support/contact";

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <ContactPage recentPosts={recentPosts} />;
}
