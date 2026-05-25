import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import { createMetadata } from "@/lib/metadata";
import { siteMetadata } from "@/lib/site-config";
import HelpPage from "@/page-components/help";

export const metadata = createMetadata({
  title: "Help",
  pageUrl: `${siteMetadata.siteUrl}/help/`,
  canonicalUrl: `${siteMetadata.siteUrl}/help/`,
});

export default function Page() {
  const recentPosts = getRecentBlogPostTeasers();
  return <HelpPage recentPosts={recentPosts} />;
}
