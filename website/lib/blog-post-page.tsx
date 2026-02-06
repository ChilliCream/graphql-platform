"use client";

import React from "react";
import styled from "styled-components";
import { MDXRemoteSerializeResult } from "next-mdx-remote";

import { SiteLayout } from "@/components/layout";
import { Link, SEO } from "@/components/misc";
import { THEME_COLORS, FONT_FAMILY_HEADING } from "@/style";
import { MdxContent } from "./mdx-content";
import type { BlogPost } from "./blog";

interface BlogPostPageProps {
  post: BlogPost;
  mdxSource: MDXRemoteSerializeResult;
}

export function BlogPostPage({ post, mdxSource }: BlogPostPageProps) {
  const formattedDate = new Date(post.date).toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });

  return (
    <SiteLayout>
      <SEO
        title={post.title}
        description={post.description}
        isArticle
        imageUrl={post.featuredImage}
      />
      <Container>
        <Article className="text-1">
          <Header>
            <Title>{post.title}</Title>
            <Metadata>
              <span>{formattedDate}</span>
              {post.readingTime && <span> · {post.readingTime}</span>}
              {post.author && <span> · {post.author}</span>}
            </Metadata>
          </Header>
          {post.featuredImage && (
            <FeaturedImage>
              <img
                src={post.featuredImage}
                alt={post.title}
                style={{ maxWidth: "100%", height: "auto" }}
              />
            </FeaturedImage>
          )}
          <Content>
            <MdxContent source={mdxSource} />
          </Content>
          {post.tags.length > 0 && (
            <Tags>
              {post.tags.map((tag) => (
                <Tag key={tag}>
                  <Link to={`/blog/tags/${tag}`}>{tag}</Link>
                </Tag>
              ))}
            </Tags>
          )}
        </Article>
      </Container>
    </SiteLayout>
  );
}

const Container = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: row;
  width: 100%;
  max-width: 820px;

  @media only screen and (min-width: 860px) {
    padding: 20px 10px 0;
  }
`;

const Article = styled.article`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  margin-bottom: 60px;
  padding-bottom: 20px;
`;

const Header = styled.header`
  padding: 20px 20px 0;

  @media only screen and (min-width: 860px) {
    padding: 20px 50px 0;
  }
`;

const Title = styled.h1`
  font-size: 2rem;
  margin-bottom: 10px;

  @media only screen and (min-width: 860px) {
    font-size: 2.5rem;
  }
`;

const Metadata = styled.div`
  display: flex;
  flex-wrap: wrap;
  gap: 0;
  margin-bottom: 20px;
  font-size: 0.875rem;
  color: ${THEME_COLORS.text};
`;

const FeaturedImage = styled.div`
  margin-bottom: 20px;
  padding: 0 20px;

  img {
    border-radius: var(--box-border-radius);
  }

  @media only screen and (min-width: 860px) {
    padding: 0 50px;
  }
`;

const Content = styled.div`
  padding: 0 20px;

  @media only screen and (min-width: 860px) {
    padding: 0 50px;
  }
`;

const Tags = styled.div`
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  padding: 20px;

  @media only screen and (min-width: 860px) {
    padding: 20px 50px;
  }
`;

const Tag = styled.span`
  padding: 4px 12px;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--button-border-radius);
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.75rem;
  font-weight: 500;

  a {
    color: ${THEME_COLORS.text};
    text-decoration: none;

    &:hover {
      color: ${THEME_COLORS.linkHover};
    }
  }
`;
