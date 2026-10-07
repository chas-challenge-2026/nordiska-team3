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

# Stage 3: build the native PDF generator (same base image as the runtime, so the libraries match)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS native-build
RUN apt-get update \
        && apt-get install -y --no-install-recommends build-essential cmake libssl-dev libhpdf-dev nlohmann-json3-dev pkg-config \
    && rm -rf /var/lib/apt/lists/*
COPY native/ /native/
RUN cmake -S /native -B /native-out -DCMAKE_BUILD_TYPE=Release \
    && cmake --build /native-out --target pdf_generator --parallel

# Stage 4: runtime - backend + pre-built frontend + native PDF generator in the same image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# libharu runtime libraries for pdf_generator (the dev package also pulls in libpng and zlib)
RUN apt-get update \
    && apt-get install -y --no-install-recommends libhpdf-dev \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=frontend-build /frontend/dist ./wwwroot
COPY --from=native-build /native-out/pdf_generator/pdf_generator ./native/build/pdf_generator/pdf_generator
COPY --from=native-build /native-out/pdf_generator/assets ./native/build/pdf_generator/assets

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "NordiskaPortal.API.dll"]