# Sistema Bancário API
**Desenvolvedor:** Guilherme Ramos  
**Repositório:** [System Bank API](https://github.com/guiorozimbo/Desenvolvedor-CSharp-API-v4-System-Bank)

---

## 📌 Visão Geral
API RESTful de sistema bancário construída com **.NET** e práticas de nível **sênior**, incluindo:
- Autenticação com **JWT Bearer**
- Mensageria assíncrona via **Kafka**
- **Injeção de dependências** organizada por camadas
- Logs estruturados com **Serilog**
- Tratamento de erros centralizado
- **Swagger** para documentação interativa
- Deploy pronto para **Docker/Kubernetes**

Principais funcionalidades:
- Criação e autenticação de usuários
- Gestão de contas bancárias
- Transações (depósitos, saques, transferências) com idempotência
- Eventos Kafka para auditoria, extrato e notificações

---

## 📚 Tecnologias
- .NET 6+/7+
- ASP.NET Core Web API
- Entity Framework Core
- JwtBearer (Microsoft.AspNetCore.Authentication.JwtBearer)
- Kafka (Confluent.Kafka)
- AutoMapper
- FluentValidation
- Serilog
- Docker & docker-compose
- Swagger / Swashbuckle
- xUnit + Moq

---

## 🏗 Arquitetura
- **Camadas**: API → Application → Domain → Infrastructure  
- **CQRS** (opcional) para separar comandos e queries  
- **Repository + Unit of Work** para persistência  
- **Mensageria** com Kafka para processamento assíncrono  
- **Idempotência** via `CorrelationId` nas transações  
- **Swagger UI** para documentação dos endpoints  

---

## ⚙️ Pré-requisitos
- .NET SDK instalado  
- Docker & docker-compose  
- Ferramenta HTTP (Postman, Insomnia ou curl)  

---

## 🔧 Configuração (exemplo `appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=BankDb;User Id=sa;Password=Your_password123;"
  },
  "Jwt": {
    "Key": "SUA_CHAVE_SUPER_SECRETA",
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

---

## ▶️ Executando com Docker Compose
```bash
docker-compose up --build
```
A API estará disponível em:  
- **http://localhost:5000** (endpoints)  
- **http://localhost:5000/swagger** (Swagger UI)  

---

## 📖 Documentação (Swagger)
A API já possui documentação interativa via **Swagger UI**.  
Basta acessar:  
```
http://localhost:5000/swagger
```

No Swagger é possível:
- Testar todos os endpoints
- Gerar exemplos de request/response
- Exportar especificação OpenAPI (JSON/YAML)

---

## 🔑 Endpoints Principais
### Autenticação
- `POST /api/auth/register` → Registrar usuário  
- `POST /api/auth/login` → Autenticar e obter JWT  

### Contas
- `GET /api/accounts` → Listar contas  
- `GET /api/accounts/{id}` → Detalhar conta  
- `POST /api/accounts` → Criar nova conta  

### Transações
- `POST /api/transactions/transfer` → Transferir valores  
- `GET /api/transactions/{accountId}` → Histórico / extrato  

---

## 📨 Kafka (Produtor/Consumer)
- **Produtor**: publica `TransactionCreated` no tópico `bank-transactions`  
- **Consumers**: notificações, auditoria, relatórios  

---

## 🔒 Segurança & Boas Práticas
- Uso obrigatório de HTTPS em produção  
- Segredos em variáveis de ambiente / secret stores  
- Tokens JWT de curta duração + refresh tokens  
- Rate limiting no login  
- Auditoria de operações críticas  

---

## 🧪 Testes
- Unit tests com **xUnit + Moq**  
- Integration tests (EF InMemory ou container DB)  
- Contract tests para Kafka  

---

## 🚀 Deploy & CI/CD
Pipeline sugerido:
1. Build  
2. Testes  
3. Análise estática (Sonar, optional)  
4. Docker build + push  
5. Deploy em staging (K8s/App Service)  
6. Smoke tests  
7. Deploy produção (blue-green/canary)  

---

## 📊 Observabilidade
- Serilog (logs estruturados)  
- Health checks (`/health`)  
- OpenTelemetry para tracing distribuído  
- Métricas Prometheus + dashboards Grafana  

---

## 🛠 Troubleshooting Rápido
- **Erro JWT** → Verifique Issuer/Audience/Key  
- **Kafka não conecta** → Checar portas e `BootstrapServers`  
- **Transações duplicadas** → Conferir idempotência via `CorrelationId`  
- **Migrations falhando** → Revisar string de conexão  

---

## 🤝 Contribuição
1. Fork  
2. Branch `feature/`  
3. Pull request com descrição clara + testes  

---

## 📜 Licença & Contato
- Licença: MIT  
- Contato: Guilherme Ramos — GitHub: [guiorozimbo](https://github.com/guiorozimbo)  
