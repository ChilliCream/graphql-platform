"use client";

import { useEffect, useState } from "react";

import "./landing-styles.css";

import { Act1 } from "./Act1";
import { Act2 } from "./Act2";
import { Act3 } from "./Act3";
import { Act4 } from "./Act4";
import { Act5 } from "./Act5";
import { ActClients } from "./ActClients";
import { AnchorProvider } from "./AnchorContext";
import { Blog, FinalCta } from "./Outro";
import { ConnectorLayer } from "./ConnectorLayer";
import { FusionLensEffect } from "./FusionLensEffect";
import { LandingRoot } from "./LandingRoot";

export function Landing() {
  const [act2Tab, setAct2Tab] = useState("platform");
  const [act3Tab, setAct3Tab] = useState("fusion-overview");
  // Callback-ref into state so the AnchorProvider re-renders the landing tree
  // once the root element mounts (a plain useRef wouldn't notify consumers).
  const [root, setRoot] = useState<HTMLDivElement | null>(null);

  useEffect(() => {
    document.body.classList.add("cc-landing-body");
    return () => {
      document.body.classList.remove("cc-landing-body");
    };
  }, []);

  return (
    <AnchorProvider root={root}>
      <FusionLensEffect />
      <LandingRoot ref={setRoot}>
        <ConnectorLayer />
        <Act1 />
        <Act2 activeTab={act2Tab} setActiveTab={setAct2Tab} />
        <Act3 activeTab={act3Tab} setActiveTab={setAct3Tab} />
        <Act4 />
        <ActClients />
        <Act5 />
        <FinalCta />
        <Blog />
      </LandingRoot>
    </AnchorProvider>
  );
}
