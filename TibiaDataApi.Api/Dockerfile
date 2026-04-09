FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN mkdir -p /data/assets && chown -R $APP_UID /data
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TibiaDataApi.Api/TibiaDataApi.Api.csproj", "TibiaDataApi.Api/"]
COPY ["TibiaDataApi.Services/TibiaDataApi.Services.csproj", "TibiaDataApi.Services/"]
COPY ["TibiaDataApi.Contracts/TibiaDataApi.Contracts.csproj", "TibiaDataApi.Contracts/"]
RUN dotnet restore "TibiaDataApi.Api/TibiaDataApi.Api.csproj"
COPY . .
WORKDIR "/src/TibiaDataApi.Api"
RUN dotnet build "./TibiaDataApi.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TibiaDataApi.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TibiaDataApi.Api.dll"]
