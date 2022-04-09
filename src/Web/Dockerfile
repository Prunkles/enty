FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Install NodeJS
RUN \
    apt-get update && apt-get upgrade \
    && curl -sL https://deb.nodesource.com/setup_16.x | bash - \
    && apt-get -y install nodejs

#RUN npm i

CMD npm start
