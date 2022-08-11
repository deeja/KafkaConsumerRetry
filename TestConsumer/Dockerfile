FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TestConsumer/TestConsumer.csproj", "TestConsumer/"]
RUN dotnet restore "TestConsumer/TestConsumer.csproj"
COPY . .
WORKDIR "/src/TestConsumer"
RUN dotnet build "TestConsumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestConsumer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestConsumer.dll"]
