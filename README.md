# FCG Notification Lambda

Serviço serverless de notificação por e-mail da FCG Cloud Platform: consome mensagens de fila SQS, aplica templates e envia e-mails (SMTP fake ou real, preparado para SES).

## Estrutura

- **src/Fcg.Notification.Lambda** – Ponto de entrada Lambda, handler SQS, retry, poison, logs
- **src/Fcg.Notification.Application** – Handler de notificação, abstrações (resolver, renderer, sender, validator, store)
- **src/Fcg.Notification.Domain** – Contratos do domínio
- **src/Fcg.Notification.Infrastructure** – Templates HTML embarcados, resolver, renderer, model builder, deserializer, validator, SMTP/fake sender, store em memória
- **src/Fcg.Notification.Contracts** – Mensagens, payloads, nomes de template/payload
- **tests/Fcg.Notification.UnitTests** – Testes do handler com fakes

## Pré-requisitos

- .NET 10 SDK
- (Opcional) Docker para empacotar por imagem

## Modelo de mensagem

Cada item da fila deve ser um JSON no formato **NotificationMessage**:

```json
{
  "messageId": "uuid ou id único",
  "templateName": "PaymentApproved",
  "channel": "Email",
  "recipient": "user@example.com",
  "language": "pt-BR",
  "correlationId": "...",
  "traceId": "...",
  "occurredAt": "2026-03-12T10:00:00Z",
  "payloadType": "PaymentApprovedEmailPayload",
  "payload": "{ ... }"
}
```

- **Payload** é uma string JSON. **PayloadType** define o tipo para deserialização e validação.
- **TemplateName** deve corresponder a um template existente (ex.: `PaymentApproved`, `PaymentFailed`).
- **Recipient** pode ser omitido se o payload tiver `userEmail` (ex.: payloads de pagamento).

## Templates e payloads

- **PaymentApproved** – E-mail de compra confirmada (placeholders: `playerName`, `gameTitle`, `orderId`, `purchaseDate`, `totalAmount`, `libraryUrl`).
- **PaymentFailed** – E-mail de pagamento não concluído (`playerName`, `gameTitle`, `orderId`, `totalAmount`, `failureReason`, `supportUrl`).

Payloads tipados: **PaymentApprovedEmailPayload**, **PaymentFailedEmailPayload** (UserId, UserName, UserEmail, GameId, GameTitle, Amount, Currency, PaymentId, PaymentStatus, PurchaseDate; FailureReason apenas no failed).

O **TemplateModelBuilder** mapeia esses payloads para o modelo esperado pelos HTML (ex.: UserName → playerName, PaymentId → orderId, datas e valores formatados).

## Decisões

- **Versionamento de templates:** por nome (ex.: `PaymentApproved`, `PaymentApproved_v2`); o resolver busca `{TemplateName}.html` em recursos embarcados ou pasta configurável.
- **Validação de payload:** por **IPayloadValidator**; payloads de pagamento exigem UserEmail e PaymentId.
- **Template inexistente:** retorno `TemplateNotFound`; mensagem não é reenviada (não retriable).
- **Idempotência:** **ISentNotificationStore** (em memória por padrão); mesmo **MessageId** não reenvia.
- **Payload para outros domínios:** novos tipos em Contracts (PayloadType + classe); registrar no **PayloadDeserializer** e no **PayloadValidator**; opcionalmente no **TemplateModelBuilder** e novos `.html`.

## Configuração

- **Notification:LibraryBaseUrl** – URL da biblioteca (template PaymentApproved).
- **Notification:SupportUrl** – URL de suporte (template PaymentFailed).
- **Notification:UseSmtp** – `true` para SMTP real.
- **Smtp:** Host, Port, EnableSsl, UserName, Password, FromAddress.

Variáveis de ambiente sobrescrevem `appsettings.json`.

## Execução local

1. Publicar e rodar (simula invocações; para SQS real use Lambda ou teste com payload no stdin conforme SDK):

```bash
cd src/Fcg.Notification.Lambda
dotnet run
```

2. Com SMTP fake (padrão), os envios são apenas logados.

## Build e testes

```bash
dotnet build src/Fcg.Notification.Lambda/Fcg.Notification.Lambda.csproj
dotnet test tests/Fcg.Notification.UnitTests/Fcg.Notification.UnitTests.csproj
```

Se os testes falharem ao executar por falta de assemblies net10.0 no ambiente, use `dotnet build` para validar a compilação.

## Docker

Empacotar por imagem (ajuste o tag da imagem .NET 10 se necessário):

```bash
docker build -t fcg-notification-lambda .
```

A imagem usa **ENTRYPOINT** com `dotnet Fcg.Notification.Lambda.dll` (bootstrap que escuta eventos Lambda). Para usar em AWS Lambda com container:

- Configure a fila SQS como origem do evento.
- Defina variáveis de ambiente (Notification, Smtp, etc.).
- Se usar imagem base oficial Lambda (`public.ecr.aws/lambda/dotnet:10`), troque o Dockerfile para essa base e use **CMD** com o handler: `Fcg.Notification.Lambda::Fcg.Notification.Lambda.Function::HandleSqsAsync` (e adapte o Function para ser instanciável pelo host Lambda, ex.: construtor sem parâmetros + ServiceProvider estático).

## Retry e poison

- **Retry:** falhas retriable (ex.: **SendFailed**) são devolvidas em **SQSBatchResponse** (batchItemFailures); só os itens falhos voltam para a fila.
- **Poison:** mensagens com **ApproximateReceiveCount** ≥ 4 são logadas e descartadas (não entram em batchItemFailures). Configure DLQ na fila SQS para capturar rejeições após o máximo de retentativas.

## Logs e métricas

- Logs estruturados (ex.: MessageId, TemplateName, Status, DurationMs, CorrelationId).
- Métricas: use os logs (ex.: `NotificationProcessed`, `Batch completed`) para CloudWatch Logs Insights ou métricas customizadas.

## Integração com Payments API

A Payments API publica eventos no outbox no formato **PaymentNotificationEvent**. O consumidor da fila (este Lambda ou um adapter) deve montar o **NotificationMessage** a partir desse evento (messageId, templateName, payloadType, payload serializado, recipient/userEmail, correlationId, traceId, occurredAt) e enviar para a fila SQS que esta Lambda processa.
