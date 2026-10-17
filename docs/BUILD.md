# Сборка

## Требования

| Что | Версия |
|---|---|
| Unity | 6 LTS (6000.0.x), Built-in Render Pipeline |
| Модуль | Windows Build Support (Mono) |
| ОС | Windows 10/11 x64 (или Linux/macOS с Unity) |
| .NET | поставляется с Unity |

Проект использует только встроенные модули Unity + `com.unity.ugui`; сторонних пакетов нет.

## Сборка в редакторе

1. Unity Hub → **Add project from disk** → `unity/NPPControlRoom`.
2. При первом открытии скрипт `Assets/Editor/NPPBuildTools.cs`:
   - создаст `Assets/Scenes/Main.unity` (пустая сцена — всё строится в рантайме),
   - добавит её в Build Settings,
   - выставит Player Settings (имя продукта, окно 1600×900, Linear).
3. Меню **NPP ▶ Собрать Windows x64**.
4. Результат: `unity/NPPControlRoom/Build/Windows/NPPControlRoom.exe`
   (папка `NPPControlRoom_Data` рядом — копировать целиком).

Запускать игру в редакторе можно кнопкой Play — сцена пустая, `Bootstrap` сам создаёт
`GameManager`.

## Ручная проверка Build Settings

*File → Build Settings* → платформа Windows, x86_64, сцена `Assets/Scenes/Main.unity`
отмечена галочкой. Если сцены нет — вызовите **NPP ▶ Подготовить проект**.

## Изменение настроек

| Что | Где |
|---|---|
| Адрес релея по умолчанию | `Assets/Resources/server.json` |
| Чувствительность/громкость по умолчанию | код `FirstPersonPlayer`, `SoundBank` |
| Качество графики | `QualitySettings` (shadowDistance в GameManager) |
| Разрешение/окно | `NPPBuildTools.ApplyPlayerSettings` |

## Сборка в GitHub Actions (основной способ)

Собирать локально не нужно. Каждый пуш в `main` запускает
`.github/workflows/build.yml`: предпроверка секретов → сборка Unity в контейнере →
zip → артефакт. По тегу `v*` дополнительно создаётся релиз.

1. Один раз добавить секреты `UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_LICENSE`
   (или `UNITY_SERIAL` для Plus/Pro) — как получить `.ulf`, см. **[docs/CI.md](CI.md)**.
2. Actions → **Build Windows x64** → **Run workflow** (или просто push).
3. Скачать **Artifacts → NPPControlRoom-Windows-x64**, распаковать, запустить `NPPControlRoom.exe`.

Сборка идёт на Linux-раннере с бэкендом **Mono2x** — IL2CPP-тулчейн не требуется.
Подробности, релизы и разбор ошибок CI — в [docs/CI.md](CI.md).

## Сборка в редакторе (по желанию)

Если Unity всё-таки есть под рукой — меню **NPP ▶ Собрать Windows x64** собирает тем же
кодом, что и CI (`BuildCI`).

## Частые проблемы

| Проблема | Решение |
|---|---|
| Розовые материалы | не скомпилировались шейдеры; перезапустите Unity, проверьте консоль |
| «Shader 'NPP/Lit' not found» | шейдеры лежат в `Assets/Resources/Shaders`, должны импортироваться автоматически |
| Пустая сцена при Play | это нормально: мир строится кодом |
| Отсутствует `LegacyRuntime.ttf` | шрифт DejaVu уже включён; при ошибке — переимпортируйте TTF |
| Ошибка ugui 2.0.0 на Unity 2022 | замените версию в `Packages/manifest.json` на `1.0.0` |
