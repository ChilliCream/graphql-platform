import {
  createPageShareCardImage,
  shareCardContentType,
  shareCardSize,
} from "@/src/og/pageShareCardImage";

export const dynamic = "force-static";

export const alt = "Fusion GraphQL Federation router (gateway)";
export const size = shareCardSize;
export const contentType = shareCardContentType;

export default createPageShareCardImage({
  metadata: { title: "Fusion: GraphQL Federation Router" },
});
