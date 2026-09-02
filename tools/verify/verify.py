#!/usr/bin/env python3
"""
MiniMica v5 offline verification suite.

Runs the static checks that need neither Windows, MSBuild nor PowerShell, so the
first real compile is spent on genuine platform problems rather than on missing
resource keys, broken XAML wiring or template-rename drift.

    python3 tools/verify/verify.py             # all checks
    python3 tools/verify/verify.py loc         # localization only
    python3 tools/verify/verify.py precompile  # XAML/C# wiring only
    python3 tools/verify/verify.py rename      # template generation only

Exit code 0 = all checks passed. Safe to run in CI on any OS.
See tools/verify/README.md for what each check does and does not cover.
"""
import csv, glob, json, os, re, shutil, sys
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
SRC  = os.path.join(REPO, "src", "MiniMicaApp")
WS   = os.path.join(REPO, "tools", "localization", "worksheet.csv")
RESX = os.path.join(SRC, "Localization", "Strings.resx")
BIN  = (".ico", ".png", ".jpg", ".jpeg", ".gif")


def _read(p):
    return open(p, encoding="utf-8").read()


def _sources():
    xamls = sorted(glob.glob(os.path.join(SRC, "**", "*.xaml"), recursive=True))
    cses  = sorted(glob.glob(os.path.join(SRC, "**", "*.cs"),   recursive=True))
    return ({p: _read(p) for p in xamls}, {p: _read(p) for p in cses})


def _loc_refs(xmltext, cstext):
    """Every resource ID the app actually asks for, including the dynamic
    maximize/restore key selected at runtime in MiniMicaWindow."""
    refs = set()
    for t in xmltext.values():
        refs |= set(re.findall(r"\{i18n:Loc\s+Key=([A-Za-z0-9_]+)", t))
    for t in cstext.values():
        refs |= set(re.findall(r'Strings\.(?:Get|Expand)\("([A-Za-z0-9_]+)"', t))
        refs |= set(re.findall(r'"(titlebar_[a-z]+)"', t))
        refs |= set(re.findall(r'"(metric_[a-z_]+)"', t))
    return refs


# --------------------------------------------------------------------------
def check_localization():
    errors, notes = [], []
    defined = {d.get("name") for d in ET.parse(RESX).getroot().findall("data")}
    xmltext, cstext = _sources()
    refs = _loc_refs(xmltext, cstext)

    missing = sorted(refs - defined)
    orphan  = sorted(defined - refs)
    if missing:
        errors.append(("referenced but not defined in Strings.resx", missing))
    if orphan:
        notes.append(("defined but never referenced", orphan))

    rows = list(csv.DictReader(open(WS, encoding="utf-8-sig")))
    cols = list(rows[0].keys())
    ids  = [r["ResourceID"] for r in rows]

    dupes = sorted({i for i in ids if ids.count(i) > 1})
    if dupes:
        errors.append(("duplicate ResourceID in worksheet", dupes))

    ph = lambda t: sorted(set(re.findall(r"\{[^{}]+\}", t or "")))
    bad = []
    for r in rows:
        rid = r["ResourceID"]
        if not re.match(r"^[A-Za-z_][A-Za-z0-9_.-]*$", rid):
            errors.append(("invalid ResourceID", [rid]))
        if not (r["en-US"] or "").strip():
            errors.append(("empty en-US value", [rid]))
        expected = ph(r["en-US"])
        for c in cols[1:]:
            v = (r[c] or "").strip()
            if v and ph(v) != expected:
                bad.append("%s/%s: expected %s, found %s" % (rid, c, expected, ph(v)))
    if bad:
        errors.append(("placeholder mismatch (would break Strings.Expand)", bad))

    if set(ids) != defined:
        errors.append(("worksheet and neutral resx disagree", [
            "only in worksheet: %s" % sorted(set(ids) - defined),
            "only in resx: %s"      % sorted(defined - set(ids)),
        ]))

    # Satellites are generated from the worksheet and committed, so they can drift.
    sat = sorted(glob.glob(os.path.join(SRC, "Localization", "Strings.*.resx")))
    ws_by_id = {r["ResourceID"]: r for r in rows}
    for path in sat:
        culture = os.path.basename(path)[len("Strings."):-len(".resx")]
        if culture not in cols:
            errors.append(("satellite for a culture the worksheet has no column for",
                           [os.path.basename(path)]))
            continue
        got = {d.get("name"): (d.find("value").text or "")
               for d in ET.parse(path).getroot().findall("data")}
        want = {rid: (r[culture] or "").strip()
                for rid, r in ws_by_id.items() if (r[culture] or "").strip()}
        if got != want:
            only_sat = sorted(set(got) - set(want))
            only_ws = sorted(set(want) - set(got))
            changed = sorted(k for k in set(got) & set(want) if got[k] != want[k])
            errors.append(("satellite out of sync with worksheet - regenerate",
                           ["%s: extra=%s missing=%s changed=%s"
                            % (os.path.basename(path), only_sat[:3], only_ws[:3], changed[:3])]))
    print("  satellites: %d in sync with the worksheet" % len(sat))

    print("  keys defined %d | referenced %d | worksheet %d x %d cultures"
          % (len(defined), len(refs), len(rows), len(cols) - 1))
    cov = {c: sum(1 for r in rows if (r[c] or "").strip()) for c in cols[1:]}
    full = [c for c, n in cov.items() if n == len(rows)]
    part = {c: n for c, n in cov.items() if n < len(rows)}
    print("  full coverage: %s" % ", ".join(full))
    if part:
        lo = min(part.values()); hi = max(part.values())
        print("  partial: %d cultures at %d-%d/%d (sample strings intentionally "
              "en-US only)" % (len(part), lo, hi, len(rows)))
    return errors, notes


