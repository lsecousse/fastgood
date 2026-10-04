#!/usr/bin/env bash
# Tests des fonctions pures de deploy/lib.sh. Lancé en CI (Linux) et sur macOS.
set -uo pipefail

ICI="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../lib.sh
source "$ICI/../lib.sh" 2>/dev/null

echecs=0
reussites=0

reussi() { reussites=$((reussites + 1)); echo "ok    - $1"; }
echoue() { echecs=$((echecs + 1)); echo "ÉCHEC - $1"; [ -n "${2:-}" ] && echo "        $2"; return 0; }

verifier_egal() {
  local nom="$1" attendu="$2" obtenu="$3"
  [ "$obtenu" = "$attendu" ] && reussi "$nom" && return
  echoue "$nom" "attendu « $attendu », obtenu « $obtenu »"
}

verifier_absent() {
  local nom="$1" motif="$2" texte="$3"
  [[ "$texte" != *"$motif"* ]] && reussi "$nom" && return
  echoue "$nom" "« $motif » présent dans : $texte"
}

verifier_echoue() { local nom="$1"; shift; "$@" >/dev/null 2>&1 || { reussi "$nom"; return; }; echoue "$nom" "la commande aurait dû échouer : $*"; }

# Ligne suivant <key>$2</key> dans le plist $1 (rang $3, 1 par défaut), sans les espaces de tête.
valeur_apres_cle() {
  printf '%s\n' "$1" | grep -A"${3:-1}" -F "<key>$2</key>" | tail -n 1 | sed -e 's/^[[:space:]]*//'
}

# --- rid_pour_arch ---------------------------------------------------------
verifier_egal "RidPourArchShould.DonnerOsxX64PourIntel" "osx-x64" "$(rid_pour_arch x86_64 2>/dev/null)"
verifier_egal "RidPourArchShould.DonnerOsxArm64PourAppleSilicon" "osx-arm64" "$(rid_pour_arch arm64 2>/dev/null)"
verifier_echoue "RidPourArchShould.RefuserUneArchitectureInconnue" rid_pour_arch i386

# --- echapper_xml ----------------------------------------------------------
verifier_egal "EchapperXmlShould.EchapperEsperluetteEtChevrons" "a &amp; &lt;b&gt;" "$(echapper_xml "a & <b>" 2>/dev/null)"

# --- generer_plist ---------------------------------------------------------
APP="/Users/test/Library/Application Support/FastFood/app"
BASE="/Users/test/Library/Application Support/Fast & Food/fastfood.db"
JOURNAL="/Users/test/Library/Logs/FastFood/fastfood.log"
PLIST="$(generer_plist net.linkeat.fastfood "$APP" "$BASE" 5080 "$JOURNAL" 2>/dev/null)"

verifier_egal "GenererPlistShould.PorterLeLabel" "<string>net.linkeat.fastfood</string>" "$(valeur_apres_cle "$PLIST" Label)"
verifier_egal "GenererPlistShould.EcouterSurLePortDonneEnLocal" "<string>http://127.0.0.1:5080</string>" "$(valeur_apres_cle "$PLIST" Urls)"
verifier_absent "GenererPlistShould.NePasUtiliserAspnetcoreUrls" "ASPNETCORE_URLS" "$PLIST"
verifier_egal "GenererPlistShould.DonnerLeCheminDeLaBaseEchappe" "<string>/Users/test/Library/Application Support/Fast &amp; Food/fastfood.db</string>" "$(valeur_apres_cle "$PLIST" FastFood__Base)"
verifier_egal "GenererPlistShould.FixerLEnvironnementProduction" "<string>Production</string>" "$(valeur_apres_cle "$PLIST" ASPNETCORE_ENVIRONMENT)"
verifier_egal "GenererPlistShould.TravaillerDansLeDossierDeLApplication" "<string>$APP</string>" "$(valeur_apres_cle "$PLIST" WorkingDirectory)"
verifier_egal "GenererPlistShould.LancerLExecutableFastFood" "<string>$APP/FastFood</string>" "$(valeur_apres_cle "$PLIST" ProgramArguments 2)"
verifier_egal "GenererPlistShould.DemarrerALOuvertureDeSession" "<true/>" "$(valeur_apres_cle "$PLIST" RunAtLoad)"
verifier_egal "GenererPlistShould.RelancerLeServiceTombe" "<true/>" "$(valeur_apres_cle "$PLIST" KeepAlive)"
verifier_egal "GenererPlistShould.EspacerLesRelancesDeDixSecondes" "<integer>10</integer>" "$(valeur_apres_cle "$PLIST" ThrottleInterval)"
verifier_egal "GenererPlistShould.EcrireLaSortieDansLeJournal" "<string>$JOURNAL</string>" "$(valeur_apres_cle "$PLIST" StandardOutPath)"
verifier_egal "GenererPlistShould.EcrireLesErreursDansLeJournal" "<string>$JOURNAL</string>" "$(valeur_apres_cle "$PLIST" StandardErrorPath)"

