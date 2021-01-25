import React, { FunctionComponent } from "react";
import styled from "styled-components";
import Highlight, { Language } from "prism-react-renderer";
import Prism from "prismjs"
import { Copy } from "./copy";

interface CodeBlockProps {
  children: any;
}

export const CodeBlock: FunctionComponent<CodeBlockProps> = ({
  children
}) => {
  const language = children.props.className.replace(/language-/, '') as Language;
  const meta = children.props.metastring;
  const shouldHighlightLine = calculateLinesToHighlight(meta);
  const code = children.props.children;

  return (
    <Container className={`gatsby-highlight code-${language}`}>
      <CodeIndicator language={language} />
      <CopyPosition>
        <Copy content={code} />
      </CopyPosition>
      <Highlight
        Prism={Prism as any}
        code={code}
        language={(language as string) === 'sdl' ? 'graphql' : language}
      >
        {({ className, style, tokens, getLineProps, getTokenProps }) => (
          <Pre className={className} style={style}>
            {tokens.map((line, i) => (
              <Line
                highlight={shouldHighlightLine(i)}
                {...getLineProps({ line: line, key: i })}
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

const CodeIndicator: FunctionComponent<CodeIndicatorProps> = ({ language }) => {
  const codeLanguage = codeLanguages[language];

  return (
    codeLanguage
      ? <IndicatorContent style={{
        color: codeLanguage.color,
        background: codeLanguage.background,
      }}>{codeLanguage.content}</IndicatorContent>
      : null
  )
}

const codeLanguages: Record<string, { content: string, color: string, background: string }> = {
  csharp: {
    content: 'C#',
    color: '#4f3903',
    background: '#ffb806'
  },
  bash: {
    content: 'Bash',
    color: '#333',
    background: '#0fd',
  },
  graphql: {
    content: 'GraphQL',
    color: '#fff',
    background: '#e535ab',
  },
  http: {
    content: 'HTTP',
    color: '#efeaff',
    background: '#8b76cc',
  },
  json: {
    content: 'JSON',
    color: '#fff',
    background: '#1da0f2',
  },
  sdl: {
    content: 'SDL',

    color: '#fff',
    background: '#e535ab',
  },
  sql: {
    content: 'SQL',
    color: '#fff',
    background: '#80f',
  },
  xml: {
    content: 'XML',
    color: '#fff',
    background: '#999',
  }
}

const IndicatorContent = styled.div`
    position: absolute;
    z-index: 1;
    top: 0;
    left: 50px;
    border-radius: 0px 0px 4px 4px;
    padding: 2px 8px;
    font-size: 0.8em;
    font-weight: bold;
    letter-spacing: 0.075em;
    line-height: 1em;
    text-transform: uppercase;
`;

const matchHighlights = /{([\d,-]+)}/

const calculateLinesToHighlight = (meta: string): (index: number) => boolean => {
  if (!matchHighlights.test(meta)) {
    return () => false;
  }

  const lineNumbers = matchHighlights.exec(meta)![1]
    .split(',')
    .map((v) => v.split('-').map((v) => parseInt(v, 10)));

  return (index) => {
    const lineNumber = index + 1
    const inRange = lineNumbers.some(([start, end]) =>
      end ? lineNumber >= start && lineNumber <= end : lineNumber === start
    )
    return inRange
  }
}

const Pre = styled.pre`
  position: relative;
  overflow: scroll;

  & .token-line {
    line-height: 1.3em;
    height: 1.3em;
  }
`;

interface LineProps {
  highlight: boolean;
}

const Line = styled.div<LineProps>`
  display: table-row;

  ${({ highlight }) => highlight && `
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
    margin: 20px 0;
    overflow: initial;
    font-size: 0.833em !important;
    padding-right: 0 !important;
    padding-left: 0 !important;

    * {
      font-family: Consolas, Monaco, "Andale Mono", "Ubuntu Mono", monospace;
      line-height: 1.5em !important;
    }

    > pre[class*="language-"] {
      margin: 0;
      padding: 30px 20px;
    }
`;

const CopyPosition = styled.div`
  position: absolute;
  z-index: 1;
  top: 0;
  right: 0;
`;