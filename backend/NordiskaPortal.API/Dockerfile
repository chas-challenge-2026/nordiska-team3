# Stage 1: build frontend
FROM node:20-alpine AS frontend-build
WORKDIR /frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm install
COPY frontend/ .
RUN npm run build

# Stage 2: build backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/NordiskaPortal.API/NordiskaPortal.API.csproj .
RUN dotnet restore
COPY backend/NordiskaPortal.API/ .
RUN dotnet publish -c Release -o /app/publish

# Stage 3: runtime - backend + pre-built frontend in the same image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=frontend-build /frontend/dist ./wwwroot

EXPOSE 5077
ENV ASPNETCORE_URLS=http://+:5077
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "NordiskaPortal.API.dll"]