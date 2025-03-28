# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy the csproj file and restore dependencies
COPY fixit.csproj ./
RUN /usr/bin/dotnet restore fixit.csproj

# Copy the project files and build the release
COPY . ./
RUN /usr/bin/dotnet publish fixit.csproj -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "fixit.dll"]