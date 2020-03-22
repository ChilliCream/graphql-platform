import { graphql } from "gatsby";
import React, { FunctionComponent } from "react";
import { GetBlogArticlesQuery } from "../../graphql-types";
import { Pagination } from "../components/misc/pagination";
import { SEO } from "../components/misc/seo";
import { Layout } from "../components/structure/layout";
import { BlogArticles } from "../components/widgets/blog-articles";

interface BlogArticlesTemplateProperties {
  pageContext: BlogArticlesTemplatePageContext;
  data: GetBlogArticlesQuery;
}

const BlogArticlesTemplate: FunctionComponent<BlogArticlesTemplateProperties> = ({
  pageContext,
  data,
}) => {
  return (
    <Layout>
      <SEO title="Blog Articles" />
      <BlogArticles data={data} />
      <Pagination
        currentPage={pageContext.currentPage}
        linkPrefix="/blog"
        totalPages={pageContext.numPages}
      ></Pagination>
    </Layout>
  );
};

export default BlogArticlesTemplate;

export const pageQuery = graphql`
  query getBlogArticles($skip: Int!, $limit: Int!) {
    allMarkdownRemark(
      limit: $limit
      skip: $skip
      sort: { fields: [frontmatter___date], order: DESC }
    ) {
      edges {
        node {
          id
          excerpt(pruneLength: 250)
          fields {
            readingTime {
              text
            }
          }
          frontmatter {
            author
            authorImageUrl
            authorUrl
            date(formatString: "MMMM DD, YYYY")
            featuredImage {
              childImageSharp {
                fluid(maxWidth: 800) {
                  ...GatsbyImageSharpFluid
                }
              }
            }
            path
            tags
            title
          }
        }
      }
    }
  }
`;

export interface BlogArticlesTemplatePageContext {
  limit: number;
  skip: number;
  numPages: number;
  currentPage: number;
}
