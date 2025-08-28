# Étape 1 : Image de base pour exécuter l'application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# HTTP
EXPOSE 8080
# HTTPS
EXPOSE 8081

# Étape 2 : Image pour construire le projet
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copie uniquement le fichier .csproj pour restaurer les dépendances
COPY ["Gateway/Gateway.csproj", "./"]
RUN dotnet restore "./Gateway.csproj"

# Copie le reste des fichiers
COPY . .
WORKDIR "/src/Gateway"
RUN dotnet build "./Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Étape 3 : Publier le projet
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Étape 4 : Image finale avec l'application publiée
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.dll"]