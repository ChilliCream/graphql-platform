"use client";

import { useEffect, useState } from "react";

import "./mobile/landing-mobile-styles.css";

import { Act1 } from "./mobile/Act1";
import { Act2 } from "./mobile/Act2";
import { Act3 } from "./mobile/Act3";
import { Act4 } from "./mobile/Act4";
import { Act5 } from "./mobile/Act5";
import { ActBundle } from "./mobile/ActBundle";
import { ActClients } from "./mobile/ActClients";
import { LandingRoot } from "./mobile/LandingRoot";
import { LazyMount } from "./mobile/LazyMount";
import { Blog, FinalCta } from "./mobile/Outro";
import { StickyCta } from "./mobile/StickyCta";

export function LandingMobile() {
  const [act2Tab, setAct2Tab] = useState("platform");
  const [act3Tab, setAct3Tab] = useState("fusion-overview");

  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <LandingRoot>
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
      <StickyCta />
    </LandingRoot>
  );
}
