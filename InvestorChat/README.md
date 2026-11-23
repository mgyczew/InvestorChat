# InvestorChat

## Uruchomienie w Dockerze

1. Skopiuj `.env.example` do `.env` i ustaw własne, silne `POSTGRES_PASSWORD` oraz losowe `JWT_KEY` (co najmniej 32 bajty).
2. W katalogu `InvestorChat` uruchom `docker compose up --build -d`.
3. Otwórz `http://localhost:8080`.

Jeśli Docker Desktop wyświetla `no container found for project`, użyj `docker compose up --build -d`, a nie `docker compose start`. Polecenie `start` uruchamia wyłącznie wcześniej utworzone kontenery; pierwsze `up` tworzy i uruchamia usługi.

Aplikacja Blazor WebAssembly wraz z API działa w kontenerze `app`, a PostgreSQL w osobnym kontenerze `postgres`. Aplikacja czeka na gotowość bazy i wykonuje migracje EF Core przy starcie. Dane PostgreSQL są przechowywane w wolumenie `postgres_data`. Polecenie `docker compose down -v` usuwa bazę.

Logi sprawdzisz przez `docker compose logs -f app`; zatrzymanie usług: `docker compose down`. Port aplikacji można zmienić przez `APP_PORT` w `.env`.

## Konfiguracja

Połączenie z bazą oraz klucz JWT są wymagane jako zmienne środowiskowe. Nie umieszczaj rzeczywistych sekretów w repozytorium. Dla testów integracyjnych wymagany jest Docker Desktop.
