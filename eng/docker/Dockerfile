FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
ARG PUBLISH_FOLDER=app/
WORKDIR /app
COPY ${PUBLISH_FOLDER} .

ENTRYPOINT ["dotnet", "/app/RinhaBackend2024Q1.Api.dll"]
