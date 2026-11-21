FROM mcr.microsoft.com/playwright:v1.65.0-noble@sha256:9beb546099e11f52d62e505537914870e01a67e0e6b00ae41da4775324e73dbe

# The official image's npm 11.16.0 is below website's >=11.18.0 contract.
RUN npm install --global npm@11.21.1 --no-audit --no-fund
