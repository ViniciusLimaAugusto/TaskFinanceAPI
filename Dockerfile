FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY .config/dotnet-tools.json .config/dotnet-tools.json
COPY TaskFinanceAPI/TaskFinanceAPI.csproj TaskFinanceAPI/
RUN dotnet restore TaskFinanceAPI/TaskFinanceAPI.csproj && dotnet tool restore
COPY TaskFinanceAPI/ TaskFinanceAPI/
RUN dotnet publish TaskFinanceAPI/TaskFinanceAPI.csproj -c Release --no-restore -o /out/api \
    && dotnet ef migrations bundle --project TaskFinanceAPI/TaskFinanceAPI.csproj \
        --configuration Release --no-build --output /out/migrations/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/api ./
COPY --from=build /out/migrations/efbundle ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskFinanceAPI.dll"]
