# Katastata

Теперь проект состоит из двух частей:

- `Katastata` — WPF-клиент.
- `Katastata.Api` — ASP.NET Core Web API для работы с данными.

## Запуск API

1. Запустите `Katastata.Api`.
2. Откройте Swagger UI: `http://localhost:5099/swagger`.
3. Проверка health endpoint: `http://localhost:5099/health`.

> В `Katastata.Api/Properties/launchSettings.json` уже добавлен профиль с `http://localhost:5099`.

## Запуск клиента

1. Запустите WPF-клиент `Katastata`.
2. При необходимости задайте адрес API через переменную окружения `KATASTATA_API_URL`.

## UI обновление

Для более современного внешнего вида добавлены библиотеки:

- `MaterialDesignThemes`
- `MaterialDesignColors`

Их словари подключены в `App.xaml`, а в основных окнах использованы карточные контейнеры с тенями.
