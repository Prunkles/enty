FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /enty
COPY ["src/pdewebq.Extensions.Serilog/pdewebq.Extensions.Serilog.fsproj", "src/pdewebq.Extensions.Serilog/"]
COPY ["src/enty.Utils/enty.Utils.fsproj", "src/enty.Utils/"]
COPY ["src/enty.Core/enty.Core.fsproj", "src/enty.Core/"]
COPY ["src/enty.ResourceStorage.Server/enty.ResourceStorage.Server.fsproj", "src/enty.ResourceStorage.Server/"]
RUN dotnet restore "src/enty.ResourceStorage.Server/enty.ResourceStorage.Server.fsproj"

COPY . .
WORKDIR "/enty/src/enty.ResourceStorage.Server"
RUN dotnet build "enty.ResourceStorage.Server.fsproj" --no-restore -c Release

FROM build AS publish
RUN dotnet publish "enty.ResourceStorage.Server.fsproj" --no-build -c Release -o /enty/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /enty/enty.ResourceStorage.Server
COPY --from=publish /enty/publish .
ENTRYPOINT ["dotnet", "enty.ResourceStorage.Server.dll"]
