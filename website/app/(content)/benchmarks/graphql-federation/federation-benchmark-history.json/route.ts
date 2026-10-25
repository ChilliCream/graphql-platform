import history from "@/src/data/federation-benchmark-history.json";

export const dynamic = "force-static";

export function GET() {
  return Response.json(history);
}
