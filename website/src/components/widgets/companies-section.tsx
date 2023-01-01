import React, { FC } from "react";
import styled from "styled-components";

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
import GiaLogoSvg from "@/images/companies/gia.svg";
import HiloLogoSvg from "@/images/companies/hilo.svg";
import IncloudLogoSvg from "@/images/companies/incloud.svg";
import InfoslipsLogoSvg from "@/images/companies/infoslips.svg";
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
        <Logos>
          <Logo width={140}>
            <Link to="https://additiv.com">
              <Company {...AdditivLogoSvg} />
            </Link>
          </Logo>
          <Logo width={160}>
            <Link to="https://aeieng.com">
              <Company {...AeiLogoSvg} />
            </Link>
          </Logo>
          <Logo width={100}>
            <Link to="https://atmina.de">
              <Company {...AtminaLogoSvg} />
            </Link>
          </Logo>
          <Logo width={180}>
            <Link to="https://www.autoguru.com.au">
              <Company {...AutoguruLogoSvg} />
            </Link>
          </Logo>
          <Logo width={150}>
            <Link to="https://bdna.com.au">
              <Company {...BdnaLogoSvg} />
            </Link>
          </Logo>
          <Logo width={150}>
            <Link to="https://www.beyable.com">
              <Company {...BeyableLogoSvg} />
            </Link>
          </Logo>
          <Logo width={100}>
            <Link to="https://www.biqh.com">
              <Company {...BiqhLogoSvg} />
            </Link>
          </Logo>
          <Logo width={180}>
            <Link to="https://carmmunity.io">
              <Company {...CarmmunityLogoSvg} />
            </Link>
          </Logo>
          <Logo width={180}>
            <Link to="https://www.compass.education">
              <Company {...CompassLogoSvg} />
            </Link>
          </Logo>
          <Logo width={90}>
            <Link to="https://www.e2m.energy">
              <Company {...E2mLogoSvg} />
            </Link>
          </Logo>
          <Logo width={130}>
            <Link to="https://www.exlrt.com">
              <Company {...ExlrtLogoSvg} />
            </Link>
          </Logo>
          <Logo width={100}>
            <Link to="https://www.ezeep.com">
              <Company {...EzeepLogoSvg} />
            </Link>
          </Logo>
          <Logo width={150}>
            <Link to="https://fulcrumpro.com/">
              <Company {...FulcrumLogoSvg} />
            </Link>
          </Logo>
          <Logo width={120}>
            <Link to="https://gia.ch">
              <Company {...GiaLogoSvg} />
            </Link>
          </Logo>
          <Logo width={70}>
            <Link to="https://www.hiloenergie.com">
              <Company {...HiloLogoSvg} />
            </Link>
          </Logo>
          <Logo width={200}>
            <Link to="https://www.incloud.de">
              <Company {...IncloudLogoSvg} />
            </Link>
          </Logo>
          <Logo width={130}>
            <Link to="https://www.infoslips.com">
              <Company {...InfoslipsLogoSvg} />
            </Link>
          </Logo>
          <Logo width={160}>
            <Link to="https://motitech.co.uk">
              <Company {...MotiviewLogoSvg} />
            </Link>
          </Logo>
          <Logo width={160}>
            <Link to="https://orderin.co.za">
              <Company {...OrderinLogoSvg} />
            </Link>
          </Logo>
          <Logo width={110}>
            <Link to="https://poweredsoft.com">
              <Company {...PoweredSoftLogoSvg} />
            </Link>
          </Logo>
          <Logo width={180}>
            <Link to="https://pushpay.com">
              <Company {...PushpayLogoSvg} />
            </Link>
          </Logo>
          <Logo width={260}>
            <Link to="http://www.railcargo.at">
              <Company {...RailCargoAustriaLogoSvg} />
            </Link>
          </Logo>
          <Logo width={120}>
            <Link to="https://www.seven2one.de">
              <Company {...Seven2OneLogoSvg} />
            </Link>
          </Logo>
          <Logo width={150}>
            <Link to="https://www.solytic.com">
              <Company {...SolyticLogoSvg} />
            </Link>
          </Logo>
          <Logo width={130}>
            <Link to="https://sonika.se">
              <Company {...SonikaLogoSvg} />
            </Link>
          </Logo>
          <Logo width={200}>
            <Link to="https://www.spectrummedical.com/">
              <Company {...SpectrumMedicalLogoSvg} />
            </Link>
          </Logo>
          <Logo width={120}>
            <Link to="https://www.speedwaymotors.com">
              <Company {...SpeedwayMotorsLogoSvg} />
            </Link>
          </Logo>
          <Logo width={180}>
            <Link to="https://splashback.io">
              <Company {...SplashbackLogoSvg} />
            </Link>
          </Logo>
          <Logo width={120}>
            <Link to="https://sweetgeeks.dk">
              <Company {...SweetGeeksLogoSvg} />
            </Link>
          </Logo>
          <Logo width={110}>
            <Link to="https://www.swisslife.ch">
              <Company {...SwissLifeLogoSvg} />
            </Link>
          </Logo>
          <Logo width={160}>
            <Link to="https://www.sytadelle.fr">
              <Company {...SytadelleLogoSvg} />
            </Link>
          </Logo>
          <Logo width={180}>
            <Link to="http://trackman.com/">
              <Company {...TrackmanLogoSvg} />
            </Link>
          </Logo>
          <Logo width={180}>
            <Link to="https://travel-soft.com">
              <Company {...TravelSoftLogoSvg} />
            </Link>
          </Logo>
          <Logo width={140}>
            <Link to="https://careers.veepee.com/vptech/">
              <Company {...VptechLogoSvg} />
            </Link>
          </Logo>
          <Logo width={120}>
            <Link to="https://xm.com">
              <Company {...XMLogoSvg} />
            </Link>
          </Logo>
          <Logo width={120}>
            <Link to="https://www.ziosk.com">
              <Company {...ZioskLogoSvg} />
            </Link>
          </Logo>
        </Logos>
      </ContentContainer>
    </SectionRow>
  </Section>
);

const Logos = styled.div`
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  align-items: center;
  justify-content: center;
`;

const Logo = styled.div<{ width?: number }>`
  flex: 0 0 auto;
  margin: 30px;
  width: ${({ width = 160 }) => width}px;

  > a > svg {
    fill: ${THEME_COLORS.text};
    transition: fill 0.2s ease-in-out;

    &:hover {
      fill: ${THEME_COLORS.heading};
    }
  }
`;
