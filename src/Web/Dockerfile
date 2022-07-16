FROM prunkles/dotnet-node:dotnet-sdk-6.0_node-16 AS build

# Install tools
WORKDIR "/enty/src/Web"
COPY [".config/dotnet-tools.json", "../../.config/dotnet-tools.json"]
COPY ["src/Web/.config/dotnet-tools.json", ".config/dotnet-tools.json"]
RUN dotnet tool restore

# Install node dependencies
WORKDIR "/enty/src/Web"
COPY ["src/Web/package.json", "package.json"]
COPY ["src/Web/package-lock.json", "package-lock.json"]
RUN npm install

# Restore dotnet dependencies
WORKDIR "/enty"
COPY ["src/enty.Utils/enty.Utils.fsproj", "src/enty.Utils/"]
COPY ["src/enty.Core/enty.Core.fsproj", "src/enty.Core/"]
COPY ["lib/Khonsu.Coding/Khonsu.Coding.fsproj", "lib/Khonsu.Coding/"]
COPY ["lib/Khonsu.Coding.Json/Khonsu.Coding.Json.fsproj", "lib/Khonsu.Coding.Json/"]
COPY ["lib/Khonsu.Coding.Json.Fable/Khonsu.Coding.Json.Fable.fsproj", "lib/Khonsu.Coding.Json.Fable/"]
COPY ["src/Web/src/enty.Mind.Client.Fable/enty.Mind.Client.Fable.fsproj", "src/Web/src/enty.Mind.Client.Fable/"]
COPY ["src/Web/src/enty.Web.Libs/enty.Web.Libs.fsproj", "src/Web/src/enty.Web.Libs/"]
COPY ["src/Web/src/enty.Web.App/enty.Web.App.fsproj", "src/Web/src/enty.Web.App/"]
RUN dotnet restore "src/Web/src/enty.Web.App/enty.Web.App.fsproj"

# Build
COPY . .
WORKDIR "/enty/src/Web"
RUN npm run build
RUN NODE_ENV=production npm run webpack

FROM nginx AS final
COPY --from=build "/enty/src/Web/dist" "/usr/share/nginx/html"
COPY "src/Web/enty-webapp-server/nginx.conf.template" "/etc/nginx/templates/default.conf.template"
