import { createStaticPage } from "@/src/helpers/staticPage";

const { Page, generateMetadata } = createStaticPage(
  "legal/privacy-policy.md"
);

export { generateMetadata };
export default Page;