# --------------------------------------------------------------------------
def check_precompile():
    errors, notes = [], []
    xmltext, cstext = _sources()
    allcs   = "\n".join(cstext.values())
    allxaml = "\n".join(xmltext.values())

    types = {}
    for p, t in cstext.items():
        m = re.search(r"^\s*namespace\s+([A-Za-z0-9_.]+)", t, re.M)
        ns = m.group(1) if m else ""
        for d in re.finditer(
                r"^\s*(?:public|internal|sealed|static|abstract|partial|\s)*"
                r"\b(?:class|struct|enum)\s+([A-Za-z0-9_]+)", t, re.M):
            types.setdefault(d.group(1), set()).add(ns)
    namespaces = {n for s in types.values() for n in s}

    defined = set()
    for t in xmltext.values():
        defined |= set(re.findall(r'\bx:Key="([^"]+)"', t))
    used = set()
    for t in xmltext.values():
        used |= set(re.findall(r"\{(?:Static|Dynamic)Resource\s+([^}]+?)\s*\}", t))
    for t in cstext.values():
        used |= set(re.findall(r'SetResourceReference\([^,]+,\s*"([^"]+)"', t))
        used |= set(re.findall(r'(?:Try)?FindResource\("([^"]+)"\)', t))
    undefined = sorted(k for k in used - defined if not k.startswith("{"))
    if undefined:
        errors.append(("resource key used but never defined", undefined))

    for p, t in xmltext.items():
        rel = os.path.relpath(p, SRC)
        m = re.search(r'\bx:Class="([A-Za-z0-9_.]+)"', t)
        if m:
            fq = m.group(1)
            ns, _, cls = fq.rpartition(".")
            if cls not in types or ns not in types.get(cls, set()):
                errors.append(("x:Class has no matching C# class", ["%s -> %s" % (rel, fq)]))
            elif not re.search(r"partial\s+class\s+%s\b" % cls, allcs):
                notes.append(("x:Class target not declared partial", [fq]))

        for d in re.finditer(r'xmlns:([A-Za-z0-9_]+)="clr-namespace:([A-Za-z0-9_.]+)"', t):
            alias, ns = d.group(1), d.group(2)
            if ns not in namespaces:
                errors.append(("clr-namespace does not exist", ["%s: %s=%s" % (rel, alias, ns)]))
            for u in re.finditer(r"[<{]%s:([A-Za-z0-9_]+)" % alias, t):
                tn = u.group(1)
                if (tn not in types or ns not in types[tn]) and (tn + "Extension") not in types:
                    errors.append(("type not found in clr-namespace",
                                   ["%s: %s:%s -> %s" % (rel, alias, tn, ns)]))

        cb = cstext.get(p + ".cs")
        for ev in ("Click", "Loaded", "Unloaded", "Checked", "Unchecked",
                   "SelectionChanged", "TextChanged", "Closing", "SizeChanged"):
            for h in re.finditer(r'\b%s="([A-Za-z0-9_]+)"' % ev, t):
                name = h.group(1)
                if cb is None:
                    errors.append(("event handler but no code-behind",
                                   ["%s: %s=%s" % (rel, ev, name)]))
                elif not re.search(r"\bvoid\s+%s\s*\(" % name, cb):
                    errors.append(("event handler method missing", ["%s: %s" % (rel, name)]))

    for p, t in cstext.items():
        for m in re.finditer(r"\[TemplatePart\(Name\s*=\s*([A-Za-z0-9_]+)", t):
            const = m.group(1)
            cm = re.search(r'%s\s*=\s*"([^"]+)"' % const, t)
            if cm and ('x:Name="%s"' % cm.group(1)) not in allxaml:
                errors.append(("declared TemplatePart absent from every template",
                               ["%s: %s" % (os.path.relpath(p, SRC), cm.group(1))]))

    # Namespace/type collisions (CS0118). A bare identifier that is also the tail of a
    # namespace declared in this project binds to the NAMESPACE, not to a same-named type
    # from the framework - e.g. inside MiniMicaApp.Settings, "Configuration" resolves to
    # MiniMicaApp.Configuration rather than System.Configuration.Configuration. The fix is
    # a using-alias. This also bites generated apps, whose namespace tails are identical.
    tails = set()
    for t in cstext.values():
        for m in re.finditer(r"^\s*namespace\s+([A-Za-z0-9_.]+)", t, re.M):
            tails.update(m.group(1).split(".")[1:])   # skip the root namespace
    aliased = set(re.findall(r"^\s*using\s+\w+\s*=\s*[\w.]*?\.(\w+)\s*;", allcs, re.M))
    for p, t in cstext.items():
        rel = os.path.relpath(p, SRC)
        for i, line in enumerate(t.splitlines(), 1):
            s = line.strip()
            if s.startswith(("//", "///", "*", "/*")) or s.startswith("using "):
                continue
            for tail in tails:
                if tail in aliased:
                    continue
                # <Tail> <identifier> in a declaration position (local, field, parameter
                # or method return type), not preceded by a dot.
                if re.search(r"(?<![.\w])%s\s+[A-Za-z_]\w*\s*[=;),(]" % re.escape(tail), line):
                    errors.append(("bare namespace tail used as a type (CS0118) - add a using-alias",
                                   ["%s:%d: %s" % (rel, i, s[:90])]))

    # Assembly attribute ownership. An SDK-style project generates AssemblyTitle,
    # AssemblyProduct, AssemblyVersion and friends from MSBuild properties. A hand-written
    # AssemblyInfo.cs alongside that produces CS0579 duplicate-attribute errors, so
    # exactly one side must own them.
    csproj_path = os.path.join(SRC, "MiniMicaApp.csproj")
    info_path = os.path.join(SRC, "Properties", "AssemblyInfo.cs")
    if os.path.exists(csproj_path):
        csproj = _read(csproj_path)
        generates = "<GenerateAssemblyInfo>false</GenerateAssemblyInfo>" not in csproj
        has_info = os.path.exists(info_path)
        OWNED = ("AssemblyTitle", "AssemblyProduct", "AssemblyDescription", "AssemblyCompany",
                 "AssemblyCopyright", "AssemblyVersion", "AssemblyFileVersion")
        if has_info and generates:
            errors.append(("AssemblyInfo.cs exists but the SDK still generates assembly "
                           "attributes - set <GenerateAssemblyInfo>false</GenerateAssemblyInfo> "
                           "or the build fails with CS0579", ["MiniMicaApp.csproj"]))
        if has_info and not generates:
            info = _read(info_path)
            # the same attribute must not be declared in both places
            dead = []
            for prop in ("AssemblyTitle", "Product", "Description", "Company", "Copyright",
                         "Version", "AssemblyVersion", "FileVersion", "InformationalVersion"):
                if re.search(r"<%s>" % prop, csproj):
                    dead.append("csproj <%s> is ignored while GenerateAssemblyInfo is false" % prop)
            if dead:
                errors.append(("assembly identity declared in two places", dead))
            missing = [a for a in OWNED if ("[assembly: %s(" % a) not in info]
            if missing:
                errors.append(("AssemblyInfo.cs is missing an attribute the SDK "
                               "no longer generates", missing))

    app = xmltext.get(os.path.join(SRC, "App.xaml"), "")
    for m in re.finditer(r'Source="([^"]+\.xaml)"', app):
        rel = m.group(1).lstrip("/")
        if not os.path.exists(os.path.join(SRC, rel)):
            errors.append(("merged ResourceDictionary file missing", [rel]))

    # A Setter can only target a DependencyProperty. Assigning a plain CLR property
    # compiles cleanly and then throws XamlParseException / ArgumentNullException
    # ("Value cannot be null. Parameter name: property") the first time the style is
    # applied - so it must be caught statically.
    CLR_ONLY = {
        "WindowStartupLocation", "Owner", "DialogResult", "RestoreBounds",
        "OwnedWindows", "Items", "Children", "Resources", "Triggers", "Inlines",
        "CommandBindings", "InputBindings",
    }
    for p, t in xmltext.items():
        rel = os.path.relpath(p, SRC)
        for m in re.finditer(r"<Setter\b[^>]*?\bProperty=\"([^\"]+)\"", t):
            prop = m.group(1).split(".")[-1]
            if prop in CLR_ONLY:
                line = t[:m.start()].count("\n") + 1
                errors.append(("Setter targets a CLR property, not a DependencyProperty",
                               ["%s:%d: %s" % (rel, line, m.group(1))]))

    # Properties declared by this codebase and used in a Setter/Trigger must be
    # registered dependency properties.
    own_dps = set(re.findall(
        r'DependencyProperty\.Register(?:ReadOnly|Attached|AttachedReadOnly)?\(\s*"([A-Za-z0-9_]+)"',
        allcs))
    own_props = set(re.findall(r"^\s*public\s+[A-Za-z0-9_<>\[\]?]+\s+([A-Za-z0-9_]+)\s*\{", allcs, re.M))
    for p, t in xmltext.items():
        rel = os.path.relpath(p, SRC)
        for m in re.finditer(r"<(?:Setter|Trigger)\b[^>]*?\bProperty=\"([A-Za-z0-9_]+)\"", t):
            prop = m.group(1)
            if prop in own_props and prop not in own_dps:
                line = t[:m.start()].count("\n") + 1
                errors.append(("Setter/Trigger targets a non-dependency property declared in this project",
                               ["%s:%d: %s" % (rel, line, prop)]))

    # ThemeManager writes brushes into Resources at runtime; Styles.xaml carries a
    # designer-safe fallback for each. If the three lists drift, a control silently
    # renders with a stale color (or nothing) in the designer and on first paint.
    keys_path = os.path.join(SRC, "Platform", "Windows", "Theme", "ThemeResourceKeys.cs")
    mgr_path = os.path.join(SRC, "Platform", "Windows", "Theme", "ThemeManager.cs")
    if os.path.exists(keys_path) and os.path.exists(mgr_path):
        declared = dict(re.findall(r'string\s+(\w+)\s*=\s*"([^"]+)"', _read(keys_path)))
        assigned = set(re.findall(r"ThemeResourceKeys\.(\w+)", _read(mgr_path)))
        styles = xmltext.get(os.path.join(SRC, "Resources", "Styles.xaml"), "")
        fallbacks = set(re.findall(r'x:Key="(MiniMica\.[A-Za-z0-9_]+)"', styles))

        unset = sorted(v for k, v in declared.items() if k not in assigned)
        if unset:
            errors.append(("theme key declared but never assigned by ThemeManager", unset))
        nofb = sorted(declared[k] for k in assigned if k in declared and declared[k] not in fallbacks)
        if nofb:
            errors.append(("theme key has no designer fallback in Styles.xaml", nofb))

        # Per-branch coverage. "Assigned somewhere" is not enough: a key set only in the
        # light branch keeps a stale value after switching to dark or to a contrast theme,
        # which is exactly how the contrast-theme title bar ended up unreadable.
        mgr = _read(mgr_path)
        try:
            hc_start = mgr.index("if (highContrast)")
            shared_start = mgr.index("resources[ThemeResourceKeys.WindowFrameBrush] = Brushes.Transparent")
            dark_start = mgr.index("if (theme == AppTheme.Dark)")
            light_start = mgr.index("ApplySampleIcons(resources, true)")
        except ValueError:
            hc_start = None
        if hc_start is not None:
            grab = lambda a, b: set(re.findall(r"ThemeResourceKeys\.(\w+)", mgr[a:b]))
            shared = grab(shared_start, dark_start)
            branches = {
                "high contrast": grab(hc_start, shared_start),
                "dark": grab(dark_start, light_start) | shared,
                "light": grab(light_start, len(mgr)) | shared,
            }
            skip = {"SampleBullet1", "SampleBullet2", "SampleBullet3"}
            for name, got in branches.items():
                missing = sorted(declared[k] for k in declared
                                 if k in assigned and k not in got and k not in skip)
                if missing:
                    errors.append(("theme key not assigned in the '%s' branch "
                                   "(will keep a stale value)" % name, missing))

    # TemplateBinding performs no type conversion. Binding a double-typed property
    # into a GridLength target fails silently and leaves the row/column at 1*.
    for p, t in xmltext.items():
        rel = os.path.relpath(p, SRC)
        for m in re.finditer(r"<(RowDefinition|ColumnDefinition)\b[^>]*?"
                             r"(Height|Width)=\"\{TemplateBinding\s+([A-Za-z0-9_]+)\s*\}\"", t):
            line = t[:m.start()].count("\n") + 1
            notes.append(("TemplateBinding into a GridLength does not type-convert",
                          ["%s:%d: %s.%s <- %s" % (rel, line, m.group(1), m.group(2), m.group(3))]))

    print("  %d XAML + %d C# files | %d types | resource keys %d defined / %d used"
          % (len(xmltext), len(cstext), len(types), len(defined), len(used)))
    return errors, notes


