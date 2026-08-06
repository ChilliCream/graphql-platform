import dynamic from "next/dynamic";
import { FromOurBlog } from "@/src/components/FromOurBlog";
import { BuildYourWay } from "@/src/components/home/BuildYourWay";
import { FusionFlow } from "@/src/components/home/FusionFlow";
import { GrabADrink } from "@/src/components/home/GrabADrink";
import { HomeHero } from "@/src/components/home/HomeHero";
import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_TITLE } from "@/src/helpers/site";

const LogoCloud = dynamic(() =>
  import("@/src/components/home/LogoCloud").then((m) => m.LogoCloud),
);
const NitroSection = dynamic(() =>
  import("@/src/components/home/nitro/NitroSection").then(
    (m) => m.NitroSection,
  ),
);
const AgenticSection = dynamic(() =>
  import("@/src/components/home/agentic/AgenticSection").then(
    (m) => m.AgenticSection,
  ),
);
const CombinedObservability = dynamic(() =>
  import("@/src/components/home/combined/CombinedObservability").then(
    (m) => m.CombinedObservability,
  ),
);
const CombinedMessaging = dynamic(() =>
  import("@/src/components/home/combined/CombinedMessaging").then(
    (m) => m.CombinedMessaging,
  ),
);
const CombinedGovernance = dynamic(() =>
  import("@/src/components/home/combined/CombinedGovernance").then(
    (m) => m.CombinedGovernance,
  ),
);
const PatternBand = dynamic(() =>
  import("@/src/components/PatternBand").then((m) => m.PatternBand),
);
const PcbBand = dynamic(() =>
  import("@/src/components/PcbBand").then((m) => m.PcbBand),
);

export const metadata = pageMetadata({
  title: SITE_TITLE,
  description:
    "The ChilliCream GraphQL Platform: build, connect, and observe GraphQL APIs with Hot Chocolate, Fusion, Strawberry Shake, and Nitro.",
  path: "/",
  absoluteTitle: true,
});

export default function Home() {
  return (
    <>
      <HomeHero />
      <LogoCloud />
      <BuildYourWay />
      <FusionFlow />
      <ProtocolCards />
      <NitroSection />
      <PatternBand
        pattern="lines"
        contain={false}
        recessed
        className="pb-16 sm:pb-24"
      >
        <AgenticSection />
      </PatternBand>
      <PatternBand
        pattern="grid"
        contain={false}
        blend
        className="pb-16 sm:pb-24"
      >
        <CombinedObservability />
      </PatternBand>
      <PcbBand className="pb-16 sm:pb-24">
        <CombinedMessaging />
      </PcbBand>
      <PatternBand
        pattern="dots"
        contain={false}
        blend
        recessedBottom
        className="pb-16 sm:pb-24"
      >
        <CombinedGovernance />
      </PatternBand>
      <NitroPricing />
      <GrabADrink />
      <div className="px-5 py-8 sm:px-12">
        <div className="mx-auto flex max-w-7xl flex-col gap-12">
          <FromOurBlog />
        </div>
      </div>
    </>
  );
}
