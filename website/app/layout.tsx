import "./globals.css";
import type { Metadata, Viewport } from "next";
import { Inter, Josefin_Sans } from "next/font/google";
import { Analytics } from "@/src/components/Analytics";
import { AnalyticsScripts } from "@/src/components/AnalyticsScripts";
import { EnableSmoothScroll } from "@/src/components/EnableSmoothScroll";
import Footer from "@/src/components/Footer";
import Header from "@/src/components/Header";
import { StructuredData } from "@/src/components/StructuredData";
import { WebVitals } from "@/src/components/WebVitals";
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
  "Build, federate, observe, and evolve GraphQL APIs with open-source Hot Chocolate, Fusion, Strawberry Shake, and Mocha, plus the Nitro control plane.";

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
  manifest: "/manifest.webmanifest",
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

export const viewport: Viewport = {
  colorScheme: "dark",
  themeColor: "#0b0f1a",
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
      <body className="flex min-h-screen flex-col">
        <AnalyticsScripts />
        <EnableSmoothScroll />
        <Header />
        <main className="flex-1 overflow-x-clip">{children}</main>
        <Footer />
        <Analytics />
        <WebVitals />
      </body>
    </html>
  );
}
