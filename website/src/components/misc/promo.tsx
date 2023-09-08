import React, { FC, useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import styled from "styled-components";

import { Close } from "@/components/misc/close";
import { State, WorkshopsState } from "@/state";
import { hidePromo, showPromo } from "@/state/common";
import { Link } from "./link";

export const Promo: FC = () => {
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
      show={show}
    >
      {show && (
        <Boundary>
          <Container>
            <Message id="promo:desc">
              <Title>{workshop.title}</Title>
              <Description>{workshop.teaser}</Description>
            </Message>
            <Actions>
              <Tickets to={workshop.url}>Get tickets!</Tickets>
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

const Dialog = styled.div<{ show: boolean }>`
  position: fixed;
  bottom: 0;
  z-index: 40;
  width: 100vw;
  background-color: #ffb806;
  display: ${({ show }) => (show ? "visible" : "none")};
`;

const Boundary = styled.div`
  position: relative;
  max-width: 1000px;
  margin: auto;
`;

const Container = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 15px 20px;

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

const Title = styled.h2`
  color: #4f3903;
`;

const Description = styled.p`
  line-height: 1.667em;
  color: #4f3903;
`;

const Actions = styled.div`
  flex: 0 0 auto;
  display: flex;
  flex-direction: column-reverse;
  align-items: flex-end;
`;

const Tickets = styled(Link)`
  width: 120px;
  padding: 10px 15px;
  margin-bottom: 10px;
  border-radius: var(--border-radius);
  font-size: 0.833em;
  font-weight: 500;
  text-align: center;
  color: #ffffff;

  background-color: #e55723;
  transition: background-color 0.2s ease-in-out;

  &:hover {
    background-color: #d1410c;
  }

  @media only screen and (min-width: 600px) {
    margin-right: 10px;
    align-self: flex-end;
  }
`;

const Dismiss = styled(Close)`
  position: absolute;
  top: 10px;
  right: 10px;
  padding: 10px;
`;
