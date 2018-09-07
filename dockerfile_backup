FROM ubuntu:18.10 AS Base

ENV MONO_VERSION 5.4.1.6

RUN apt-get update \
  && apt-get install gnupg2 -y

RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF

RUN echo "deb http://download.mono-project.com/repo/debian stretch/snapshots/$MONO_VERSION main" > /etc/apt/sources.list.d/mono-official.list \
  && apt-get update \
  && apt-get install -y mono-runtime \
  && rm -rf /var/lib/apt/lists/* /tmp/*

RUN apt-get update \
  && apt-get install -y binutils curl mono-devel ca-certificates-mono fsharp mono-vbnc nuget referenceassemblies-pcl \
  && rm -rf /var/lib/apt/lists/* /tmp/*

RUN apt-get install apt-transport-https \
  && apt-get update \
  && apt-get install wget -y

RUN wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb \
  && dpkg -i packages-microsoft-prod.deb

RUN apt-get install apt-transport-https \
  && apt-get update \
  && apt-get install dotnet-sdk-2.1 -y

RUN apt-get update \
  && apt-get install default-jdk -y \
  && apt-get install git -y

FROM Base AS Builder

COPY ./build.sh ./build.cake ./NuGet.config ./tools/ ./build/

RUN ./build/build.sh -t Clean