# --------------------------------------------------------------------------
def check_rename(keep=False):
    errors, notes = [], []
    tj = json.load(open(os.path.join(SRC, ".template.config", "template.json"), encoding="utf-8"))
    source = tj["sourceName"]
    syms = {s["replaces"]: s["defaultValue"] for s in tj.get("symbols", {}).values() if "replaces" in s}
    name = "Contoso"
    out = os.path.join(REPO, "artifacts", "rename-check", name)

    if os.path.exists(out):
        shutil.rmtree(out)
    os.makedirs(out)

    for root, dirs, files in os.walk(SRC):
        dirs[:] = [d for d in dirs if d not in (".template.config", "bin", "obj")]
        for f in files:
            s = os.path.join(root, f)
            rel = os.path.relpath(s, SRC).replace(source, name)
            d = os.path.join(out, rel)
            os.makedirs(os.path.dirname(d), exist_ok=True)
            if f.lower().endswith(BIN):
                shutil.copy2(s, d)
            else:
                t = _read(s).replace(source, name)
                for k, v in syms.items():
                    t = t.replace(k, v)
                open(d, "w", encoding="utf-8", newline="").write(t)

    stale, sentinels = [], []
    for root, _, files in os.walk(out):
        for f in files:
            p = os.path.join(root, f)
            rel = os.path.relpath(p, out)
            if source in rel:
                stale.append("%s (path)" % rel)
            if f.lower().endswith(BIN):
                continue
            t = _read(p)
            for m in re.finditer(source + r"[A-Za-z0-9_.]*", t):
                stale.append("%s: %s" % (rel, m.group(0)))
            for s in syms:
                if s in t:
                    sentinels.append("%s: %s" % (rel, s))

    # Case-variant residue. `dotnet new` rewrites camel/pascal/snake variants of
    # sourceName; a literal replacement does not. Anything matching case-insensitively
    # but not exactly is therefore a spot where the two disagree - worth a look even
    # though the real template engine may well handle it.
    residue = []
    ci = re.compile(re.escape(source), re.I)
    for root, _, files in os.walk(out):
        for f in files:
            if f.lower().endswith(BIN):
                continue
            p = os.path.join(root, f)
            for m in ci.finditer(_read(p)):
                if m.group(0) != source:
                    residue.append("%s: %s" % (os.path.relpath(p, out), m.group(0)))
    if residue:
        notes.append(("case-variant of '%s' left by literal rename" % source,
                      sorted(set(residue))))

    if stale:
        errors.append(("stale '%s' identifier in generated app" % source, sorted(set(stale))))
    if sentinels:
        errors.append(("unreplaced template sentinel", sorted(set(sentinels))))

    g = lambda rel: _read(os.path.join(out, rel))
    invariants = [
        ("project file renamed",      os.path.exists(os.path.join(out, name + ".csproj"))),
        ("RootNamespace rewritten",   "<RootNamespace>%s</RootNamespace>" % name in g(name + ".csproj")),
        ("AssemblyName rewritten",    "<AssemblyName>%s</AssemblyName>" % name in g(name + ".csproj")),
        ("GenerateAssemblyInfo off",   "<GenerateAssemblyInfo>false</GenerateAssemblyInfo>" in g(name + ".csproj")),
        ("AssemblyInfo product renamed", 'AssemblyProduct("%s")' % name in g("Properties/AssemblyInfo.cs")),
        ("AssemblyInfo title renamed",  'AssemblyTitle("%s")' % name in g("Properties/AssemblyInfo.cs")),
        ("ResourceManager base name", '"%s.Localization.Strings"' % name in g("Localization/Strings.cs")),
        ("manifest identity",         'name="%s.app"' % name in g("app.manifest")),
        # DisplayName derives from the assembly rather than a literal, so the invariant
        # is that no hardcoded product name survives in code.
        ("no hardcoded product name",  'DisplayNameOverride = ""' in g("Configuration/AppOptions.cs")),
        (".template.config excluded", not os.path.exists(os.path.join(out, ".template.config"))),
    ]
    failed = [n for n, ok in invariants if not ok]
    if failed:
        errors.append(("generated-app invariant failed", failed))

    print("  generated %s/ | %d invariants checked | stale=%d sentinels=%d"
          % (name, len(invariants), len(set(stale)), len(set(sentinels))))
    if not keep:
        shutil.rmtree(os.path.join(REPO, "artifacts", "rename-check"), ignore_errors=True)
    return errors, notes


