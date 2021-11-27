import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";
import styled from "styled-components";
import { GetBlogPostEfMeetsGraphQlImageQuery } from "../../../graphql-types";

export const BlogPostEFMeetsGraphQL: FC = () => {
  const data = useStaticQuery<GetBlogPostEfMeetsGraphQlImageQuery>(graphql`
    query getBlogPostEFMeetsGraphQLImage {
      file(
        relativePath: {
          eq: "2020-03-18-entity-framework/banner-entityframework.png"
        }
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
        alt="Get started with Hot Chocolate and Entity Framework"
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
