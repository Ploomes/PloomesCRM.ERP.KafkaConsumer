FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-stage
WORKDIR /src

ARG GH_NUGET_USER
ARG GH_NUGET_TOKEN
COPY . .

RUN dotnet build
RUN dotnet publish -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 as serve-stage
WORKDIR /app
COPY --from=build-stage /publish/ .
ARG BUILD_TIMESTAMP
ARG GIT_COMMIT
ENV ASPNETCORE_URLS "http://+:80"
ENV BUILD_TIMESTAMP=$BUILD_TIMESTAMP
ENV GIT_COMMIT=$GIT_COMMIT
ENV TZ=America/Sao_Paulo

#begin-links:serve
RUN ln -s /secrets/appsettings.json /app/appsettings.json
#end-links:serve

ENTRYPOINT dotnet "PloomesCRM.ERP.KafkaConsumer.dll"
