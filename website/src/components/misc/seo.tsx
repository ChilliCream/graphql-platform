/**
 * SEO component that queries for data with
 *  Gatsby's useStaticQuery React hook
 *
 * See: https://www.gatsbyjs.org/docs/use-static-query/
 */

import { graphql, useStaticQuery } from "gatsby";
import React, { FC } from "react";
import { Helmet } from "react-helmet";

export interface SEOProps {
  readonly description?: string;
  readonly imageUrl?: string;
  readonly isArticle?: boolean;
  readonly lang?: string;
  readonly title: string;
}

export const SEO: FC<SEOProps> = ({
  description,
  imageUrl,
  isArticle,
  lang,
  title,
}) => {
  const { site, image } = useStaticQuery(
    graphql`
      query SEO {
        site {
          siteMetadata {
            title
            description
            company
            author
            siteUrl
          }
        }
        image: file(
          relativePath: { eq: "chillicream-graphql-banner.png" }
          sourceInstanceName: { eq: "images" }
        ) {
          childImageSharp {
            gatsbyImageData(layout: FIXED, width: 1200, quality: 100)
          }
        }
      }
    `
  );

  const metaSiteUrl = site.siteMetadata.siteUrl;
  const metaAuthor = `@${site.siteMetadata.author}`;
  const metaCompany = site.siteMetadata.company;
  const metaDescription = description || site.siteMetadata.description;
  const metaImageUrl = `${metaSiteUrl}${
    imageUrl ?? image?.childImageSharp!.gatsbyImageData!.images.fallback.src
  }`;
  const metaType = isArticle ? "article" : "website";

  return (
    <Helmet
      htmlAttributes={{
        lang,
      }}
    >
      <title>
        {title} - {site.siteMetadata.title}
      </title>
      <meta name="description" content={metaDescription} />

      <meta property="og:url" content={metaSiteUrl} />
      <meta property="og:title" content={title} />
      <meta property="og:description" content={metaDescription} />
      <meta property="og:type" content={metaType} />
      <meta property="og:image" content={metaImageUrl} />

      <meta name="twitter:card" content="summary_large_image" />
      <meta name="twitter:title" content={title} />
      <meta name="twitter:site" content={metaAuthor} />
      <meta name="twitter:creator" content={metaAuthor} />
      <meta name="twitter:image" content={metaImageUrl} />
      {description && <meta name="twitter:description" content={description} />}

      <script type="application/ld+json">
        {JSON.stringify({
          "@context": "https://schema.org/",
          "@type": metaType,
          "@id": metaSiteUrl,
          headline: title,
          description: metaDescription,
          author: {
            "@type": "Organization",
            name: metaCompany,
            contactPoint: {
              "@type": "ContactPoint",
              email: "mailto:contact@chillicream.com",
              contactType: "Customer Support",
            },
          },
        })}
      </script>
    </Helmet>
  );
};

SEO.defaultProps = {
  lang: `en`,
  description: ``,
};
