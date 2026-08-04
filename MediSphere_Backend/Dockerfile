# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy the entire backend source code
COPY . .

# Restore dependencies
RUN dotnet restore MediSphere.API/MediSphere.API.csproj

# Publish the application
RUN dotnet publish MediSphere.API/MediSphere.API.csproj \
    -c Release \
    -o /app/publish

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copy published files from the build stage
COPY --from=build /app/publish .

# Configure ASP.NET Core to listen on port 10000
ENV ASPNETCORE_URLS=http://+:10000

# Expose the port
EXPOSE 10000

# Start the application
ENTRYPOINT ["dotnet", "MediSphere.API.dll"]