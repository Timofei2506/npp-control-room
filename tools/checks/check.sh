#!/usr/bin/env bash
# Офлайн-проверка проекта без Unity:
#   1) компиляция всех C# скриптов (runtime + editor) против заглушек Unity;
#   2) прогон эталонных сценариев физики;
#   3) опционально — сетевой smoke-тест против релея (нужен mono + Node-релей).
#
# Требования: mono (mcs), node. Запуск: tools/checks/check.sh [ws-url]
set -euo pipefail
cd "$(dirname "$0")/../.."

SRC=$(find unity/NPPControlRoom/Assets -name '*.cs' | sort)
STUBS="tools/unity-stubs/UnityStubs.cs tools/unity-stubs/UnityEditorStubs.cs"
OUT=$(mktemp -d)

echo "== 1/3 Компиляция C# (Unity-заглушки, включая Editor) =="
mcs -target:library -langversion:latest -define:UNITY_EDITOR -nowarn:0169,0414,0649,0219,0168 \
    -out:"$OUT/all.dll" $STUBS $SRC -r:System.dll -r:System.Core.dll
echo "   OK — ${SRC//$'\n'/ } "

echo "== 2/3 Эталонные сценарии физики =="
node tools/sim-prototype/physics.js

if [ $# -ge 1 ] && [ -n "$1" ]; then
  echo "== 3/3 Сетевой smoke-тест: $1 =="
  mcs -langversion:latest -out:"$OUT/net.exe" tools/unity-stubs/UnityStubs.cs \
      unity/NPPControlRoom/Assets/Scripts/Net/MiniJson.cs \
      unity/NPPControlRoom/Assets/Scripts/Net/RelayClient.cs \
      unity/NPPControlRoom/Assets/Scripts/Net/NetworkSession.cs \
      unity/NPPControlRoom/Assets/Scripts/Sim/PlantSimulation.cs \
      unity/NPPControlRoom/Assets/Scripts/Sim/CommandState.cs \
      tools/checks/NetSmoke.cs -r:System.dll
  mono "$OUT/net.exe" "$1"
else
  echo "== 3/3 Сетевой smoke-тест пропущен (укажите ws-url: tools/checks/check.sh wss://.../ws) =="
fi
