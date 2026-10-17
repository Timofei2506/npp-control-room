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

## Один раз: учётка Unity

Unity не собирает без лицензии, но **файл `.ulf` для Unity Personal больше не нужен**:
Unity убрал ручную активацию Personal, а GameCI научился поднимать Personal-место
прямо по логину и паролю (через Unity Licensing Client). Поэтому достаточно двух секретов.

**Settings → Secrets and variables → Actions → New repository secret**:

| Секрет | Обязателен | Что это |
|---|---|---|
| `UNITY_EMAIL` | **да** | почта аккаунта Unity |
| `UNITY_PASSWORD` | **да** | пароль (лучше без спецсимволов — известная проблема GameCI) |
| `UNITY_SERIAL` | нет | серийник, только если у вас Unity Plus/Pro |
| `UNITY_LICENSE` | нет | содержимое `.ulf`, только для Enterprise/Industry |

Что произойдёт при сборке:

- заданы только почта и пароль → активируется **Personal**-место, после сборки возвращается;
- задан `UNITY_SERIAL` → активация **Plus/Pro**;
- задан `UNITY_LICENSE` → используется файл лицензии (**Enterprise/Industry** или
  self-hosted раннер с совпадающим `Unity_lic.ulf`).

> ⚠️ Пароль вводите **сами** прямо в GitHub Secrets. Не присылайте его в чат —
> всё, что попадает в переписку, считается скомпрометированным.
>
> Если у аккаунта включена двухфакторная аутентификация, активация по паролю может
> не пройти — тогда либо временный пароль приложения, либо отдельный аккаунт Unity
> только для CI, либо путь B ниже.

### Если очень нужен `.ulf` (Enterprise/Industry)

Только для этих лицензий: Unity Hub → *Preferences → Licenses → Add → Activate with
license request* → `.alf` → <https://license.unity3d.com/manual> → `.ulf` → вставить
содержимое в секрет `UNITY_LICENSE`. Для Personal этот путь закрыт со стороны Unity.

### Путь B — Unity Build Automation (без ключей вообще)

1. <https://cloud.unity.com> → **Build Automation** → подключить этот GitHub-репозиторий.
2. Конфигурация: target **Windows**, ветка `main`, версия определяется из проекта.
3. Запустить — Unity соберёт exe в своём облаке.

Бесплатный тариф: **200 Windows-минут в месяц** (сборка ~10–15 мин). Проект готов:
pre-build хук `NPPPreBuild` сам создаёт сцену и Build Settings.

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
