import * as page from "./page";
import {
  createPageShareCardImage,
  shareCardContentType,
  shareCardSize,
} from "@/src/og/pageShareCardImage";

export const dynamic = "force-static";

export const alt = "What is GraphQL Federation? - ChilliCream";
export const size = shareCardSize;
export const contentType = shareCardContentType;

export default createPageShareCardImage(page);
