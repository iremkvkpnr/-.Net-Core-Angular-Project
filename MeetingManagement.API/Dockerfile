# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY MeetingManagement.sln .

# Copy project files
COPY MeetingManagement.API/MeetingManagement.API.csproj MeetingManagement.API/
COPY MeetingManagement.Business/MeetingManagement.Business.csproj MeetingManagement.Business/
COPY MeetingManagement.Data/MeetingManagement.Data.csproj MeetingManagement.Data/
COPY MeetingManagement.Models/MeetingManagement.Models.csproj MeetingManagement.Models/

# Restore dependencies
RUN dotnet restore MeetingManagement.API/MeetingManagement.API.csproj

# Copy source code
COPY . .

# Build the application
WORKDIR /src/MeetingManagement.API
RUN dotnet build -c Release -o /app/build

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create uploads directory
RUN mkdir -p uploads/documents uploads/profiles

# Copy published application
COPY --from=build /app/publish .

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "MeetingManagement.API.dll"]