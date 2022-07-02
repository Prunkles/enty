FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /enty
COPY ["lib/Khonsu.Coding/Khonsu.Coding.fsproj", "lib/Khonsu.Coding/"]
COPY ["lib/Khonsu.Coding.Json/Khonsu.Coding.Json.fsproj", "lib/Khonsu.Coding.Json/"]
COPY ["lib/Khonsu.Coding.Json.Net/Khonsu.Coding.Json.Net.fsproj", "lib/Khonsu.Coding.Json.Net/"]
COPY ["src/pdewebq.Extensions.Serilog/pdewebq.Extensions.Serilog.fsproj", "src/pdewebq.Extensions.Serilog/"]
COPY ["src/enty.Utils/enty.Utils.fsproj", "src/enty.Utils/"]
COPY ["src/enty.Core/enty.Core.fsproj", "src/enty.Core/"]
COPY ["src/enty.Mind/enty.Mind.fsproj", "src/enty.Mind/"]
COPY ["src/enty.Mind.Parsing/enty.Mind.Parsing.fsproj", "src/enty.Mind.Parsing/"]
COPY ["src/enty.Mind.Server/enty.Mind.Server.fsproj", "src/enty.Mind.Server/"]
RUN dotnet restore "src/enty.Mind.Server/enty.Mind.Server.fsproj"
COPY . .
WORKDIR "/enty/src/enty.Mind.Server"
RUN dotnet build "enty.Mind.Server.fsproj" --no-restore -c Release

FROM build AS publish
RUN dotnet dev-certs https --clean
RUN dotnet dev-certs https #-ep /usr/local/share/ca-certificates/aspnet/https.crt --format PEM
RUN dotnet dev-certs https --trust
RUN dotnet publish "enty.Mind.Server.fsproj" --no-build -c Release -o /enty/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /enty/enty.Mind.Server
COPY --from=publish /enty/publish .
#COPY --from=publish "/usr/local/share/ca-certificates/aspnet/https.crt" "/usr/local/share/ca-certificates/aspnet/https.crt"
COPY --from=publish /root/.dotnet/corefx/cryptography/x509stores/my/* /root/.dotnet/corefx/cryptography/x509stores/my/
RUN update-ca-certificates
ENTRYPOINT ["dotnet", "enty.Mind.Server.dll"]
