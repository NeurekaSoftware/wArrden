FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG APP_VERSION
WORKDIR /app
COPY wArrden/wArrden.csproj .
RUN dotnet restore -a $TARGETARCH
COPY wArrden/ .
RUN dotnet publish -c Release -a $TARGETARCH -o /app/bin

FROM mcr.microsoft.com/dotnet/runtime:10.0
ARG APP_VERSION
ENV APP_VERSION=$APP_VERSION
WORKDIR /app
COPY --from=build /app/bin /app/bin
VOLUME ["/app/data"]
ENTRYPOINT ["dotnet", "/app/bin/wArrden.dll"]
