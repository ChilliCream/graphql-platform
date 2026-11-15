import {
  createSectionShareCardImage,
  shareCardContentType,
  shareCardSize,
} from "@/src/og/pageShareCardImage";

export const dynamic = "force-static";

export const alt = "ChilliCream documentation";
export const size = shareCardSize;
export const contentType = shareCardContentType;

export default createSectionShareCardImage("Documentation");