# --------------------------------------------------------------------------
# --------------------------------------------------------------------------
def check_chrome():
    """Pixel-precision contract inherited from MiniMica v4.1.

    v4.1 tuned the title bar against the Windows 11 Calculator. Two regressions have
    already been introduced by "modernising" these values, and neither is detectable
    by a compiler - only by eye, on Windows, side by side. So they are asserted here.

    Segoe MDL2 Assets and Segoe Fluent Icons both carry E921/E922/E923/E8BB, but the
    Fluent Icons outlines are metrically different: at 10pt the minimize bar loses a
    pixel and the maximize square's 1px stroke antialiases across two rows instead of
    snapping to crisp black. E713 (the gear) is a Fluent Icons glyph and is the one
    deliberate exception.
    """
    errors, notes = [], []
    chrome = _read(os.path.join(SRC, "Resources", "WindowChrome.xaml"))
    fixed = _read(os.path.join(SRC, "Resources", "FixedPageHost.xaml"))

    def style_block(text, key):
        m = re.search(r'<Style x:Key="%s".*?</Style>' % re.escape(key), text, re.S)
        return m.group(0) if m else ""

    def font_of(block):
        f = re.search(r'Property="FontFamily" Value="([^"]+)"', block)
        s = re.search(r'Property="FontSize" Value="([^"]+)"', block)
        return (f.group(1) if f else None, s.group(1) if s else None)

    expected = [
        ("MiniMica.CaptionButtonStyle", chrome, ("Segoe MDL2 Assets", "10"),
         "caption glyphs must use MDL2 at 10 (v4.1/Calculator parity)"),
        ("MiniMica.PanButtonStyle", fixed, ("Segoe MDL2 Assets", "12"),
         "pan chevrons must use MDL2 at 12 (v4.1 ScrollArrowButton parity)"),
    ]
    for key, text, exp, why in expected:
        block = style_block(text, key)
        if not block:
            errors.append(("style not found", [key]))
            continue
        got = font_of(block)
        if got != exp:
            errors.append(("v4.1 font contract broken - " + why,
                           ["%s: expected %s, found %s" % (key, exp, got)]))

    gear = re.search(r'PART_SettingsButton.*?/>', chrome, re.S)
    if gear:
        g = gear.group(0)
        # Fluent Icons must come FIRST so Windows 11 renders the intended drawing;
        # MDL2 must be listed after it so Windows 10 has a font that exists.
        if 'FontFamily="Segoe Fluent Icons, Segoe MDL2 Assets"' not in g or 'FontSize="12"' not in g:
            errors.append(("Settings gear must be Segoe Fluent Icons at 12 with a Segoe "
                           "MDL2 Assets fallback (Fluent Icons is Windows 11 only)",
                           [" ".join(g.split())[:120]]))

    for label, text, want in [
        ("caption", chrome, {"&#xE713;", "&#xE921;", "&#xE922;", "&#xE923;", "&#xE8BB;"}),
        ("pan", fixed, {"&#xE0E2;", "&#xE0E3;"}),
    ]:
        found = set(re.findall(r'Value="(&#x[0-9A-Fa-f]+;)"', text)) | \
                set(re.findall(r'Content="(&#x[0-9A-Fa-f]+;)"', text))
        unexpected = sorted(found - want)
        if unexpected:
            notes.append(("%s glyph codepoint not in the v4.1 set" % label, unexpected))

    # Settings dialog geometry. 480x300 DIP is v4.1's; an earlier v5 build shipped
    # 600x375, which is that dialog measured off a 125%-DPI screenshot.
    dlg_path = os.path.join(SRC, "Settings", "SettingsWindow.xaml")
    dlg = _read(dlg_path) if os.path.exists(dlg_path) else ""
    if dlg:
        for attr, want in (("Width", "480"), ("Height", "300"),
                           ("MinWidth", "480"), ("MinHeight", "300"),
                           ("MaxWidth", "480"), ("MaxHeight", "300")):
            m = re.search(r'\b%s="(\d+)"' % attr, dlg)
            if not m or m.group(1) != want:
                errors.append(("Settings dialog must keep v4.1 geometry (480x300 DIP)",
                               ["%s=%s, expected %s" % (attr, m.group(1) if m else "?", want)]))
        # The dialog is deliberately accent-free and uses stock controls.
        if "MiniMica.AccentBrush" in dlg or "MiniMica.AccentTextBrush" in dlg:
            errors.append(("Settings dialog must not use the accent color", ["AccentBrush reference"]))
        for styled in ("MiniMica.RadioButtonStyle", "MiniMica.CheckBoxStyle"):
            if styled in dlg:
                errors.append(("Settings dialog must use stock WPF radio/check controls",
                               [styled]))

    print("  caption font %s | pan font %s | gear override: %s | dialog %sx%s"
          % (font_of(style_block(chrome, "MiniMica.CaptionButtonStyle")),
             font_of(style_block(fixed, "MiniMica.PanButtonStyle")),
             bool(gear and 'Segoe Fluent Icons' in gear.group(0)),
             (re.search(r'\bWidth="(\d+)"', dlg) or [None, "?"])[1],
             (re.search(r'\bHeight="(\d+)"', dlg) or [None, "?"])[1]))
    return errors, notes


