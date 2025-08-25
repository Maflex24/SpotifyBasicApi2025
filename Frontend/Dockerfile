# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SpotifyBasicApi2025.sln ./
COPY Frontend/Frontend.csproj Frontend/
RUN dotnet restore Frontend/Frontend.csproj

COPY Frontend/. ./Frontend/
WORKDIR /src/Frontend
RUN dotnet publish -c Release -o /app/publish

# Final stage: serve static Blazor WASM via nginx
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
EXPOSE 80
# Copy published wwwroot to nginx html
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
# Default nginx CMD/ENTRYPOINT are fine


