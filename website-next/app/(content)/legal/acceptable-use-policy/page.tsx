import { createStaticPage } from "@/src/helpers/staticPage";

const { Page, generateMetadata } = createStaticPage(
  "legal/acceptable-use-policy.md"
);

export { generateMetadata };
export default Page;
