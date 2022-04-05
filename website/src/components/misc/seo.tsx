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
      query {
        site {
          siteMetadata {
            title
            description
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

  const metaAuthor = `@${site.siteMetadata.author}`;
  const metaDescription = description || site.siteMetadata.description;
  const metaImageUrl = `${site.siteMetadata.siteUrl}${
    imageUrl ?? image?.childImageSharp!.gatsbyImageData!.images.fallback.src
  }`;

  return (
    <Helmet
      htmlAttributes={{
        lang,
      }}
      title={title}
      titleTemplate={`%s | ${site.siteMetadata.title}`}
      meta={[
        {
          name: `description`,
          content: metaDescription,
        },
        {
          property: `og:url`,
          content: site.siteMetadata.siteUrl,
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
          content: !!isArticle ? `article` : `website`,
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
    />
  );
};

SEO.defaultProps = {
  lang: `en`,
  meta: [],
  description: ``,
};
