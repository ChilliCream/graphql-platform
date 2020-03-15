import { createGlobalStyle } from "styled-components";

export const GlobalStyle = createGlobalStyle`
  html {
    font-family: sans-serif;
    -ms-text-size-adjust: 100%;
    -webkit-text-size-adjust: 100%;
  }

  body {
    width: 100vw;
    height: 100vh;
    font-size: 16px;
    line-height: 19px;
    overflow: auto;
    background-color: #ccc;
    color: #333;

    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
  }

  * {
    margin: 0;
    padding: 0;
    overflow: hidden;
    /*user-select: none;*/
    font-family: sans-serif;
    font-size: 1em;
    line-height: 1.188em;
    font-weight: normal;
  }

  *:focus {
    outline: none;
  }

  a {
    color: #f40010;
    text-decoration: none;
  }

  button {
    cursor: pointer;
    background-color: transparent;
    border: 0 none;
  }

  h1, h2, h3, h4, h5, h6 {
    font-family: "Roboto", sans-serif;
    font-weight: bold;
    text-rendering: optimizeLegibility;
  }

  p {
    margin-bottom: 1.188em;
    line-height: 1.5em;
  }

  h1 {
    font-size: 2em;
    line-height: 2em;
  }

  h2 {
    font-size: 1.667em;
    line-height: 2em;
  }

  h3 {
    font-size: 1.667em;
    line-height: 2em;
  }

  h4 {
    font-size: 1.5em;
    line-height: 2em;
  }

  h5 {
    font-size: 1.375em;
    line-height: 2em;
  }

  h6 {
    font-size: 1.25em;
    line-height: 2em;
  }

  hr {
    margin-bottom: 1.188em;
    border: none;
    height: 1px;
    background: #aaa;
  }

  em {
    font-style: italic;
  }

  strong {
    font-weight: bold;
  }

  ul {
    margin: 0 0 1.188em 1.188em;
    list-style-position: inside;
    list-style-image: none;
    list-style-type: disc;
  }

  ol {
    margin: 0 0 1.188em 1.188em;
    list-style-position: inside;
    list-style-image: none;
    list-style-type: decimal;
  }

  li {
    margin-bottom: calc(1.188em / 2);
  }

  li > ol {
    margin-top: calc(1.188em / 2);
    margin-bottom: calc(1.188em / 2);
    margin-left: 1.188em;
  }

  li > ul {
    margin-top: calc(1.188em / 2);
    margin-bottom: calc(1.188em / 2);
    margin-left: 1.188em;
  }

  li *:last-child {
    margin-bottom: 0;
  }

  li > p {
    margin-bottom: calc(1.188em / 2);
  }

  table {
    margin-bottom: 2em;
    border-collapse: collapse;
    width: 100%;
  }

  thead {
    text-align: left;
  }

  td,
  th {
    border-bottom: 1px solid #aaa;
    padding: 0.625em 1em;
    font-feature-settings: "tnum";
    text-align: left;
  }

  th {
    font-weight: bold;
  }

  th:first-child,
  td:first-child {
    padding-left: 0;
  }

  th:last-child,
  td:last-child {
    padding-right: 0;
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
