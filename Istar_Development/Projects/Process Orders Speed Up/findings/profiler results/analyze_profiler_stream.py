"""Stream a Request Profiler SQL export (one JSON record per line) and break
a Process Orders long-run request down by order, owner and call site.

usage: python analyze_stream.py <folder> <long-run RecordId> [bucket size]
"""
import json, re, sys
from collections import Counter, defaultdict

folder, parent = sys.argv[1], int(sys.argv[2])
bucket = int(sys.argv[3]) if len(sys.argv) > 3 else 10

OWNERS = [
    ('FlexMFG', r'CreateShipmentExtensionFLXExt|FlxMFG|SNPMFG'),
    ('ASCiStarWMS', r'ASCiStarWMS'),
    ('ASCiStarKohls', r'ASCiStarKohls'),
    ('WMS', r'\bWMS\.'),
    ('TrueCommerce', r'TCAddon'),
    ('Asgard', r'AA\.Objects|Asgard'),
    ('RuntimeCode', r'MasterPack|SelectedPackageSort|PackageContentDefaultIssueFrom|PackMode|PickPackShip'),
]
FRAME = re.compile(r'at ([\w\.\+`<>]+)\(')

def owner_site(st):
    for m in FRAME.finditer(st or ''):
        f = m.group(1)
        for name, pat in OWNERS:
            if re.search(pat, f):
                return name, f
    return 'Base', ''

def records(path):
    with open(path, encoding='utf-8-sig') as fh:
        for line in fh:
            line = line.strip().lstrip('[').rstrip(']').rstrip(',')
            if line:
                yield json.loads(line)

orderRe = re.compile(r"SO0000\d{4}")
rows = []
for r in records(f'{folder}/SMPerformanceInfoSQL.log'):
    if r.get('ParentId') != parent:
        continue
    own, site = owner_site(r.get('StackTrace'))
    txt = (r.get('SQLText') or '').lstrip()
    rows.append((r['RequestStartTime'], r.get('SqlTimeMs') or 0, own, site, txt[:40],
                 orderRe.findall(r.get('SQLParams') or ''), 'UpdateShipmentCustomerOrderNbr' in (r.get('StackTrace') or '')))
rows.sort(key=lambda x: x[0])

first = {}
for t, *_rest in rows:
    pass
for row in rows:
    for o in row[5]:
        first.setdefault(o, row[0])
# Only real test orders (exclude numbering-sequence noise like SO00000005).
ends = [row[0] for row in rows if re.match(r'UPDATE\s+\[?SOOrder\]?\s', row[4])]
orders = [(f'#{i+1}', t) for i, t in enumerate(ends)]
first_q = rows[0][0]
starts = [first_q] + ends[:-1]
tail_start = ends[-1] + 0.001
end = rows[-1][0]

def seg(t):
    if t >= tail_start:
        return 'tail'
    lo, hi = 0, len(starts) - 1
    while lo < hi:
        mid = (lo + hi + 1) // 2
        if starts[mid] <= t: lo = mid
        else: hi = mid - 1
    return lo

tot, cnt, siteT, siteN = Counter(), Counter(), Counter(), Counter()
per = defaultdict(lambda: Counter())
for t, ms, own, site, txt, _, _ in rows:
    tot[own] += ms; cnt[own] += 1
    if site: siteT[(own, site)] += ms; siteN[(own, site)] += 1
    s = seg(t); p = per[s]
    p['n'] += 1; p['sql'] += ms
    if re.match(r'UPDATE\s+\[?SOPackageDetail\]?\s', txt): p['pkgUpd'] += 1
    if re.match(r'UPDATE\s+\[?SOShipment\]?\s', txt): p['shipUpd'] += 1

print(f'== {folder}: {len(rows)} queries, {sum(tot.values())/1000:.1f} s SQL, {len(orders)} orders, span {rows[0][0]/1000:.1f}-{end/1000:.1f} s')
print(f'   orders {starts[0]/1000:.1f}-{tail_start/1000:.1f} s | post-run loop {(end-tail_start)/1000:.1f} s, {per["tail"]["n"]} queries, {per["tail"]["sql"]/1000:.1f} s SQL, {per["tail"]["shipUpd"]} shipment saves')
print('-- by owner (queries, sql s)')
for k, v in cnt.most_common():
    print(f'  {k:14} {v:7} {tot[k]/1000:7.1f}')
print('-- top call sites (queries, sql s)')
for (own, s), v in sorted(siteN.items(), key=lambda kv: -siteT[kv[0]])[:16]:
    print(f'  {own:13} {v:6} {siteT[(own, s)]/1000:6.1f}  {s}')
print(f'-- per bucket of {bucket} orders: avg wall ms/order, avg queries/order, avg sql ms/order, avg package UPDATEs/order')
for b in range(0, len(orders), bucket):
    idx = range(b, min(b + bucket, len(orders)))
    wall = sum(((starts[i + 1] if i + 1 < len(starts) else tail_start) - starts[i]) for i in idx)
    n = len(idx)
    print(f'  orders {b+1:3}-{b+n:3}: {wall/n:7.0f} {sum(per[i]["n"] for i in idx)/n:6.0f} {sum(per[i]["sql"] for i in idx)/n:6.0f} {sum(per[i]["pkgUpd"] for i in idx)/n:6.1f}')

# Growth by call site: early orders vs late orders (per-order averages).
if len(sys.argv) > 4:
    a0, a1, b0, b1 = map(int, sys.argv[4].split(','))
    early, late = Counter(), Counter(); earlyN, lateN = Counter(), Counter()
    for t, ms, own, site, txt, _, _ in rows:
        s = seg(t)
        if s == 'tail': continue
        key = f'{own}: {site.split(".")[-1] if site else "(base)"}'
        if a0 <= s + 1 <= a1: early[key] += ms; earlyN[key] += 1
        if b0 <= s + 1 <= b1: late[key] += ms; lateN[key] += 1
    ne, nl = a1 - a0 + 1, b1 - b0 + 1
    print(f'-- growth: orders {a0}-{a1} vs {b0}-{b1} (per order: queries, sql ms)')
    keys = sorted(set(early) | set(late), key=lambda k: -(late[k]/nl - early[k]/ne))
    for k in keys[:12]:
        print(f'  {k:62} {earlyN[k]/ne:6.0f} {early[k]/ne:6.0f}  ->  {lateN[k]/nl:6.0f} {late[k]/nl:6.0f}')
