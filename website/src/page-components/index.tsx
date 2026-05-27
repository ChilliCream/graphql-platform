"use client";

import dynamic from "next/dynamic";
import React, { FC, useEffect, useState } from "react";

import { RecentBlogPost } from "@/components/widgets/most-recent-blog-posts-section";

const LandingDesktopPage = dynamic(() => import("./landing-desktop"), {
  ssr: true,
});
const LandingMobilePage = dynamic(() => import("./landing-mobile"), {
  ssr: true,
});

export type LandingVariant = "mobile" | "desktop";

// Desktop owns >= 700px viewports; the mobile landing tree picks up below
// that. Raised from 767px when the desktop tree became responsive via CSS
// (clamp + percentage-positioned stages) instead of a single ScaledCanvas.
const MOBILE_QUERY = "(max-width: 699px)";

interface IndexPageProps {
  initialVariant?: LandingVariant;
  recentPosts?: RecentBlogPost[];
}

const IndexPage: FC<IndexPageProps> = ({ initialVariant = "desktop" }) => {
  const [variant, setVariant] = useState<LandingVariant>(initialVariant);

  useEffect(() => {
    const mql = window.matchMedia(MOBILE_QUERY);
    const apply = () => setVariant(mql.matches ? "mobile" : "desktop");
    apply();
    mql.addEventListener("change", apply);
    return () => mql.removeEventListener("change", apply);
  }, []);

  return variant === "mobile" ? <LandingMobilePage /> : <LandingDesktopPage />;
};

export default IndexPage;
