FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PokemonDamageCalculatorForStory.Web/PokemonDamageCalculatorForStory.Web.csproj", "PokemonDamageCalculatorForStory.Web/"]
RUN dotnet restore "./PokemonDamageCalculatorForStory.Web/PokemonDamageCalculatorForStory.Web.csproj"
COPY . .
WORKDIR "/src/PokemonDamageCalculatorForStory.Web"
RUN dotnet build "./PokemonDamageCalculatorForStory.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PokemonDamageCalculatorForStory.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PokemonDamageCalculatorForStory.Web.dll"]
