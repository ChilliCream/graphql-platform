import styled from "styled-components";

export const ArticleWrapper = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;

  @media only screen and (min-width: 820px) {
    padding: 20px 10px 0;
  }
`;

export const Article = styled.article`
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  margin-bottom: 40px;
  padding-bottom: 20px;

  @media only screen and (min-width: 820px) {
    border-radius: 4px;
    box-shadow: 0 3px 6px rgba(0, 0, 0, 0.25);
  }
`;

export const ArticleHeader = styled.header`
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

  > .gatsby-code-button-container {
    padding: 0;
  }

  > .gatsby-highlight {
    padding-right: 0;
    padding-left: 0;

    > pre[class*="language-"] {
      padding: 30px 20px;

      ::before {
        left: 20px;
      }
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

    > .gatsby-code-button-container {
      padding: 0;
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
