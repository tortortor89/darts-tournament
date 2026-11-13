"""Extrait la grille FSGT (formats de tableaux selon le nombre d'inscrits) vers un fichier JSON.

Source : onglet « Feuil1 » de « 2 Répartition des Joueurs par poules.xlsx » (5 à 80 joueurs).
Pour chaque nombre de joueurs, le classeur propose une ou plusieurs variantes :
composition des poules, qualifiés par poule, tour de sortie et affiches du tableau final.

Seules les variantes sans barrage (« prélim » 701) sont gardées. Une variante par nombre de
joueurs est marquée `default` (tour de sortie attendu, voir EXPECTED_STAGE). Le JSON produit
est la référence : il peut être retouché à la main (déplacer `default`, corriger une affiche).

Usage : python tools/fsgt/extract_fsgt_formats.py [xlsx] [json]
"""

import json
import re
import sys
from math import comb
from pathlib import Path

import openpyxl

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_XLSX = REPO_ROOT.parent / "docs-fsgt-flechettes" / "2 Répartition des Joueurs par poules.xlsx"
DEFAULT_JSON = REPO_ROOT / "backend" / "DartsTournament.Api" / "Data" / "fsgt-formats.json"

STAGES = {"demi": 4, "1/4": 8, "8em": 16, "16em": 32}
STAGE_NAMES = {4: "SemiFinal", 8: "QuarterFinal", 16: "RoundOf16", 32: "RoundOf32"}


def expected_bracket_size(players: int) -> int | None:
    """Tour de sortie de l'onglet « Point Tournois » (41-42 : 8es, choix du 2026-11-11)."""
    if players <= 5:
        return None
    if players <= 10:
        return 4
    if players <= 20:
        return 8
    if players <= 42:
        return 16
    return 32


# Barème « Point Tournois » : points du tour atteint (ils incluent la participation).
# Éliminés en poules (et poule unique) : participation + points par victoire en poule.
# Ligne 6-10 : le classeur donne 6-8-10-12 pour 3 tours, retenu 6-8-10 (2026-11-11).
POINTS = {
    "poolElimination": {"participation": 2, "perWin": 2},
    "knockout": [
        {"minPlayers": 6, "maxPlayers": 10, "semiFinal": 6, "final": 8, "winner": 10},
        {"minPlayers": 11, "maxPlayers": 16, "quarterFinal": 8, "semiFinal": 10, "final": 12, "winner": 14},
        {"minPlayers": 17, "maxPlayers": 20, "quarterFinal": 10, "semiFinal": 12, "final": 14, "winner": 16},
        {"minPlayers": 21, "maxPlayers": 40, "roundOf16": 10, "quarterFinal": 12, "semiFinal": 14, "final": 16, "winner": 18},
        {"minPlayers": 41, "maxPlayers": 80, "roundOf32": 10, "roundOf16": 12, "quarterFinal": 14, "semiFinal": 16, "final": 18, "winner": 20},
    ],
}

# Départage en poule, dans l'ordre (wins, legDifference, headToHead, legsWon), puis ordre
# manuel de l'admin (701 joué hors appli). Choix du 2026-11-11 ; legsWon à ajouter si besoin
TIE_BREAKERS = ["wins", "legDifference", "headToHead"]

BRACKET_COLS = [7, 8, 9, 10, 12, 13, 14, 15, 17, 18, 19, 20, 22, 23, 24, 25]  # G..Y sans les séparateurs


def bracket_cells(top_row: int, pairs: str) -> dict:
    cells = {}
    for col, pair in zip(BRACKET_COLS, pairs.split()):
        top, bottom = pair.split("-")
        cells[(top_row, col)] = top
        cells[(top_row + 1, col)] = bottom
    return cells


# Erreurs de saisie de la grille, corrigées d'après les classeurs détaillés « A N inscrits » (onglet Tableau)
CELL_OVERRIDES = {
    # 43 joueurs : 2p11 en double, le classeur détaillé donne 2p7 - 2p8
    (212, 9): "2 p 8",
    # 46 joueurs : 2p13 en double et 2p9 absent ; repris du classeur détaillé (poules de 4 = 6, 7, 13, 14)
    (235, 3): "P: 1;2;3;4;5;8;9;10;11;12",
    (236, 3): "P: 6;7;13;14",
    **bracket_cells(235, "1p1-3p14 1p3-3p13 1p5-2p14 1p7-2p13 2p11-2p12 1p6-2p10 1p4-2p9 1p2-2p8 "
                         "1p8-2p7 1p9-2p6 1p11-2p5 2p3-2p4 1p10-2p2 1p12-2p1 1p13-3p7 1p14-3p6"),
}

SPEC_RE = re.compile(r"^\s*(\d+)\s*[pP]\s*(\d+)\s*$")
SLOT_RE = re.compile(r"^\s*(\d+)\s*[pP]\s*(\d+)\s*$")
QUALIF_RE = re.compile(r"(\d+)\s*Q")
MATCHES_RE = re.compile(r"(\d+)\s*matchs")
BRACKET_FIRST_COL = 7  # colonne G


