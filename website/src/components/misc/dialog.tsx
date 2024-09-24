import styled from "styled-components";
import { Link } from "./link";

export const Dialog = styled.div<{ show: boolean }>`
  position: fixed;
  bottom: 0;
  z-index: 30;
  width: 100vw;
  background-color: #aba0ff;
  display: ${({ show }) => (show ? "visible" : "none")};
`;

export const DialogButton = styled.button`
  border-radius: var(--border-radius);
  max-width: 160px;
  padding: 10px;
  height: 40px;
  font-size: var(--font-size);
  color: #e9e7f4;
  background-color: #2e2857;
  transition: background-color 0.2s ease-in-out, border-color 0.2s ease-in-out,
    color 0.2s ease-in-out;

  &:hover {
    color: #e9e7f4;
    background-color: #3b3370;
  }
`;

export const DialogLinkButton = styled(Link)`
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  border-radius: var(--button-border-radius);
  max-width: 140px;
  height: 20px;
  padding: 10px;
  color: #e9e7f4;
  background-color: #2e2857;
  font-size: var(--font-size);
  text-decoration: none;
  transition: background-color 0.2s ease-in-out, border-color 0.2s ease-in-out,
    color 0.2s ease-in-out;

  :hover {
    color: #e9e7f4;
    background-color: #3b3370;
  }
`;

export const DialogContainer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 15px 20px;
`;

export const LearnMoreLink = styled(Link)`
  text-decoration: underline;
  color: #0b0722;

  &:hover {
    color: #3b3370;
  }
`;
