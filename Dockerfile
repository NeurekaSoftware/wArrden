FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG APP_VERSION
ARG PUID
ARG PGID
WORKDIR /app
COPY wArrden/wArrden.csproj .
RUN dotnet restore -a $TARGETARCH
COPY wArrden/ .
RUN dotnet publish -c Release -a $TARGETARCH -o /app/bin

FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime:10.0
ARG APP_VERSION
ARG PUID
ARG PGID
ENV APP_VERSION=$APP_VERSION
ENV PUID=$PUID
ENV PGID=$PGID
RUN apt-get update \
 && apt-get install -y --no-install-recommends gosu \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/bin /app/bin
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
VOLUME ["/app/data"]
ENTRYPOINT ["/entrypoint.sh"]
CMD ["dotnet", "/app/bin/wArrden.dll"]
