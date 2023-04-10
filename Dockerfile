# Set the base image for your container
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

# Install Git
RUN apt-get update && \
    apt-get install -y git && \
    apt-get clean

# Build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Api/Api.csproj", "Api/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["src/Application/Application.csproj", "Application/"]
RUN dotnet restore "Api/Api.csproj"

COPY src . 
WORKDIR "/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release -o /app/publish

# Copy the published app to the base image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [ "./Tarik.Api" ]
