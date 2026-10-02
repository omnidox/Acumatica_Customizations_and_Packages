import json, re, sys
from collections import Counter, defaultdict

run = sys.argv[1]
parent = int(sys.argv[2])
rows = [r for r in json.load(open(f'{run}/SMPerformanceInfoSQL.log', encoding='utf-8-sig'))
        if r.get('ParentId') == parent]
rows.sort(key=lambda r: r['RequestStartTime'])

# Attribute each query to the first customization frame in its stack.
OWNERS = [
    ('FlexMFG', r'CreateShipmentExtensionFLXExt|FlxMFG|SNPMFG'),
    ('ASCiStarKohls', r'ASCiStarKohls'),
    ('WMS', r'\bWMS\.'),
    ('TrueCommerce', r'TCAddon'),
    ('Asgard', r'AA\.Objects|Asgard'),
    ('RuntimeCode', r'MasterPack|SelectedPackageSort|PackageContentDefaultIssueFrom|SOShipmentEntry_Ext'),
]
def owner(st):
    for line in (st or '').split('\n'):
        for name, pat in OWNERS:
            if re.search(pat, line):
                return name
    return 'Base'

# The method a query was issued from, inside the owning code.
def site(st, own):
    pat = dict(OWNERS).get(own)
    for line in (st or '').split('\n'):
        if pat and re.search(pat, line):
            m = re.search(r'at ([\w\.\+`<>]+)\(', line)
            return m.group(1) if m else line.strip()[:120]
    return ''

# Split into orders: an order starts at the first query whose params mention it.
orderRe = re.compile(r"SO0000\d{4}")
first_seen = {}
for r in rows:
    for o in orderRe.findall(r.get('SQLParams') or ''):
        first_seen.setdefault(o, r['RequestStartTime'])
orders = sorted(first_seen.items(), key=lambda kv: kv[1])
bounds = [t for _, t in orders] + [rows[-1]['RequestStartTime'] + 1]

def order_of(t):
    for i in range(len(orders)):
        if bounds[i] <= t < bounds[i + 1]:
            return i
    return -1

tot = Counter(); cnt = Counter(); sites = Counter(); sitesT = Counter()
per = defaultdict(lambda: {'n': 0, 'sql': 0.0, 'upd': 0, 'shipUpd': 0, 'pkgUpd': 0})
for r in rows:
    own = owner(r.get('StackTrace'))
    t = r.get('SqlTimeMs') or 0
    tot[own] += t; cnt[own] += 1
    s = site(r.get('StackTrace'), own)
    if s:
        sites[(own, s)] += 1; sitesT[(own, s)] += t
    i = order_of(r['RequestStartTime'])
    p = per[i]; p['n'] += 1; p['sql'] += t
    txt = (r.get('SQLText') or '').lstrip()
    if txt.startswith(('UPDATE', 'INSERT', 'DELETE')):
        p['upd'] += 1
        if re.match(r'UPDATE\s+\[?SOShipment\]?\s', txt): p['shipUpd'] += 1
        if re.match(r'UPDATE\s+\[?SOPackageDetail\]?\s', txt): p['pkgUpd'] += 1

print(f'== {run}: {len(rows)} queries, {sum(tot.values()):.0f} ms SQL, {len(orders)} orders detected')
print('-- by owner (queries, sql ms)')
for k, v in cnt.most_common():
    print(f'  {k:14} {v:6} {tot[k]:8.0f}')
print('-- top call sites in custom code (queries, sql ms)')
for (own, s), v in sorted(sites.items(), key=lambda kv: -sitesT[kv[0]])[:18]:
    print(f'  {own:13} {v:5} {sitesT[(own, s)]:7.0f}  {s}')
print('-- per order: wall ms (to next order), queries, sql ms, writes, SOShipment updates, SOPackageDetail updates')
for i, (o, t) in enumerate(orders):
    p = per[i]
    print(f'  {i+1:2} {o} {bounds[i+1]-t:8.0f} {p["n"]:5} {p["sql"]:7.0f} {p["upd"]:4} {p["shipUpd"]:3} {p["pkgUpd"]:3}')
