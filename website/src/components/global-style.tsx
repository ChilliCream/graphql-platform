import { createGlobalStyle } from "styled-components";

export const GlobalStyle = createGlobalStyle`
  body,
  html,
  #root {
    width: 100vw;
    height: 100vh;
    font-size: 12px;
  }

  * {
    margin: 0;
    padding: 0;
    overflow: hidden;
    /*user-select: none;*/
    font-family: system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial,
      sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol";
    font-size: 1em;
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
`;
