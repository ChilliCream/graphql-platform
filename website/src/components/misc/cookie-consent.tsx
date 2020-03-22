import React, { FunctionComponent, useState } from "react";
import { useCookies } from "react-cookie";
import styled from "styled-components";
import { Link } from "./link";

export const CookieConsent: FunctionComponent = () => {
  const cookieName = "chillicream-cookie-consent";
  const [cookies, setCookie] = useCookies([cookieName]);
  const [showDialog, setShowDialog] = useState(cookies[cookieName] !== "true");

  const clickDismiss = () => {
    setCookie(cookieName, "true", { path: "/" });
    setShowDialog(false);
  };

  return (
    <Dialog
      role="dialog"
      aria-live="polite"
      aria-label="cookieconsent"
      aria-describedby="cookieconsent:desc"
      show={showDialog}
    >
      <Container>
        <Message id="cookieconsent:desc">
          This website uses cookies to ensure you get the best experience on our
          website.{" "}
          <LearnMoreLink to="https://cookiesandyou.com">
            Learn more
          </LearnMoreLink>
        </Message>
        <AgreeButton
          aria-label="dismiss cookie message"
          role="button"
          onClick={clickDismiss}
        >
          Got it!
        </AgreeButton>
      </Container>
    </Dialog>
  );
};

const Dialog = styled.div<{ show: boolean }>(props => ({
  position: "absolute",
  bottom: 0,
  zIndex: 20,
  display: props.show ? "initial" : "none",
  width: "100vw",
  backgroundColor: "#ffb806",
  opacity: props.show ? 1 : 0,
  transition: "opacity 0.2s ease-in-out",
}));

const Container = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 15px 20px;

  @media only screen and (min-width: 400px) {
    flex-direction: row;
  }
`;

const Message = styled.div`
  flex: 0 1 auto;
  padding-bottom: 20px;
  font-size: 0.889em;
  line-height: 1.667em;
  color: #4f3903;

  @media only screen and (min-width: 400px) {
    padding-bottom: initial;
    padding-right: 20px;
  }
`;

const LearnMoreLink = styled(Link)`
  text-decoration: underline;
  color: #4f3903;
`;

const AgreeButton = styled.a`
  flex: 0 0 auto;
  margin-left: auto;
  padding: 10px 0;
  width: 100%;
  background-color: #f40010;
  font-size: 0.889em;
  text-align: center;
  color: #fff;
  cursor: pointer;

  @media only screen and (min-width: 400px) {
    flex: 0 0 160px;
    width: initial;
  }
`;