if command -v plutil >/dev/null; then
  FICHIER_PLIST="$(mktemp)"
  printf '%s\n' "$PLIST" > "$FICHIER_PLIST"
  plutil -lint "$FICHIER_PLIST" >/dev/null 2>&1 && reussi "GenererPlistShould.ProduireUnPlistValide" || echoue "GenererPlistShould.ProduireUnPlistValide"
  rm -f "$FICHIER_PLIST"
else
  echo "info  - plutil absent (hors macOS) : validation du plist ignorée"
fi

# --- charger_configuration -------------------------------------------------
config_par_defaut() { (unset FASTFOOD_LABEL FASTFOOD_PORT FASTFOOD_DONNEES FASTFOOD_JOURNAUX; HOME=/h; charger_configuration && echo "$LABEL|$PORT|$APP|$APP_PRECEDENT|$APP_NOUVEAU|$BASE|$JOURNAL|$PLIST_AGENT"); }
config_d_essai() { (FASTFOOD_LABEL=essai FASTFOOD_PORT=5081 FASTFOOD_DONNEES=/d FASTFOOD_JOURNAUX=/j; HOME=/h; charger_configuration && echo "$LABEL|$PORT|$APP|$BASE|$JOURNAL|$PLIST_AGENT"); }
config_avec() { (FASTFOOD_DONNEES="$1" FASTFOOD_JOURNAUX="$2"; HOME=/h; charger_configuration); }
verifier_egal "ChargerConfigurationShould.PrendreLesEmplacementsMacOSParDefaut" \
  "net.linkeat.fastfood|5080|/h/Library/Application Support/FastFood/app|/h/Library/Application Support/FastFood/app.precedent|/h/Library/Application Support/FastFood/app.nouveau|/h/Library/Application Support/FastFood/fastfood.db|/h/Library/Logs/FastFood/fastfood.log|/h/Library/LaunchAgents/net.linkeat.fastfood.plist" \
  "$(config_par_defaut 2>/dev/null)"
verifier_egal "ChargerConfigurationShould.SuivreLesVariablesFastfood" \
  "essai|5081|/d/app|/d/fastfood.db|/j/fastfood.log|/h/Library/LaunchAgents/essai.plist" \
  "$(config_d_essai 2>/dev/null)"
verifier_echoue "ChargerConfigurationShould.RefuserDesDonneesEnCheminRelatif" config_avec "donnees" "/j"
verifier_echoue "ChargerConfigurationShould.RefuserDesJournauxEnCheminRelatif" config_avec "/d" "journaux"

# --- premier_pid_etranger --------------------------------------------------
verifier_egal "PremierPidEtrangerShould.DonnerLeSeulOccupant" "777" "$(premier_pid_etranger 412 777 2>/dev/null)"
verifier_egal "PremierPidEtrangerShould.IgnorerLeServiceDuLabel" "777" "$(premier_pid_etranger 412 412 777 2>/dev/null)"
verifier_egal "PremierPidEtrangerShould.NeRienDonnerSiSeulLeServiceEcoute" "" "$(premier_pid_etranger 412 412 2>/dev/null)"

echo
echo "$reussites réussi(s), $echecs échec(s)"
[ "$echecs" -eq 0 ]
