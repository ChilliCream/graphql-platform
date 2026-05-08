"use client";

import React, { useEffect, useState } from "react";

import { Act1 } from "@/components/landing/Act1";
import { Act2 } from "@/components/landing/Act2";
import { Act3 } from "@/components/landing/Act3";
import { Act4 } from "@/components/landing/Act4";
import { Act5 } from "@/components/landing/Act5";
import { ActBundle } from "@/components/landing/ActBundle";
import { ActClients } from "@/components/landing/ActClients";
import {
  LandingGlobalStyle,
  LandingRoot,
} from "@/components/landing/LandingRoot";
import { LazyMount } from "@/components/landing/LazyMount";
import { Blog, FinalCta, LandingFooter } from "@/components/landing/Outro";
import { StickyCta } from "@/components/landing/StickyCta";
import { Topbar } from "@/components/landing/Topbar";

const LandingPage: React.FC = () => {
  const [act2Tab, setAct2Tab] = useState("platform");
  const [act3Tab, setAct3Tab] = useState("fusion-overview");

  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <>
      <LandingGlobalStyle />
      <LandingRoot>
        <Topbar />
        <Act1 />
        <ActBundle lanes={4} />
        <Act2 activeTab={act2Tab} setActiveTab={setAct2Tab} />
        <LazyMount minHeight={720}>
          <Act3 activeTab={act3Tab} setActiveTab={setAct3Tab} />
        </LazyMount>
        <ActBundle lanes={5} />
        <LazyMount minHeight={520}>
          <Act4 />
        </LazyMount>
        <ActBundle lanes={4} />
        <LazyMount minHeight={520}>
          <ActClients />
        </LazyMount>
        <ActBundle lanes={4} />
        <LazyMount minHeight={1200}>
          <Act5 />
        </LazyMount>
        <LazyMount minHeight={500}>
          <FinalCta />
        </LazyMount>
        <LazyMount minHeight={1200}>
          <Blog />
        </LazyMount>
        <LandingFooter />
        <StickyCta />
      </LandingRoot>
    </>
  );
};

export default LandingPage;
