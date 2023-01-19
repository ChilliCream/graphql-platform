import { createGlobalStyle } from "styled-components";

import {
  DEFAULT_THEME_COLORS,
  FONT_FAMILY,
  FONT_FAMILY_HEADING,
  THEME_COLORS,
} from "@/shared-style";

export const GlobalStyle = createGlobalStyle`
  :root {
    ${DEFAULT_THEME_COLORS}

    --border-radius: 4px;
    --font-size: .833rem;
  }

  html {
    height: 100vh;
    overflow: hidden;
    font-family: ${FONT_FAMILY};
    -webkit-text-size-adjust: 100%;
  }

  body {
    margin: 0;
    overflow: hidden;
    font-size: 18px;
    line-height: 30px;
    color: ${THEME_COLORS.text};
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
  }

  * {
    margin: 0;
    padding: 0;
    font-family: ${FONT_FAMILY};
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
    color: ${THEME_COLORS.link};
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
    font-family: ${FONT_FAMILY_HEADING};
    font-weight: 500;
    line-height: 1.250em;
    text-rendering: optimizeLegibility;
    color: ${THEME_COLORS.heading};
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
    font-weight: 600;
  }

  blockquote {
    margin-bottom: 20px;
    background-color: ${THEME_COLORS.backgroundAlt};

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
      background-color: ${THEME_COLORS.backgroundAlt};
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
    font-weight: 600;
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
    border: 1px solid ${THEME_COLORS.boxBorder};
    border-radius: .3em;
    background-color: initial;
    color: ${THEME_COLORS.text};
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
