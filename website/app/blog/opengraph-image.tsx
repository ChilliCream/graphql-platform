import {
  createSectionShareCardImage,
  shareCardContentType,
  shareCardSize,
} from "@/src/og/pageShareCardImage";

export const dynamic = "force-static";

export const alt = "ChilliCream blog";
export const size = shareCardSize;
export const contentType = shareCardContentType;

export default createSectionShareCardImage("Blog");
