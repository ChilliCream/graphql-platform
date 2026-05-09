"use client";

import React from "react";

const OSS_PRODUCTS = [
  "Hot Chocolate",
  "Mocha",
  "Strawberry Shake",
  "Fusion (OSS)",
];

// The "open source belt": rendered inside an inverted Band, so this strip
// drops its own card chrome and lays out as content-on-band.
export const OssStrip: React.FC = () => {
  return (
    <div className="cc-oss-strip">
      <div className="cc-section-label">
        <span className="num">02</span> Open source
      </div>
      <div className="cc-oss-inner">
        <div className="cc-oss-copy">
          <div className="cc-oss-chips">
            <span className="cc-oss-chip is-tag">Free forever</span>
            {OSS_PRODUCTS.map((label) => (
              <span key={label} className="cc-oss-chip">
                {label}
              </span>
            ))}
          </div>
          <p className="cc-oss-line">
            <strong>MIT-licensed.</strong> No account needed. No upsell. Build,
            ship, and scale a production GraphQL platform on the OSS stack
            alone.
          </p>
        </div>
        <div
          className="cc-oss-terminal"
          aria-label="Install Hot Chocolate from NuGet"
        >
          <span className="prompt">$</span>
          <span>
            <span className="cmd">dotnet add package </span>
            <span className="pkg">HotChocolate</span>
          </span>
        </div>
      </div>
    </div>
  );
};
