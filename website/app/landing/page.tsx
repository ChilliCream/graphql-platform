import React from "react";
import type { Metadata } from "next";
import { Space_Grotesk, JetBrains_Mono } from "next/font/google";

import LandingPage from "@/page-components/landing";

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

export const metadata: Metadata = {
  title: "ChilliCream — The API Platform for Humans and Agents",
  description:
    "Unify all your APIs into a comprehensive company graph, streamlining data accessibility and enhancing integration.",
};

export default function Page() {
  return (
    <div className={`${spaceGrotesk.variable} ${jetbrainsMono.variable}`}>
      <LandingPage />
    </div>
  );
}
