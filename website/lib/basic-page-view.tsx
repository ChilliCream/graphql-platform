"use client";

import React from "react";
import styled from "styled-components";
import { MDXRemoteSerializeResult } from "next-mdx-remote";

import { SiteLayout } from "@/components/layout";
import { SEO } from "@/components/misc";
import { MdxContent } from "./mdx-content";

interface BasicPageViewProps {
  title: string;
  mdxSource: MDXRemoteSerializeResult;
}

export function BasicPageView({ title, mdxSource }: BasicPageViewProps) {
  return (
    <SiteLayout>
      <SEO title={title} />
      <Container>
        <Article className="text-2">
          <ArticleTitle>{title}</ArticleTitle>
          <Content>
            <MdxContent source={mdxSource} />
          </Content>
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
  min-width: 0;
  margin-bottom: 60px;
  padding: 0 20px 20px;

  @media only screen and (min-width: 860px) {
    padding: 0 50px 20px;
  }
`;

const ArticleTitle = styled.h1`
  font-size: 2rem;
  margin-bottom: 20px;

  @media only screen and (min-width: 860px) {
    font-size: 2.5rem;
  }
`;

const Content = styled.div`
  overflow: visible;
`;
