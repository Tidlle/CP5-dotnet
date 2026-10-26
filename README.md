# 🐾 PetAdoption API — CP5 Advanced Business Development with .NET

API RESTful para **adoção de pets**, construída com **ASP.NET Core 8 (Controllers)**. Abrigos (ONGs/canis) cadastram seus animais e usuários autenticados podem consultá-los e adotá-los. O endereço dos abrigos é preenchido automaticamente por um **serviço externo (ViaCEP)**.

O projeto aplica **Clean Architecture**, **Operation Result**, **erros customizados** (retornados e lançados pelo Repository), **PageResult**, **HATEOAS**, **JWT com perfis (Admin/User)**, otimizações de consulta e uma suíte de **testes de unidade e de integração** que roda no próprio banco da aplicação.

| | |
|---|---|
| 🎥 **Vídeo de demonstração** | **[INSERIR LINK DO VÍDEO AQUI](https://youtu.be/SEU-LINK)** |
| 📦 **Repositório** | **[INSERIR LINK DO REPOSITÓRIO AQUI](https://github.com/SEU-USUARIO/SEU-REPO)** |

### 👥 Integrantes

| Nome completo | RM |
|---|---|
| NOME COMPLETO 1 | RM00000 |
| NOME COMPLETO 2 | RM00000 |
| NOME COMPLETO 3 | RM00000 |

---

## 📚 Sumário

1. [Domínio](#-domínio)
2. [Arquitetura](#-arquitetura)
3. [Requisitos do CP5 e onde encontrá-los](#-requisitos-do-cp5-e-onde-encontrá-los)
4. [Endpoints](#-endpoints)
5. [Como rodar o projeto](#-como-rodar-o-projeto)
6. [Como executar os testes](#-como-executar-os-testes)
7. [Configurações](#-configurações)
8. [Exemplos de respostas](#-exemplos-de-respostas)

---

## 🐶 Domínio

| Entidade | Descrição | Regras principais |
|---|---|---|
| **Abrigo** | ONG/canil que mantém pets. Possui um **Endereco** (value object) obtido no ViaCEP. | Nome obrigatório e **único**; telefone com 10–11 dígitos; não pode ser removido se possuir pets. |
| **Pet** | Animal vinculado a um abrigo (`Cachorro`, `Gato`, `Coelho`, `Ave`, `Outro`). | Nome obrigatório; idade entre 0 e 360 meses; **não pode ser adotado duas vezes**. |
| **Usuario** | Usuário da API, com perfil `Admin` ou `User`. | E-mail único; senha armazenada com hash PBKDF2. |

---

## 🏛 Arquitetura

```
PetAdoption.sln
├── src
│   ├── PetAdoption.Domain          → Entidades, Value Objects, OperationResult, PageResult, Errors, Exceptions, interfaces de Repository
│   ├── PetAdoption.Application     → Casos de uso (Services), DTOs, projeções, interfaces de serviços externos
│   ├── PetAdoption.Infrastructure  → EF Core (SQL Server), Repositories, Migrations, ViaCEP (HttpClient + Polly), JWT, hash de senha
│   └── PetAdoption.API             → Controllers, HATEOAS, autenticação/autorização, tratamento global de erros, Swagger
└── tests
    ├── PetAdoption.UnitTests        → Domínio, Services (NSubstitute), cliente ViaCEP (HttpMessageHandler fake), segurança
    └── PetAdoption.IntegrationTests → API completa via WebApplicationFactory + banco SQL Server da aplicação
```

**Regra de dependência** (as setas apontam para dentro; o Domain não depende de ninguém):

```
API ──► Application ──► Domain
 │                        ▲
 └──► Infrastructure ─────┘  (implementa as interfaces do Domain/Application)
```

| Camada | Componentes |
|---|---|
| **Domain** | `Abrigo`, `Pet`, `Usuario`, `Endereco`, `OperationResult`/`OperationResult<T>`, `Error`/`ErrorType`, `DomainErrors`, `PageResult<T>`/`PageRequest`, `DomainException` (`EntityNotFoundException`, `DuplicateEntityException`, `BusinessRuleException`), `IPetRepository`, `IAbrigoRepository`, `IUsuarioRepository` |
| **Application** | `PetService`, `AbrigoService`, `AuthService`, DTOs de request/response, `Projections` (expressões traduzidas para SQL), `ICepService`, `ITokenService`, `IPasswordHasher` |
| **Infrastructure** | `AppDbContext` + configurações Fluent API + Migrations, `PetRepository`, `AbrigoRepository`, `UsuarioRepository`, `ViaCepService`, `JwtTokenService`, `Pbkdf2PasswordHasher`, `DatabaseInitializer` (migrations + seed) |
| **API** | `PetsController`, `AbrigosController`, `AuthController`, `CepController`, `RootController`, `LinkFactory` (HATEOAS), `GlobalExceptionHandler`, `ProblemDetailsMapper`, configuração de JWT/Swagger/compressão |

**Tecnologias:** .NET 8 · ASP.NET Core Controllers · Entity Framework Core 8 (SQL Server) · JWT Bearer · Microsoft.Extensions.Http.Resilience (Polly) · Swashbuckle (Swagger) · xUnit · NSubstitute · Microsoft.AspNetCore.Mvc.Testing

---

## ✅ Requisitos do CP5 e onde encontrá-los

### 1. Arquitetura (Clean Architecture)
Quatro projetos com dependências estritas (ver diagrama acima). O Domain não referencia nenhum pacote; a Infrastructure implementa as interfaces; a API só orquestra HTTP.

### 2. Otimização
| Técnica | Onde |
|---|---|
| `AsNoTracking()` em todas as leituras | `Infrastructure/Repositories/*Repository.cs` |
| **Projeção direta para DTO** (`Select` com `Expression`) — só as colunas necessárias saem do banco | `Application/Mappings/Projections.cs` + `GetPagedAsync<TResult>` |
| **Paginação no banco** (`OFFSET/FETCH`) e `COUNT` sem carregar entidades; página vazia não executa a 2ª query | `GetPagedAsync` nos repositórios |
| **Índices** (único em `Abrigo.Nome` e `Usuario.Email`; compostos em `Pet(AbrigoId, Status)`, `Pet(Especie, Status)`, `Abrigo(Uf, Cidade)`) | `Persistence/Configurations/EntityConfigurations.cs` |
| `ExecuteDeleteAsync` (DELETE direto, sem SELECT prévio) | `PetRepository.DeleteAsync`, `AbrigoRepository.DeleteAsync` |
| **Consulta compilada** (`EF.CompileAsyncQuery`) no login | `UsuarioRepository` |
| **Pool de DbContext** (`AddDbContextPool`) + `EnableRetryOnFailure` | `Infrastructure/DependencyInjection.cs` |
| **Cache em memória** das consultas ao ViaCEP | `ViaCepService` |
| **Compressão de resposta** (Brotli/Gzip) | `API/Extensions/ServiceCollectionExtensions.cs` |
| `CancellationToken` propagado do controller até o banco/HTTP | todas as camadas |

### 3. Testes automatizados
- **88 testes** — 59 de unidade + 29 de integração.
- **Unidade** (`tests/PetAdoption.UnitTests`): `OperationResult`, `PageResult`/`PageRequest`, regras das entidades, Services com repositórios mockados (NSubstitute), `ViaCepService` com `HttpMessageHandler` fake (sucesso, CEP inexistente, formato inválido, falha de rede, HTTP 500, cache), hash de senha e geração de JWT.
- **Integração/Funcionais** (`tests/PetAdoption.IntegrationTests`): sobem a API completa com `WebApplicationFactory<Program>` e usam **o mesmo banco SQL Server da aplicação** (`ConnectionStrings:DefaultConnection`, sem SQLite/InMemory). Cobrem autenticação (401/403), CRUD, adoção, paginação, HATEOAS e os erros customizados. Somente o ViaCEP é substituído por um fake (`FakeCepService`) para que os testes não dependam da internet. Os registros criados usam o prefixo `ITEST` / domínio `@integration.test` e são **removidos ao final**.

### 4. Operation Result
`Domain/Common/OperationResult.cs` — `OperationResult` e `OperationResult<T>` com `IsSuccess`, `IsFailure`, `Error`, `Value`, `Map` e conversões implícitas (`return pet;` / `return DomainErrors.Pet.JaAdotado;`). É usado do Repository até o Controller, onde `ApiControllerBase.HandleFailure` converte o `Error` em `ProblemDetails` com o status correto:

| `ErrorType` | HTTP |
|---|---|
| Validation | 400 |
| Unauthorized | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| BusinessRule | 422 |
| ExternalService | 503 |
| Unexpected | 500 |

### 5. Erros customizados — retorno **e** lançamento no Repository
- **Catálogo de erros:** `Domain/Errors/DomainErrors.cs` (ex.: `Pet.NotFound`, `Pet.JaAdotado`, `Abrigo.PossuiPets`, `Cep.ServicoIndisponivel`).
- **Exceções customizadas:** `Domain/Exceptions/DomainException.cs` → `EntityNotFoundException`, `DuplicateEntityException`, `BusinessRuleException` (cada uma carrega `Code` e `ErrorType`).
- **Retorno de erro no Repository:** `GetByIdAsync` / `GetByEmailAsync` devolvem `OperationResult.Failure(DomainErrors.X.NotFound(...))`.
- **Lançamento de erro no Repository:** `DeleteAsync` lança `EntityNotFoundException` quando nada foi removido; `AddAsync`/`UpdateAsync` lançam `DuplicateEntityException` (verificação prévia + tradução da violação de índice único do SQL Server, erros 2601/2627).
- **Tratamento das exceções** de duas formas:
  1. **Convertida em OperationResult** no Service: `AuthService.RegisterAsync` captura `DuplicateEntityException` → `409`.
  2. **Propagada até o `GlobalExceptionHandler`** (`IExceptionHandler`): `PetService.DeleteAsync` / `AbrigoService.CreateAsync` → `404` / `409`.
- Toda resposta de erro é um **ProblemDetails** (`application/problem+json`) com `code` e `origin` (`OperationResult`, `Exception` ou `Authentication`), o que deixa visível por qual caminho o erro passou.

### 6. Chamada de serviço externo
`Infrastructure/ExternalServices/ViaCep/ViaCepService.cs` consome `https://viacep.com.br/ws/{cep}/json/`:
- `HttpClient` tipado registrado com **`AddStandardResilienceHandler`** (Polly): retry com backoff exponencial + jitter, timeout por tentativa e total, circuit breaker.
- Tratamento de resposta: CEP com formato inválido → `400`; `{"erro": true}` → `404 Cep.NaoEncontrado`; falha de rede/timeout/circuito aberto/HTTP 5xx → `503 Cep.ServicoIndisponivel`.
- Cache em memória das respostas válidas.
- Usado no cadastro/alteração de abrigos e exposto diretamente em `GET /api/cep/{cep}`.

### 7. PageResult e HATEOAS
- `Domain/Common/PageResult.cs`: `Items`, `Page`, `PageSize`, `TotalItems`, `TotalPages`, `HasPrevious`, `HasNext`. `PageRequest` normaliza a página (mín. 1) e o tamanho (1–50, padrão 10).
- `API/Hateoas/LinkFactory.cs` gera links absolutos a partir das rotas nomeadas:
  - **Coleções:** `self`, `first`, `previous`, `next`, `last` (preservando filtros) e `create` (só para Admin).
  - **Pet:** `self`, `abrigo`, `collection`, `adotar` (só se estiver disponível), `update`/`delete` (só Admin).
  - **Abrigo:** `self`, `pets`, `collection`, `update`, `delete` (só Admin e só se não tiver pets), `create-pet`.
  - **Raiz** `GET /api`: ponto de entrada com os links de navegação.

### 8. Autenticação e autorização
- **JWT Bearer** (HMAC-SHA256) gerado em `JwtTokenService`; validação de issuer, audience, assinatura e expiração.
- **Política padrão (fallback)**: todo endpoint exige usuário autenticado, exceto os marcados com `[AllowAnonymous]` (raiz, login e cadastro).
- **Perfis:** `User` (consulta e adoção) e `Admin` (cadastro/alteração/remoção). `[Authorize(Roles = "Admin")]` nos endpoints de escrita.
- Respostas **401** (sem token/token inválido/expirado) e **403** (perfil sem permissão) padronizadas em ProblemDetails.
- Senhas com **PBKDF2-SHA256** (100.000 iterações, salt aleatório, comparação em tempo constante).

---

## 🔌 Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/api` | Público | Ponto de entrada (links HATEOAS) |
| POST | `/api/auth/register` | Público | Cria conta `User` e devolve token |
| POST | `/api/auth/login` | Público | Autentica e devolve token |
| GET | `/api/auth/me` | Autenticado | Dados do usuário logado |
| GET | `/api/pets?page=&pageSize=&nome=&especie=&status=&abrigoId=` | Autenticado | Lista paginada com filtros |
| GET | `/api/pets/{id}` | Autenticado | Detalhe do pet |
| POST | `/api/pets` | **Admin** | Cadastra pet |
| PUT | `/api/pets/{id}` | **Admin** | Atualiza pet |
| POST | `/api/pets/{id}/adocao` | Autenticado | Adota o pet (422 se já adotado) |
| DELETE | `/api/pets/{id}` | **Admin** | Remove pet |
| GET | `/api/abrigos?page=&pageSize=&nome=&cidade=&uf=` | Autenticado | Lista paginada com filtros |
| GET | `/api/abrigos/{id}` | Autenticado | Detalhe do abrigo |
| GET | `/api/abrigos/{id}/pets` | Autenticado | Pets do abrigo (paginado) |
| POST | `/api/abrigos` | **Admin** | Cadastra abrigo (endereço via ViaCEP) |
| PUT | `/api/abrigos/{id}` | **Admin** | Atualiza abrigo |
| DELETE | `/api/abrigos/{id}` | **Admin** | Remove abrigo sem pets |
| GET | `/api/cep/{cep}` | Autenticado | Consulta direta ao ViaCEP |

O arquivo [`src/PetAdoption.API/PetAdoption.API.http`](src/PetAdoption.API/PetAdoption.API.http) traz todas essas requisições prontas para o VS Code (REST Client) / Visual Studio / Rider.

---

## ▶️ Como rodar o projeto

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (SDKs mais novos também compilam o projeto)
- **SQL Server** — qualquer uma das opções:
  - **SQL Server LocalDB** (já vem com o Visual Studio no Windows) → funciona sem configurar nada; ou
  - **SQL Server em Docker** (Windows/Linux/macOS) → `docker compose up -d` (veja abaixo).
- Acesso à internet para o ViaCEP (só para a API em execução; os testes não precisam).

### Passo a passo

```bash
git clone <URL-DO-REPOSITORIO>
cd CP5-dotnet
dotnet restore
dotnet run --project src/PetAdoption.API --launch-profile http
```

Acesse o Swagger em **http://localhost:5080/swagger**.

Na primeira execução a API **aplica as migrations automaticamente** e cria:
- o usuário **Admin** → `admin@petadoption.com` / `Admin@123`
- 2 abrigos e 12 pets de exemplo (para demonstrar a paginação)

> Para se autenticar no Swagger: faça `POST /api/auth/login`, copie o `accessToken` e cole no botão **Authorize**.
> Para testar como usuário comum, crie uma conta em `POST /api/auth/register`.

### Usando SQL Server em Docker (opcional)

```bash
docker compose up -d
```

Depois aponte a API para esse servidor pela variável de ambiente (PowerShell):

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=PetAdoptionDb;User Id=sa;Password=PetAdoption@2026;TrustServerCertificate=True"
dotnet run --project src/PetAdoption.API --launch-profile http
```

(no bash: `export ConnectionStrings__DefaultConnection="..."`)

### Migrations (opcional — já são aplicadas na inicialização)

```bash
dotnet tool restore
dotnet ef database update --project src/PetAdoption.Infrastructure --startup-project src/PetAdoption.API
```

---

## 🧪 Como executar os testes

```bash
dotnet test
```

- Os **testes de integração usam o banco da aplicação** (mesma connection string `DefaultConnection` do `appsettings.json` da API, ou a variável `ConnectionStrings__DefaultConnection`). O SQL Server precisa estar acessível; as migrations são aplicadas automaticamente.
- Não é necessário internet: o ViaCEP é substituído por `FakeCepService` somente nos testes de integração (o cliente real é testado nos testes de unidade com um `HttpMessageHandler` fake).
- Os dados criados pelos testes são apagados ao final da execução; os dados de exemplo e o Admin permanecem.

Resultado esperado:

```
Aprovado!  – Com falha: 0, Aprovado: 59, Total: 59 - PetAdoption.UnitTests.dll (net8.0)
Aprovado!  – Com falha: 0, Aprovado: 29, Total: 29 - PetAdoption.IntegrationTests.dll (net8.0)
```

> Os projetos têm `RollForward=Major`: se a máquina tiver apenas um runtime mais novo que o .NET 8 (ex.: .NET 10), a API e os testes rodam nele normalmente.

---

## ⚙️ Configurações

Todas as chaves ficam em `src/PetAdoption.API/appsettings.json` e podem ser sobrescritas por variáveis de ambiente (`Secao__Chave`).

| Chave | Padrão | Descrição |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `Server=(localdb)\MSSQLLocalDB;Database=PetAdoptionDb;...` | Banco SQL Server da aplicação **e** dos testes |
| `Database:ApplyMigrationsOnStartup` | `true` | Aplica migrations + seed ao iniciar |
| `Jwt:Issuer` / `Jwt:Audience` | `PetAdoption.API` / `PetAdoption.Clients` | Emissor/audiência do token |
| `Jwt:Key` | chave de desenvolvimento | Chave HMAC (mín. 32 caracteres) — **troque em produção** (`Jwt__Key`) |
| `Jwt:ExpirationMinutes` | `60` | Validade do token |
| `Seed:AdminEmail` / `Seed:AdminPassword` | `admin@petadoption.com` / `Admin@123` | Usuário Admin criado na inicialização |
| `Seed:SampleData` | `true` | Cria abrigos/pets de exemplo |
| `ExternalServices:ViaCep:BaseUrl` | `https://viacep.com.br/` | URL do serviço externo |
| `ExternalServices:ViaCep:AttemptTimeoutSeconds` | `5` | Timeout por tentativa |
| `ExternalServices:ViaCep:TotalTimeoutSeconds` | `20` | Timeout total (com retries) |
| `ExternalServices:ViaCep:MaxRetryAttempts` | `3` | Número de retries |
| `ExternalServices:ViaCep:CacheMinutes` | `360` | Tempo de cache dos CEPs |

O ViaCEP é público e **não exige chave de API**.

---

## 📄 Exemplos de respostas

**`GET /api/pets?page=2&pageSize=2`** (Admin) — PageResult + HATEOAS:

```json
{
  "items": [
    {
      "data": {
        "id": "226cb5ca-5b36-40d9-ba21-be816e1afab9",
        "nome": "Luna",
        "especie": "Gato",
        "raca": "Siamês",
        "idadeMeses": 12,
        "status": "Disponivel",
        "abrigoId": "18ccc5ff-5f20-4191-ac07-b680059a9aea",
        "abrigoNome": "Abrigo Patas Felizes",
        "createdAt": "2026-10-25T23:31:55.567"
      },
      "links": [
        { "href": "http://localhost:5080/api/pets/226cb5ca-...", "rel": "self", "method": "GET" },
        { "href": "http://localhost:5080/api/abrigos/18ccc5ff-...", "rel": "abrigo", "method": "GET" },
        { "href": "http://localhost:5080/api/pets", "rel": "collection", "method": "GET" },
        { "href": "http://localhost:5080/api/pets/226cb5ca-.../adocao", "rel": "adotar", "method": "POST" },
        { "href": "http://localhost:5080/api/pets/226cb5ca-...", "rel": "update", "method": "PUT" },
        { "href": "http://localhost:5080/api/pets/226cb5ca-...", "rel": "delete", "method": "DELETE" }
      ]
    }
  ],
  "page": 2,
  "pageSize": 2,
  "totalItems": 12,
  "totalPages": 6,
  "hasPrevious": true,
  "hasNext": true,
  "links": [
    { "href": "http://localhost:5080/api/pets?page=2&pageSize=2", "rel": "self", "method": "GET" },
    { "href": "http://localhost:5080/api/pets?page=1&pageSize=2", "rel": "first", "method": "GET" },
    { "href": "http://localhost:5080/api/pets?page=1&pageSize=2", "rel": "previous", "method": "GET" },
    { "href": "http://localhost:5080/api/pets?page=3&pageSize=2", "rel": "next", "method": "GET" },
    { "href": "http://localhost:5080/api/pets?page=6&pageSize=2", "rel": "last", "method": "GET" },
    { "href": "http://localhost:5080/api/pets", "rel": "create", "method": "POST" }
  ]
}
```

**Erro retornado pelo Repository (OperationResult)** — `GET /api/pets/{id-inexistente}`:

```json
{
  "type": "https://httpstatuses.io/404",
  "title": "Recurso não encontrado",
  "status": 404,
  "detail": "Pet com id '00000000-0000-0000-0000-000000000001' não foi encontrado.",
  "instance": "/api/pets/00000000-0000-0000-0000-000000000001",
  "code": "Pet.NotFound",
  "origin": "OperationResult",
  "traceId": "0HNPIU0KBJLJN:00000001"
}
```

**Erro lançado pelo Repository (exceção → GlobalExceptionHandler)** — `POST /api/abrigos` com nome repetido:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflito",
  "status": 409,
  "detail": "Já existe Abrigo com Nome 'Abrigo Patas Felizes'.",
  "instance": "/api/abrigos",
  "code": "Abrigo.Duplicate",
  "origin": "Exception"
}
```

**Acesso bloqueado** — usuário `User` tentando `POST /api/pets`:

```json
{
  "status": 403,
  "title": "Acesso negado",
  "detail": "Seu perfil não tem permissão para executar esta operação.",
  "code": "Auth.AcessoNegado",
  "origin": "Authentication"
}
```
