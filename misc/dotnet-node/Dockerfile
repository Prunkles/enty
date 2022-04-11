FROM mcr.microsoft.com/dotnet/sdk:6.0

# Install NodeJS 16
RUN \
    apt-get update && apt-get upgrade -y \
    && curl -sL https://deb.nodesource.com/setup_16.x | bash - \
    && apt-get -y install nodejs
