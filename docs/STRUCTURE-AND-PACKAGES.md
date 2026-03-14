# Estrutura e pacotes – Fase3-NotificationLambda

Este documento descreve a árvore de pastas do projeto e os pacotes NuGet utilizados **sem dependência de Fase3-Shared**.

## Árvore final (principais pastas e arquivos)

```
Fase3-NotificationLambda/
├── docs/
│   └── STRUCTURE-AND-PACKAGES.md
├── src/
│   ├── Fcg.Notification.Contracts/
│   │   ├── Enums/
│   │   │   └── NotificationChannel.cs
│   │   ├── Messages/
│   │   │   └── NotificationMessage.cs
│   │   ├── Payloads/
│   │   │   ├── PaymentApprovedEmailPayload.cs
│   │   │   ├── PaymentFailedEmailPayload.cs
│   │   │   └── PaymentEmailPayloadBase.cs
│   │   ├── PayloadTypeNames.cs
│   │   └── TemplateNames.cs
│   ├── Fcg.Notification.Domain/
│   ├── Fcg.Notification.Application/
│   │   ├── Abstractions/
│   │   ├── Extensions/
│   │   └── Services/
│   │       └── NotificationHandler.cs
│   ├── Fcg.Notification.Infrastructure/
│   │   ├── Email/
│   │   ├── Extensions/
│   │   ├── Options/
│   │   ├── Payloads/
│   │   ├── Storage/
│   │   └── Templates/
│   └── Fcg.Notification.Lambda/
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs   # AddLambdaObservability()
│       ├── Observability/
│       │   ├── ActivityRunContext.cs            # Tracing a partir da mensagem (TraceId/SpanId/CorrelationId)
│       │   ├── FcgLogPropertyNames.cs           # Nomes padronizados para logs
│       │   ├── FcgMetricNames.cs                # emails.sent, emails.failed, exceptions.count
│       │   ├── FcgMeters.cs                     # Métricas
│       │   └── ObservabilityContext.cs          # CorrelationId, TraceId, SpanId
│       ├── Telemetry/
│       │   └── LambdaActivitySource.cs          # ActivitySource para spans customizados
│       ├── Function.cs                          # Handler SQS, poison, retry, métricas
│       └── Program.cs                           # Bootstrap, AddLambdaObservability(), sem Shared
├── tests/
│   ├── Fcg.Notification.UnitTests/             # Application + Contracts (handler, validação, etc.)
│   │   └── NotificationHandlerTests.cs
│   └── Fcg.Notification.Lambda.Tests/           # Observabilidade (FcgMeters, ActivityRunContext)
│       ├── ActivityRunContextTests.cs
│       └── FcgMetersTests.cs
└── ...
```

## Pacotes NuGet (sem Shared)

### Fcg.Notification.Lambda

| Pacote | Versão | Uso |
|--------|--------|-----|
| Amazon.Lambda.Core | 2.8.1 | ILambdaContext |
| Amazon.Lambda.RuntimeSupport | 1.14.2 | Bootstrap |
| Amazon.Lambda.SQSEvents | 2.2.1 | SQSEvent, SQSBatchResponse |
| Amazon.Lambda.Serialization.SystemTextJson | 2.4.5 | Serialização |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 10.0.0 | Config |
| Microsoft.Extensions.Configuration.Json | 10.0.0 | appsettings.json |
| Microsoft.Extensions.DependencyInjection | 10.0.0 | DI |
| Microsoft.Extensions.Logging | 10.0.0 | ILogger |
| Microsoft.Extensions.Logging.Console | 10.0.0 | Console |

Métricas e tracing usam apenas **BCL** (`System.Diagnostics.Metrics`, `System.Diagnostics.Activity`); não é necessário pacote OpenTelemetry para o básico.

### Outros projetos (Application, Infrastructure, Contracts, Domain)

Conforme definido nos respectivos `.csproj`; nenhum referencia Fase3-Shared.

## Observabilidade internalizada

- **Métricas**: `FcgMeters` – `emails.sent`, `emails.failed`, `exceptions.count` (tag `exception.type`).
- **Tracing**: `ActivityRunContext` – continua o trace da mensagem (TraceId/SpanId) e define tag de correlation id.
- **ActivitySource**: `Telemetry/LambdaActivitySource` – nome `Fcg.Notification.Lambda` para spans adicionais se necessário.
- **Logs**: `FcgLogPropertyNames` – nomes padronizados (TraceId, SpanId, CorrelationId, MessageId, TemplateName, ExceptionType).
- **Registro**: `AddLambdaObservability()` em `Extensions/ServiceCollectionExtensions.cs` – registra `FcgMeters` e `LambdaActivitySource`.

## Testes

- **Fcg.Notification.UnitTests**: handler, validação, template não encontrado, idempotência (sem referência à Lambda).
- **Fcg.Notification.Lambda.Tests**: `FcgMeters` (RecordEmailSent/Failed/Exception) e `ActivityRunContext` (propagação de trace/correlation).

Se o host de testes falhar com erro de assembly (ex.: `Amazon.Lambda.Core` ou `Microsoft.Extensions.DependencyInjection`), verifique se o SDK e os pacotes restaurados são compatíveis com `net10.0` no seu ambiente.
