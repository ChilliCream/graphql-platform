"use client";

import dynamic from "next/dynamic";
import { useEffect, useState } from "react";

// The desktop animated layout starts breaking around 880px (cup labels run
// off the edges, the catalog merge column collides with the tab panel).
// Below that we render the dedicated mobile tree directly — there's no
// useful in-between state.
const MOBILE_QUERY = "(max-width: 879px)";

const Landing = dynamic(() => import("./Landing").then((m) => m.Landing), {
  ssr: false,
});
const LandingMobile = dynamic(
  () => import("./LandingMobile").then((m) => m.LandingMobile),
  { ssr: false }
);

export function LandingSwitch() {
  const [variant, setVariant] = useState<"mobile" | "desktop" | null>(null);

  useEffect(() => {
    const mql = window.matchMedia(MOBILE_QUERY);
    const apply = () => setVariant(mql.matches ? "mobile" : "desktop");
    apply();
    mql.addEventListener("change", apply);
    return () => mql.removeEventListener("change", apply);
  }, []);

  if (variant === null) {
    // Reserve some height so the page doesn't shift when the chosen tree
    // hydrates and mounts.
    return <div style={{ minHeight: "100vh" }} aria-hidden />;
  }
  return variant === "mobile" ? <LandingMobile /> : <Landing />;
}
