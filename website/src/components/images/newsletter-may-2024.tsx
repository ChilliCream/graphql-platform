import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";
import styled from "styled-components";

import { GetNewsletterMay2024ImageQuery } from "@/graphql-types";

export const NewsletterMay2024: FC = () => {
  const data = useStaticQuery<GetNewsletterMay2024ImageQuery>(graphql`
    query getNewsletterMay2024Image {
      file(
        relativePath: { eq: "2024-05-21-newsletter-may/header.png" }
        sourceInstanceName: { eq: "blog" }
      ) {
        childImageSharp {
          gatsbyImageData(layout: CONSTRAINED, width: 1200, quality: 100)
        }
      }
    }
  `);

  return (
    <Container>
      <GatsbyImage
        image={data.file?.childImageSharp?.gatsbyImageData}
        alt="Newsletter May 2024"
      />
    </Container>
  );
};

const Container = styled.div`
  padding: 30px;

  .gatsby-image-wrapper {
    border-radius: var(--border-radius);
    box-shadow: 0 9px 18px rgba(0, 0, 0, 0.25);
  }
`;
