import { createGlobalStyle } from "styled-components";

import {
  DEFAULT_THEME_COLORS,
  FONT_FAMILY,
  FONT_FAMILY_HEADING,
  THEME_COLORS,
} from "./shared-style";

export const GlobalStyle = createGlobalStyle`
  :root {
    ${DEFAULT_THEME_COLORS}

    --border-radius: 4px;
    --box-border-radius: 12px;
    --button-border-radius: 6px;
    // TODO: Get rid of --font-size
    --font-size: .875rem;
  }

  html {
    height: 100vh;
    overflow: hidden;
    font-family: ${FONT_FAMILY};
    font-size: 16px;
    -webkit-text-size-adjust: 100%;
  }

  body {
    margin: 0;
    overflow: hidden;
    font-size: 1rem;
    letter-spacing: 0.025rem;
    line-height: 1.6em;
    color: ${THEME_COLORS.text};
    background-image: radial-gradient(ellipse at bottom, #151135 0%, ${THEME_COLORS.background} 40%);
    background-color: ${THEME_COLORS.background};
    background-size: auto;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;

    @media only screen and (min-width: 992px) {
      font-size: 1.25rem;
    }
  }

  * {
    margin: 0;
    padding: 0;
    //font-family: ${FONT_FAMILY};
    //font-size: 1.25rem;
    //letter-spacing: 0.025rem;
    //line-height: 1.6em;
    //font-weight: normal;
    scrollbar-width: thin;
    scrollbar-color: ${THEME_COLORS.text} ${THEME_COLORS.backgroundMenu};
  }

  .text-1 {
    font-family: ${FONT_FAMILY};
    font-size: 1rem;
    letter-spacing: 0.025rem;
    line-height: 1.6em;
    word-break: break-word;

    @media only screen and (min-width: 992px) {
      font-size: 1.25rem;
    }
  }

  .text-2 {
    font-family: ${FONT_FAMILY};
    font-size: 0.875rem;
    letter-spacing: 0.025rem;
    line-height: 1.6em;
    word-break: break-word;

    @media only screen and (min-width: 992px) {
      font-size: 1rem;
    }
  }

  .text-3 {
    font-family: ${FONT_FAMILY};
    font-size: 0.75rem;
    letter-spacing: 0.025rem;
    line-height: 1.6em;
    word-break: break-word;

    @media only screen and (min-width: 992px) {
      font-size: 0.875rem;
    }
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
    transition: color 0.2s ease-in-out;

    :hover {
      color: ${THEME_COLORS.linkHover};
    }
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
    font-family: ${FONT_FAMILY_HEADING};
    font-weight: 700;
    line-height: 1.2em;
    letter-spacing: normal;
    text-rendering: optimizeLegibility;
    word-break: break-word;
    color: ${THEME_COLORS.heading};
  }

  p {
    margin-bottom: 16px;
    word-break: break-word;
  }

  h1 {
    font-size: 2rem;
    letter-spacing: -0.025rem;

    @media only screen and (min-width: 992px) {
      font-size: 5rem;
      letter-spacing: normal;
    }
  }

  h1.prominent {
    font-size: 2.5rem;

    @media only screen and (min-width: 992px) {
      font-size: 6rem;
    }
  }

  h2 {
    font-size: 2rem;
    letter-spacing: -0.025rem;

    @media only screen and (min-width: 992px) {
      font-size: 4rem;
      font-weight: 600;
      line-height: 1.12em;
    }
  }

  h3 {
    font-size: 1.5rem;
    line-height: 1.12em;
    letter-spacing: -0.025rem;

    @media only screen and (min-width: 992px) {
      font-size: 2.75rem;
    }
  }

  h4 {
    font-size: 2rem;
    letter-spacing: -0.025rem;
  }

  h5 {
    font-size: 1.5rem;
    line-height: 1.5em;
  }

  h6 {
    font-size: 1.375rem;
    line-height: 1.5em;
  }

  hr {
    margin-bottom: 16px;
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
    margin-bottom: 16px;
    border: 1px solid ${THEME_COLORS.boxBorder};
    border-radius: var(--box-border-radius);
    background-color: #30566a99;

    > p:last-child {
      margin-bottom: 0;
    }
  }

  ul {
    margin: 0 0 16px 24px;
    list-style-position: outside;
    list-style-image: none;
    list-style-type: disc;
  }

  ol {
    margin: 0 0 16px 24px;
    list-style-position: outside;
    list-style-image: none;
    list-style-type: decimal;
  }

  li {
    margin-bottom: 8px;
    line-height: 1.6em;
    word-break: break-word;
  }

  li > ol {
    margin: 8px 0 8px 24px;
  }

  li > ul {
    margin: 8px 0 8px 24px;
  }

  li > p {
    margin-bottom: 8px;
  }

  table {
    margin-bottom: 24px;
    border-collapse: collapse;
    width: 100%;
  }

  thead {
    text-align: left;
  }

  tbody {
    > tr:nth-child(odd) {
      background-color: ${THEME_COLORS.backgroundAlt};
    }

    > tr:hover {
      background-color: #ffffff1a;
    }
  }

  td,
  th {
    border-bottom: 1px solid ${THEME_COLORS.boxBorder};
    padding: 5px 10px;
    font-feature-settings: "tnum";
    font-size: var(--font-size);
    line-height: 1.6em;
  }

  th {
    border-top: 1px solid ${THEME_COLORS.boxBorder};
    border-bottom: 2px solid ${THEME_COLORS.boxBorder};
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

  .gatsby-resp-image-wrapper {
    margin-bottom: 16px;
  }

  .mermaid {
    display: flex;
    justify-content: center;
    margin-bottom: 16px;
  }

  /* Inline code style */
  :not(pre) > code {
    border: 1px solid ${THEME_COLORS.boxBorder};
    border-radius: var(--button-border-box);
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

  ::-webkit-scrollbar
  {
    width: 8px;
    background-color: ${THEME_COLORS.backgroundMenu};
  }

  ::-webkit-scrollbar-track
  {
    background-color: ${THEME_COLORS.backgroundMenu};
  }

  ::-webkit-scrollbar-thumb
  {
    border-radius: 4px;
    background-color: ${THEME_COLORS.text};
  }
`;
