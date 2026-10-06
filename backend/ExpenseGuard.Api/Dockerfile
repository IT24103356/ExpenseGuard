FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN if [ -f ExpenseGuard.Api.csproj ]; then \
      PROJECT=ExpenseGuard.Api.csproj; \
    elif [ -f backend/ExpenseGuard.Api/ExpenseGuard.Api.csproj ]; then \
      PROJECT=backend/ExpenseGuard.Api/ExpenseGuard.Api.csproj; \
    else \
      echo "ExpenseGuard.Api.csproj not found in the Docker build context" && find . -name '*.csproj' && exit 1; \
    fi && \
    dotnet publish "$PROJECT" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExpenseGuard.Api.dll"]
