/**
 * SEO component that queries for data with
 *  Gatsby's useStaticQuery React hook
 *
 * See: https://www.gatsbyjs.org/docs/use-static-query/
 */

import { graphql, useStaticQuery } from "gatsby";
import React, { FunctionComponent } from "react";
import { Helmet } from "react-helmet";

interface SEOProperties {
  description?: string;
  lang?: string;
  meta?: JSX.IntrinsicElements["meta"][];
  title: string;
}

export const SEO: FunctionComponent<SEOProperties> = ({
  description,
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
          }
        }
        image: file(
          relativePath: { eq: "chillicream-graphql-banner.png" }
          sourceInstanceName: { eq: "images" }
        ) {
          childImageSharp {
            fixed(width: 1200, pngQuality: 90) {
              src
            }
          }
        }
      }
    `
  );

  const metaDescription = description || site.siteMetadata.description;

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
          property: `og:title`,
          content: title,
        },
        {
          property: `og:description`,
          content: metaDescription,
        },
        {
          property: `og:type`,
          content: `website`,
        },
        {
          property: `og:image`,
          content: `${image?.childImageSharp!.fixed!.src}`,
        },
        {
          name: `twitter:card`,
          content: `summary_large_image`,
        },
        {
          name: `twitter:creator`,
          content: `@${site.siteMetadata.author}`,
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
