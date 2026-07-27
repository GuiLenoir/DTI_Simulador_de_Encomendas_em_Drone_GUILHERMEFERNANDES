# Simulador de Entregas por Drone

Aplicacao full-stack para simular entregas urbanas por drones, desenvolvida para o desafio tecnico da DTI.

O sistema permite cadastrar pedidos, gerenciar drones, planejar viagens com um ou mais pedidos, simular entregas por timestamps, acompanhar bateria/recarga, visualizar rotas em plano cartesiano, criar zonas de exclusao aerea e consultar relatorios.

## Deploy de demonstracao pronto

O projeto tambem ja esta publicado em ambiente online:

- Frontend na Vercel: https://dti-simulador-de-encomendas-em-dron.vercel.app/
- Backend no Railway, usando Docker
- Banco de dados MySQL no Railway

O ambiente local com Docker Compose continua funcionando normalmente e usa um MySQL local em container.

## Tecnologias

- Frontend: React, TypeScript e Vite
- Backend: ASP.NET Core 8 Web API
- ORM: Entity Framework Core
- Banco de dados: MySQL
- Containers: Docker e Docker Compose
- Testes backend: xUnit
- Testes frontend: Vitest, Testing Library e jsdom
- Deploy: Vercel, Railway e MySQL Railway

## Estrutura do projeto

```text
.
├── AGENTS.md
├── rules.md
├── TODO.md
├── TESTING.md
├── prompts/
├── backend/
│   ├── DroneDelivery.Api/
│   └── DroneDelivery.Tests/
├── frontend/
└── docker-compose.yml
```

## Como executar com Docker

Crie o arquivo `.env` a partir do exemplo:

```bash
cp .env.example .env
```

Suba a aplicacao:

```bash
docker compose up --build
```

Acesse:

- Frontend: `http://localhost:3000`
- Backend: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- MySQL: `localhost:3306`

O backend aplica migrations pendentes automaticamente na inicializacao.

## Como executar localmente sem subir tudo no Docker

Requisitos:

- .NET 8 SDK ou superior
- Node.js 22 ou superior
- Docker, para subir apenas o MySQL local

Suba o banco:

```bash
docker compose up database -d
```

Rode o backend:

```bash
dotnet restore backend/DroneDelivery.Api/DroneDelivery.Api.csproj
dotnet run --project backend/DroneDelivery.Api/DroneDelivery.Api.csproj
```

Em outro terminal, rode o frontend:

```bash
cd frontend
npm install
npm run dev
```

Frontend local do Vite:

```text
http://localhost:5173
```


## Como executar os testes principais

### Todos os testes backend

```bash
dotnet test backend/DroneDelivery.Tests/DroneDelivery.Tests.csproj
```

Esses testes validam as principais regras de negocio, incluindo:

- alocacao de drones;
- limite de peso;
- limite de alcance;
- validacao de bateria;
- consumo de bateria apos entrega;
- fila de pedidos;
- planejamento global de viagens;
- agrupamento de pedidos;
- prioridade dos pedidos;
- comportamento deterministico do planejador;
- zonas de exclusao aerea;
- rotas com desvio;
- entregas individuais;
- atualizacao de status por timestamps;
- relatorios;
- migrations principais.

### Build do backend

```bash
dotnet build backend/DroneDelivery.Api/DroneDelivery.Api.csproj
```

### Todos os testes frontend

```bash
cd frontend
npm test
```

Esses testes validam:

- cliente HTTP;
- traducao de erros da API;
- servicos HTTP principais;
- tela de relatorios.

### Build do frontend

```bash
cd frontend
npm run build
```

### Cobertura do frontend

```bash
cd frontend
npm run test:coverage
```

Mais detalhes estao em `TESTING.md`.

## Deploy publicado

O projeto ja possui um ambiente de demonstracao pronto:

- Frontend: Vercel
- Backend: Railway
- Banco de dados: MySQL no Railway

URL publica:

