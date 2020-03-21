import Img, { FluidObject } from "gatsby-image";
import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { GetBlogArticlesQuery } from "../../../graphql-types";
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
          <Article key={node.id}>
            <Link to={node.frontmatter!.path!}>
              {node?.frontmatter?.featuredImage?.childImageSharp?.fluid && (
                <Img
                  fluid={
                    node.frontmatter.featuredImage.childImageSharp
                      .fluid as FluidObject
                  }
                />
              )}
              <Title>{node.frontmatter!.title}</Title>
            </Link>
            <Metadata>
              <AuthorLink to={node.frontmatter!.authorUrl!}>
                <AuthorImage src={node.frontmatter!.authorImageUrl!} />
                {node.frontmatter!.author}
              </AuthorLink>{" "}
              ・ {node.frontmatter!.date} ・ {node.fields!.readingTime!.text}
            </Metadata>
            {existingTags.length > 0 && (
              <Tags>
                {existingTags.map(tag => (
                  <Tag>
                    <TagLink to={`/blog/tags/${tag}`}>{tag}</TagLink>
                  </Tag>
                ))}
              </Tags>
            )}
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

const Title = styled.h1`
  margin-top: 20px;
  margin-right: 20px;
  margin-left: 20px;
  font-size: 1.667em;

  @media only screen and (min-width: 800px) {
    margin-right: 50px;
    margin-left: 50px;
  }
`;

const Metadata = styled.div`
  display: flex;
  flex-direction: row;
  align-items: center;
  margin: 0 20px 20px;
  font-size: 0.778em;

  @media only screen and (min-width: 800px) {
    margin: 0 50px 20px;
  }
`;

const AuthorLink = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  align-items: center;
  color: #666;
`;

const AuthorImage = styled.img`
  flex: 0 0 auto;
  margin-right: 0.5em;
  border-radius: 15px;
  width: 30px;
`;

const Tags = styled.ul`
  margin: 0 20px 20px;
  list-style-type: none;

  @media only screen and (min-width: 800px) {
    margin: 0 50px 20px;
  }
`;

const Tag = styled.li`
  display: inline-block;
  margin: 0 5px 0 0;
  border-radius: 4px;
  padding: 0;
  background-color: #f40010;
  font-size: 0.722em;
  letter-spacing: 0.05em;
  color: #fff;
`;

const TagLink = styled(Link)`
  display: block;
  padding: 5px 15px;
  color: #fff;
`;
