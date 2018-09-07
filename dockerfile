FROM chillicream/dotnet-build:2.0 AS Build

COPY ./ /build/

WORKDIR /build/

RUN ./build.sh -t Clean
