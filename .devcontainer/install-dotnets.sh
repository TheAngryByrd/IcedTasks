#!/bin/bash

# downloads installer script https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script
curl -SL --output dotnet-install.sh https://dot.net/v1/dotnet-install.sh


# Add additional versions if required
DOTNET_VERSIONS=(
    '6.0'
    '7.0'
    # 'LTS'
    # 'STS'
    #'5.0.100'
)
for version in ${DOTNET_VERSIONS[@]}; do
   echo "installing dotnet $version"
   sudo chown 777 ./dotnet-install.sh
   /bin/bash ./dotnet-install.sh --verbose --channel $version
done


# install via global.json
# FILE=global.json
# if test -f "$FILE"; then
#     echo "installing dotnet via $FILE"
#     /bin/bash dotnet-install.sh --verbose --jsonfile $FILE
# fi
