# FROM debian:buster-slim
FROM mcr.microsoft.com/devcontainers/base:debian
COPY --from=mcr.microsoft.com/dotnet/sdk:6.0 /usr/share/dotnet /usr/share/dotnet
COPY --from=mcr.microsoft.com/dotnet/sdk:7.0 /usr/share/dotnet /usr/share/dotnet

RUN ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

USER vscode

ENV \
    # Enable detection of running in a container
    DOTNET_RUNNING_IN_CONTAINER=true \
    # DOTNET_INSTALL_DIR=/usr/share/dotnet/ \
    DOTNET_ROOT=/usr/share/dotnet/ \
    # DOTNET_ROOT=/home/vscode/.dotnet \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=false

# Change the default shell to zsh
SHELL ["/bin/zsh", "-c"]

# Install starship
RUN curl -sS https://starship.rs/install.sh | sh -s -- -y
RUN echo 'eval "$(starship init zsh)"' >> ~/.zshrc
RUN echo "export PATH=$PATH:$DOTNET_ROOT" >> ~/.zshrc
RUN echo "export PATH=$PATH:$DOTNET_ROOT" >> ~/.bashrc
RUN source ~/.zshrc
WORKDIR /home/vscode/

CMD [ "/bin/zsh" ]
