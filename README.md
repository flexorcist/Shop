Итоговый результат:

- Основные требования к функциональности реализованы полностью.

- Реализованы два микросервиса - Order Service и Payments Service.

- Чистая DDD-архитектура, заказы и платежи разделены, связь - асинхронные события.

- Реализованы очереди сообщений.

- Безопасность денег; атомарная операция на снятие гарантирует "exactly once" списание.

- Применен Transactional Outbox/Inbox.

- Реализован Swagger с покрытием всего API.

- Применено unit-тестирование с необходимым процентом покрытия.

- Приложение контейнеризировано и полностью разворачивается с помощью docker compose.

- Реализован frontend в виде веб-приложения с необходимым функционалом (также поднимается через docker compose up).

- В исходном коде присутствуют комментарии и пояснения




Более подробно:

0. После отправки заказа необходимо подождать несколько секунд, чтобы его статус обновился.


1. Общий обзор

- OrderService отвечает за заказы и шлет события в очередь ("transactional outbox").

- PaymentsService обрабатывает события, ведет счета, списывает средства и шлет PaymentResult.

- Frontend (React + Vite) обращается к обоим API и показывает баланс/статусы.

- Связь сервисов через RabbitMQ, хранение через PostgreSQL 16.

- Все упаковано в Docker-контейнеры и поднимается одной командой, либо запускается локально из VS 2022.



2. Архитектура и стек

2.1. OrderService

Ответственен за REST API заказов, Transactional Outbox, Consumer PaymentResult.
Реализовано с помощью .NET 9, EF Core 9.0.1, Npgsql 9.0.4, RabbitMQ.Client

2.2. PaymentsService

Ответственен за REST API счетов, Transactional Inbox/Outbox, idempotent debit.
Стек тот же что и в предыдущем пункте

2.3. RabbitMQ (rabbitmq:3.13-management)

Ответственен за шину сообщений, фан-аут exchange, shop.payments -> shop.orders.

2.4. PostgreSQL (postgres:16)

Ответственен за хранение заказов, счетов, outbox-таблиц.

2.5. Frontend

React-SPA; позволяет пополнить счет, создать заказ, наблюдать статусы заказов.
Реализовано с помощью React 18, Vite 5, Node 20, Nginx 1.25



3. Документация к API

3.1. Статусы

0 - NEW

1 - FINISHED (ACCEPTED)

2 - CANCELLED (REJECTED)

3.2. Payments API

3.2.1. POST/accounts (создать счет; идемпотентно)

тело: userId={guid}

ответ: 201

3.2.2. GET/accounts/{userId} (баланс счета)

тело: -

ответ: { userId, balance }

3.2.3. POST/accounts/{userId}/top-up (пополнить счет, возвращает новый баланс)

тело: amount=double

ответ: 200; double


3.3. Orders API

3.3.1. POST/orders (создать заказ; статус = NEW)

тело: { userId, amount, description }

ответ: 200; guid 


3.3.2. GET/orders (получение всех заказов)

тело: -

ответ: [{ id, amount, description, status }]


3.3.3. GET/orders/{id} (получение деталей заказа)

тело: -

ответ: { ... }


4. Доступ к сервисам
5. 
По установленным портам по этим URL открываются следующие сервисы:

localhost:3000 - Frontend (Summer Sale Shop)

localhost:5001/swagger - Swagger UI OrderService

localhost:5002/swagger - Swagger UI PaymentsService

localhost:15672 - RabbitMQ UI (guest/guest)


5. Запуск без docker compose (зачем?)

docker run -d --name rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3.13-management

docker run -d --name orders-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=orders   -p 5432:5432 postgres:16

docker run -d --name payments-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=payments -p 5433:5432 postgres:16

dotnet ef database update --project src/OrderService

dotnet ef database update --project src/PaymentsService

F5 в VS или в двух разных терминалах:

dotnet run --project src/OrderService --urls "http://localhost:5001"

dotnet run --project src/PaymentsService --urls "http://localhost:5002"
