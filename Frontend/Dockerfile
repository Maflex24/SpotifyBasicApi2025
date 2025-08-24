# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SpotifyBasicApi2025.sln ./
COPY Frontend/Frontend.csproj Frontend/
RUN dotnet restore Frontend/Frontend.csproj

COPY Frontend/. ./Frontend/
WORKDIR /src/Frontend
RUN dotnet publish -c Release -o /app/publish

# Serve via ASP.NET static files
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8888
EXPOSE 8888
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Frontend.dll"]


