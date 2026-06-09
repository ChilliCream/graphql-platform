import "./globals.css";
import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { Analytics } from "@/src/components/Analytics";
import { AnalyticsScripts } from "@/src/components/AnalyticsScripts";
import Footer from "@/src/components/Footer";
import Header from "@/src/components/Header";
import { SITE_URL } from "@/src/helpers/siteUrl";

const inter = Inter({
  subsets: ["latin"],
  display: "swap",
  variable: "--font-inter",
});

const TITLE = "ChilliCream GraphQL Platform";
const DESCRIPTION =
  "The ChilliCream GraphQL Platform: build, connect, and observe GraphQL APIs with Hot Chocolate, Fusion, Strawberry Shake, and Nitro.";

export const metadata: Metadata = {
  metadataBase: new URL(SITE_URL),
  // Preview/staging deployments emit `<meta name="robots" content="noindex, nofollow">`.
  ...(process.env.NEXT_PUBLIC_NOINDEX === "true"
    ? { robots: { index: false, follow: false } }
    : {}),
  title: {
    default: TITLE,
    template: "%s - ChilliCream",
  },
  description: DESCRIPTION,
  applicationName: "ChilliCream",
  openGraph: {
    type: "website",
    siteName: "ChilliCream",
    url: SITE_URL,
    title: TITLE,
    description: DESCRIPTION,
  },
  twitter: {
    card: "summary_large_image",
    site: "@Chilli_Cream",
    title: TITLE,
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
    <html lang="en" className={`${inter.variable} font-sans`}>
      <head>
        <link rel="preconnect" href="https://consent.cookiebot.com" />
        <link rel="preconnect" href="https://consentcdn.cookiebot.com" />
        <link rel="dns-prefetch" href="https://www.googletagmanager.com" />
      </head>
      <body>
        <AnalyticsScripts />
        <Header />
        <main>{children}</main>
        <Footer />
        <Analytics />
      </body>
    </html>
  );
}
