#!/usr/bin/env bash
# Installe ou met à jour FastFood comme LaunchAgent macOS.
# Variables : FASTFOOD_LABEL, FASTFOOD_PORT, FASTFOOD_DONNEES, FASTFOOD_JOURNAUX.
set -euo pipefail

ICI="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RACINE="$(cd "$ICI/.." && pwd)"
# shellcheck source=lib.sh
source "$ICI/lib.sh"
charger_configuration || exit 1

DOMAINE="gui/$(id -u)"
SERVICE="$DOMAINE/$LABEL"
URL="http://127.0.0.1:$PORT"
DELAI_SANTE=60
DELAI_DECHARGEMENT=10
ESSAIS_BOOTSTRAP=3

etape() { echo "[$1/5] $2"; }
echec() { echo "Erreur : $*" >&2; exit 1; }

# --- 1. Préalables ----------------------------------------------------------
verifier_prealables() {
  [ "$(uname -s)" = "Darwin" ] || echec "ce script ne s'exécute que sur macOS."
  local outil
  for outil in launchctl lsof curl; do
    command -v "$outil" >/dev/null || echec "$outil introuvable."
  done
  DOTNET="$(command -v dotnet || true)"
  [ -n "$DOTNET" ] || { [ -x "$HOME/.dotnet/dotnet" ] && DOTNET="$HOME/.dotnet/dotnet"; } \
    || echec "dotnet introuvable (ni dans le PATH, ni dans ~/.dotnet)."
  RID="$(rid_pour_arch "$(uname -m)")" || echec "architecture $(uname -m) non prise en charge."
  echo "  dotnet : $DOTNET, cible : $RID"
}

# --- 2. Port ----------------------------------------------------------------
pid_du_service() {
  { launchctl print "$SERVICE" 2>/dev/null || true; } | awk '$1 == "pid" && $2 == "=" { print $3; exit }'
}

verifier_port() {
  local pids etranger
  pids="$(lsof -nP -tiTCP:"$PORT" -sTCP:LISTEN 2>/dev/null || true)"
  if [ -n "$pids" ]; then
    # shellcheck disable=SC2086 # un PID par mot
    etranger="$(premier_pid_etranger "$(pid_du_service)" $pids)"
    [ -z "$etranger" ] || echec "le port $PORT est occupé par $(ps -o comm= -p "$etranger" 2>/dev/null || echo "un processus inconnu") (PID $etranger) : arrêtez-le ou choisissez un autre port avec FASTFOOD_PORT."
  fi
  echo "  port $PORT libre ou tenu par $LABEL"
}

# --- 3. Publication ---------------------------------------------------------
publier() {
  rm -rf "$APP_NOUVEAU"
  mkdir -p "$DONNEES" "$JOURNAUX"
  "$DOTNET" publish "$RACINE/src/FastFood" -c Release -r "$RID" --self-contained -o "$APP_NOUVEAU" --nologo \
    || echec "la publication a échoué."
  [ -x "$APP_NOUVEAU/FastFood" ] || echec "exécutable absent après publication : $APP_NOUVEAU/FastFood"
}

# --- 4. Bascule -------------------------------------------------------------
service_charge() { launchctl print "$SERVICE" >/dev/null 2>&1; }
service_decharge() { ! service_charge; }

arreter_service() {
  service_charge || return 0
  echo "  arrêt de $LABEL"
  launchctl bootout "$SERVICE" 2>/dev/null || true
  attendre_jusqu_a "$DELAI_DECHARGEMENT" service_decharge \
    || echec "$LABEL est toujours chargé $DELAI_DECHARGEMENT s après launchctl bootout."
}

# Quelques essais : launchd peut refuser juste après un bootout.
demarrer_service() {
  attendre_jusqu_a "$ESSAIS_BOOTSTRAP" launchctl bootstrap "$DOMAINE" "$PLIST_AGENT"
}

# La version en place devient app.precedent/ ; le retour arrière la remet en service.
remplacer_application() {
  rm -rf "$APP_PRECEDENT"
  [ ! -d "$APP" ] || mv "$APP" "$APP_PRECEDENT"
  mv "$APP_NOUVEAU" "$APP"
}

ecrire_plist() {
  mkdir -p "$(dirname "$PLIST_AGENT")"
  generer_plist "$LABEL" "$APP" "$BASE" "$PORT" "$JOURNAL" > "$PLIST_AGENT"
}

basculer() {
  arreter_service
  remplacer_application
  ecrire_plist
  demarrer_service && echo "  $LABEL chargé depuis $PLIST_AGENT" && return
  echo "  launchctl bootstrap a échoué $ESSAIS_BOOTSTRAP fois" >&2
  revenir_en_arriere
}

# --- 5. Santé et retour arrière ---------------------------------------------
repond() { curl -fsS -o /dev/null --max-time 2 "$URL/api/sante" 2>/dev/null; }
attendre_sante() { attendre_jusqu_a "$DELAI_SANTE" repond; }

revenir_en_arriere() {
  [ -d "$APP_PRECEDENT" ] || { arreter_service; echec "la nouvelle version ne répond pas et il n'y a pas de version précédente : service arrêté (journal : $JOURNAL)."; }
  echo "  retour à la version précédente" >&2
  arreter_service
  rm -rf "$APP"
  mv "$APP_PRECEDENT" "$APP"
  demarrer_service || echec "launchctl bootstrap échoue aussi pour l'ancienne version. À relancer : launchctl bootstrap $DOMAINE \"$PLIST_AGENT\""
  attendre_sante && echec "la nouvelle version n'a pas démarré : l'ancienne a repris la main sur $URL (journal : $JOURNAL)."
  echec "retour arrière effectué mais l'ancienne version ne répond pas non plus (journal : $JOURNAL)."
}

verifier_sante() {
  attendre_sante && echo "  $URL/api/sante répond" && return
  echo "  $URL ne répond pas après $DELAI_SANTE s" >&2
  tail -n 40 "$JOURNAL" >&2 2>/dev/null || echo "(journal vide ou absent)" >&2
  revenir_en_arriere
}

afficher_resume() {
  echo
  echo "FastFood installé ($LABEL)."
  echo "  URL locale : $URL"
  echo "  Journal    : $JOURNAL"
  echo "  Base       : $BASE"
  echo "Pour l'exposer sur le tailnet (à lancer vous-même) :"
  echo "  tailscale serve --bg --https=8443 $URL"
}

main() {
  etape 1 "Préalables"; verifier_prealables
  etape 2 "Port $PORT"; verifier_port
  etape 3 "Publication dans $APP_NOUVEAU"; publier
  etape 4 "Bascule"; basculer
  etape 5 "Santé"; verifier_sante
  afficher_resume
}

main "$@"
