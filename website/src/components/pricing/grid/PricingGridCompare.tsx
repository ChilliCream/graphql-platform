"use client";

import React from "react";
import styled from "styled-components";

import {
  GridCard,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import {
  Cell,
  COMPARISON_COLUMNS,
  COMPARISON_MATRIX,
  ComparisonColumnKey,
} from "@/data/pricing/comparisonMatrix";

const CheckIcon: React.FC = () => (
  <svg viewBox="0 0 16 16" width="16" height="16" aria-hidden>
    <path
      d="M3 8.5 L6.5 12 L13 4.5"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </svg>
);

const ComparisonCell: React.FC<{ cell: Cell; isAccent: boolean }> = ({
  cell,
  isAccent,
}) => {
  const className = "cc-grid-cell" + (isAccent ? " is-accent" : "");
  switch (cell.kind) {
    case "check":
      return (
        <Td className={className}>
          <CellCheck aria-label="Included">
            <CheckIcon />
          </CellCheck>
        </Td>
      );
    case "value":
      return (
        <Td className={className}>
          <CellValue>{cell.label}</CellValue>
        </Td>
      );
    case "meter":
      return (
        <Td className={className}>
          <CellMeter>
            <span className="included">{cell.included}</span>
            {cell.overage && cell.unit ? (
              <span className="overage">
                then {cell.overage} / {cell.unit}
              </span>
            ) : null}
          </CellMeter>
        </Td>
      );
    case "custom":
      return (
        <Td className={className}>
          <CellCustom aria-label="Custom — contact sales">Custom</CellCustom>
        </Td>
      );
    case "none":
    default:
      return (
        <Td className={className}>
          <CellNone aria-label="Not included">—</CellNone>
        </Td>
      );
  }
};

// Archetype F. The full feature x tier matrix, rendered as a real <table>
// inside a no-padding GridCard so the hairlines between rows and columns are
// load-bearing. No alternating row tints. The active accent surfaces only on
// the column heading underline so the page identity reads on the chrome.
export const PricingGridCompare: React.FC = () => {
  return (
    <GridSection variant="default" hairlineBottom>
      <SectionHeading>
        <Eyebrow>Compare plans</Eyebrow>
        <Title>Every meter, every cell.</Title>
        <Lede>
          Everything in Open Source is free forever, MIT-licensed, and runs
          without ChilliCream. Nitro tiers add hosted operations, schema
          governance, and 24x7 support on top.
        </Lede>
      </SectionHeading>

      <GridCard noPadding>
        <Scroll>
          <Table>
            <thead>
              <tr>
                <ColHead className="is-feature" scope="col">
                  <span className="label">Feature</span>
                </ColHead>
                {COMPARISON_COLUMNS.map((col) => (
                  <ColHead
                    key={col.key}
                    scope="col"
                    className={"is-tier" + (col.accent ? " is-accent" : "")}
                  >
                    <span className="label">{col.label}</span>
                    <span className="price">{col.priceLabel}</span>
                    <span className="sub">{col.subLabel}</span>
                  </ColHead>
                ))}
              </tr>
            </thead>
            <tbody>
              {COMPARISON_MATRIX.map((group) => (
                <React.Fragment key={group.title}>
                  <GroupRow>
                    <th
                      scope="colgroup"
                      colSpan={1 + COMPARISON_COLUMNS.length}
                    >
                      <GroupTitle>{group.title}</GroupTitle>
                      {group.summary ? (
                        <GroupSummary>{group.summary}</GroupSummary>
                      ) : null}
                    </th>
                  </GroupRow>
                  {group.rows.map((row) => (
                    <DataRow key={row.label}>
                      <RowLabel scope="row">
                        {row.label}
                        {row.hint ? <RowHint>{row.hint}</RowHint> : null}
                      </RowLabel>
                      {COMPARISON_COLUMNS.map((col) => (
                        <ComparisonCell
                          key={col.key}
                          cell={row.cells[col.key as ComparisonColumnKey]}
                          isAccent={!!col.accent}
                        />
                      ))}
                    </DataRow>
                  ))}
                </React.Fragment>
              ))}
            </tbody>
          </Table>
        </Scroll>
      </GridCard>

      <Foot>
        Hard limits, budget alerts, no surprise invoices on every Nitro tier.
        Pay-as-you-go is opt-in.
      </Foot>
    </GridSection>
  );
};

const SectionHeading = styled.div`
  text-align: center;
  max-width: 760px;
  margin: 0 auto 48px;
`;

const Eyebrow = styled.div`
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
  margin-bottom: 16px;
`;

const Title = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: ${GRID_TOKENS.h2Size};
  font-weight: 600;
  line-height: 1.05;
  letter-spacing: -0.03em;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0 0 14px;
`;

const Lede = styled.p`
  font-size: 16px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  max-width: 64ch;
  margin: 0 auto;
  text-wrap: pretty;
`;

const Scroll = styled.div`
  overflow-x: auto;
  overflow-y: visible;
`;

const Table = styled.table`
  width: 100%;
  min-width: 1080px;
  border-collapse: collapse;
  font-family: var(--cc-font-sans), sans-serif;
  color: ${GRID_TOKENS.inkPrimary};

  th,
  td {
    border-right: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
    border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  }

  th:last-child,
  td:last-child {
    border-right: 0;
  }

  tbody tr:last-child th,
  tbody tr:last-child td {
    border-bottom: 0;
  }
`;

const ColHead = styled.th`
  text-align: left;
  vertical-align: bottom;
  padding: 22px 20px 18px;
  background: ${GRID_TOKENS.bgCard};
  border-bottom: 1px solid
    var(--cc-grid-hairline-strong, ${GRID_TOKENS.hairlineStrong});

  &.is-feature {
    width: 280px;
    min-width: 280px;
  }

  &.is-tier {
    width: 18%;
    min-width: 160px;
  }

  &.is-tier.is-accent {
    border-bottom-color: var(--cc-accent, ${GRID_TOKENS.hairlineStrong});
  }

  .label {
    display: block;
    font-size: 14px;
    font-weight: 500;
    color: ${GRID_TOKENS.inkPrimary};
    margin-bottom: 6px;
  }

  .price {
    display: block;
    font-family: var(--cc-font-sans), sans-serif;
    font-size: 18px;
    font-weight: 600;
    color: ${GRID_TOKENS.inkPrimary};
    letter-spacing: -0.01em;
    margin-bottom: 4px;
  }

  .sub {
    display: block;
    font-family: var(--cc-font-mono), monospace;
    font-size: 10px;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: ${GRID_TOKENS.inkMuted};
    line-height: 1.4;
  }
`;

const GroupRow = styled.tr`
  th {
    text-align: left;
    padding: 28px 20px 14px;
    background: ${GRID_TOKENS.bgBase};
    border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
    border-right: 0;
  }
`;

const GroupTitle = styled.span`
  display: block;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  font-weight: 500;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  margin-bottom: 4px;
`;

const GroupSummary = styled.span`
  display: block;
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 13px;
  font-weight: 400;
  letter-spacing: 0;
  text-transform: none;
  color: ${GRID_TOKENS.inkBody};
  line-height: 1.5;
  max-width: 80ch;
`;

const DataRow = styled.tr``;

const RowLabel = styled.th`
  text-align: left;
  font-weight: 400;
  vertical-align: top;
  padding: 16px 20px;
  font-size: 14px;
  color: ${GRID_TOKENS.inkPrimary};
`;

const RowHint = styled.span`
  display: block;
  margin-top: 4px;
  font-size: 12px;
  color: ${GRID_TOKENS.inkMuted};
  line-height: 1.4;
`;

const Td = styled.td`
  vertical-align: top;
  padding: 16px 20px;
  font-size: 14px;
  color: ${GRID_TOKENS.inkPrimary};
`;

const CellCheck = styled.span`
  display: inline-flex;
  color: var(--cc-accent, ${GRID_TOKENS.success});
`;

const CellValue = styled.span`
  color: ${GRID_TOKENS.inkPrimary};
`;

const CellMeter = styled.span`
  display: flex;
  flex-direction: column;
  gap: 4px;

  .included {
    color: ${GRID_TOKENS.inkPrimary};
    font-weight: 500;
  }

  .overage {
    font-family: var(--cc-font-mono), monospace;
    font-size: 11px;
    letter-spacing: 0.04em;
    color: ${GRID_TOKENS.inkMuted};
  }
`;

const CellCustom = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const CellNone = styled.span`
  color: ${GRID_TOKENS.inkFaint};
  font-family: var(--cc-font-mono), monospace;
  font-size: 14px;
`;

const Foot = styled.p`
  margin: 28px auto 0;
  text-align: center;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;
