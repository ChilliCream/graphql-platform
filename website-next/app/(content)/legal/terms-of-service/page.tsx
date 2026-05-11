import { createStaticPage } from "@/src/helpers/staticPage";

const { Page, generateMetadata } = createStaticPage(
  "legal/terms-of-service.md"
);

export { generateMetadata };
export default Page;
