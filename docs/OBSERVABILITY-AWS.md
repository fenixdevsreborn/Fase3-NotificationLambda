# Observabilidade da Notification Lambda (AWS)

## Decisão: ADOT + X-Ray + CloudWatch

**Abordagem implementada: ADOT Lambda layer + OpenTelemetry .NET SDK + X-Ray + CloudWatch.**

- **Traces:** OpenTelemetry SDK envia spans via OTLP para o **ADOT Lambda extension** (collector em layer); o extension exporta para **AWS X-Ray**. O handler é envolvido com `AWSLambdaWrapper.TraceAsync`; spans customizados (por mensagem) usam `LambdaActivitySource` e continuam o trace da mensagem (TraceId/CorrelationId).
- **Métricas:** `MeterProvider` envia métricas via OTLP para o mesmo extension; o extension pode ser configurado para exportar para **CloudWatch Metrics**. Métricas de negócio: `emails.sent`, `emails.failed`, `exceptions.count`.
- **Logs:** Saída estruturada via `ILogger` (Console); cada processamento de mensagem usa um **scope** com `TraceId`, `SpanId`, `CorrelationId`, `MessageId`, `TemplateName`. Em AWS, o **CloudWatch Logs** captura stdout (configuração da função).

**Por que ADOT e não só X-Ray?**

- Continuação do trace a partir das APIs (mesmo modelo OTLP/W3C).
- Métricas customizadas (emails.sent, etc.) no mesmo pipeline.
- Um único layer e um único endpoint (extension) para traces e métricas.

**Alternativa (só X-Ray):** Ativar apenas Active Tracing na Lambda e não usar o layer ADOT; traces da invocação e subsegmentos manuais aparecem no X-Ray; métricas customizadas teriam que ser enviadas via EMF (log JSON) ou PutMetricData.

---

## Caminho dos dados

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Fcg.Notification.Lambda                                                     │
│  • AWSLambdaWrapper.TraceAsync → span da invocação                           │
│  • ActivityRunContext → span por mensagem (continua TraceId da fila)         │
│  • LambdaActivitySource + FcgMeters (emails.sent, emails.failed, exceptions) │
│  • OTLP exporter (traces + metrics) → http://localhost:4318 (extension)     │
│  • ILogger → Console (stdout) → CloudWatch Logs (por configuração da Lambda) │
└─────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼ OTLP (traces, metrics)
┌─────────────────────────────────────────────────────────────────────────────┐
│  ADOT Lambda Extension (layer)                                               │
│  • Recebe OTLP na porta configurada (ex.: 4318)                              │
│  • Exporta traces → AWS X-Ray                                                │
│  • Exporta metrics → CloudWatch Metrics (se configurado no collector)        │
└─────────────────────────────────────────────────────────────────────────────┘
                                        │
                    ┌───────────────────┴───────────────────┐
                    ▼                                       ▼
            ┌──────────────┐                        ┌─────────────────┐
            │  AWS X-Ray   │                        │ CloudWatch      │
            │  (traces)    │                        │ Metrics / Logs  │
            └──────────────┘                        └─────────────────┘
```

---

## Variáveis de ambiente (OTEL / ADOT)

| Variável | Descrição | Exemplo (local) | Exemplo (produção) |
|----------|-----------|------------------|--------------------|
| `OTEL_SERVICE_NAME` | Nome do serviço (resource) | `Fcg.Notification.Lambda` | `Fcg.Notification.Lambda` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Endpoint OTLP para o extension | (vazio = não exportar) | `http://localhost:4318` |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` ou `http/protobuf` | `http/protobuf` | `http/protobuf` |

**Nota:** A exportação OTLP só é ativada quando `OTEL_EXPORTER_OTLP_ENDPOINT` está definido. Em produção (com layer ADOT), defina `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318`. Em local, não defina para não exportar.

---

## Configuração AWS recomendada

### 1. Active Tracing

- No console da Lambda: **Configuration → Monitoring and operations → Tracing**.
- Ative **Active tracing** (X-Ray).
- Necessário para que o X-Ray receba os segmentos da invocação e do ADOT.

### 2. Lambda Layer (ADOT Collector)

- Adicione o **layer do ADOT Collector** à função.
- ARN (formato, por região e arquitetura):
  - **amd64:** `arn:aws:lambda:<region>:901920570463:layer:aws-otel-collector-amd64-ver-0-117-0:1`
  - **arm64:** `arn:aws:lambda:<region>:901920570463:layer:aws-otel-collector-arm64-ver-0-117-0:1`
