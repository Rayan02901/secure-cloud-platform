FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY bin/Release/net8.0/publish/ .
EXPOSE 8000
COPY appsettings.Docker.json /app/
ENTRYPOINT ["dotnet", "api-gateway-dotnet.dll"]
