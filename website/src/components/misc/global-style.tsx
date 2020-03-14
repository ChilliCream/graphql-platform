import { createGlobalStyle } from "styled-components";

export const GlobalStyle = createGlobalStyle`
  body,
  html {
    width: 100vw;
    height: 100vh;
    font-size: 16px;
    overflow: auto;
    background-color: #ccc;
  }

  * {
    margin: 0;
    padding: 0;
    overflow: hidden;
    /*user-select: none;*/
    font-family: system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial,
      sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol";
    font-size: 1em;
    line-height: 1.188em;
    font-weight: normal;
  }

  *:focus {
    outline: none;
  }

  button {
    cursor: pointer;
    background-color: transparent;
    border: 0 none;
  }

  h1, h2, h3, h4, h5, h6 {
    font-family: "Roboto", sans-serif;
  }

  strong {
    font-weight: bold;
  }

  /**
  * If you already use line highlighting
  */

  /* Adjust the position of the line numbers */
  .gatsby-highlight pre[class*="language-"].line-numbers {
    padding-left: 2.8em;
  }

  /**
  * If you only want to use line numbering
  */

  code[class*="language-"] {
    font-size: 0.875em;
  }

  .gatsby-highlight {
    margin: 15px 0;
    overflow: auto;
    font-size: 0.875 !important;

    * {
      font-family: Consolas, Monaco, "Andale Mono", "Ubuntu Mono", monospace;
      line-height: 1.188em !important;
    }

    > pre[class*="language-"].line-numbers {
      border-radius: 5px;
      padding: 10px 10px 10px 50px;
      overflow: initial;

      > .line-numbers-rows {
        border-right: 1px solid #444;
        padding: 10px 5px 10px 15px;
      }
    }
  }
`;
