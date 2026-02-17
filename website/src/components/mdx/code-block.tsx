import Highlight, { Language } from "prism-react-renderer";
import Prism from "prismjs";
import React, { FC } from "react";
import styled, { css } from "styled-components";

import { Play } from "@/components/mdx/play";
import { FONT_FAMILY_CODE, THEME_COLORS } from "@/style";
import { Copy } from "./copy";

export interface CodeBlockProps {
  readonly children: any;
  readonly language?: Language;
  readonly hideLanguageIndicator?: boolean;
  readonly playUrl?: string;
}

export const CodeBlock: FC<CodeBlockProps> = ({
  children,
  language: fallbackLanguage,
  hideLanguageIndicator,
  playUrl,
}) => {
  const language =
    (children.props?.className?.replace(/language-/, "") as Language) ||
    fallbackLanguage;
  const meta = children.props?.metastring;
  const shouldHighlightLine = calculateLinesToHighlight(meta);
  const code = children.props?.children || children;

  return (
    <Container className={`gatsby-highlight code-${language}`}>
      {!hideLanguageIndicator && language ? (
        <CodeIndicator language={language} />
      ) : null}

      <ActionsContainer>
        {playUrl ? <StyledPlay content={code} url={playUrl} /> : null}

        <StyledCopy content={code} />
      </ActionsContainer>

      <Highlight
        Prism={Prism as any}
        code={code}
        language={(language as string) === "sdl" ? "graphql" : language}
      >
        {({ className, style, tokens, getLineProps, getTokenProps }) => (
          <Pre className={className} style={style}>
            {tokens.map((line, i) => (
              <Line
                highlight={shouldHighlightLine(i)}
                {...getLineProps({ line, key: i })}
              >
                <LineContent>
                  {line.map((token, key) => (
                    <span key={key} {...getTokenProps({ token, key })} />
                  ))}
                </LineContent>
              </Line>
            ))}
          </Pre>
        )}
      </Highlight>
    </Container>
  );
};

interface CodeIndicatorProps {
  language: string;
}

const CodeIndicator: FC<CodeIndicatorProps> = ({ language }) => {
  const codeLanguage = codeLanguages[language];

  return codeLanguage ? (
    <IndicatorContent
      style={{
        color: codeLanguage.color,
      }}
    >
      {codeLanguage.content}
    </IndicatorContent>
  ) : null;
};

const codeLanguages: Record<
  string,
  {
    readonly content: string;
    readonly color: string;
  }
> = {
  csharp: {
    content: "C#",
    color: "#ffe261",
  },
  bash: {
    content: "Bash",
    color: "#74dfc4",
  },
  graphql: {
    content: "GraphQL",
    color: "#eb64b9",
  },
  http: {
    content: "HTTP",
    color: "#b381c5",
  },
  json: {
    content: "JSON",
    color: "#40b4c4",
  },
  sdl: {
    content: "SDL",
    color: "#eb64b9",
  },
  sql: {
    content: "SQL",
    color: "#b4dce7",
  },
  xml: {
    content: "XML",
    color: "#ffffff",
  },
};

const IndicatorContent = styled.div`
  position: absolute;
  z-index: 1;
  top: 0;
  left: 20px;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-top: 0 none;
  border-radius: 0px 0px var(--button-border-radius) var(--button-border-radius);
  padding: 2px 8px;
  font-size: 0.875rem;
  font-weight: 600;
  letter-spacing: 0.025rem;
  line-height: 1em;
  text-transform: uppercase;
`;

const matchHighlights = /{([\d,-]+)}/;

const calculateLinesToHighlight = (
  meta: string
): ((index: number) => boolean) => {
  if (!matchHighlights.test(meta)) {
    return () => false;
  }

  const lineNumbers = matchHighlights
    .exec(meta)![1]
    .split(",")
    .map((v) => v.split("-").map((v) => parseInt(v, 10)));

  return (index) => {
    const lineNumber = index + 1;
    const inRange = lineNumbers.some(([start, end]) =>
      end ? lineNumber >= start && lineNumber <= end : lineNumber === start
    );
    return inRange;
  };
};

const Pre = styled.pre`
  position: relative;
  max-width: 100vw;
  box-sizing: border-box;
  border-radius: 0 !important;

  & .token-line {
    line-height: 1.3em;
    height: 1.3em;
  }

  @media only screen and (min-width: 700px) {
    border-radius: var(--box-border-radius) !important;
  }
`;

interface LineProps {
  highlight: boolean;
}

const Line = styled.div<LineProps>`
  display: table-row;

  ${({ highlight }) =>
    highlight &&
    css`
      display: block;
      background-color: #444;
      margin: 0 -50px;
      padding: 0 50px;
    `};
`;

const LineContent = styled.span`
  display: table-cell;
`;

const Container = styled.div`
  position: relative;
  margin-bottom: 24px;
  font-size: 1rem !important;
  padding-right: 0 !important;
  padding-left: 0 !important;
  width: 100%;
  overflow: visible;

  * {
    font-family: ${FONT_FAMILY_CODE};
    line-height: 1.5em !important;
  }

  > pre[class*="language-"] {
    margin: 0;
    padding: 40px 30px;
    width: 100%;
    overflow: visible;
  }

  @media only screen and (min-width: 700px) {
    > pre[class*="language-"] {
      padding: 40px 40px;
    }
  }
`;

const ActionsContainer = styled.div`
  position: absolute;
  z-index: 2;
  top: 12px;
  right: 12px;
  display: flex;
  align-items: center;
  gap: 6px;
`;

const sharedButtonStyles = css`
  border-radius: var(--button-border-radius) !important;
  border: 1px solid ${THEME_COLORS.boxBorder} !important;
  padding: 10px !important;
  background-color: ${THEME_COLORS.background} !important;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.2) !important;
  transition: all 0.2s ease-in-out !important;

  svg {
    width: 20px !important;
    height: 20px !important;
  }

  > div {
    width: 20px !important;
    height: 20px !important;
  }

  &:hover {
    background-color: ${THEME_COLORS.backgroundAlt} !important;
    transform: translateY(-2px);
    box-shadow: 0 4px 10px rgba(0, 0, 0, 0.25) !important;
  }
`;

const StyledCopy = styled(Copy)`
  ${sharedButtonStyles}
`;

const StyledPlay = styled(Play)`
  ${sharedButtonStyles}
`;
