FROM mcr.microsoft.com/playwright:v1.62.1-noble@sha256:dcc5531e97840b9b5e794f2814476b21571c5124a3fca2267d73041f56e7580e

# The official image's npm 11.16.0 is below website's >=11.18.0 contract.
RUN npm install --global npm@11.21.1 --no-audit --no-fund
