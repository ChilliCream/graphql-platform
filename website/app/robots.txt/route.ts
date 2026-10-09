import { SITE_URL } from "@/src/helpers/siteUrl";

export const dynamic = "force-static";

export function GET(): Response {
  // Preview/staging deployments opt out of indexing entirely.
  const body =
    process.env.NEXT_PUBLIC_NOINDEX === "true"
      ? "User-Agent: *\nDisallow: /\n\n"
      : "User-Agent: *\n" +
        "Allow: /\n" +
        "Content-Signal: ai-train=yes, search=yes, ai-input=yes\n" +
        "\n" +
        `Sitemap: ${SITE_URL}/sitemap.xml\n`;

  return new Response(body, {
    headers: { "Content-Type": "text/plain" },
  });
}
