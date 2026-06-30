import "./globals.css";
import type { Metadata } from "next";
import { Inter, Josefin_Sans } from "next/font/google";
import { Analytics } from "@/src/components/Analytics";
import { AnalyticsScripts } from "@/src/components/AnalyticsScripts";
import { EnableSmoothScroll } from "@/src/components/EnableSmoothScroll";
import Footer from "@/src/components/Footer";
import Header from "@/src/components/Header";
import { StructuredData } from "@/src/components/StructuredData";
import {
  SITE_NAME,
  SITE_TITLE,
  TITLE_TEMPLATE,
  TWITTER_HANDLE,
} from "@/src/helpers/site";
import { SITE_URL } from "@/src/helpers/siteUrl";

const inter = Inter({
  subsets: ["latin"],
  display: "swap",
  variable: "--font-inter",
});

const josefinSans = Josefin_Sans({
  subsets: ["latin"],
  display: "swap",
  variable: "--font-josefin-sans",
});

const DESCRIPTION =
  "The ChilliCream GraphQL Platform: build, connect, and observe GraphQL APIs with Hot Chocolate, Fusion, Strawberry Shake, and Nitro.";

export const metadata: Metadata = {
  metadataBase: new URL(SITE_URL),
  // Preview/staging deployments emit `<meta name="robots" content="noindex, nofollow">`.
  ...(process.env.NEXT_PUBLIC_NOINDEX === "true"
    ? { robots: { index: false, follow: false } }
    : {}),
  title: {
    default: SITE_TITLE,
    template: TITLE_TEMPLATE,
  },
  description: DESCRIPTION,
  applicationName: SITE_NAME,
  openGraph: {
    type: "website",
    siteName: SITE_NAME,
    url: SITE_URL,
    description: DESCRIPTION,
  },
  twitter: {
    card: "summary_large_image",
    site: TWITTER_HANDLE,
    description: DESCRIPTION,
  },
  alternates: {
    types: {
      "application/rss+xml": [
        { url: "/blog/rss.xml", title: "ChilliCream Blog" },
      ],
    },
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      className={`${inter.variable} ${josefinSans.variable} font-sans`}
    >
      <head>
        <link rel="preconnect" href="https://consent.cookiebot.com" />
        <link rel="preconnect" href="https://consentcdn.cookiebot.com" />
        <link rel="dns-prefetch" href="https://www.googletagmanager.com" />
        <StructuredData />
      </head>
      <body>
        <AnalyticsScripts />
        <EnableSmoothScroll />
        <Header />
        <main>{children}</main>
        <Footer />
        <Analytics />
      </body>
    </html>
  );
}
