FROM mcr.microsoft.com/dotnet/sdk:10.0
RUN apt-get update && apt-get install -y --no-install-recommends libfreetype6 libfontconfig1 fonts-dejavu-core && rm -rf /var/lib/apt/lists/*
WORKDIR /source
COPY . .
RUN dotnet restore Firmeza.sln && dotnet build Firmeza.sln -c Release --no-restore
CMD ["dotnet", "test", "tests/Firmeza.Tests/Firmeza.Tests.csproj", "-c", "Release", "--no-build", "--no-restore", "--logger", "console;verbosity=normal"]
