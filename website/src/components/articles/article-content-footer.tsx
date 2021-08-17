import React, { FC } from "react";
import styled from "styled-components";

interface ArticleContentFooterProps {
  readonly lastUpdated: string;
  readonly lastAuthorName: string;
}

export const ArticleContentFooter: FC<ArticleContentFooterProps> = ({
  lastUpdated,
  lastAuthorName,
}) => {
  return (
    <Footer>
      <Small>
        Last updated on <strong>{lastUpdated}</strong> by{" "}
        <strong>{lastAuthorName}</strong>
      </Small>
    </Footer>
  );
};

const Small = styled.small`
  font-style: italic;
  font-size: 0.7em;
`;

const Footer = styled.div`
  text-align: right;
  margin-top: 30px;
`;
