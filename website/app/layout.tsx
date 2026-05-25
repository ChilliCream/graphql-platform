import React from "react";
import type { Metadata } from "next";
import { Radio_Canada } from "next/font/google";
import Script from "next/script";

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
  metadataBase: new URL(siteMetadata.siteUrl),
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
      <head>
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
        {process.env.NEXT_PUBLIC_COOKIEBOT_CBID &&
          process.env.NEXT_PUBLIC_GTM_ID && (
            <Script
              id="gtm-script"
              strategy="beforeInteractive"
              dangerouslySetInnerHTML={{
                __html: `
                window.dataLayer = window.dataLayer || [];
                (function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':
                new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],
                j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src=
                'https://www.googletagmanager.com/gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);
                })(window,document,'script','dataLayer','${process.env.NEXT_PUBLIC_GTM_ID}');
              `,
              }}
            />
          )}
      </head>
      <body>
        {process.env.NEXT_PUBLIC_COOKIEBOT_CBID &&
          process.env.NEXT_PUBLIC_GTM_ID && (
            <noscript>
              <iframe
                src={`https://www.googletagmanager.com/ns.html?id=${process.env.NEXT_PUBLIC_GTM_ID}`}
                height="0"
                width="0"
                style={{ display: "none", visibility: "hidden" }}
              />
            </noscript>
          )}
        <Providers latestBlogPost={latestBlogPost}>{children}</Providers>
      </body>
    </html>
  );
}
