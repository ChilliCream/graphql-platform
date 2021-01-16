import { createGlobalStyle } from "styled-components";

export const GlobalStyle = createGlobalStyle`
  html {
    display: flex;
    flex-direction: column;
    flex: 1;
    overflow: hidden;
    height: 100%;

    font-family: sans-serif;
    -ms-text-size-adjust: 100%;
    -webkit-text-size-adjust: 100%;
  }

  body {
    display: flex;
    flex-direction: column;
    flex: 1;
    overflow: hidden;
    height: 100%;
    margin: 0;

    font-size: 18px;
    line-height: 30px;
    color: #667;
    scroll-behavior: smooth;

    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;

    > div {
      height: 100%;
      display: block;
      > div {
        height: 100%;
        display: grid;
        grid-template-rows: 60px auto;
        grid-template-columns: 1fr;
      }
    }
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
    margin-bottom: 10px;
    font-family: "Roboto", sans-serif;
    font-weight: bold;
    line-height: 1.250em;
    text-rendering: optimizeLegibility;
    color: #334;
  }

  p {
    margin-bottom: 20px;
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
    margin-bottom: 20px;
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
    margin-bottom: 20px;
    background-color: #e7e9eb;

    > p:last-child {
      margin-bottom: 0;
    }
  }

  ul {
    margin: 0 0 20px 20px;
    list-style-position: outside;
    list-style-image: none;
    list-style-type: disc;
  }

  ol {
    margin: 0 0 20px 20px;
    list-style-position: outside;
    list-style-image: none;
    list-style-type: decimal;
  }

  li {
    margin-bottom: 10px;
    line-height: 1.667em;
  }

  li > ol {
    margin: 10px 0 10px 20px;
  }

  li > ul {
    margin: 10px 0 10px 20px;
  }

  li > p {
    margin-bottom: 10px;
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
    font-size: 0.833em;
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

  .gatsby-code-button-container {
    position: relative;
    top: 54px;
    z-index: 1;
    display: flex;
    margin: -34px 0 0;

    > .gatsby-code-button {
      position: relative;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 0 0 0 4px;
      padding: 8px 8px;
      background-color: #aaa;
      transition: all 0.4s ease-in;

      &:after {
        visibility: hidden;
        position: initial;
        display: none;
      }

      &:hover {
        &:after {
          visibility: hidden;
          display: none;
        }

        > svg {
          fill:	#2d2d2d;
        }
      }

      &:focus:after {
        visibility: hidden;
        display: none;
      }

      > svg {
      width: 18px;
      height: 18px;
        fill: #2d2d2d;
      }
    }
  }

  .gatsby-code-button-toaster {
    top: initial;
    right: 0;
    bottom: 30px;
    left: 0;
    display: flex;
    justify-content: center;
    width: 100%;
    height: initial;

    > .gatsby-code-button-toaster-text {
      flex: 0 0 60%;
      border-radius: 4px;
      font-family: sans-serif;
      font-size: 1em;
      font-weight: bold;
      line-height: 1em;
      background-color: #aaa;
      color: #667;
    }

    @media only screen and (min-width: 400px) {
      > .gatsby-code-button-toaster-text {
        max-width: 300px;
      }
    }
  }

  .gatsby-highlight {
    position: relative;
    margin: 20px 0;
    overflow: initial;
    font-size: 0.833em !important;

    * {
      font-family: Consolas, Monaco, "Andale Mono", "Ubuntu Mono", monospace;
      line-height: 1.5em !important;
    }

    > pre[class*="language-"] {
      margin: 0;

      ::before {
        position: absolute;
        top: 0;
        left: 50px;
        border-radius: 0px 0px 4px 4px;
        padding: 6px 8px;
        font-size: 0.800em;
        font-weight: bold;
        letter-spacing: 0.075em;
        line-height: 1em;
        text-transform: uppercase;
      }
    }

    > pre[class="language-bash"]::before {
      content: "Bash";
      color: #333;
      background: #0fd;
    }

    > pre[class="language-csharp"]::before {
      content: "C#";
      color: #4f3903;
      background: #ffb806;
    }

    > pre[class="language-graphql"]::before {
      content: "GraphQL";
      color: #fff;
      background: #e535ab;
    }

    > pre[class="language-http"]::before {
      content: "HTTP";
      color: #efeaff;
      background: #8b76cc;
    }

    > pre[class="language-json"]::before {
      content: "JSON";
      color: #fff;
      background: #1da0f2;
    }

    > pre[class="language-sdl"]::before {
      content: "SDL";
      color: #fff;
      background: #e535ab;
    }

    > pre[class="language-sql"]::before {
      content: "SQL";
      color: #fff;
      background: #80f;
    }

    > pre[class="language-xml"]::before {
      content: "XML";
      color: #fff;
      background: #999;
    }
  }

  .gatsby-highlight-code-line {
    background-color: #444;
    display: block;
    margin: 0 -50px;
    padding: 0 50px;
  }

  .mermaid {
    display: flex;
    justify-content: center;
    margin-bottom: 20px;

  }

  /* Inline code style */
  :not(pre) > code[class*="language-"] {
    border: 1px solid #aaa;
    background-color: initial;
    color: #666;

    .token.comment,
    .token.block-comment,
    .token.prolog,
    .token.doctype,
    .token.cdata {
      color: #999;
    }

    .token.punctuation {
      color: #666;
    }

    .token.tag,
    .token.attr-name,
    .token.namespace,
    .token.deleted {
      color: #e2777a;
    }

    .token.function-name {
      color: #6196cc;
    }

    .token.boolean,
    .token.number,
    .token.function {
      color: #f08d49;
    }

    .token.property,
    .token.class-name,
    .token.constant,
    .token.symbol {
      color: #f8c555;
    }

    .token.selector,
    .token.important,
    .token.atrule,
    .token.keyword,
    .token.builtin {
      color: #cc99cd;
    }

    .token.string,
    .token.char,
    .token.attr-value,
    .token.regex,
    .token.variable {
      color: #7ec699;
    }

    .token.operator,
    .token.entity,
    .token.url {
      color: #67cdcc;
    }

    .token.inserted {
      color: green;
    }
  }
`;
