FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PokenaeTemplate.Web/PokenaeTemplate.Web.csproj", "PokenaeTemplate.Web/"]
RUN dotnet restore "./PokenaeTemplate.Web/PokenaeTemplate.Web.csproj"
COPY . .
WORKDIR "/src/PokenaeTemplate.Web"
RUN dotnet build "./PokenaeTemplate.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PokenaeTemplate.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PokenaeTemplate.Web.dll"]