# --------------------------------------------------------------------------
def check_styles():
    """Centralized styling contract.

    Views position elements and name a style; they do not set typography. Keeping
    fonts, sizes, weights and colors in Resources/Styles.xaml is what makes a fork
    restylable from one file, and it is easy to erode one inline attribute at a time.

    Also checks that every asset a view references exists on disk and is declared as
    a <Resource> in the csproj - a missing Resource entry compiles fine and then
    throws IOException at runtime when the pack URI cannot be resolved.
    """
    errors, notes = [], []
    xmltext, _ = _sources()
    csproj = _read(os.path.join(SRC, "MiniMicaApp.csproj"))

    TYPOGRAPHY = ("FontFamily", "FontSize", "FontWeight", "FontStyle", "Foreground")
    views = [p for p in xmltext if os.sep + "Views" + os.sep in p]
    for p in views:
        rel = os.path.relpath(p, SRC)
        for attr in TYPOGRAPHY:
            for m in re.finditer(r'\b%s="([^"]*)"' % attr, xmltext[p]):
                line = xmltext[p][:m.start()].count("\n") + 1
                errors.append(("inline typography in a view - move it to Styles.xaml",
                               ["%s:%d: %s=\"%s\"" % (rel, line, attr, m.group(1))]))

    # Asset references: on disk and declared in the project.
    declared = set(re.findall(r'<Resource Include="([^"]+)"', csproj))
    declared_norm = {d.replace("\\", "/") for d in declared}
    referenced = set()
    for t in xmltext.values():
        referenced |= set(re.findall(r'(?:Source|UriSource)="(/Resources/[^"]+)"', t))
    for ref in sorted(referenced):
        rel = ref.lstrip("/")
        if not os.path.exists(os.path.join(SRC, rel)):
            errors.append(("asset referenced by a view does not exist", [ref]))
        elif rel not in declared_norm:
            errors.append(("asset not declared as <Resource> in the csproj "
                           "(pack URI will fail at runtime)", [ref]))

    # Brush role confusion. A brush whose contrast-theme mapping is a FILL (button face,
    # window/page background) is invisible when used as Foreground, because in most
    # contrast themes those colors equal the window background. The reverse is equally
    # wrong. This is how the sample subtitle disappeared under every contrast theme: it
    # shared MiniMica.BrandAccentBrush with the action button's fill.
    FILL_ROLE = {
        "MiniMica.BrandAccentBrush", "MiniMica.BrandAccentHoverBrush",
        "MiniMica.ControlBrush", "MiniMica.ControlHoverBrush", "MiniMica.ControlPressedBrush",
        "MiniMica.SurfaceBrush", "MiniMica.SurfaceStrongBrush",
        "MiniMica.PageBackgroundBrush", "MiniMica.ChromeBackgroundBrush",
        "MiniMica.WindowBackgroundBrush", "MiniMica.CaptionHoverBrush",
        "MiniMica.CaptionPressedBrush", "MiniMica.CaptionCloseHoverBrush",
        "MiniMica.CaptionClosePressedBrush", "MiniMica.TitleBarBackgroundBrush",
        "MiniMica.TitleBarInactiveBackgroundBrush",
    }
    TEXT_ROLE = {
        "MiniMica.BrandInkBrush", "MiniMica.BrandAccentTextBrush",
        "MiniMica.TextPrimaryBrush", "MiniMica.TextSecondaryBrush",
        "MiniMica.TitleBarForegroundBrush", "MiniMica.TitleBarInactiveForegroundBrush",
        "MiniMica.CaptionGlyphBrush", "MiniMica.CaptionGlyphInactiveBrush",
        "MiniMica.CaptionHoverForegroundBrush", "MiniMica.CaptionCloseHoverForegroundBrush",
        "MiniMica.BrandOnAccentBrush",
    }
    FILL_PROPS = ("Background", "Fill")
    for p, raw in xmltext.items():
        rel = os.path.relpath(p, SRC)
        t = re.sub(r"<!--.*?-->", "", raw, flags=re.S)
        for m in re.finditer(r'(?:Property=")?(\w+)"?\s*(?:Value=)?"\{DynamicResource (MiniMica\.\w+)\}"', t):
            prop, key = m.group(1), m.group(2)
            line = t[:m.start()].count("\n") + 1
            if prop == "Foreground" and key in FILL_ROLE:
                errors.append(("fill-role brush used as text (invisible under a contrast theme)",
                               ["%s:%d: Foreground <- %s" % (rel, line, key)]))
            elif prop in FILL_PROPS and key in TEXT_ROLE:
                errors.append(("text-role brush used as a fill",
                               ["%s:%d: %s <- %s" % (rel, line, prop, key)]))

    print("  %d view(s) free of inline typography | %d asset reference(s) resolved | "
          "brush roles checked" % (len(views), len(referenced)))
    return errors, notes


