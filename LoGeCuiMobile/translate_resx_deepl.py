import os
import time
import requests
import xml.etree.ElementTree as ET
from copy import deepcopy

# =========================
# CONFIG
# =========================

# Dossier du script (permet de lancer depuis n'importe où)
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))

# Mets ta clé DeepL ici (option A) OU en variable d'environnement DEEPL_API_KEY (option B)
# Option A (simple) : colle ta clé entre guillemets
DEEPL_API_KEY = "06e6cf18-7959-4a0a-b6d4-5d95fb180700:fx"

# Fichier source (FR)
SOURCE_RESX = os.path.join(SCRIPT_DIR, "Resources", "Lang", "AppResources.resx")

# Dossier de sortie (même dossier que le source)
OUT_DIR = os.path.dirname(SOURCE_RESX)

# DeepL endpoint
# Si tu es en API Free -> api-free.deepl.com
DEEPL_ENDPOINT = "https://api-free.deepl.com/v2/translate"
# Si tu es en API Pro -> décommente la ligne suivante
# DEEPL_ENDPOINT = "https://api.deepl.com/v2/translate"

# Langues cibles (suffix fichier -> target_lang DeepL)
TARGET_LANGS = {
    "en": "EN",
    "de": "DE",
    "es": "ES",
    "it": "IT",
    "nl": "NL",
    "pl": "PL",
    "ro": "RO",
    "cs": "CS",
    "tr": "TR",
    "ja": "JA",
    "ko": "KO",
    "zh": "ZH",      # Chinois (simplifié, selon support DeepL)
    "pt-BR": "PT-BR",
    "pt-PT": "PT-PT",
    "ru": "RU",
    "uk": "UK",
    "id": "ID",
    "sv": "SV",
    "da": "DA",
    "fi": "FI",
    "nb": "NB",
}

# Limite simple pour éviter d'enchaîner trop vite
SLEEP_BETWEEN_CALLS_SEC = 0.35

# =========================
# HELPERS
# =========================

def ensure_key():
    if not DEEPL_API_KEY:
        raise RuntimeError(
            "Clé API DeepL manquante.\n"
            "➡️ Soit tu définis une variable d'environnement DEEPL_API_KEY,\n"
            "➡️ soit tu décommentes la ligne DEEPL_API_KEY = \"COLLE_TA_CLE_ICI:fx\""
        )


def is_string_data_elem(data_elem: ET.Element) -> bool:
    """Ne traduit que les <data><value> string, pas les binary/type."""
    if data_elem.tag != "data":
        return False
    if "type" in data_elem.attrib or "mimetype" in data_elem.attrib:
        return False
    val = data_elem.find("value")
    if val is None or val.text is None:
        return False
    return True


def deepl_translate(text: str, target_lang: str) -> str:
    if not text.strip():
        return text

    headers = {
        "Authorization": f"DeepL-Auth-Key {DEEPL_API_KEY}",
        "Content-Type": "application/x-www-form-urlencoded",
    }

    data = {
        "text": text,
        "target_lang": target_lang,
        "source_lang": "FR",
    }

    resp = requests.post(DEEPL_ENDPOINT, headers=headers, data=data, timeout=30)

    if resp.status_code != 200:
        print(f"DeepL error {resp.status_code}: {resp.text}")

    resp.raise_for_status()
    j = resp.json()
    return j["translations"][0]["text"]



def load_resx(path: str) -> ET.ElementTree:
    return ET.parse(path)


def write_resx(tree: ET.ElementTree, out_path: str) -> None:
    tree.write(out_path, encoding="utf-8", xml_declaration=True)


# =========================
# MAIN
# =========================

def main():
    ensure_key()

    if not os.path.isfile(SOURCE_RESX):
        raise FileNotFoundError(f"Source resx introuvable: {SOURCE_RESX}")

    src_tree = load_resx(SOURCE_RESX)
    src_root = src_tree.getroot()

    source_strings = []
    for data in src_root.findall("data"):
        if is_string_data_elem(data):
            key = data.attrib.get("name", "")
            val = data.find("value").text or ""
            source_strings.append((key, val))

    print(f"✅ {len(source_strings)} clés string trouvées dans {SOURCE_RESX}")

    for lang_suffix, deepl_target in TARGET_LANGS.items():
        out_file = os.path.join(OUT_DIR, f"AppResources.{lang_suffix}.resx")
        print(f"\n🌍 Génération: {out_file} (DeepL target={deepl_target})")

        out_root = deepcopy(src_root)
        data_map = {d.attrib.get("name"): d for d in out_root.findall("data") if d.attrib.get("name")}

        translated_count = 0

        for key, fr_text in source_strings:
            data_elem = data_map.get(key)
            if data_elem is None:
                continue

            val_elem = data_elem.find("value")
            if val_elem is None or val_elem.text is None:
                continue

            try:
                translated = deepl_translate(fr_text, deepl_target)
                val_elem.text = translated
                translated_count += 1
            except Exception as ex:
                print(f"  ⚠️ Traduction échouée pour '{key}': {ex}")
                val_elem.text = fr_text

            time.sleep(SLEEP_BETWEEN_CALLS_SEC)

        out_tree = ET.ElementTree(out_root)
        write_resx(out_tree, out_file)
        print(f"✅ {translated_count} clés traduites -> {out_file}")

    print("\n🎉 Terminé.")


if __name__ == "__main__":
    main()
