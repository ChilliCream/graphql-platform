import type { Metadata } from "next";
import { SITE_NAME, TWITTER_HANDLE } from "@/src/helpers/site";

interface PageMetadataInput {
  /**
   * Page title. Unless {@link PageMetadataInput.absoluteTitle} is set, the
   * `<title>` tag uses the root `"%s - ChilliCream"` template while the social
   * (`og:title` / `twitter:title`) variants are expanded to the same value.
   */
  readonly title: string;
  readonly description: string;
  /** Canonical path for the page, e.g. `"/blog"` or `"/"`. */
  readonly path: string;
  /** Keywords describing the page, emitted as the `<meta name="keywords">` tag. */
  readonly keywords?: readonly string[];
  /** Use the title verbatim instead of applying the `"%s - ChilliCream"` template. */
  readonly absoluteTitle?: boolean;
}

/**
 * Builds the page-level metadata for a route.
 *
 * Next merges nested metadata fields (`openGraph`, `twitter`, `alternates`)
 * shallowly: a page that sets any of them replaces the root value entirely. To
 * avoid dropping the shared social fields and to keep `og:title` / `twitter:title`
 * in sync with the `<title>` tag (which the template does not do on its own),
 * every static page builds its metadata through this helper.
 */
export function pageMetadata({
  title,
  description,
  path,
  keywords,
  absoluteTitle = false,
}: PageMetadataInput): Metadata {
  const socialTitle = absoluteTitle ? title : `${title} - ${SITE_NAME}`;

  return {
    title: absoluteTitle ? { absolute: title } : title,
    description,
    keywords: keywords?.slice(),
    alternates: { canonical: path },
    openGraph: {
      type: "website",
      siteName: SITE_NAME,
      url: path,
      title: socialTitle,
      description,
    },
    twitter: {
      card: "summary_large_image",
      site: TWITTER_HANDLE,
      title: socialTitle,
      description,
    },
  };
}
