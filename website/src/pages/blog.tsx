import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetAllBlogArticlesQuery } from "../../graphql-types";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { BlogArticleLink } from "../components/widgets/blog-article-link";

interface BlogPageProperties {
  data: GetAllBlogArticlesQuery;
}

const BlogPage: FunctionComponent<BlogPageProperties> = ({
  data: {
    allMarkdownRemark: { edges: articles },
  },
}) => {
  return (
    <Layout>
      <SEO title="Blog" />
      <BlogArticleList>
        {articles
          .filter(article => !!article.node.frontmatter) // You can filter your posts based on some criteria
          .map(article => (
            <BlogArticleLink
              key={article.node.id}
              date={article.node.frontmatter!.date!}
              path={article.node.frontmatter!.path!}
              title={article.node.frontmatter!.title!}
            />
          ))}
      </BlogArticleList>
    </Layout>
  );
};

export default BlogPage;

export const pageQuery = graphql`
  query getAllBlogArticles {
    allMarkdownRemark(sort: { order: DESC, fields: [frontmatter___date] }) {
      edges {
        node {
          id
          excerpt(pruneLength: 250)
          frontmatter {
            date(formatString: "MMMM DD, YYYY")
            path
            title
          }
        }
      }
    }
  }
`;

const BlogArticleList = styled.section`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  width: 100%;
  max-width: 1100px;
`;
