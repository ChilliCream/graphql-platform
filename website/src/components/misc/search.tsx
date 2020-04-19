import React, { FunctionComponent } from "react";
import styled from "styled-components";

export const Search: FunctionComponent = () => {
  return (
    <Container>
      <SearchField placeholder="Search ..." />
    </Container>
  );
};

const Container = styled.div`
  display: flex;
  flex: 1 1 auto;
  flex-direction: row;
  align-items: center;

  @media only screen and (min-width: 992px) {
    display: flex;
    flex: 0 0 auto;
  }
`;

const SearchField = styled.input`
  border: 0;
  border-radius: 4px;
  padding: 10px 15px;
  width: 100%;
  font-family: "Roboto", sans-serif;
  font-size: 0.833em;
  background-color: #fff;
`;
