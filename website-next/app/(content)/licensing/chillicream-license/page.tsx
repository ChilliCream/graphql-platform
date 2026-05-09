import { createStaticPage } from "@/src/helpers/staticPage";

const { Page, generateMetadata } = createStaticPage(
  "licensing/chillicream-license.md"
);

export { generateMetadata };
export default Page;
