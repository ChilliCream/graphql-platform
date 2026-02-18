import React from "react";
import type { Metadata } from "next";

import { Providers } from "@/lib/providers";
import { siteMetadata } from "@/lib/site-config";
import { getLatestBlogPostForHeader } from "@/lib/blog";

export const metadata: Metadata = {
  title: {
    default: siteMetadata.title,
    template: `%s - ${siteMetadata.title}`,
  },
  description: siteMetadata.description,
  icons: {
    icon: "/icon.png",
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const latestBlogPost = getLatestBlogPostForHeader();

  return (
    <html lang="en">
      <head>
        <link
          href="https://fonts.googleapis.com/css2?family=Radio+Canada:wght@400;500;600;700&display=swap"
          rel="stylesheet"
        />
      </head>
      <body>
        <Providers latestBlogPost={latestBlogPost}>{children}</Providers>
      </body>
    </html>
  );
}
