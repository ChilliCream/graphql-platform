import { createGlobalStyle } from "styled-components";

export const GlobalLayoutStyle = createGlobalStyle`
 html {
    overflow: hidden;
    height: 100%;
  }

  body {
    overflow: hidden;
    height: 100%;
    margin: 0;

    > div:first-child {
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
`;

export const GlobalStyle = createGlobalStyle`
  :root {
    --primary-color: #3b4f74;//before: f40010;
    --secondary-color: #516083;
    --tertiary-color: #7989ab;
    --quaternary-color: #bfcef1;
    --link-color: #f4125b;
    --text-color: #667;
    --heading-text-color: #3b4f74;
    --footer-text-color: #c6c6ce;
    --text-color-contrast: #fff;
    --box-highlight-color: #e8ecf5;
    --box-border-color: #bfcef1;
    --warning-color: #ffba00;
    --border-radius: 4px;
    --font-size: .833rem;
  }

  html {
    font-family: sans-serif;
    -ms-text-size-adjust: 100%;
    -webkit-text-size-adjust: 100%;
  }

  body {
    font-size: 18px;
    line-height: 30px;
    color: var(--text-color);
    scroll-behavior: smooth;

    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
  }

  * {
    margin: 0;
    padding: 0;
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
    color: var(--link-color);
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
    color: var(--heading-text-color);
  }

  p {
    margin-bottom: 20px;
    line-height: 1.667em;
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
    background-color: #e8ecf5;

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
      background-color: #e8ecf5;
    }
  }

  td,
  th {
    border-bottom: 1px solid #aaa;
    padding: 5px 10px;
    font-feature-settings: "tnum";
    font-size: var(--font-size);
    line-height: 1.667em;
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

  .mermaid {
    display: flex;
    justify-content: center;
    margin-bottom: 20px;
  }

  /* Inline code style */
  :not(pre) > code {
    border: 1px solid var(--box-border-color);
    border-radius: .3em;
    background-color: initial;
    color: var(--text-color);
  }

  a.anchor {
    position: absolute;
    left: 0;
    visibility: hidden;
  }

  h1:hover a.anchor,
  h2:hover a.anchor,
  h3:hover a.anchor,
  h4:hover a.anchor,
  h5:hover a.anchor,
  h6:hover a.anchor {
    visibility: visible;
  }
`;
