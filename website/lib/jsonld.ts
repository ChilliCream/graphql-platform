import { siteMetadata } from "./site-config";

const BASE_URL = siteMetadata.siteUrl;

interface BreadcrumbItem {
  name: string;
  url?: string;
}

const publisher = {
  "@type": "Organization" as const,
  name: siteMetadata.company,
  url: BASE_URL,
  logo: {
    "@type": "ImageObject" as const,
    url: `${BASE_URL}/icon.png`,
  },
};

export function createArticleJsonLd(options: {
  title: string;
  description?: string;
  url: string;
  dateModified?: string;
  breadcrumbs: BreadcrumbItem[];
}) {
  return {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "Article",
        headline: options.title,
        ...(options.description && { description: options.description }),
        ...(options.dateModified && { dateModified: options.dateModified }),
        author: {
          "@type": "Organization",
          name: siteMetadata.company,
          url: BASE_URL,
        },
        publisher,
        mainEntityOfPage: options.url,
      },
      buildBreadcrumbList(options.breadcrumbs),
    ],
  };
}

export function createBlogPostJsonLd(options: {
  title: string;
  description?: string;
  url: string;
  datePublished?: string;
  author?: string;
  image?: string;
}) {
  return {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "BlogPosting",
        headline: options.title,
        ...(options.description && { description: options.description }),
        ...(options.datePublished && { datePublished: options.datePublished }),
        author: options.author
          ? { "@type": "Person", name: options.author }
          : {
              "@type": "Organization",
              name: siteMetadata.company,
              url: BASE_URL,
            },
        publisher,
        ...(options.image && { image: `${BASE_URL}${options.image}` }),
        mainEntityOfPage: options.url,
      },
      buildBreadcrumbList([
        { name: "Home", url: `${BASE_URL}/` },
        { name: "Blog", url: `${BASE_URL}/blog/` },
        { name: options.title },
      ]),
    ],
  };
}

export function createOrganizationJsonLd() {
  return {
    "@context": "https://schema.org",
    "@type": "Organization",
    name: siteMetadata.company,
    url: BASE_URL,
    logo: `${BASE_URL}/icon.png`,
    description: siteMetadata.description,
    sameAs: [
      siteMetadata.tools.github,
      siteMetadata.tools.linkedIn,
      siteMetadata.tools.youtube,
      `https://x.com/${siteMetadata.author}`,
    ],
  };
}

function buildBreadcrumbList(items: BreadcrumbItem[]) {
  return {
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      ...(index < items.length - 1 && item.url ? { item: item.url } : {}),
    })),
  };
}
