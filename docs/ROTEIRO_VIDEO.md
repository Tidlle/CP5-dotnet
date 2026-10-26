# Roteiro do vídeo (máx. 5 minutos)

Antes de gravar: `dotnet run --project src/PetAdoption.API --launch-profile http`, abrir http://localhost:5080/swagger e deixar um terminal pronto na raiz da solução.

| Tempo | O que mostrar | Como |
|---|---|---|
| 0:00–0:30 | **Arquitetura** | Mostrar a solution: Domain → Application → Infrastructure → API e os 2 projetos de teste. |
| 0:30–1:00 | **Testes** | Rodar `dotnet test` no terminal → 59 + 29 aprovados (comentar que a integração usa o banco SQL Server da aplicação). |
| 1:00–1:40 | **Autenticação/Autorização** | `GET /api/pets` sem token → **401**. `POST /api/auth/register` (usuário comum) → Authorize → `POST /api/abrigos` → **403**. Login Admin (`admin@petadoption.com` / `Admin@123`) → Authorize. |
| 1:40–2:30 | **Serviço externo** | `GET /api/cep/01310-100` → endereço vindo do ViaCEP. `GET /api/cep/99999999` → **404 Cep.NaoEncontrado**. `POST /api/abrigos` com o CEP → endereço preenchido automaticamente (comentar retry/timeout/circuit breaker + cache). |
| 2:30–3:20 | **PageResult + HATEOAS** | `GET /api/pets?page=2&pageSize=3` → mostrar `totalItems`, `totalPages`, `hasNext`, links `self/first/previous/next/last` e os links de cada pet (`adotar`, `update`, `delete`). Comparar com o usuário comum (sem `update/delete`). |
| 3:20–4:30 | **Operation Result + Erros customizados** | `GET /api/pets/{id-inexistente}` → 404 `origin: OperationResult` (erro **retornado** pelo Repository). `DELETE /api/pets/{id-inexistente}` → 404 `origin: Exception` (erro **lançado** pelo Repository e tratado pelo GlobalExceptionHandler). `POST /api/abrigos` com nome repetido → 409. Adotar o mesmo pet duas vezes → 422 `Pet.JaAdotado`. |
| 4:30–5:00 | **Otimização** (rápido) | Mostrar `Projections.cs` (projeção para DTO), `AsNoTracking` + paginação no repositório e os índices na configuração do EF. |
