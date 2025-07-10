import styled from "styled-components";

import { IsSmallDesktop, THEME_COLORS } from "@/style";

export const ArticleHeader = styled.header`
  position: relative;

  > .gatsby-image-wrapper {
    margin-bottom: 36px;
  }

  ${IsSmallDesktop(`
    padding-top: 72px;
  `)}

  @media only screen and (min-width: 700px) {
    > .gatsby-image-wrapper {
      border-radius: var(--border-radius) var(--border-radius) 0 0;
    }
  }
`;

export interface ArticleVideoProps {
  readonly videoId: string;
}

export const ArticleVideo = styled.iframe.attrs<ArticleVideoProps>(
  ({ videoId }) => ({
    src: `https://www.youtube.com/embed/${videoId}`,
    frameBorder: 0,
    allowFullScreen: true,
  })
)<ArticleVideoProps>`
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 100%;
`;

export const ArticleHeaderVideoContainer = styled.div`
  position: relative;
  padding-top: 56.22%;
  margin-bottom: 36px;
  overflow: hidden;

  @media only screen and (min-width: 700px) {
    border-radius: var(--border-radius) var(--border-radius) 0 0;

    > ${ArticleVideo} {
      border-radius: var(--border-radius) var(--border-radius) 0 0;
    }
  }
`;

export const ArticleContentVideoContainer = styled.div`
  position: relative;
  margin-bottom: 16px;
  padding-top: 56.22%;
  overflow: hidden;
`;

export const ArticleTitle = styled.h1`
  margin-right: 16px;
  margin-bottom: 24px;
  margin-left: 16px;
  font-size: 3rem;

  @media only screen and (min-width: 860px) {
    margin-right: 0;
    margin-left: 0;
  }
`;

export const ArticleContent = styled.div`
  > * {
    font-size: 1.125rem;
    line-height: 1.6em;
  }

  > h1,
  > h2,
  > h3,
  > h4,
  > h5,
  > h6 {
    line-height: 1.12em;
    margin: 48px 16px 24px;
  }

  > h1 {
    font-size: 2.5rem;
  }

  > h2 {
    font-size: 2.25rem;
  }

  > h3 {
    font-size: 2rem;
  }

  > h4 {
    font-size: 1.875rem;
  }

  > h5 {
    font-size: 1.5rem;
  }

  > h5 {
    font-size: 1.375rem;
  }

  > h1 > a.anchor.before,
  > h2 > a.anchor.before,
  > h3 > a.anchor.before,
  > h4 > a.anchor.before,
  > h5 > a.anchor.before,
  > h6 > a.anchor.before {
    display: none;
    padding-right: 4px;
    transform: translateX(0px);
  }

  > blockquote {
    padding: 16px;
  }

  > blockquote,
  > p,
  > ol > li,
  > ul > li {
    text-align: justify;
  }

  > p,
  > ol > li,
  > ul > li {
    margin-right: 16px;
    margin-left: 16px;
  }

  > table {
    th:first-child,
    td:first-child {
      padding-left: 20px;
    }

    th:last-child,
    td:last-child {
      padding-right: 20px;
    }
  }

  @media only screen and (min-width: 700px) {
    > h1,
    > h2,
    > h3,
    > h4,
    > h5,
    > h6 {
      line-height: 1.12em;
      margin-right: 0;
      margin-left: 0;
    }

    > h1 > a.anchor.before,
    > h2 > a.anchor.before,
    > h3 > a.anchor.before,
    > h4 > a.anchor.before,
    > h5 > a.anchor.before,
    > h6 > a.anchor.before {
      transform: translateX(30px);
    }

    > blockquote {
      padding: 20px;
    }

    > p,
    > ol > li,
    > ul > li {
      margin-right: 0;
      margin-left: 0;
    }

    > .gatsby-highlight {
      padding-right: 20px;
      padding-left: 20px;

      > pre[class*="language-"] {
        border: 1px solid ${THEME_COLORS.boxBorder};
        border-radius: var(--box-border-radius);
      }
    }
  }
`;

export const ScrollContainer = styled.div`
  overflow-y: auto;
`;
