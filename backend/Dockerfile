# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY Phonebook.csproj .
RUN dotnet restore Phonebook.csproj

COPY . .
RUN dotnet publish Phonebook.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=build /app/publish .

# The API listens on port 8000 inside the Docker network only;
# Nginx is the public entry point. Command-line --urls overrides the
# localhost URL in appsettings.json (which is for host-side runs).
EXPOSE 8000
ENTRYPOINT ["dotnet", "Phonebook.dll", "--urls", "http://+:8000"]
