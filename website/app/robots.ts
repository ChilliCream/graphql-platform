import type { MetadataRoute } from "next";
import { SITE_URL } from "@/src/helpers/siteUrl";

export const dynamic = "force-static";

export default function robots(): MetadataRoute.Robots {
  // Preview/staging deployments opt out of indexing entirely.
  if (process.env.NEXT_PUBLIC_NOINDEX === "true") {
    return {
      rules: {
        userAgent: "*",
        disallow: "/",
      },
    };
  }

  return {
    rules: {
      userAgent: "*",
      allow: "/",
    },
    sitemap: `${SITE_URL}/sitemap.xml`,
    host: SITE_URL,
  };
}
