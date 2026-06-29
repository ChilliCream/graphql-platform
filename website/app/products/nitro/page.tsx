import React from "react";

import { getRecentNitroBlogPostTeasers } from "@/lib/blog";
import { createMetadata } from "@/lib/metadata";
import { siteMetadata } from "@/lib/site-config";
import NitroPage from "@/page-components/products/nitro";

export const metadata = createMetadata({
  title: "Nitro - GraphQL IDE",
  description:
    "Nitro is an incredible, beautiful, and feature-rich GraphQL IDE for developers that works with any GraphQL APIs.",
  pageUrl: `${siteMetadata.siteUrl}/products/nitro/`,
  canonicalUrl: `${siteMetadata.siteUrl}/products/nitro/`,
});

export default function Page() {
  const recentPosts = getRecentNitroBlogPostTeasers();
  return <NitroPage recentPosts={recentPosts} />;
}