class Variant:
    def __init__(self, players: int, first_row: int):
        self.players = players
        self.first_row = first_row
        self.rows: list[dict[int, object]] = []
        self.warnings: list[str] = []

    @property
    def label(self) -> str:
        return f"{self.players} joueurs (ligne {self.first_row})"


def text(value) -> str:
    return "" if value is None else str(value).strip()


def read_variants(xlsx: Path) -> list[Variant]:
    ws = openpyxl.load_workbook(xlsx, data_only=True)["Feuil1"]
    variants: list[Variant] = []
    current: Variant | None = None
    for row in ws.iter_rows():
        cells = {c.column: CELL_OVERRIDES.get((c.row, c.column), c.value) for c in row}
        cells = {col: val for col, val in cells.items() if text(val)}
        if not cells:
            continue
        a = cells.get(1)
        if isinstance(a, (int, float)):
            current = Variant(int(a), row[0].row)
            variants.append(current)
        elif "cibles" in text(cells.get(2)).lower() or text(a).lower().startswith("nbr"):
            current = None  # en-têtes de section (« 8 cibles », « 16 cibles »...)
            continue
        if current is not None:
            current.rows.append(cells)
    return variants


def has_prelim(v: Variant) -> bool:
    return any("prelim" in text(val).lower() for r in v.rows for val in r.values())


def parse_variant(v: Variant) -> dict | None:
    specs = []  # (x, size, qualifiers, explicit group numbers)
    stage = None
    stated_matches = None
    qualifiers = None
    for r in v.rows:
        if stage is None and text(r.get(6)).lower() in STAGES:
            stage = STAGES[text(r.get(6)).lower()]
        m = MATCHES_RE.search(text(r.get(1)))
        if m:
            stated_matches = int(m.group(1))
        m = SPEC_RE.match(text(r.get(2)))
        if not m:
            continue
        q = QUALIF_RE.search(text(r.get(4)))
        if q:
            qualifiers = int(q.group(1))
        if qualifiers is None:
            v.warnings.append(f"qualifiés absents pour « {text(r.get(2))} »")
            return None
        numbers = [int(n) for n in re.findall(r"\d+", text(r.get(3)))]
        specs.append((int(m.group(1)), int(m.group(2)), qualifiers, numbers))

    if not specs:
        v.warnings.append("aucune poule lisible")
        return None

    # « 2P6 » veut parfois dire « poule n° 2 de 6 » et non « 2 poules de 6 » :
    # on garde l'interprétation qui retombe sur le bon nombre de joueurs.
    counts = resolve_counts(v, specs)
    if counts is None:
        return None

    group_count = sum(counts)
    groups: list[dict | None] = [None] * group_count
    free = list(range(1, group_count + 1))
    pending = []
    for (x, size, q, numbers), count in zip(specs, counts):
        valid = len(numbers) == count and all(1 <= n <= group_count for n in numbers)
        if numbers and not valid:
            v.warnings.append(f"numéros de poules incohérents pour {count} poule(s) de {size} : {numbers}, renumérotées")
        if valid:
            for n in numbers:
                if groups[n - 1] is not None:
                    v.warnings.append(f"poule {n} attribuée deux fois")
                    return None
                groups[n - 1] = {"size": size, "qualifiers": q}
                free.remove(n)
        else:
            pending.append((count, size, q))
    for count, size, q in pending:
        for _ in range(count):
            groups[free.pop(0) - 1] = {"size": size, "qualifiers": q}

    bracket = read_bracket(v, groups)
    if bracket is None:
        return None

    bracket_size = len(bracket) * 2
    if stage is not None and stage != bracket_size:
        v.warnings.append(f"tour annoncé ({STAGE_NAMES[stage]}) différent du tableau lu ({bracket_size} places)")
        return None

    expected = {(rank, g + 1) for g, grp in enumerate(groups) for rank in range(1, grp["qualifiers"] + 1)}
    slots = [slot for match in bracket for slot in match]
    if len(set(slots)) != len(slots):
        v.warnings.append("une place du tableau est utilisée deux fois")
        return None
    if set(slots) != expected:
        missing = sorted(expected - set(slots))
        extra = sorted(set(slots) - expected)
        v.warnings.append(f"tableau incohérent avec les qualifiés (manquants {missing}, en trop {extra})")
        return None
    for g, grp in enumerate(groups):
        if grp["qualifiers"] > grp["size"]:
            v.warnings.append(f"poule {g + 1} : plus de qualifiés que de joueurs")
            return None
    for a, b in bracket:
        if a[1] == b[1]:
            v.warnings.append(f"affiche entre joueurs de la même poule : {slot_str(a)} - {slot_str(b)}")

    computed = sum(comb(g["size"], 2) for g in groups) + bracket_size - 1
    if stated_matches is not None and stated_matches != computed:
        v.warnings.append(f"nombre de matchs annoncé {stated_matches}, calculé {computed}")

    return {
        "knockout": STAGE_NAMES[bracket_size],
        "matchCount": computed,
        "groups": groups,
        "bracket": [[slot_str(a), slot_str(b)] for a, b in bracket],
    }


