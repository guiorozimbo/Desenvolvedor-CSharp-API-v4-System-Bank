# Sistema Bancário API
**Desenvolvedor:** Guilherme Ramos  
**Repositório:** https://github.com/guiorozimbo/Desenvolvedor-CSharp-API-v4-System-Bank

---

## Visão geral
API RESTful de sistema bancário construída com foco em padrões de arquitetura e práticas de nível **sênior**: autenticação JWT (JwtBearer), mensageria com **Kafka**, injeção de dependências, segregação de responsabilidades, tratamento de erros centralizado, logs estruturados, testes e configuração preparada para contêineres e CI/CD.

Principais responsabilidades:
- Autenticação & Autorização (JWT Bearer)
- Gestão de usuários e contas
- Transações (transferências, depósitos, saques) com consistência e idempotência
- Mensageria assíncrona via Kafka para eventos (extratos, notificações, auditoria)
- Boas práticas de segurança, observabilidade e deploy

---

# Índice
1. [Tecnologias](#tecnologias)  
2. [Arquitetura e padrões](#arquitetura-e-padrões)  
3. [Pré-requisitos](#pré-requisitos)  
4. [Configuração (exemplo)](#configuração-exemplo)  
5. [Executando localmente (docker-compose)](#executando-localmente-docker-compose)  
6. [Endpoints principais (exemplos)](#endpoints-principais-exemplos)  
7. [Fluxo Kafka (produtor/consumer)](#fluxo-kafka-produtorconsumer)  
8. [Segurança & melhores práticas](#segurança--melhores-práticas)  
9. [Testes](#testes)  
10. [Deploy & CI/CD](#deploy--cicd)  
11. [Observabilidade](#observabilidade)  
12. [Troubleshooting rápido](#troubleshooting-rápido)  
13. [Contribuição](#contribuição)  
14. [License & contato](#license--contato)

---

## Tecnologias
- .NET (versão utilizada no projeto)
- ASP.NET Core Web API
- Entity Framework Core (migrations)
- JwtBearer (Microsoft.AspNetCore.Authentication.JwtBearer)
- Kafka (Confluent.Kafka ou outra lib compatível)
- Dependency Injection (built-in + abstrações)
- AutoMapper (opcional)
- FluentValidation (validações)
- Serilog (logs estruturados)
- Docker / docker-compose
- SQL Server / PostgreSQL (conforme configuração)
- xUnit / NUnit + Moq para testes

---

## Arquitetura e padrões
- **Camadas**: API → Application (serviços casos de uso) → Domain (modelos) → Infrastructure (EFCore, Kafka, email, file storage)
- **CQRS** para separação de comandos e queries (opcional)
- **Repository + Unit of Work** para abstração do acesso a dados
- **Mensageria**: eventos emitidos quando transações são confirmadas (ex.: `TransactionCreated`, `BalanceUpdated`)
- **Idempotência**: tokens/correlations para evitar execução duplicada de transações
- **Tratamento global**: middleware para exceções, resposta padronizada de erros e logging
- **Injeção de dependências** configurada via `IServiceCollection` com scopes corretos (Singleton, Scoped, Transient)

---

## Pré-requisitos
- .NET SDK instalado
- Docker & docker-compose
- (Opcional) IDE: Visual Studio / VS Code
- Ferramenta para executar requests: curl / httpie / Postman

---

## Configuração (exemplo `appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=BankDb;User Id=sa;Password=Your_password123;"
  },
  "Jwt": {
    "Key": "SUA_CHAVE_SECRETA_MUITO_LONGA_AQUI",
    "Issuer": "SystemBank",
    "Audience": "SystemBankClients",
    "ExpiresMinutes": 60
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "ProducerTopic": "bank-transactions",
    "ConsumerGroup": "bank-service-group"
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

**Observação:** **NUNCA** comite segredos. Use variáveis de ambiente / secret stores para produção.

---

## Exemplo de registro de serviços (Startup / Program.cs)
```csharp
// Program.cs (parcial)
builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      var jwt = configuration.GetSection("Jwt");
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwt["Issuer"],
          ValidAudience = jwt["Audience"],
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]))
      };
  });

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();

// Kafka producer/consumer
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>(); // implementation wraps Confluent.Kafka
builder.Services.AddHostedService<KafkaConsumerBackgroundService>(); // consumer as background service

// Validation, AutoMapper, Controllers
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddControllers();
```

---

## docker-compose (exemplo: SQL Server + Zookeeper + Kafka)
```yaml
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      SA_PASSWORD: "Your_password123"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"

  zookeeper:
    image: confluentinc/cp-zookeeper:7.3.0
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000

  kafka:
    image: confluentinc/cp-kafka:7.3.0
    depends_on:
      - zookeeper
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
    ports:
      - "9092:9092"

  api:
    build: .
    depends_on:
      - sqlserver
      - kafka
    environment:
      ConnectionStrings__DefaultConnection: "Server=sqlserver;Database=BankDb;User Id=sa;Password=Your_password123;"
      Jwt__Key: "dev_secret_key_replace"
      Kafka__BootstrapServers: "kafka:9092"
    ports:
      - "5000:80"
```

---

## Endpoints principais (exemplos)
### Autenticação
- `POST /api/auth/register`
- `POST /api/auth/login`

### Contas
- `GET /api/accounts`
- `GET /api/accounts/{id}`
- `POST /api/accounts`

### Transações
- `POST /api/transactions/transfer`
- `GET /api/transactions/{accountId}`

---

## Fluxo Kafka (produtor / consumer)
- **Produtor**: após confirmação de transação, publicar evento `TransactionCreated`.
- **Consumer**: consome tópico `bank-transactions` para notificações, auditoria e relatórios.

---

## Segurança & melhores práticas
- HTTPS obrigatório
- Segredos em variáveis de ambiente
- Tokens curtos + refresh tokens
- Rate limiting
- Auditoria de ações críticas

---

## Testes
- Unit tests: xUnit + Moq
- Integration tests: EF Core InMemory ou container DB
- Contract tests: Testcontainers Kafka

---

## Deploy & CI/CD
- Build, tests, análise estática
- Docker image + push
- Deploy em staging (K8s/App Service)
- Smoke tests
- Deploy prod (blue-green/canary)

---

## Observabilidade
- Serilog + sinks
- OpenTelemetry tracing
- Health checks
- Métricas Prometheus/Grafana

---

## Troubleshooting rápido
- Erros JWT: revisar issuer/audience/key
- Kafka unreachable: verificar portas/config
- Transações duplicadas: checar idempotência
- Migrations falhando: conferir credenciais DB

---

## Contribuição
1. Fork
2. Nova branch `feature/`
3. Pull request com descrição clara e testes

---

## Licença & Contato
- Licença: MIT
- Contato: Guilherme Ramos — GitHub: [guiorozimbo](https://github.com/guiorozimbo)
