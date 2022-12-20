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
  readonly meta?: JSX.IntrinsicElements["meta"][];
  readonly title: string;
}

export const SEO: FC<SEOProps> = ({
  description,
  imageUrl,
  isArticle,
  lang,
  meta,
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
      title={title}
      titleTemplate={`%s - ${site.siteMetadata.title}`}
      meta={[
        {
          name: `description`,
          content: metaDescription,
        },
        {
          property: `og:url`,
          content: metaSiteUrl,
        },
        {
          property: `og:title`,
          content: title,
        },
        {
          property: `og:description`,
          content: metaDescription,
        },
        {
          property: `og:type`,
          content: metaType,
        },
        {
          property: `og:image`,
          content: metaImageUrl,
        },
        {
          property: `twitter:title`,
          content: title,
        },
        {
          name: `twitter:card`,
          content: `summary_large_image`,
        },
        {
          property: `twitter:site`,
          content: metaAuthor,
        },
        {
          name: `twitter:creator`,
          content: metaAuthor,
        },
        {
          property: `twitter:image:src`,
          content: metaImageUrl,
        },
        ...meta!,
      ]}
    >
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
  meta: [],
  description: ``,
};
