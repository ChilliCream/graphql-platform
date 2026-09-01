import { HelpClosing } from "@/src/components/help/HelpClosing";
import { HELP_FAQ_ITEMS, HelpFaq } from "@/src/components/help/HelpFaq";
import { HelpHero } from "@/src/components/help/HelpHero";
import { HELP_TIERS, HelpTiers } from "@/src/components/help/HelpTiers";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { SelfServeGrid } from "@/src/components/help/SelfServeGrid";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import {
  createFaqNode,
  createItemListNode,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

const PAGE = {
  title: "GraphQL Help for Hot Chocolate and Fusion",
  description:
    "Find GraphQL help for Hot Chocolate, Fusion, and Nitro through documentation, the ChilliCream community, an advisory engagement, or a support plan.",
  keywords: [
    "GraphQL help",
    "Hot Chocolate help",
    "Fusion help",
    "Nitro help",
    "ChilliCream community",
    "GraphQL documentation",
  ],
  path: "/help",
} as const;

export const metadata = pageMetadata(PAGE);

const ITEM_LIST = createItemListNode(
  PAGE.path,
  "Ways to get GraphQL help",
  HELP_TIERS.map((tier) => ({
    name: tier.title,
    url: tier.callToAction.link,
    description: tier.description,
    itemType: "Service",
  })),
);
const FAQ = createFaqNode(PAGE.path, HELP_FAQ_ITEMS);

export default function HelpPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Help" }]}
        mainEntity={schemaRef(schemaId(PAGE.path, "item-list"))}
        additionalNodes={[ITEM_LIST, FAQ]}
      />
      <HelpHero />
      <HelpTiers />
      <SelfServeGrid />
      <HelpFaq />
      <HelpClosing />
    </>
  );
}
