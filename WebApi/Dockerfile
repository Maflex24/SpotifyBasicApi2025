# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SpotifyBasicApi2025.sln ./
COPY WebApi/SpotifyBasicApi2025.csproj WebApi/
RUN dotnet restore WebApi/SpotifyBasicApi2025.csproj

COPY WebApi/. ./WebApi/
WORKDIR /src/WebApi
RUN dotnet publish -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8889
EXPOSE 8889
COPY --from=build /app/publish .

# Przekazanie envów przez docker-compose
ENTRYPOINT ["dotnet", "SpotifyBasicApi2025.dll"]


