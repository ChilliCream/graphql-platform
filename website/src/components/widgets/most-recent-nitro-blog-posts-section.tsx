import { graphql, useStaticQuery } from "gatsby";
import React, { FC } from "react";

import { ContentSection } from "@/components/misc";
import { SrOnly } from "@/components/misc/sr-only";
import { GetMostRecentNitroBlogPostsDataQuery } from "@/graphql-types";
import { BlogArticleTeaser } from "./blog-article-teaser";
import { Boxes } from "./box-elements";

export const MostRecentNitroBlogPostsSection: FC = () => {
  const data = useStaticQuery<GetMostRecentNitroBlogPostsDataQuery>(graphql`
    query getMostRecentNitroBlogPostsData {
      allMdx(
        limit: 3
        filter: {
          frontmatter: {
            tags: { in: ["bananacakepop", "nitro"] }
            path: { glob: "/blog/**/*" }
          }
        }
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
        Here you find the latest news about Nitro the GraphQL IDE to explore and
        test any GraphQL API.
      </SrOnly>
      <Boxes>
        {data.allMdx.edges.map(({ node }) => (
          <BlogArticleTeaser key={`article-${node.id}`} data={node} />
        ))}
      </Boxes>
    </ContentSection>
  );
};
