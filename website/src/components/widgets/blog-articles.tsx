import Img, { FluidObject } from "gatsby-image";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetBlogArticlesQuery } from "../../../graphql-types";
import { ArticleTitle } from "../misc/blog-article-elements";
import { BlogArticleMetadata } from "../misc/blog-article-metadata";
import { BlogArticleTags } from "../misc/blog-article-tags";
import { Link } from "../misc/link";

interface BlogArticlesProperties {
  data: GetBlogArticlesQuery;
}

export const BlogArticles: FunctionComponent<BlogArticlesProperties> = ({
  data: { allMarkdownRemark },
}) => {
  const { edges } = allMarkdownRemark;

  return (
    <Container>
      {edges.map(({ node }) => {
        const existingTags: string[] = node?.frontmatter?.tags
          ? (node.frontmatter.tags.filter(
              tag => tag && tag.length > 0
            ) as string[])
          : [];

        return (
          <Article key={`article-${node.id}`}>
            <Link to={node.frontmatter!.path!}>
              {node?.frontmatter?.featuredImage?.childImageSharp?.fluid && (
                <Img
                  fluid={
                    node.frontmatter.featuredImage.childImageSharp
                      .fluid as FluidObject
                  }
                />
              )}
              <ArticleTitle>{node.frontmatter!.title}</ArticleTitle>
            </Link>
            <BlogArticleMetadata
              author={node.frontmatter!.author!}
              authorImageUrl={node.frontmatter!.authorImageUrl!}
              authorUrl={node.frontmatter!.authorUrl!}
              date={node.frontmatter!.date!}
              readingTime={node.fields!.readingTime!.text!}
            />
            <BlogArticleTags tags={existingTags} />
          </Article>
        );
      })}
    </Container>
  );
};

const Container = styled.ul`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  margin: 0;
  width: 100%;
  max-width: 800px;
  list-style-type: none;
`;

const Article = styled.li`
  margin-bottom: 15px;

  @media only screen and (min-width: 800px) {
    border: 1px solid #ccc;
  }
`;