def check_docs():
    """Documentation consistency.

    Docs drift silently: a renamed class or a deleted file leaves prose that reads
    plausibly and is wrong. These are the two failures that actually mislead —
    broken internal links, and code paths that no longer exist.
    """
    errors, notes = [], []
    md = sorted(glob.glob(os.path.join(REPO, "docs", "**", "*.md"), recursive=True))
    for name in ("README.md", "CHANGELOG.md", "CONTRIBUTING.md"):
        p = os.path.join(REPO, name)
        if os.path.exists(p):
            md.append(p)

    broken, ghosts = [], []
    for p in md:
        rel = os.path.relpath(p, REPO)
        text = _read(p)
        base = os.path.dirname(p)
        for m in re.finditer(r"\[([^\]]+)\]\(([^)#][^)]*?)(?:#[^)]*)?\)", text):
            target = m.group(2).strip()
            if target.startswith(("http://", "https://", "mailto:")):
                continue
            if not os.path.exists(os.path.normpath(os.path.join(base, target))):
                broken.append("%s -> %s" % (rel, target))
        for m in re.finditer(r"`((?:src|tools|scripts|templates|docs)/[A-Za-z0-9_./-]+\.[A-Za-z0-9]+)`", text):
            path = m.group(1)
            if "<" in path or ">" in path:
                continue
            if not os.path.exists(os.path.join(REPO, path)):
                ghosts.append("%s -> %s" % (rel, path))

    if broken:
        errors.append(("broken internal doc link", sorted(set(broken))))
    if ghosts:
        errors.append(("doc references a path that does not exist", sorted(set(ghosts))))

    # A renamed type leaves prose referring to the old name.
    stale = []
    for p in md:
        rel = os.path.relpath(p, REPO)
        if "0001" in rel:            # the superseded ADR may discuss the old design
            continue
        text = _read(p)
        if re.search(r"(?<!Mini)\bMicaWindow\b", text):
            stale.append("%s: MicaWindow (renamed to MiniMicaWindow)" % rel)
        if re.search(r"\bMiniMica\.sln\b", text):
            stale.append("%s: MiniMica.sln (now MiniMica.slnx)" % rel)
    if stale:
        errors.append(("stale identifier in documentation", sorted(set(stale))))

    print("  %d doc(s) checked for links, paths and stale identifiers" % len(md))
    return errors, notes


