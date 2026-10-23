# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore tách riêng để tận dụng cache layer
COPY Banova.slnx ./
COPY src/Banova.Api/Banova.Api.csproj src/Banova.Api/
RUN dotnet restore src/Banova.Api/Banova.Api.csproj

# Publish
COPY . .
RUN dotnet publish src/Banova.Api/Banova.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Banova.Api.dll"]
