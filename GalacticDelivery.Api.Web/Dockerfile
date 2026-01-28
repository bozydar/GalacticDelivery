FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GalacticDelivery.Api.Web/GalacticDelivery.Api.Web.csproj", "GalacticDelivery.Api.Web/"]
RUN dotnet restore "GalacticDelivery.Api.Web/GalacticDelivery.Api.Web.csproj"
COPY . .
WORKDIR "/src/GalacticDelivery.Api.Web"
RUN dotnet build "./GalacticDelivery.Api.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GalacticDelivery.Api.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GalacticDelivery.Api.Web.dll"]