def resolve_counts(v: Variant, specs) -> list[int] | None:
    best = None
    for mask in range(1 << len(specs)):
        counts = [1 if mask >> i & 1 else x for i, (x, *_rest) in enumerate(specs)]
        if sum(c * s[1] for c, s in zip(counts, specs)) == v.players:
            reinterpreted = bin(mask).count("1")
            if best is None or reinterpreted < best[0]:
                best = (reinterpreted, counts)
    if best is None:
        v.warnings.append("composition des poules incompatible avec le nombre de joueurs")
        return None
    if best[0]:
        v.warnings.append("notation « N p T » lue comme « poule n° N de T joueurs » sur au moins une ligne")
    return best[1]


def read_bracket(v: Variant, groups) -> list[tuple] | None:
    lines = []
    for r in v.rows:
        cols = sorted(c for c in r if c >= BRACKET_FIRST_COL)
        if cols:
            lines.append([r[c] for c in cols])
    if not lines:
        # 6 joueurs : pas d'affiches dans le classeur, croisement classique des 2 poules
        if len(groups) == 2 and all(g["qualifiers"] == 2 for g in groups):
            v.warnings.append("affiches absentes, croisement 1p1-2p2 / 1p2-2p1 par défaut")
            return [((1, 1), (2, 2)), ((1, 2), (2, 1))]
        v.warnings.append("affiches du tableau absentes")
        return None
    if len(lines) != 2 or len(lines[0]) != len(lines[1]):
        v.warnings.append(f"tableau illisible ({len(lines)} lignes d'affiches)")
        return None
    bracket = []
    for top, bottom in zip(*lines):
        a, b = parse_slot(text(top)), parse_slot(text(bottom))
        if a is None or b is None:
            v.warnings.append(f"affiche illisible : « {text(top)} » - « {text(bottom)} »")
            return None
        bracket.append((a, b))
    if len(bracket) * 2 not in STAGE_NAMES:
        v.warnings.append(f"taille de tableau inattendue : {len(bracket) * 2}")
        return None
    return bracket


def parse_slot(value: str):
    m = SLOT_RE.match(value)
    return (int(m.group(1)), int(m.group(2))) if m else None


def slot_str(slot) -> str:
    return f"{slot[0]}p{slot[1]}"


def single_group(players: int) -> dict:
    return {
        "knockout": None,
        "matchCount": comb(players, 2),
        "groups": [{"size": players, "qualifiers": 0}],
        "bracket": [],
    }


def to_json(data) -> str:
    raw = json.dumps(data, indent=2, ensure_ascii=False)
    # Tableaux et objets « feuilles » sur une seule ligne pour garder le fichier lisible
    leaf = re.compile(r"([\[{])\s*\n\s*([^\[\]{}]*?)\s*\n\s*([\]}])")
    while True:
        collapsed = leaf.sub(lambda m: m.group(1) + re.sub(r"\s*\n\s*", " ", m.group(2)) + m.group(3), raw)
        if collapsed == raw:
            return raw
        raw = collapsed


def main() -> int:
    xlsx = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_XLSX
    out = Path(sys.argv[2]) if len(sys.argv) > 2 else DEFAULT_JSON

    by_players: dict[int, list[dict]] = {n: [single_group(n)] for n in range(2, 6)}
    report = []
    for v in read_variants(xlsx):
        if has_prelim(v):
            report.append(f"- {v.label} : ignorée (barrage 701)")
            continue
        parsed = parse_variant(v)
        status = "REJETÉE" if parsed is None else "ok"
        if parsed is not None:
            by_players.setdefault(v.players, []).append(parsed)
        if parsed is None or v.warnings:
            report.append(f"- {v.label} : {status}" + "".join(f"\n    · {w}" for w in v.warnings))

    formats = []
    for players in sorted(by_players):
        variants = by_players[players]
        size = expected_bracket_size(players)
        default = next((x for x in variants if size is None or x["knockout"] == STAGE_NAMES[size]), None)
        if default is None:
            default = variants[0]
            report.append(f"- {players} joueurs : aucune variante en {STAGE_NAMES[size]}, défaut = {default['knockout']}")
        for x in variants:
            x["default"] = x is default
        formats.append({"players": players, "variants": [{"default": x.pop("default"), **x} for x in variants]})

    missing = [n for n in range(2, 81) if n not in by_players]
    if missing:
        report.append(f"- nombres de joueurs sans variante utilisable : {missing}")

    data = {
        "source": xlsx.name,
        "generatedBy": "tools/fsgt/extract_fsgt_formats.py",
        "points": POINTS,
        "tieBreakers": TIE_BREAKERS,
        "formats": formats,
    }
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(to_json(data) + "\n", encoding="utf-8")

    print(f"{out} : {len(formats)} nombres de joueurs, {sum(len(f['variants']) for f in formats)} variantes")
    print("\n".join(report) if report else "aucune remarque")
    return 1 if missing else 0


if __name__ == "__main__":
    sys.exit(main())
