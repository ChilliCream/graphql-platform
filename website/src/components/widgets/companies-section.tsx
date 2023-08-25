import React, { FC } from "react";
import styled, { keyframes } from "styled-components";

import { Link } from "@/components/misc/link";
import {
  ContentContainer,
  Section,
  SectionRow,
  SectionTitle,
} from "@/components/misc/marketing-elements";
import { Company } from "@/components/sprites";
import { THEME_COLORS } from "@/shared-style";

// Logos
import AdditivLogoSvg from "@/images/companies/additiv.svg";
import AeiLogoSvg from "@/images/companies/aei.svg";
import AtminaLogoSvg from "@/images/companies/atmina.svg";
import AutoguruLogoSvg from "@/images/companies/autoguru.svg";
import BdnaLogoSvg from "@/images/companies/bdna.svg";
import BeyableLogoSvg from "@/images/companies/beyable.svg";
import BiqhLogoSvg from "@/images/companies/biqh.svg";
import CarmmunityLogoSvg from "@/images/companies/carmmunity.svg";
import CompassLogoSvg from "@/images/companies/compass.svg";
import E2mLogoSvg from "@/images/companies/e2m.svg";
import ExlrtLogoSvg from "@/images/companies/exlrt.svg";
import EzeepLogoSvg from "@/images/companies/ezeep.svg";
import FulcrumLogoSvg from "@/images/companies/fulcrum.svg";
import GalaxusLogoSvg from "@/images/companies/galaxus.svg";
import GiaLogoSvg from "@/images/companies/gia.svg";
import HiloLogoSvg from "@/images/companies/hilo.svg";
import IncloudLogoSvg from "@/images/companies/incloud.svg";
import InfoslipsLogoSvg from "@/images/companies/infoslips.svg";
import MicrosoftLogoSvg from "@/images/companies/microsoft.svg";
import MotiviewLogoSvg from "@/images/companies/motiview.svg";
import OrderinLogoSvg from "@/images/companies/orderin.svg";
import PoweredSoftLogoSvg from "@/images/companies/powered-soft.svg";
import PushpayLogoSvg from "@/images/companies/pushpay.svg";
import RailCargoAustriaLogoSvg from "@/images/companies/rail-cargo-austria.svg";
import Seven2OneLogoSvg from "@/images/companies/seven-2-one.svg";
import SolyticLogoSvg from "@/images/companies/solytic.svg";
import SonikaLogoSvg from "@/images/companies/sonika.svg";
import SpectrumMedicalLogoSvg from "@/images/companies/spectrum-medical.svg";
import SpeedwayMotorsLogoSvg from "@/images/companies/speedway-motors.svg";
import SplashbackLogoSvg from "@/images/companies/splashback.svg";
import SweetGeeksLogoSvg from "@/images/companies/sweetgeeks.svg";
import SwissLifeLogoSvg from "@/images/companies/swiss-life.svg";
import SytadelleLogoSvg from "@/images/companies/sytadelle.svg";
import TrackmanLogoSvg from "@/images/companies/trackman.svg";
import TravelSoftLogoSvg from "@/images/companies/travel-soft.svg";
import VptechLogoSvg from "@/images/companies/vptech.svg";
import XMLogoSvg from "@/images/companies/xm.svg";
import ZioskLogoSvg from "@/images/companies/ziosk.svg";

