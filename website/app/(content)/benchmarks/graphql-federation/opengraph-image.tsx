import { BENCHMARK_SHARE_TITLE } from "@/src/components/benchmarks/benchmarkRoutes";
import {
  createPageShareCardImage,
  shareCardContentType,
  shareCardSize,
} from "@/src/og/pageShareCardImage";

export const dynamic = "force-static";
export const alt = BENCHMARK_SHARE_TITLE;
export const size = shareCardSize;
export const contentType = shareCardContentType;

export default createPageShareCardImage({
  metadata: { title: BENCHMARK_SHARE_TITLE },
});
