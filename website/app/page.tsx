import React from "react";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import { createOrganizationJsonLd } from "@/lib/jsonld";
import { createMetadata } from "@/lib/metadata";
import { siteMetadata } from "@/lib/site-config";
import IndexPage from "@/page-components/index";

export const metadata = createMetadata({
  title: "Home",
  pageUrl: `${siteMetadata.siteUrl}/`,
  canonicalUrl: `${siteMetadata.siteUrl}/`,
});

export default function HomePage() {
  const recentPosts = getRecentBlogPostTeasers();
  const jsonLd = createOrganizationJsonLd();

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
      <IndexPage recentPosts={recentPosts} />
    </>
  );
}
