FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

COPY . .

RUN dotnet restore FirmezaApi/FirmezaApi.csproj

EXPOSE 8080

CMD ["dotnet", "run", "--project", "FirmezaApi/FirmezaApi.csproj", "--urls=http://0.0.0.0:8080"]