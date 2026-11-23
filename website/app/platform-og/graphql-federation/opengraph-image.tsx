import * as page from "@/app/(content)/platform/graphql-federation/page";
import {
  createPageShareCardImage,
  shareCardContentType,
  shareCardSize,
} from "@/src/og/pageShareCardImage";

// The share card at a stable URL for structured data. Route groups add a hash
// to the file-convention URL under `app/(content)`.
export const dynamic = "force-static";

export const alt = "GraphQL Federation: what, why, and how - ChilliCream";
export const size = shareCardSize;
export const contentType = shareCardContentType;

export default createPageShareCardImage(page);
