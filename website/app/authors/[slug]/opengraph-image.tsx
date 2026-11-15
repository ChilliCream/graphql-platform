import { AUTHOR_PROFILES } from "@/src/data/authors";
import {
  createSectionShareCardImage,
  shareCardContentType,
  shareCardSize,
} from "@/src/og/pageShareCardImage";

export const dynamicParams = false;

export const alt = "ChilliCream blog authors";
export const size = shareCardSize;
export const contentType = shareCardContentType;

export function generateStaticParams() {
  return AUTHOR_PROFILES.map(({ slug }) => ({ slug }));
}

export default createSectionShareCardImage("Authors");