```text
https://dti-simulador-de-encomendas-em-dron.vercel.app/
```

O deploy usa `VITE_API_URL` no frontend, connection string por variavel de ambiente no backend e CORS restrito ao dominio publicado da Vercel. O ambiente local com Docker Compose nao depende desse deploy e continua usando o MySQL local em container.

## Principais funcionalidades

- Cadastro, edicao, listagem e remocao logica de drones.
- Cadastro, edicao, listagem e remocao de pedidos.
- Alocacao manual de um pedido para um drone.
- Planejamento global de entregas.
- Agrupamento de varios pedidos em uma mesma viagem quando possivel.
- Fila de entrega por prioridade e data de entrada.
- Simulacao por timestamps, sem background worker obrigatorio.
- Bateria consumida por distancia percorrida.
- Recarga simulada por timestamps.
- Regras para drone continuar entregando se ainda tiver bateria suficiente.
- Dashboard em tempo quase real com polling de 1 segundo.
- Relatorios operacionais.
- Cliente simulado para criar e acompanhar um pedido.
- Zonas de Exclusao Aerea com CRUD e visualizacao no mapa.
- Recalculo de rota com desvio quando uma zona ativa bloqueia o caminho.
- Mapa cartesiano com rotas, fluxo temporal das viagens, zoom e arrastar.

## Endpoints principais

```http
GET    /api/drones
GET    /api/drones/{id}
GET    /api/drones/status
POST   /api/drones
PUT    /api/drones/{id}
PATCH  /api/drones/{id}/activate
PATCH  /api/drones/{id}/deactivate
DELETE /api/drones/{id}

GET    /api/drone-settings
PUT    /api/drone-settings

GET    /api/orders
GET    /api/orders/{id}
GET    /api/orders/queue
POST   /api/orders
POST   /api/orders/{id}/queue
DELETE /api/orders/{id}/queue
PUT    /api/orders/{id}
DELETE /api/orders/{id}

GET    /api/deliveries
GET    /api/deliveries/{id}
GET    /api/deliveries/routes
POST   /api/deliveries/allocate/{orderId}
POST   /api/deliveries/simulate/{deliveryId}
DELETE /api/deliveries/{id}

GET    /api/dashboard

GET    /api/reports?from=&to=&droneId=&priority=

POST   /api/customer-simulation/orders
GET    /api/customer-simulation/orders/{id}/tracking

POST   /api/delivery-planning/plan
GET    /api/delivery-planning
GET    /api/trips
GET    /api/trips/upcoming
GET    /api/trips/{id}

GET    /api/no-fly-zones
GET    /api/no-fly-zones/{id}
POST   /api/no-fly-zones
PUT    /api/no-fly-zones/{id}
DELETE /api/no-fly-zones/{id}
```

## Regras dos drones

Cada drone possui:

- codigo e nome;
- capacidade maxima em kg;
- alcance maximo em km;
- bateria atual;
- consumo de bateria por km;
- margem global de seguranca;
- posicao atual na malha 2D;
- status operacional;
- flag de ativo/inativo.

Um drone nao pode iniciar entrega quando:

- esta inativo;
- esta executando entrega ou viagem;
- o peso ultrapassa sua capacidade;
- a rota ultrapassa seu alcance;
- a bateria atual nao cobre consumo estimado + margem de seguranca;
- a rota esta bloqueada por uma zona de exclusao sem caminho alternativo.

## Regras dos pedidos

Cada pedido possui:

- nome do cliente;
- coordenadas de destino `(X, Y)`;
- peso do pacote;
- prioridade `Low`, `Medium` ou `High`;
- data de criacao;
- data de entrada na fila;
- status do pedido;
- status de fila.

A fila considera:

1. prioridade alta antes de media e baixa;
2. data de entrada na fila;
3. peso e identificador como desempates deterministicos.

## Planejamento global

O endpoint:

```http
POST /api/delivery-planning/plan
```

planeja pedidos pendentes e em fila, respeitando as regras do desafio.

