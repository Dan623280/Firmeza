FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore FirmezaApi.slnx
RUN dotnet publish FirmezaApi/FirmezaApi.csproj --configuration Release --no-restore --output /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
RUN mkdir -p /app/storage/receipts && chown -R app:app /app/storage
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FirmezaApi.dll"]
