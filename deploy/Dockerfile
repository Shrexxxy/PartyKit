# Сборка из корня репозитория: docker build -f deploy/Dockerfile .

FROM node:24-alpine AS web
WORKDIR /src/web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
# Vite кладёт сборку в ../src/PartyKit.Api/wwwroot.
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY src/PartyKit.Api/PartyKit.Api.csproj src/PartyKit.Api/
RUN dotnet restore src/PartyKit.Api/PartyKit.Api.csproj
COPY src/ src/
COPY theme/ theme/
COPY --from=web /src/src/PartyKit.Api/wwwroot src/PartyKit.Api/wwwroot
RUN dotnet publish src/PartyKit.Api/PartyKit.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app .
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Storage__DataDir=/data
VOLUME /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "PartyKit.Api.dll"]
