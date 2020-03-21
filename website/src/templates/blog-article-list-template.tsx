import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetBlogArticleListQuery } from "../../graphql-types";
import { Link } from "../components/misc/link";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";

interface BlogArticleTemplateProperties {
  data: GetBlogArticleListQuery;
}

const BlogArticleTemplate: FunctionComponent<BlogArticleTemplateProperties> = ({
  data: { allMarkdownRemark },
}) => {
  const articles = allMarkdownRemark!.edges;

  return (
    <Layout>
      <SEO title="Home" />
      {articles.map(({ node }) => {
        const title = node.frontmatter!.title! || node.fields!.slug;

        return (
          <Link to={node.frontmatter!.path!} key={node.fields!.slug!}>
            <Title>{title}</Title>
            <PublishDate>{node.frontmatter!.date}</PublishDate>
          </Link>
        );
      })}
    </Layout>
  );
};

export default BlogArticleTemplate;

export const pageQuery = graphql`
  query getBlogArticleList($skip: Int!, $limit: Int!) {
    allMarkdownRemark(
      sort: { fields: [frontmatter___date], order: DESC }
      limit: $limit
      skip: $skip
    ) {
      edges {
        node {
          fields {
            slug
          }
          frontmatter {
            author
            authorImageUrl
            authorUrl
            date(formatString: "MMMM DD, YYYY")
            path
            title
          }
        }
      }
    }
  }
`;

const Title = styled.h1``;

const PublishDate = styled.div``;
