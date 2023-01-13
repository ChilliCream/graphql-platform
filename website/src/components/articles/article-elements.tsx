import styled from "styled-components";

import { IsSmallDesktop } from "@/shared-style";

export interface ArticleHeaderProps {
  readonly kind: "blog" | "doc";
}

export const ArticleHeader = styled.header<ArticleHeaderProps>`
  position: relative;

  ${({ kind }) =>
    kind === "doc"
      ? IsSmallDesktop(`
    padding-top: 60px;
  `)
      : ""}

  @media only screen and (min-width: 860px) {
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
  overflow: hidden;
  padding-top: 56.22%;
  border-radius: var(--border-radius) var(--border-radius) 0 0;

  > ${ArticleVideo} {
    border-radius: var(--border-radius) var(--border-radius) 0 0;
  }
`;

export const ArticleContentVideoContainer = styled.div`
  position: relative;
  overflow: hidden;
  padding-top: 56.22%;
  margin-bottom: 20px;
`;

export const ArticleTitle = styled.h1`
  margin: 20px 20px 10px;
  font-size: 2em;

  @media only screen and (min-width: 860px) {
    margin: 20px 50px 10px;
  }
`;

export const ArticleContent = styled.div`
  > * {
    padding-right: 20px;
    padding-left: 20px;
  }

  > h1 > a.anchor.before,
  > h2 > a.anchor.before,
  > h3 > a.anchor.before,
  > h4 > a.anchor.before,
  > h5 > a.anchor.before,
  > h6 > a.anchor.before {
    padding-right: 4px;
    transform: translateX(0px);
  }

  > blockquote {
    padding: 30px 20px;
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

  @media only screen and (min-width: 860px) {
    > * {
      padding-right: 50px;
      padding-left: 50px;
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
      padding: 30px 50px;
    }

    > table {
      th:first-child,
      td:first-child {
        padding-left: 50px;
      }

      th:last-child,
      td:last-child {
        padding-right: 50px;
      }
    }

    > .gatsby-highlight {
      > pre[class*="language-"] {
        padding-right: 50px;
        padding-left: 50px;

        ::before {
          left: 50px;
        }
      }
    }
  }
`;
