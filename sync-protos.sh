#!/usr/bin/env bash
# Sincroniza los .proto que ms-notification consume desde ms-route y
# ms-user-management.
#
# El .proto es el contrato entre servicios, pero cada servicio corre su propio
# generador: no se comparte el artefacto generado. Esta copia reescribe
# `option csharp_namespace` para que las clases cliente vivan en el namespace de
# quien las consume y no en el del servidor (ms_route.Api.* /
# ms_user_management.Api.*). Es la unica linea que cambia: servicio, mensajes,
# campos y numeros de campo son identicos, asi que ambos lados siguen hablando
# exactamente el mismo protocolo.
#
# Uso: ./sync-protos.sh   (desde backend/ms-notification-qr)
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$HERE/src/ms-notification.Api/Protos"

sync() {
  local source_dir="$1"
  local name="$2"
  local out="$TARGET/$name"

  sed 's|^option csharp_namespace = .*|option csharp_namespace = "ms_notification.Api.Infrastructure.Grpc";|' \
    "$source_dir/$name" > "$out"
  echo "sincronizado: $name -> $out"
}

mkdir -p "$TARGET"

sync "$HERE/../ms-route/src/ms-route.Api/Protos" "route.proto"
sync "$HERE/../ms-user-management/src/ms-user-management.Api/Protos" "family.proto"
