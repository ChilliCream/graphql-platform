import report from "@/src/data/federation-benchmark-report.json";

export const dynamic = "force-static";

export function GET() {
  return Response.json(report);
}
