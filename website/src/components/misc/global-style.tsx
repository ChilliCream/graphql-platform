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
    font-size: 18px;
    line-height: 30px;
    background-color: #ccc;
    color: #667;
    scroll-behavior: smooth;

    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
  }

  * {
    margin: 0;
    padding: 0;
    /*user-select: none;*/
    font-family: sans-serif;
    font-size: 1em;
    line-height: 1em;
    font-weight: normal;
  }

  *:focus {
    outline: none;
  }

  div, span {
    overflow: hidden;
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

  h1,
  h2,
  h3,
  h4,
  h5,
  h6 {
    margin-bottom: 0.5em;
    font-family: "Roboto", sans-serif;
    font-weight: bold;
    line-height: 1.250em;
    text-rendering: optimizeLegibility;
    color: #334;
  }

  p {
    margin-bottom: 1.188em;
    line-height: 1.667em;

    code[class*="language-"] {
      padding: 2px 5px;
      font-size: 0.833em;
    }
  }

  h1 {
    font-size: 1.750em;
  }

  h2 {
    font-size: 1.625em;
  }

  h3 {
    font-size: 1.5em;
  }

  h4 {
    font-size: 1.375em;
  }

  h5 {
    font-size: 1.25em;
  }

  h6 {
    font-size: 1.125em;
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

  blockquote {
    margin-bottom: 1.188em;
    background-color: #e7e9eb;

    > p:last-child {
      margin-bottom: 0;
    }
  }

  ul {
    margin: 0 0 1.188em 1.188em;
    list-style-position: outside;
    list-style-image: none;
    list-style-type: disc;
  }

  ol {
    margin: 0 0 1.188em 1.188em;
    list-style-position: outside;
    list-style-image: none;
    list-style-type: decimal;
  }

  li {
    margin-bottom: calc(1.188em / 2);
    line-height: 1.667em;
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

  tbody {
    > tr:nth-child(odd) {
      background-color: #f6f8fa;
    }

    > tr:hover {
      background-color: #e7e9eb;
    }
  }

  td,
  th {
    border-bottom: 1px solid #aaa;
    padding: 5px 10px;
    font-feature-settings: "tnum";
    font-size: 0.875em;
    line-height: 1.667em;
    text-align: left;
  }

  th {
    border-top: 1px solid #aaa;
    border-bottom: 2px solid #aaa;
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

  .gatsby-highlight {
    position: relative;
    margin: 1.188em 0;
    overflow: auto;
    font-size: 0.833em !important;

    * {
      font-family: Consolas, Monaco, "Andale Mono", "Ubuntu Mono", monospace;
      line-height: 1.5em !important;
    }

    pre[class*="language-"] {
      margin: 0;

      ::before {
        position: absolute;
        top: 0;
        right: 50px;
        left: initial;
        border-radius: 0px 0px 4px 4px;
        padding: 6px 8px;
        font-size: 0.833em;
        font-weight: bold;
        letter-spacing: 0.075em;
        line-height: 1em;
        text-transform: uppercase;
      }
    }

    pre[class="language-csharp"]::before {
      content: "C#";
      color: #4f3903;
      background: #ffb806;
    }

    pre[class="language-graphql"]::before {
      content: "GraphQL";
      color: #ffffff;
      background: #e535ab;
    }
  }

  .gatsby-highlight-code-line {
    background-color: #444;
    display: block;
    margin: 0 -50px;
    padding: 0 50px;
  }

  .mermaid {
    margin-bottom: 1.188em;
  }
`;
