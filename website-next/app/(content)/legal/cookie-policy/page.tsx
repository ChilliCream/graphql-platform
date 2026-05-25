import { createStaticPage } from "@/src/helpers/staticPage";

const { Page, generateMetadata } = createStaticPage(
  "legal/cookie-policy.md"
);

export { generateMetadata };
export default Page;
