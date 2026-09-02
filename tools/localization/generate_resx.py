#!/usr/bin/env python3
"""
Generate MiniMica satellite .resx files from worksheet.csv.

Same output as generate-resx.ps1, but runs anywhere - useful in CI and on a
machine without PowerShell, and it is the version covered by the verification
suite. Either script is fine; do not run both into the same directory.

    python3 tools/localization/generate_resx.py --tier 14
    python3 tools/localization/generate_resx.py --tier All --out src/MiniMicaApp/Localization

Rules that matter:
  * a blank cell is OMITTED, so .NET ResourceManager falls back to the parent
    culture and finally to neutral English. Never write an empty string - that
    would render as blank UI instead of falling back.
  * every translated cell must carry the same {Placeholders} as its English
    source, or Strings.Expand silently produces the wrong text.
  * rows whose ID starts with "metric_" are per-culture layout numbers, not
    prose. They are emitted like any other string and read back as doubles by
    LocalizationManager.ApplyMetrics.
"""
import argparse
import csv
import os
import re
import sys

TIERS = {
    "14": ["en-US", "de-DE", "fr-FR", "es-MX", "es-ES", "pt-BR", "pt-PT", "zh-TW",
           "zh-CN", "it-IT", "ru-RU", "uk-UA", "nl-NL", "pl-PL"],
    "22": ["en-US", "de-DE", "fr-FR", "es-MX", "es-ES", "pt-BR", "pt-PT", "zh-TW",
           "zh-CN", "it-IT", "ru-RU", "uk-UA", "nl-NL", "pl-PL", "sv-SE", "da-DK",
           "nb-NO", "fi-FI", "ja-JP", "ko-KR", "cs-CZ", "tr-TR"],
    "25": ["en-US", "de-DE", "fr-FR", "es-MX", "es-ES", "pt-BR", "pt-PT", "zh-TW",
           "zh-CN", "it-IT", "ru-RU", "uk-UA", "nl-NL", "pl-PL", "sv-SE", "da-DK",
           "nb-NO", "fi-FI", "ja-JP", "ko-KR", "cs-CZ", "tr-TR", "id-ID", "th-TH",
           "vi-VN"],
}

HEADER = """<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
"""

PLACEHOLDER = re.compile(r"\{[^{}]+\}")


def escape(text):
    return (text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"))


def validate(rows, cultures):
    problems = []
    ids = [r["ResourceID"] for r in rows]
    for dup in sorted({i for i in ids if ids.count(i) > 1}):
        problems.append("duplicate ResourceID: %s" % dup)

    for row in rows:
        rid = row["ResourceID"]
        if not re.match(r"^[A-Za-z_][A-Za-z0-9_.-]*$", rid):
            problems.append("invalid ResourceID: %s" % rid)
        english = (row.get("en-US") or "").strip()
        if not english:
            problems.append("missing en-US value: %s" % rid)
            continue

        expected = sorted(set(PLACEHOLDER.findall(english)))
        for culture in cultures:
            value = (row.get(culture) or "").strip()
            if not value:
                continue
            if sorted(set(PLACEHOLDER.findall(value))) != expected:
                problems.append("placeholder mismatch %s/%s: expected %s"
                                % (rid, culture, expected))
            if rid.startswith("metric_"):
                try:
                    float(value)
                except ValueError:
                    problems.append("metric_ row must be numeric: %s/%s = %r"
                                    % (rid, culture, value))
    return problems


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    repo = os.path.abspath(os.path.join(here, "..", ".."))

    ap = argparse.ArgumentParser()
    ap.add_argument("--worksheet", default=os.path.join(here, "worksheet.csv"))
    ap.add_argument("--out", default=os.path.join(repo, "src", "MiniMicaApp", "Localization"))
    ap.add_argument("--tier", default="14", choices=sorted(TIERS) + ["All"])
    ap.add_argument("--base-name", default="Strings")
    ap.add_argument("--include-neutral", action="store_true",
                    help="also rewrite the neutral Strings.resx from en-US")
    ap.add_argument("--clean", action="store_true",
                    help="delete existing satellites for this base name first")
    args = ap.parse_args()

    rows = list(csv.DictReader(open(args.worksheet, encoding="utf-8-sig")))
    if not rows:
        sys.exit("worksheet has no rows")
    columns = list(rows[0].keys())

    cultures = [c for c in columns if c != "ResourceID"] if args.tier == "All" else TIERS[args.tier]
    missing = [c for c in cultures if c not in columns]
    if missing:
        sys.exit("worksheet is missing culture column(s): %s" % ", ".join(missing))

    problems = validate(rows, cultures)
    if problems:
        for p in problems:
            print("ERROR: %s" % p, file=sys.stderr)
        sys.exit(1)

    os.makedirs(args.out, exist_ok=True)
    if args.clean:
        for name in os.listdir(args.out):
            if re.match(r"^%s\.[A-Za-z-]+\.resx$" % re.escape(args.base_name), name):
                os.remove(os.path.join(args.out, name))

    written = []
    for culture in cultures:
        neutral = culture == "en-US"
        if neutral and not args.include_neutral:
            continue

        name = "%s.resx" % args.base_name if neutral else "%s.%s.resx" % (args.base_name, culture)
        path = os.path.join(args.out, name)

        body = []
        for row in rows:
            value = (row.get(culture) or "").strip()
            if not value:
                continue          # omit -> ResourceManager falls back
            body.append('  <data name="%s" xml:space="preserve">\n    <value>%s</value>\n  </data>\n'
                        % (row["ResourceID"], escape(value)))

        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(HEADER + "".join(body) + "</root>\n")
        written.append((name, len(body)))

    for name, count in written:
        print("  %-28s %3d entries" % (name, count))
    print("generated %d file(s) into %s (tier %s)" % (len(written), args.out, args.tier))
    return 0


if __name__ == "__main__":
    sys.exit(main())
