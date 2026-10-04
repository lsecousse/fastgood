#!/usr/bin/env bash
# Fonctions pures de l'installateur, testées par deploy/tests/installer.test.sh (Linux et macOS).

# Emplacements du service, surchargeables par FASTFOOD_* pour les essais.
charger_configuration() {
  LABEL="${FASTFOOD_LABEL:-net.linkeat.fastfood}"
  PORT="${FASTFOOD_PORT:-5080}"
  DONNEES="${FASTFOOD_DONNEES:-$HOME/Library/Application Support/FastFood}"
  JOURNAUX="${FASTFOOD_JOURNAUX:-$HOME/Library/Logs/FastFood}"
  APP="$DONNEES/app"
  APP_PRECEDENT="$DONNEES/app.precedent"
  APP_NOUVEAU="$DONNEES/app.nouveau"
  BASE="$DONNEES/fastfood.db"
  JOURNAL="$JOURNAUX/fastfood.log"
  PLIST_AGENT="$HOME/Library/LaunchAgents/$LABEL.plist"
  exiger_chemin_absolu FASTFOOD_DONNEES "$DONNEES" && exiger_chemin_absolu FASTFOOD_JOURNAUX "$JOURNAUX"
}

# launchd n'accepte que des chemins absolus.
exiger_chemin_absolu() {
  [[ "$2" == /* ]] && return 0
  echo "$1 doit être un chemin absolu : $2" >&2
  return 1
}

# Premier PID (après le premier argument, PID du service du label) qui n'est pas ce service.
premier_pid_etranger() {
  local service="$1" pid
  shift
  for pid in "$@"; do
    [ "$pid" = "$service" ] || { echo "$pid"; return 0; }
  done
}

# Relance la commande toutes les secondes jusqu'à sa réussite ou l'échéance (en secondes écoulées).
attendre_jusqu_a() {
  local delai="$1" debut=$SECONDS
  shift
  while [ $((SECONDS - debut)) -lt "$delai" ]; do
    "$@" && return 0
    sleep 1
  done
  return 1
}

rid_pour_arch() {
  case "$1" in
    x86_64) echo "osx-x64" ;;
    arm64) echo "osx-arm64" ;;
    *) echo "Architecture non prise en charge : $1" >&2; return 1 ;;
  esac
}

# sed plutôt que ${x//…} : bash 5.2 donne un sens à « & » dans le remplacement.
echapper_xml() {
  printf '%s' "$1" | sed -e 's/&/\&amp;/g' -e 's/</\&lt;/g' -e 's/>/\&gt;/g'
}

# `Urls` et non ASPNETCORE_URLS : la clé Urls d'appsettings.json écraserait cette dernière.
generer_plist() {
  local label app base port journal
  label="$(echapper_xml "$1")"
  app="$(echapper_xml "$2")"
  base="$(echapper_xml "$3")"
  port="$(echapper_xml "$4")"
  journal="$(echapper_xml "$5")"
  cat <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>Label</key>
  <string>$label</string>
  <key>ProgramArguments</key>
  <array>
    <string>$app/FastFood</string>
  </array>
  <key>WorkingDirectory</key>
  <string>$app</string>
  <key>EnvironmentVariables</key>
  <dict>
    <key>ASPNETCORE_ENVIRONMENT</key>
    <string>Production</string>
    <key>Urls</key>
    <string>http://127.0.0.1:$port</string>
    <key>FastFood__Base</key>
    <string>$base</string>
  </dict>
  <key>RunAtLoad</key>
  <true/>
  <key>KeepAlive</key>
  <true/>
  <key>ThrottleInterval</key>
  <integer>10</integer>
  <key>StandardOutPath</key>
  <string>$journal</string>
  <key>StandardErrorPath</key>
  <string>$journal</string>
</dict>
</plist>
PLIST
}
