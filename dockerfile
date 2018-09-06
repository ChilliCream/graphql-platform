FROM chillicream/dotnet-build:1.0 AS builder

COPY ./ ./work

ENV Version 0.5.0-build
ENV sonarLogin eab9e4c6dc7d68aca12a4784520831344e7d2ed7

WORKDIR ./work

RUN ./build.sh -t release
