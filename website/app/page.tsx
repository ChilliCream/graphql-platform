import { FromOurBlog } from "@/src/components/FromOurBlog";
import { BuildYourWay } from "@/src/components/home/BuildYourWay";
import { FusionFlow } from "@/src/components/home/FusionFlow";
import { GrabADrink } from "@/src/components/home/GrabADrink";
import { HomeHero } from "@/src/components/home/HomeHero";
import { LogoCloud } from "@/src/components/home/LogoCloud";
import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_TITLE } from "@/src/helpers/site";

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
