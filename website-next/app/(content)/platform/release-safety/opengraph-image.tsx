import * as page from "./page";
import {
  createPageShareCardImage,
  shareCardContentType,
  shareCardSize,
} from "@/src/og/pageShareCardImage";

export const dynamic = "force-static";

export const alt = "Safe GraphQL Schema Evolution";
export const size = shareCardSize;
export const contentType = shareCardContentType;

export default createPageShareCardImage(page);
