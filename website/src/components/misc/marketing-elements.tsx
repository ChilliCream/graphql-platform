import styled from "styled-components";

import CheckIconSvg from "../../images/check.svg";
import EnvelopeIconSvg from "../../images/envelope.svg";
import SlackIconSvg from "../../images/slack.svg";

export const SectionRow = styled.div`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: space-around;
  width: 100%;
  max-width: 1100px;
`;

export const Section = styled.section`
  display: flex;
  flex: 0 0 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 70px 0 50px;
  width: 100%;
  overflow: hidden;

  &:nth-child(odd) {
    background-color: #efefef;
  }

  @media only screen and (min-width: 992px) {
    &:nth-child(even) > ${SectionRow} {
      flex-direction: row;
    }

    &:nth-child(odd) > ${SectionRow} {
      flex-direction: row-reverse;
    }
  }
`;

export const ImageContainer = styled.div<{ large?: boolean }>`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  margin-bottom: 50px;
  padding: 0 20px;
  width: 100%;
  max-width: ${({ large }) => (large ? 480 : 380)}px;

  @media only screen and (min-width: 992px) {
    flex: 0 0 35%;
    box-sizing: initial;
    margin-bottom: initial;
    padding: 0;
    max-width: ${({ large }) => (large ? 380 : 280)}px;
  }

  > * {
    width: 100%;
  }
`;

export const ContentContainer = styled.div<{ noImage?: boolean }>`
  display: flex;
  flex-direction: column;
  padding: 0 40px;

  > p {
    text-align: center;
  }

  @media only screen and (min-width: 992px) {
    flex: 0 0 ${({ noImage }) => (noImage ? 100 : 55)}%;
    padding: 0;

    > p {
      text-align: initial;
    }
  }
`;

export const SectionTitle = styled.h1<{ centerAlways?: boolean }>`
  flex: 0 0 auto;
  font-size: 1.75em;
  color: #667;
  text-align: center;

  @media only screen and (min-width: 768px) {
    margin-bottom: 20px;
  }

  @media only screen and (min-width: 992px) {
    text-align: ${({ centerAlways }) => (centerAlways ? "center" : "initial")};
  }
`;

export const List = styled.ul`
  list-style-type: none;
  align-self: center;

  @media only screen and (min-width: 992px) {
    align-self: initial;
  }
`;

export const ListItem = styled.li``;

export const CheckIcon = styled(CheckIconSvg)`
  margin: 0 10px 5px 0;
  width: 24px;
  height: 24px;
  vertical-align: middle;
  fill: green;
`;

export const EnvelopeIcon = styled(EnvelopeIconSvg)`
  width: 24px;
  height: 24px;
  vertical-align: middle;
  fill: #666;

  &:hover {
    fill: #000;
  }
`;

export const SlackIcon = styled(SlackIconSvg)`
  width: 24px;
  height: 24px;
  vertical-align: middle;
  fill: #666;

  &:hover {
    fill: #000;
  }
`;
