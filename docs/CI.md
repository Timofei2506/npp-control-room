# Сборка в GitHub Actions (без Unity на своём ПК)

Всё собирается в облаке GitHub. Локальный Unity, Unity Hub и Windows-машина для сборки
**не нужны** — только браузер.

## Что происходит при пуше

```
push в main
   │
   ├─ preflight   — проверяет, что заданы секреты Unity (иначе падает с инструкцией)
   ├─ build       — game-ci/unity-builder в контейнере Unity 6000.0.58f1 (Linux-раннер → Windows-сборка)
   │                 └─ NPP.EditorTools.NPPBuildTools.BuildCI: сцена + Player Settings + exe
   ├─ zip + артефакт «NPPControlRoom-Windows-x64»
   └─ release     — только по тегу v*: публикует релиз с zip
```

Артефакт лежит: **Actions → Build Windows x64 → (последний запуск) → Artifacts**.
Распаковать zip и запустить `NPPControlRoom.exe` на Windows x64.

Репозиторий публичный, поэтому минуты Actions не тратятся из лимита.
Первая сборка — 15–25 минут, последующие (с кэшем `Library`) — 5–10.

## Один раз: лицензия Unity

Unity не даёт собирать проекты без лицензии — это её правило, обойти нельзя.
Нужны три секрета в **Settings → Secrets and variables → Actions → New repository secret**:

| Секрет | Что положить |
|---|---|
| `UNITY_EMAIL` | почта аккаунта Unity |
| `UNITY_PASSWORD` | пароль аккаунта (лучше без спецсимволов) |
| `UNITY_LICENSE` | содержимое файла `Unity_lic.ulf` (Personal) |
| `UNITY_SERIAL` | вместо `.ulf` — серийник, если у вас Unity Plus/Pro |

### Как получить `Unity_lic.ulf` (Unity Personal)

Файл `.ulf` создаётся один раз на любой машине — Unity для этого не требуется ставить целиком,
достаточно Unity Hub:

1. Скачать **Unity Hub** (<https://unity.com/download>) и войти в аккаунт Unity.
2. Hub → **Preferences (⚙) → Licenses → Add → Get a free personal license**.
   Важно нажать **Add** и пройти активацию — иначе файл не создастся.
3. Забрать файл:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
4. Открыть его блокнотом, скопировать **всё содержимое** и вставить в секрет `UNITY_LICENSE`.

Лицензия не привязана к версии Unity и к ОС — активировали на Windows, собираем на Linux.

> Если в Unity Hub нет пункта «Get a free personal license» (в некоторых версиях Hub его
> убрали) — варианты:
> - активировать через `game-ci` локально в Docker (`.github/workflows` не нужен, просто разово);
> - взять триал Unity Pro и использовать `UNITY_SERIAL` (серийник из <https://id.unity.com/en/subscriptions>);
> - написать в Discord GameCI — там ведут актуальные способы.
>
> Что **не** сработает: анонимная сборка, чужая лицензия в публичном репозитории (нарушение EULA),
> или «сборка» без Unity — проект на C# с Unity-API, компилятором без Unity его не собрать.

## Запуск вручную

Actions → **Build Windows x64** → **Run workflow** → ветка `main` → Run.

## Релиз

```bash
git tag v0.1.0
git push --tags
```

Появится релиз с zip-архивом игры и автоматическими заметками об изменениях.

## Что именно собирается

`NPPBuildTools.BuildCI` (вызывается через `buildMethod`):

1. создаёт `Assets/Scenes/Main.unity`, если её нет — **без диалогов** (в batch-режиме
   любой вопрос вешает сборку);
2. прописывает сцену в Build Settings;
3. выставляет Player Settings: окно 1600×900, Linear, **Scripting Backend = Mono2x**
   (Mono собирается на Linux-раннере; IL2CPP потребовал бы Windows-раннер и нативные тулчейны);
4. собирает `NPPControlRoom.exe` в путь, который передаёт GameCI (`-buildPath`).

Если сборка не удалась — метод бросает исключение, и шаг Actions краснеет (а не «зелёный
пустой артефакт»). Дополнительно шаг «Проверить, что exe действительно собрался» ищет
`NPPControlRoom.exe` в `build/`.

## Частые проблемы CI

| Симптом | Причина / решение |
|---|---|
| `Build не запущен: нет секретов Unity` | добавить `UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_LICENSE` |
| `No matching image` / `version not found` | в `build.yml` заменить `UNITY_VERSION` на существующий тег образа `unityci/editor` |
| Активация падает с `LICENSE` | `.ulf` устарел (сменили пароль/аккаунт) — пересоздать файл и секрет |
| `exit code 2` на Build | смотреть лог Unity выше; локально воспроизвести: `tools/checks/check.sh` (ловит ошибки компиляции) |
| Сборка идёт > 90 мин | первый запуск без кэша на слабом образе; повторный запуск обычно 5–10 мин |