export const CompaniesSection: FC = () => (
  <Section>
    <SectionRow>
      <ContentContainer noImage>
        <SectionTitle centerAlways>Companies Who Trust Us</SectionTitle>
        <Carousel>
          <MicrosoftLogo>
            <Link to="https://www.microsoft.com">
              <Company {...MicrosoftLogoSvg} />
            </Link>
          </MicrosoftLogo>
          <SwissLifeLogo>
            <Link to="https://www.swisslife.ch">
              <Company {...SwissLifeLogoSvg} />
            </Link>
          </SwissLifeLogo>
          <GalaxusLogo>
            <Link to="https://www.galaxus.ch">
              <Company {...GalaxusLogoSvg} />
            </Link>
          </GalaxusLogo>
        </Carousel>
        <Ticker>
          <GenericLogo width={140}>
            <Link to="https://additiv.com">
              <Company {...AdditivLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={160}>
            <Link to="https://aeieng.com">
              <Company {...AeiLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={100}>
            <Link to="https://atmina.de">
              <Company {...AtminaLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={180}>
            <Link to="https://www.autoguru.com.au">
              <Company {...AutoguruLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={150}>
            <Link to="https://bdna.com.au">
              <Company {...BdnaLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={150}>
            <Link to="https://www.beyable.com">
              <Company {...BeyableLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={100}>
            <Link to="https://www.biqh.com">
              <Company {...BiqhLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={180}>
            <Link to="https://carmmunity.io">
              <Company {...CarmmunityLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={180}>
            <Link to="https://www.compass.education">
              <Company {...CompassLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={90}>
            <Link to="https://www.e2m.energy">
              <Company {...E2mLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={130}>
            <Link to="https://www.exlrt.com">
              <Company {...ExlrtLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={100}>
            <Link to="https://www.ezeep.com">
              <Company {...EzeepLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={150}>
            <Link to="https://fulcrumpro.com/">
              <Company {...FulcrumLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={120}>
            <Link to="https://gia.ch">
              <Company {...GiaLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={70}>
            <Link to="https://www.hiloenergie.com">
              <Company {...HiloLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={200}>
            <Link to="https://www.incloud.de">
              <Company {...IncloudLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={130}>
            <Link to="https://www.infoslips.com">
              <Company {...InfoslipsLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={160}>
            <Link to="https://motitech.co.uk">
              <Company {...MotiviewLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={160}>
            <Link to="https://orderin.co.za">
              <Company {...OrderinLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={110}>
            <Link to="https://poweredsoft.com">
              <Company {...PoweredSoftLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={180}>
            <Link to="https://pushpay.com">
              <Company {...PushpayLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={260}>
            <Link to="http://www.railcargo.at">
              <Company {...RailCargoAustriaLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={120}>
            <Link to="https://www.seven2one.de">
              <Company {...Seven2OneLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={150}>
            <Link to="https://www.solytic.com">
              <Company {...SolyticLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={130}>
            <Link to="https://sonika.se">
              <Company {...SonikaLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={200}>
            <Link to="https://www.spectrummedical.com/">
              <Company {...SpectrumMedicalLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={120}>
            <Link to="https://www.speedwaymotors.com">
              <Company {...SpeedwayMotorsLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={180}>
            <Link to="https://splashback.io">
              <Company {...SplashbackLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={120}>
            <Link to="https://sweetgeeks.dk">
              <Company {...SweetGeeksLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={160}>
            <Link to="https://www.sytadelle.fr">
              <Company {...SytadelleLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={180}>
            <Link to="http://trackman.com/">
              <Company {...TrackmanLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={180}>
            <Link to="https://travel-soft.com">
              <Company {...TravelSoftLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={140}>
            <Link to="https://careers.veepee.com/vptech/">
              <Company {...VptechLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={120}>
            <Link to="https://xm.com">
              <Company {...XMLogoSvg} />
            </Link>
          </GenericLogo>
          <GenericLogo width={120}>
            <Link to="https://www.ziosk.com">
              <Company {...ZioskLogoSvg} />
            </Link>
          </GenericLogo>
        </Ticker>
      </ContentContainer>
    </SectionRow>
  </Section>
);

const FADE = keyframes`
  0%, 33% {
    opacity: 0;
  }
  8%, 24% {
    opacity: 1;
  }
`;

const Carousel = styled.div`
  height: 140px;
  position: relative;
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  align-items: center;
  justify-content: center;

  &:hover {
    & > * {
      animation-play-state: paused;
    }
  }

  & > * {
    position: absolute;
    animation: ${FADE} 15s infinite;
    opacity: 0;
    z-index: 0;
  }

  & > *:nth-child(1) {
    animation-delay: 0;
  }

  & > *:nth-child(2) {
    animation-delay: 5s;
  }

  & > *:nth-child(3) {
    animation-delay: 10s;
  }
`;

const MicrosoftLogo = styled.div`
  & svg {
    min-height: 16px;
    min-width: 72px;
    height: 40px;
    margin: 40px;
  }
`;

const SwissLifeLogo = styled.div`
  & svg {
    height: 80px;
    margin: 20px;
  }
`;

const GalaxusLogo = styled.div`
  & svg {
    min-height: 30px;
    min-width: 150px;
    height: 90px;
    margin: 20px;
  }
`;

const TICKER = keyframes`
  0% {
    transform: translate3d(0, 0, 0);
  }
  100% {
    transform: translate3d(-50%, 0, 0);
  }
`;

const Ticker = styled.div`
  width: max-content;
  display: flex;
  flex-direction: row;
  flex-wrap: nowrap;
  align-items: center;
  justify-content: flex-start;
  animation: ${TICKER} 45s linear infinite;

  &:hover {
    animation-play-state: paused;
  }
`;

const GenericLogo = styled.div<{ width?: number }>`
  flex: 0 0 auto;
  margin: 30px;
  width: ${({ width = 160 }) => width}px;

  & svg {
    fill: ${THEME_COLORS.text};
    transition: fill 0.2s ease-in-out;

    &:hover {
      fill: ${THEME_COLORS.heading};
    }
  }
`;
