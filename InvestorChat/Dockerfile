FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["InvestorChat.Domain/InvestorChat.Domain.csproj", "InvestorChat.Domain/"]
COPY ["InvestorChat.Application/InvestorChat.Application.csproj", "InvestorChat.Application/"]
COPY ["InvestorChat.Infrastructure/InvestorChat.Infrastructure.csproj", "InvestorChat.Infrastructure/"]
COPY ["InvestorChat.Client/InvestorChat.Client.csproj", "InvestorChat.Client/"]
COPY ["InvestorChat.Server/InvestorChat.Server.csproj", "InvestorChat.Server/"]
RUN dotnet restore "InvestorChat.Server/InvestorChat.Server.csproj"

COPY . .
RUN dotnet publish "InvestorChat.Server/InvestorChat.Server.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InvestorChat.Server.dll"]
