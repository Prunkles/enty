FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /enty
COPY ["lib/Khonsu.Coding/Khonsu.Coding.fsproj", "lib/Khonsu.Coding/"]
COPY ["lib/Khonsu.Coding.Json/Khonsu.Coding.Json.fsproj", "lib/Khonsu.Coding.Json/"]
COPY ["lib/Khonsu.Coding.Json.Net/Khonsu.Coding.Json.Net.fsproj", "lib/Khonsu.Coding.Json.Net/"]
COPY ["src/enty.Core/enty.Core.fsproj", "src/enty.Core/"]
COPY ["src/enty.Utils/enty.Utils.fsproj", "src/enty.Utils/"]
COPY ["src/enty.Mind.Grpc.Client/enty.Mind.Grpc.Client.csproj", "src/enty.Mind.Grpc.Client/"]
COPY ["src/enty.Mind.Client/enty.Mind.Client.fsproj", "src/enty.Mind.Client/"]
COPY ["src/enty.Mind/enty.Mind.fsproj", "src/enty.Mind/"]
COPY ["src/enty.Mind.Gateway/enty.Mind.Gateway.fsproj", "src/enty.Mind.Gateway/"]
COPY ["src/enty.Mind.Gateway.Server/enty.Mind.Gateway.Server.fsproj", "src/enty.Mind.Gateway.Server/"]
RUN dotnet restore "src/enty.Mind.Gateway.Server/enty.Mind.Gateway.Server.fsproj"
COPY . .
WORKDIR "/enty/src/enty.Mind.Gateway.Server"
RUN dotnet build "enty.Mind.Gateway.Server.fsproj" --no-restore -c Release

FROM build AS publish
RUN dotnet publish "enty.Mind.Gateway.Server.fsproj" --no-build -c Release -o /enty/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /enty/enty.Mind.Gateway.Server
COPY --from=publish /enty/publish .
ENTRYPOINT ["dotnet", "enty.Mind.Gateway.Server.dll"]
