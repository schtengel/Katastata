# Katastata

Теперь проект состоит из двух частей:

- `Katastata` — WPF-клиент.
- `Katastata.Api` — ASP.NET Core Web API для работы с данными.

## Запуск

1. Запустите API (`Katastata.Api`) — по умолчанию используется `http://localhost:5099`.
2. Запустите WPF-клиент (`Katastata`).
3. При необходимости укажите адрес API через переменную окружения `KATASTATA_API_URL`.

После изменений клиент обращается к API и не работает напрямую с SQLite.
