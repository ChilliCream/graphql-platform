import React, { FC, useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";

import { Close } from "@/components/misc/close";
import { State, WorkshopsState } from "@/state";
import { hidePromo, showPromo } from "@/state/common";
import { Dialog, DialogContainer, DialogLinkButton } from "./dialog";

export const Promo: FC = () => {
  const showCookieConsent = useSelector<State, boolean>(
    (state) => state.common.showCookieConsent
  );
  const show = useSelector<State, boolean>((state) => state.common.showPromo);
  const workshop = useSelector<State, WorkshopsState[number] | undefined>(
    (state) => state.workshops.find(({ banner, active }) => banner && active)
  );
  const dispatch = useDispatch();

  const storageKey = workshop && `banner:workshop-${workshop.id}`;

  const handleDismiss = () => {
    if (storageKey) {
      localStorage.setItem(storageKey, "true");
      dispatch(hidePromo());
    }
  };

  useEffect(() => {
    if (storageKey) {
      const dismissed = localStorage.getItem(storageKey);

      if (dismissed !== "true") {
        dispatch(showPromo());
      }
    }
  }, []);

  if (!workshop) {
    return null;
  }

  return (
    <Dialog
      role="dialog"
      aria-live="polite"
      aria-label="promo"
      aria-describedby="promo:desc"
      show={!showCookieConsent && show}
    >
      {!showCookieConsent && show && (
        <Boundary>
          <Container>
            <Message id="promo:desc">
              <Title>{workshop.title}</Title>
              <Description>{workshop.teaser}</Description>
            </Message>
            <Actions>
              <DialogLinkButton to={workshop.url}>
                Check it out!
              </DialogLinkButton>
              <Dismiss
                aria-label="dismiss promo message"
                onClick={handleDismiss}
              />
            </Actions>
          </Container>
        </Boundary>
      )}
    </Dialog>
  );
};

const Boundary = styled.div`
  position: relative;
  max-width: 1000px;
  margin: auto;
`;

const Container = styled(DialogContainer)`
  @media only screen and (min-width: 600px) {
    flex-direction: row;
    align-items: flex-end;
    gap: 20px;
  }
`;

const Message = styled.div`
  flex: 1 1 auto;
  font-size: 16px;

  @media only screen and (min-width: 600px) {
    font-size: 18px;
  }
`;

const Title = styled.h5`
  color: #0b0722;
`;

const Description = styled.p`
  line-height: 1.667em;
  color: #0b0722;
`;

const Actions = styled.div`
  flex: 0 0 auto;
  display: flex;
  flex-direction: column-reverse;
  align-items: flex-end;

  @media only screen and (min-width: 600px) {
    ${DialogLinkButton} {
      align-self: flex-end;
      margin-right: 10px;
      width: 140px;
    }
  }
`;

const Dismiss = styled(Close)`
  position: absolute;
  top: 10px;
  right: 10px;
  padding: 10px;
`;