CHECKS = [
    ("localization", "resource keys, worksheet integrity, placeholder safety", check_localization),
    ("precompile",   "XAML/C# wiring the compiler would reject",              check_precompile),
    ("chrome",       "v4.1 pixel-precision title bar contract",               check_chrome),
    ("styles",       "centralized styling and asset wiring",                  check_styles),
    ("rename",       "dotnet new template generation and stale identifiers",  check_rename),
    ("docs",         "links, code paths and stale identifiers in the docs",   check_docs),
]
ALIAS = {"loc": "localization", "l10n": "localization", "pre": "precompile",
         "ren": "rename", "chr": "chrome", "sty": "styles", "doc": "docs"}


def main():
    want = sys.argv[1:] or [n for n, _, _ in CHECKS]
    want = {ALIAS.get(w, w) for w in want}
    total_err = 0
    print("MiniMica v5 verification suite")
    print("repo: %s" % REPO)
    for nm, desc, fn in CHECKS:
        if nm not in want:
            continue
        print("\n== %s - %s" % (nm, desc))
        try:
            errors, notes = fn()
        except Exception as exc:                       # a broken check must never look like a pass
            print("  CHECK CRASHED: %s: %s" % (type(exc).__name__, exc))
            total_err += 1
            continue
        for label, items in notes:
            print("  note: %s" % label)
            for i in items[:12]:
                print("        %s" % i)
        for label, items in errors:
            print("  FAIL: %s" % label)
            for i in items[:12]:
                print("        %s" % i)
            total_err += len(items)
        if not errors:
            print("  OK")
    print("\n%s" % ("PASSED - no static problems detected" if total_err == 0
                    else "FAILED - %d problem(s)" % total_err))
    return 1 if total_err else 0


if __name__ == "__main__":
    sys.exit(main())
