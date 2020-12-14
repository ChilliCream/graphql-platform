import React, {FunctionComponent} from 'react';
import styled from 'styled-components';

export const ContentComponent: FunctionComponent = ({ children }) => {
  return (
      <Content>{children}</Content>
  );
};

const Content = styled.main`
  place-items: center;
  display: grid;
  overflow: visible;
  width: 100%;
`;
