import React from "react";
import { Space_Grotesk, JetBrains_Mono } from "next/font/google";

import { getRecentBlogPostTeasers } from "@/lib/blog";
import { createOrganizationJsonLd } from "@/lib/jsonld";
import { createMetadata } from "@/lib/metadata";
import { siteMetadata } from "@/lib/site-config";
import IndexPage from "@/page-components/index";

const spaceGrotesk = Space_Grotesk({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
  display: "swap",
  variable: "--cc-font-sans",
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ["latin"],
  weight: ["400", "500"],
  display: "swap",
  variable: "--cc-font-mono",
});

export const metadata = createMetadata({
  title: "Home",
  pageUrl: `${siteMetadata.siteUrl}/`,
  canonicalUrl: `${siteMetadata.siteUrl}/`,
});

export default function HomePage() {
  const recentPosts = getRecentBlogPostTeasers();
  const jsonLd = createOrganizationJsonLd();

  return (
    <div className={`${spaceGrotesk.variable} ${jetbrainsMono.variable}`}>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
      <IndexPage recentPosts={recentPosts} />
    </div>
  );
}
