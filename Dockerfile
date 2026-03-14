# Build — context = raiz do repositório do serviço.
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY src/Fcg.Notification.Contracts/*.csproj src/Fcg.Notification.Contracts/
COPY src/Fcg.Notification.Domain/*.csproj src/Fcg.Notification.Domain/
COPY src/Fcg.Notification.Application/*.csproj src/Fcg.Notification.Application/
COPY src/Fcg.Notification.Infrastructure/*.csproj src/Fcg.Notification.Infrastructure/
COPY src/Fcg.Notification.Lambda/*.csproj src/Fcg.Notification.Lambda/

RUN dotnet restore src/Fcg.Notification.Lambda/Fcg.Notification.Lambda.csproj

COPY src/ src/
RUN dotnet publish src/Fcg.Notification.Lambda/Fcg.Notification.Lambda.csproj \
    -c Release -o /app/publish \
    -r linux-x64 --self-contained false \
    -p:PublishReadyToRun=true

# Runtime: use imagem base .NET (Lambda com RIC espera handler no CMD)
# Alternativa: public.ecr.aws/lambda/dotnet:10 e CMD ["Fcg.Notification.Lambda::Fcg.Notification.Lambda.Function::HandleSqsAsync"]
# Nesse caso o Function precisa de construtor sem parâmetros ou inicialização estática do DI.
FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS runtime
WORKDIR /var/task

COPY --from=build /app/publish .
COPY src/Fcg.Notification.Lambda/appsettings.json .

ENTRYPOINT ["dotnet", "Fcg.Notification.Lambda.dll"]
