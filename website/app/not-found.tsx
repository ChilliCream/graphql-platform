"use client";

import React, { ReactNode } from "react";
import styled from "styled-components";
import NextLink from "next/link";

import { SiteLayout } from "@/components/layout";

export default function NotFoundPage() {
  return (
    <SiteLayout disableStars>
      <Container>
        <Article>
          <Title>NOT FOUND</Title>
          <Content>
            <p>The page you&#39;re looking for doesn&#39;t exist.</p>
            <NextLink href="/">Return to the homepage</NextLink>
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

  @media only screen and (min-width: 860px) {
    padding: 20px 10px 0;
    max-width: 820px;
  }
`;

const Article = styled.article`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  margin-bottom: 60px;
  padding-bottom: 20px;

  @media only screen and (min-width: 860px) {
    border-radius: var(--border-radius);
  }
`;

const Title = styled.h1`
  margin-top: 20px;
  margin-right: 20px;
  margin-left: 20px;
  font-size: 2rem;

  @media only screen and (min-width: 860px) {
    margin-right: 50px;
    margin-left: 50px;
  }
`;

const Content = styled.div`
  > * {
    padding-right: 20px;
    padding-left: 20px;
    line-height: normal;
  }

  @media only screen and (min-width: 860px) {
    > * {
      padding-right: 50px;
      padding-left: 50px;
    }
  }
`;
