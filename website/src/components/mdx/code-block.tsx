import React, { FC } from "react";
import styled from "styled-components";
import Highlight, { Language } from "prism-react-renderer";
import Prism from "prismjs"
import { Copy } from "./copy";

interface CodeBlockProps {
  children: any;
}

export const CodeBlock: FC<CodeBlockProps> = ({
  children
}) => {
  const language = children.props.className.replace(/language-/, '') as Language;
  const meta = children.props.metastring;
  const shouldHighlightLine = calculateLinesToHighlight(meta);
  const code = children.props.children;

  return (
    <Container className="gatsby-highlight">
      <Highlight
        Prism={Prism as any}
        code={code}
        language={(language as string) === 'sdl' ? 'graphql' : language}
      >
        {({ className, style, tokens, getLineProps, getTokenProps }) => (
          <Pre className={`${className} code-${language}`} style={style}>
            <CopyPosition>
              <Copy content={code} />
            </CopyPosition>
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

const RE = /{([\d,-]+)}/

const calculateLinesToHighlight = (meta: string): (index: number) => boolean => {
  if (!RE.test(meta)) {
    return () => false;
  }

  const lineNumbers = RE.exec(meta)![1]
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

    > pre[class="code-bash"]::before {
      content: "Bash";
      color: #333;
      background: #0fd;
    }

    > pre[class*="code-csharp"]::before {
      content: "C#";
      color: #4f3903;
      background: #ffb806;
    }

    > pre[class*="code-graphql"]::before {
      content: "GraphQL";
      color: #fff;
      background: #e535ab;
    }

    > pre[class*="code-http"]::before {
      content: "HTTP";
      color: #efeaff;
      background: #8b76cc;
    }

    > pre[class*="code-json"]::before {
      content: "JSON";
      color: #fff;
      background: #1da0f2;
    }

    > pre[class*="code-sdl"]::before {
      content: "SDL";
      color: #fff;
      background: #e535ab;
    }

    > pre[class*="code-sql"]::before {
      content: "SQL";
      color: #fff;
      background: #80f;
    }

    > pre[class*="code-xml"]::before {
      content: "XML";
      color: #fff;
      background: #999;
    }
`;

const CopyPosition = styled.div`
  position: absolute;
  top: 0;
  right: 0;
`;