O planejador:

- considera pedidos pendentes ou viagens planejadas ainda nao iniciadas;
- nao altera viagens em `Loading` ou estados posteriores;
- tenta reduzir o numero total de viagens;
- permite mais de um pedido por viagem quando o drone suporta;
- calcula rota Base -> Entregas -> Base;
- respeita peso, alcance, bateria e margem de seguranca;
- compara planos candidatos antes de persistir viagens;
- usa resultado deterministico.

### Criterios de desempate do plano

Quando mais de um plano usa a mesma quantidade de viagens, o sistema prefere:

1. maior quantidade de pedidos de maior prioridade na primeira viagem;
2. menor distancia total somada;
3. maior aproveitamento da capacidade dos drones;
4. menor drone capaz de executar cada viagem;
5. menor identificador de drone/pedido para manter determinismo.

## Heuristica utilizada

O planejador usa uma heuristica deterministica de Multi-Knapsack com Best Fit.

Ele nao busca o otimo matematico global, mas tenta gerar um plano pratico e previsivel:

- ordena pedidos por prioridade e entrada na fila;
- gera combinacoes validas de pedidos por drone;
- rejeita combinacoes que violam peso, alcance, rota ou bateria;
- combina candidatos de viagens sem reutilizar drone ou pedido;
- compara o plano completo antes de salvar.

## Calculo de bateria

A bateria usa pontos percentuais.

Exemplo:

```text
Distancia da rota: 10 km
Consumo do drone: 2.5 p.p./km
Consumo estimado: 25 p.p.
Margem de seguranca: 5 p.p.
Bateria minima para iniciar: 30%
```

Depois que a entrega ou viagem termina, o consumo estimado e descontado da bateria do drone.

O drone so entra em recarga quando nao consegue atender nenhum pedido pendente ou em fila com a bateria atual. Se durante a recarga a bateria atual ja for suficiente para algum pedido da fila, ele pode parar de recarregar e voltar para operacao.

Valores padrao da simulacao:

- carregamento de pacote: `3` segundos;
- entrega ao cliente: `3` segundos;
- voo: `2` segundos por km;
- recarga: `1` ponto percentual por segundo;
- consumo padrao: `2.5` pontos percentuais por km;
- margem padrao: `5` pontos percentuais.

## Rotas e Zonas de Exclusao Aerea

A cidade e representada por uma malha 2D.

Quando nao ha obstaculo ativo, a distancia usa calculo euclidiano. Quando uma zona de exclusao aerea ativa cruza o segmento da rota, o backend monta um grafo de visibilidade usando os vertices dos poligonos e busca o menor desvio valido.

As zonas possuem:

- nome;
- status ativo/inativo;
- lista de pontos `(X, Y)`;
- poligono visualizado no frontend.

Se um pedido estiver dentro de uma zona ativa, a API rejeita o cadastro. Se uma rota nao tiver caminho valido, a viagem tambem e rejeitada.


## Simulacao por timestamps

A simulacao nao usa `Thread.Sleep`, tarefas longas nem processamento em loop infinito.

Cada entrega ou viagem salva uma linha do tempo

O backend calcula o status atual comparando esses timestamps com o horario atual. Isso permite que a simulacao continue correta mesmo se a aplicacao reiniciar.

## Relatorios

A aba de relatorios consolida:

- entregas concluidas;
- tempo medio;
- drone mais eficiente;
- distancia total;
- bateria consumida;
- mapa das entregas realizadas.

O score de eficiencia usa:

```text
(entregas concluidas + peso transportado) / (distancia total + bateria consumida)
```

## Observacoes importantes

- O backend e a fonte da verdade para regras de negocio.
- O frontend apenas apresenta dados e traduz mensagens/enum para pt-BR.
- A heuristica de planejamento e pratica e deterministica, mas nao garante otimo matematico.
- Docker Compose e voltado para desenvolvimento local.
- Vercel/Railway sao usados para demonstracao online.
