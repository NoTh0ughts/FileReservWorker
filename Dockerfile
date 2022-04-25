FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app
COPY ["SingularisTestTask/appsettings.json", "./"]
VOLUME app/tmp
COPY ["SingularisTestTask/tmp/source", "tmp/source"]


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SingularisTestTask/SingularisTestTask.csproj", "SingularisTestTask/"]
RUN dotnet restore "SingularisTestTask/SingularisTestTask.csproj"
COPY . .
WORKDIR "/src/SingularisTestTask"
RUN dotnet build "SingularisTestTask.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SingularisTestTask.csproj" -c Release -o /app/publish


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SingularisTestTask.dll"]
