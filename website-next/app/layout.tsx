import "./globals.css";
import type { Metadata } from "next";
import Footer from "@/src/components/Footer";
import Header from "@/src/components/Header";
import { SITE_URL } from "@/src/helpers/siteUrl";

const TITLE = "ChilliCream GraphQL Platform";
const DESCRIPTION =
  "The ChilliCream GraphQL Platform: build, connect, and observe GraphQL APIs with Hot Chocolate, Fusion, Strawberry Shake, and Nitro.";

export const metadata: Metadata = {
  metadataBase: new URL(SITE_URL),
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
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>
        <Header />
        <main>{children}</main>
        <Footer />
      </body>
    </html>
  );
}
