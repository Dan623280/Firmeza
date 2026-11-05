FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props global.json ./
COPY src/Firmeza.Domain/*.csproj src/Firmeza.Domain/
COPY src/Firmeza.Application/*.csproj src/Firmeza.Application/
COPY src/Firmeza.Infrastructure/*.csproj src/Firmeza.Infrastructure/
COPY src/Firmeza.API/*.csproj src/Firmeza.API/
RUN dotnet restore src/Firmeza.API/Firmeza.API.csproj
COPY src/ src/
RUN dotnet publish src/Firmeza.API/Firmeza.API.csproj -c Release -o /out --no-restore /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl libfreetype6 libfontconfig1 fonts-dejavu-core && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p wwwroot/recibos && chown -R app:app /app
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=10 CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Firmeza.API.dll"]
