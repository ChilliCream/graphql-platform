import styled from "styled-components";
import { IsPhablet, IsSmallDesktop } from "../doc-page/shared-style";

export const ArticleHeader = styled.header`
  position: relative;

  ${IsSmallDesktop(`
    padding-top: 54px;
  `)}

  ${IsPhablet(`
    padding-top: 20px;
  `)}

  @media only screen and (min-width: 820px) {
    > .gatsby-image-wrapper {
      border-radius: 4px 4px 0 0;
    }
  }
`;

export const ArticleTitle = styled.h1`
  margin: 20px 20px 10px;
  font-size: 2em;

  @media only screen and (min-width: 820px) {
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

  @media only screen and (min-width: 820px) {
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
