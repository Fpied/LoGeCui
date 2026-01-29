import os
import xml.etree.ElementTree as ET
from datetime import datetime

# ====== CONFIG ======
# Chemin vers le dossier de ton projet MAUI (celui qui contient le .csproj)
PROJECT_DIR = r"./LoGeCuiMobile"

# Dossier cible des .resx
LANG_DIR = os.path.join(PROJECT_DIR, "Resources", "Lang")

# Nom de base des ressources
BASE_NAME = "AppResources"

# Langues à générer (ajoute/enlève ce que tu veux)
# None => AppResources.resx (fichier neutre / par défaut)
LANGS = [
    None,  # fichier par défaut AppResources.resx

    # Europe
    "fr","en","de","it","es","pt","nl","el","sv","da","fi","no","is",
    "pl","cs","sk","hu","ro","bg","hr","sr","sl","et","lv","lt","uk","ru",

    # Moyen-Orient / Afrique
    "ar","he","fa","ur","sw","af","zu","xh","am",

    # Asie
    "hi","bn","ta","te","mr","gu","pa","ml","kn",
    "th","vi","id","ms","fil",
    "zh-Hans","zh-Hant","ja","ko",

    # Autres variantes utiles
    "en-AU","en-CA","en-GB",
    "es-MX","es-AR","es-CO",
    "pt-BR"
]

# Les clés de ton UI (valeurs de base — ici en français)
STRINGS_BASE = {
    "Email": "Email",
    "Password": "Mot de passe",
    "StayConnected": "Rester connecté",
    "LoginButton": "Se connecter",
    "SignupButton": "Créer un compte",
    "ForgotPassword": "Mot de passe oublié ?",
    "ErrorTitle": "Erreur",
    "BadPassword": "Mauvais mot de passe",
}
# =====================


def resx_filename(lang: str | None) -> str:
    if lang is None:
        return f"{BASE_NAME}.resx"
    return f"{BASE_NAME}.{lang}.resx"


def make_resx_root() -> ET.Element:
    # Format resx minimal compatible
    root = ET.Element("root")

    def resheader(name: str, value: str):
        h = ET.SubElement(root, "resheader", {"name": name})
        ET.SubElement(h, "value").text = value

    resheader("resmimetype", "text/microsoft-resx")
    resheader("version", "2.0")
    resheader(
        "reader",
        "System.Resources.ResXResourceReader, System.Windows.Forms, "
        "Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
    )
    resheader(
        "writer",
        "System.Resources.ResXResourceWriter, System.Windows.Forms, "
        "Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
    )

    return root


def add_string(root: ET.Element, key: str, value: str):
    data = ET.SubElement(root, "data", {"name": key, "xml:space": "preserve"})
    ET.SubElement(data, "value").text = value


def indent(elem: ET.Element, level: int = 0):
    # Pretty print XML
    i = "\n" + level * "  "
    if len(elem):
        if not elem.text or not elem.text.strip():
            elem.text = i + "  "
        for e in elem:
            indent(e, level + 1)
        if not elem.tail or not elem.tail.strip():
            elem.tail = i
    else:
        if level and (not elem.tail or not elem.tail.strip()):
            elem.tail = i


def main():
    os.makedirs(LANG_DIR, exist_ok=True)

    created = 0
    skipped = 0

    for lang in LANGS:
        filename = resx_filename(lang)
        path = os.path.join(LANG_DIR, filename)

        # Sécurité: ne pas écraser si déjà présent
        if os.path.exists(path):
            print(f"SKIP (exists): {path}")
            skipped += 1
            continue

        root = make_resx_root()

        for k, v in STRINGS_BASE.items():
            add_string(root, k, v)

        indent(root)
        ET.ElementTree(root).write(path, encoding="utf-8", xml_declaration=True)

        print(f"CREATED: {path}")
        created += 1

    print("\nDone.")
    print(f"Created: {created} | Skipped: {skipped}")
    print(f"Output dir: {LANG_DIR}")
    print(f"Timestamp: {datetime.now().isoformat(timespec='seconds')}")


if __name__ == "__main__":
    main()
