FROM chillicream/dotnet-build:2.0 AS Build

COPY ./build.sh ./build.cake ./NuGet.config ./tools/ ./build/

RUN ./build/build.sh -t Clean