- Consulte a [documentação ADOT Lambda](https://aws-otel.github.io/docs/getting-started/lambda/lambda-dotnet/) para ARNs atualizados por região.

### 3. Variáveis de ambiente na função

- `OTEL_SERVICE_NAME` = `Fcg.Notification.Lambda`
- `OTEL_EXPORTER_OTLP_ENDPOINT` = `http://localhost:4318` (ou a porta configurada no config do extension)
- `OTEL_EXPORTER_OTLP_PROTOCOL` = `http/protobuf` (ou `grpc`, conforme o config do extension)

### 4. Permissões IAM

A role da Lambda deve permitir:

- **X-Ray:** envio de segmentos.
  - Ex.: `xray:PutTraceSegments`, `xray:PutTelemetryRecords`
- **CloudWatch Logs:** escrita do log da função (já comum em Lambdas).
  - Ex.: `logs:CreateLogGroup`, `logs:CreateLogStream`, `logs:PutLogEvents`
- **CloudWatch Metrics:** se o extension estiver configurado para exportar métricas.
  - Ex.: `cloudwatch:PutMetricData`

Exemplo de política (apenas X-Ray e logs; ajuste se usar PutMetricData):

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "xray:PutTraceSegments",
        "xray:PutTelemetryRecords"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "arn:aws:logs:<region>:<account>:log-group:/aws/lambda/<function-name>:*"
    }
  ]
}
```

---

## Estrutura interna (Observability / Telemetry / Extensions)

```
Observability/
├── ActivityRunContext.cs      # Continua trace da mensagem (TraceId/SpanId/CorrelationId), usa LambdaActivitySource
├── FcgMeters.cs               # emails.sent, emails.failed, exceptions.count
├── FcgMetricNames.cs          # Nomes das métricas
├── FcgLogPropertyNames.cs     # TraceId, SpanId, CorrelationId, MessageId, TemplateName, ExceptionType
└── ObservabilityContext.cs    # GetCurrentTraceId/SpanId/CorrelationId a partir do Activity

Telemetry/
└── LambdaActivitySource.cs    # ActivitySource para spans customizados (nome do serviço)

Extensions/
├── ServiceCollectionExtensions.cs       # AddLambdaObservability (FcgMeters, ActivitySource)
└── OpenTelemetryLambdaExtensions.cs    # ConfigureOpenTelemetry, TraceSqsHandlerAsync, TracerProvider/MeterProvider
```

---

## Métricas

| Nome               | Tipo   | Descrição                          |
|--------------------|--------|------------------------------------|
| `emails.sent`      | Counter| Emails enviados com sucesso        |
| `emails.failed`    | Counter| Falhas no envio ou descarte poison |
| `exceptions.count` | Counter| Exceções (tag: exception.type)     |

---

## Exemplo local (sem collector)

- Não defina `OTEL_EXPORTER_OTLP_ENDPOINT` (ou deixe vazio).
- O código não inicia TracerProvider/MeterProvider; o handler roda sem wrapper de tracing (ou com wrapper inerte).
- Logs continuam no Console com scope (TraceId, CorrelationId, etc.) quando a mensagem tiver esses campos.

---

## Exemplo produção (com layer ADOT)

1. Adicione o layer ADOT à função (ARN da sua região/arquitetura).
2. Active tracing: ativado.
3. Variáveis de ambiente:
   - `OTEL_SERVICE_NAME` = `Fcg.Notification.Lambda`
   - `OTEL_EXPORTER_OTLP_ENDPOINT` = `http://localhost:4318`
   - `OTEL_EXPORTER_OTLP_PROTOCOL` = `http/protobuf`
4. Role com permissões de X-Ray e CloudWatch Logs (e CloudWatch Metrics se configurar o extension para métricas).
5. Log group da função: padrão `/aws/lambda/<nome-da-função>`; os logs estruturados (com TraceId, CorrelationId) aparecem no CloudWatch Logs.

---

## Propagação de trace/correlation

- A mensagem SQS (body) deve conter **TraceId** e **CorrelationId** (ex.: enviados pela Payments API ao publicar na fila).
- **ActivityRunContext.RunWithActivityAsync** cria um span filho desse trace (W3C) e define a tag `correlation.id`.
- Assim, no X-Ray o span da Lambda fica ligado ao trace da API de origem (quando o producer envia TraceId/SpanId na mensagem). **SpanId** na mensagem é opcional; se não vier, o parent é identificado só pelo TraceId.

---

## Pacotes NuGet (Lambda)

| Pacote | Versão |
|--------|--------|
| `OpenTelemetry.Instrumentation.AWSLambda` | 1.15.0 |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.15.0 |
| `OpenTelemetry.Extensions.AWS` | 1.15.0 |

(Transitivos: Amazon.Lambda.SQSEvents, OpenTelemetry, etc.)
