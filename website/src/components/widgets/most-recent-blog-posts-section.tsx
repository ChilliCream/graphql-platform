import { graphql, useStaticQuery } from "gatsby";
import React, { FC } from "react";

import { ContentSection, SrOnly } from "@/components/misc";
import { GetMostRecentBlogPostsDataQuery } from "@/graphql-types";
import { BlogArticleTeaser } from "./blog-article-teaser";
import { Boxes } from "./box-elements";

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
            ...BlogArticleTeaser
          }
        }
      }
    }
  `);

  return (
    <ContentSection title="From Our Blog" noBackground>
      <SrOnly>
        Here you find the latest news about the ChilliCream and its entire
        GraphQL Platform.
      </SrOnly>
      <Boxes>
        {data.allMdx.edges.map(({ node }) => (
          <BlogArticleTeaser key={`article-${node.id}`} data={node} />
        ))}
      </Boxes>
    </ContentSection>
  );
};
