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
# Uso: ./sync-protos.sh
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="$HERE/src/ms-notification.Api/Protos"

# Ojo con route: en ms-route, `route.proto` es el contrato CLIENTE de ms-fleet
# (package route.v1) y el contrato SERVIDOR que consumimos aqui vive en
# `route_server.proto` (package route). Por eso este caso tiene un nombre de
# origen distinto al de destino; family si comparte nombre.
sync() {
  local source_dir="$1"
  local source_name="$2"
  local target_name="$3"

  sed 's|^option csharp_namespace = .*|option csharp_namespace = "ms_notification.Api.Infrastructure.Grpc";|' \
    "$source_dir/$source_name" > "$TARGET/$target_name"
  echo "sincronizado: $source_name -> $TARGET/$target_name"
}

mkdir -p "$TARGET"

sync "$HERE/../ms-route/src/ms-route.Api/Protos" "route_server.proto" "route.proto"
sync "$HERE/../ms-user-management/src/ms-user-management.Api/Protos" "family.proto" "family.proto"
