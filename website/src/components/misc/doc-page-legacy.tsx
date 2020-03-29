import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { Link } from "./link";

export const DocPageLegacy: FunctionComponent = () => {
  return (
    <Dialog
      role="dialog"
      aria-live="polite"
      aria-label="legacydoc"
      aria-describedby="legacydoc:desc"
    >
      <Container>
        <Message id="legacydoc:desc">
          <strong>Important:</strong> This documentation covers Hot Chocolate
          11. For version 10 and lower click{" "}
          <LegacyDocLink to="https://hotchocolate.io">here</LegacyDocLink>.
        </Message>
      </Container>
    </Dialog>
  );
};

const Dialog = styled.div`
  background-color: #ffb806;
`;

const Container = styled.div`
  display: flex;
  flex-direction: column;
  padding: 10px 20px;

  @media only screen and (min-width: 800px) {
    padding: 10px 50px;
  }
`;

const Message = styled.div`
  flex: 0 1 auto;
  font-size: 0.889em;
  line-height: 1.667em;
  color: #4f3903;
`;

const LegacyDocLink = styled(Link)`
  text-decoration: underline;
  color: #4f3903;
`;
