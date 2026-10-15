import * as page from "./page";
import {
  createPageShareCardImage,
  shareCardContentType,
  shareCardSize,
} from "@/src/og/pageShareCardImage";

export const dynamic = "force-static";
export const alt =
  "GraphQL federation router and gateway performance benchmarks";
export const size = shareCardSize;
export const contentType = shareCardContentType;

export default createPageShareCardImage(page);
