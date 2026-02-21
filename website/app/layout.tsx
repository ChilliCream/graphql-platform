import React from "react";
import type { Metadata } from "next";
import { Radio_Canada } from "next/font/google";

import { Providers } from "@/lib/providers";
import { siteMetadata } from "@/lib/site-config";
import { getLatestBlogPostForHeader } from "@/lib/blog";

const radioCanada = Radio_Canada({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
  display: "swap",
  variable: "--font-radio-canada",
});

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
    <html lang="en" className={radioCanada.variable}>
      <body>
        <Providers latestBlogPost={latestBlogPost}>{children}</Providers>
      </body>
    </html>
  );
}
