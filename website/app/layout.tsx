import React from "react";
import type { Metadata } from "next";
import { Radio_Canada } from "next/font/google";
import Script from "next/script";
import { GoogleAnalytics } from "@next/third-parties/google";

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
        {process.env.NEXT_PUBLIC_COOKIEBOT_CBID && (
          <>
            <Script
              id="google-consent-default"
              strategy="beforeInteractive"
              dangerouslySetInnerHTML={{
                __html: `
                  window.dataLayer = window.dataLayer || [];
                  function gtag(){dataLayer.push(arguments);}
                  gtag('consent', 'default', {
                    analytics_storage: 'denied',
                    ad_storage: 'denied',
                    ad_user_data: 'denied',
                    ad_personalization: 'denied',
                    wait_for_update: 500
                  });
                `,
              }}
            />
            <Script
              id="Cookiebot"
              src="https://consent.cookiebot.com/uc.js"
              data-cbid={process.env.NEXT_PUBLIC_COOKIEBOT_CBID}
              data-blockingmode="auto"
              strategy="beforeInteractive"
            />
          </>
        )}
        <Providers latestBlogPost={latestBlogPost}>{children}</Providers>
        {process.env.NEXT_PUBLIC_COOKIEBOT_CBID &&
          process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID && (
            <GoogleAnalytics gaId={process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID} />
          )}
      </body>
    </html>
  );
}
