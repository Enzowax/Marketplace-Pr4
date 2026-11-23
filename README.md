# Marketplace — практическая работа №4

Учебный магазин на WPF. Покупатель может искать товары, добавлять их в корзину и оформлять заказы. Администратор управляет товарами, категориями, пунктами выдачи и статусами заказов.

В проекте используются C#, WPF, ASP.NET Core, SQL Server и Entity Framework 6 с моделью `Marketplace.edmx`.

## Как запустить

Нужны Visual Studio 2022 с поддержкой WPF и ASP.NET, .NET SDK 9, SQL Server Express и SSMS 20.

1. В SSMS выполните [create-database.sql](Marketplace.Api/Data/create-database.sql). Выберите базу `MarketplacePr4` и выполните [schema.sql](Marketplace.Api/Data/schema.sql), если таблицы ещё не созданы.
2. Выполните [seed.sql](Marketplace.Api/Data/seed.sql), чтобы добавить учебные товары и пункты выдачи.
3. В [appsettings.Development.json](Marketplace.Api/appsettings.Development.json) проверьте имя сервера. По умолчанию указан `.\SQLEXPRESS` с входом через Windows.
4. Откройте [Marketplace.sln](Marketplace.sln) в Visual Studio. Запустите `Marketplace.Api` с профилем `http`, затем `Marketplace.Wpf`. Адрес API — `http://localhost:5124`.

## Кто за что отвечал

- **Новиков Дмитрий (Enzowax)** — база данных, API и коллекция Postman.
- **Коскина Наталья (abobus228online)** — окна WPF и подключение клиента к API.
