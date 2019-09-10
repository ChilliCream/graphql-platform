FROM mcr.microsoft.com/dotnet/core/sdk:3.0.100-preview9-bionic AS Build

RUN wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb \
  && dpkg -i packages-microsoft-prod.deb \
  && apt-get update \
  && apt-get install dotnet-sdk-2.1 -y

COPY ./ /hc
WORKDIR /hc
RUN export PATH="$PATH:/root/.dotnet/tools" \
  && dotnet tool install Cake.Tool -g --version 0.34.1

