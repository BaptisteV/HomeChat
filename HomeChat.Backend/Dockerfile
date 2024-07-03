FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
RUN apt update
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
# RUN dotnet publish -c Release -o out --runtime win-x64
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0-nanoserver-ltsc2022
WORKDIR /App
COPY --from=build-env /App/out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HomeChat.Backend.dll"]
