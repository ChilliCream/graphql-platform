"use client";

import React, { FC, useEffect, useState } from "react";

import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { LandingGlobalStyle } from "@/components/landing/LandingRoot";
import { Act1 } from "@/components/landing/desktop/Act1";
import { Act2 } from "@/components/landing/desktop/Act2";
import { Act3 } from "@/components/landing/desktop/Act3";
import { Act4 } from "@/components/landing/desktop/Act4";
import { Act5 } from "@/components/landing/desktop/Act5";
import { ActClients } from "@/components/landing/desktop/ActClients";
import { Blog, FinalCta } from "@/components/landing/desktop/Outro";
import { DesktopLandingRoot } from "@/components/landing/desktop/DesktopLandingRoot";
import { AnchorProvider } from "@/components/landing/desktop/AnchorContext";
import { ConnectorLayer } from "@/components/landing/desktop/ConnectorLayer";
import { FusionLensEffect } from "@/components/landing/desktop/FusionLensEffect";

const LandingDesktopPage: FC = () => {
  const [act2Tab, setAct2Tab] = useState("platform");
  const [act3Tab, setAct3Tab] = useState("fusion-overview");
  // Callback-ref into state so the AnchorProvider re-renders the landing
  // tree once the root element actually mounts (a plain useRef wouldn't).
  const [root, setRoot] = useState<HTMLDivElement | null>(null);

  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <SiteLayout disableStars>
      <SEO title="Home" />
      <LandingGlobalStyle />
      <AnchorProvider root={root}>
        <FusionLensEffect />
        <DesktopLandingRoot ref={setRoot}>
          <ConnectorLayer />
          <Act1 />
          <Act2 activeTab={act2Tab} setActiveTab={setAct2Tab} />
          <Act3 activeTab={act3Tab} setActiveTab={setAct3Tab} />
          <Act4 />
          <ActClients />
          <Act5 />
          <FinalCta />
          <Blog />
        </DesktopLandingRoot>
      </AnchorProvider>
    </SiteLayout>
  );
};

export default LandingDesktopPage;
