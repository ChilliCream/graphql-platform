import { graphql, useStaticQuery } from "gatsby";
import { GatsbyImage } from "gatsby-plugin-image";
import React, { FC } from "react";
import styled from "styled-components";

import { Link } from "@/components/misc/link";
import {
  ContentContainer,
  Section,
  SectionRow,
  SectionTitle,
} from "@/components/misc/marketing-elements";
import { GetMostRecentBlogPostsDataQuery } from "@/graphql-types";
import { THEME_COLORS } from "@/shared-style";

export const MostRecentBlogPostsSection: FC = () => {
  const data = useStaticQuery<GetMostRecentBlogPostsDataQuery>(graphql`
    query getMostRecentBlogPostsData {
      allMdx(
        limit: 3
        filter: { frontmatter: { path: { glob: "/blog/**/*" } } }
        sort: { fields: [frontmatter___date], order: DESC }
      ) {
        edges {
          node {
            id
            fields {
              readingTime {
                text
              }
            }
            frontmatter {
              featuredImage {
                childImageSharp {
                  gatsbyImageData(layout: CONSTRAINED, width: 800, quality: 100)
                }
              }
              path
              title
              date(formatString: "MMMM DD, YYYY")
            }
          }
        }
      }
    }
  `);

  return (
    <Section>
      <SectionRow>
        <ContentContainer noImage>
          <SectionTitle centerAlways>From Our Blog</SectionTitle>
          <Articles>
            {data.allMdx.edges.map(({ node }) => {
              const featuredImage =
                node?.frontmatter!.featuredImage?.childImageSharp
                  ?.gatsbyImageData;

              return (
                <Article key={`article-${node.id}`}>
                  <Link to={node.frontmatter!.path!}>
                    {featuredImage && (
                      <GatsbyImage
                        image={featuredImage}
                        alt={node.frontmatter!.title}
                      />
                    )}
                    <ArticleMetadata>
                      {node.frontmatter!.date!} ・{" "}
                      {node.fields!.readingTime!.text!}
                    </ArticleMetadata>
                    <ArticleTitle>{node.frontmatter!.title}</ArticleTitle>
                  </Link>
                </Article>
              );
            })}
          </Articles>
        </ContentContainer>
      </SectionRow>
    </Section>
  );
};

const Articles = styled.ul`
  display: flex;
  flex-direction: column;
  align-items: stretch;
  justify-content: space-around;
  margin: 0 0 20px;
  list-style-type: none;

  @media only screen and (min-width: 860px) {
    flex-direction: row;
    flex-wrap: wrap;
  }
`;

const Article = styled.li`
  display: flex;
  margin: 20px 0 0;
  width: 100%;
  border-radius: var(--border-radius);
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);

  > a {
    flex: 1 1 auto;
  }

  > a > .gatsby-image-wrapper {
    border-radius: var(--border-radius) var(--border-radius) 0 0;
  }

  @media only screen and (min-width: 860px) {
    width: 30%;
  }
`;

const ArticleMetadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 15px 20px 7px;
  font-size: 0.778em;
  color: ${THEME_COLORS.text};
`;

const ArticleTitle = styled.h1`
  margin: 0 20px 15px;
  font-size: 1em;
`;
