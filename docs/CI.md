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

Unity не собирает проекты без лицензии — это её правило, обойти нельзя. Есть **два пути**,
оба не требуют собирать что-либо на своём ПК. Выберите один.

### Путь A — GitHub Actions (уже настроен), нужен файл `Unity_lic.ulf`

Три секрета в **Settings → Secrets and variables → Actions → New repository secret**:

| Секрет | Что положить |
|---|---|
| `UNITY_EMAIL` | почта аккаунта Unity |
| `UNITY_PASSWORD` | пароль аккаунта (лучше без спецсимволов) |
| `UNITY_LICENSE` | содержимое файла `Unity_lic.ulf` (Personal) |
| `UNITY_SERIAL` | вместо `.ulf` — серийник, если у вас Unity Plus/Pro |

`Unity_lic.ulf` создаётся один раз через **Unity Hub** (это только лаунчер, ~150 МБ;
сам Unity Editor и сборка не нужны):

1. <https://unity.com/download> → Unity Hub → войти в аккаунт Unity.
2. Hub → **Preferences (⚙) → Licenses → Add → Get a free personal license**.
   Обязательно нажать **Add** и пройти активацию — иначе файл не появится.
3. Забрать файл:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
4. Открыть блокнотом, скопировать **всё содержимое** → секрет `UNITY_LICENSE`.

Лицензия не привязана к версии Unity и к ОС: активировали на Windows — собираем на Linux.

> Ручная активация через `.alf`/`license.unity3d.com/manual` для **Unity Personal больше
> не поддерживается** (официально: только Enterprise/Industry и legacy-серийники).
> Поэтому `.ulf` берётся из Unity Hub.

### Путь B — Unity Build Automation (совсем без установок)

Если не хочется ставить даже Unity Hub: облачная сборка самой Unity.

1. <https://cloud.unity.com> → **Build Automation** → подключить этот GitHub-репозиторий.
2. Создать конфигурацию: target **Windows**, версия **6000.0.58f1**, ветка `main`.
3. Запустить билд — Unity соберёт exe в своём облаке.

Лимит бесплатного тарифа — **200 Windows-минут в месяц** (сборка этого проекта ~10–15 мин,
то есть ~15 сборок в месяц). Проект к этому готов: сцена и Build Settings создаются
pre-build хуком `NPPPreBuild` при любой сборке, отдельная настройка не нужна.

### Чего не будет работать

- «Собрать без Unity» нельзя: игра использует Unity-API, а не только C#.
- Чужая/публичная лицензия — нарушение EULA и бан аккаунта.

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
4. собирает `NPPControlRoom.exe` в путь, который передаёт GameCI (`-customBuildPath` —
   это уже полный путь к exe; `buildName: NPPControlRoom` задаёт имя файла).

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
