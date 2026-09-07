"""Offline x86 verification against the installed game; never attaches to it."""
from emulate import *
from unicorn import UC_HOOK_MEM_WRITE
import hashlib, collections
manifest=json.loads((ROOT.parent/'patch-manifest.json').read_text())
assert hashlib.sha256(data).hexdigest().upper()==manifest['sha256']
rows=json.loads((ROOT/'collision_functions.json').read_text())
objects=json.loads((ROOT/'objects.json').read_text())
def is_player(obj):
    seen=set()
    while obj>=0 and obj not in seen:
        if obj==44:return True
        seen.add(obj);obj=objects[str(obj)]['parent']
    return False
bullet=[r for r in rows if r['target']==25]
assert len(bullet)==32 and all(is_player(r['object']) for r in bullet)
original_routes=[event(r['object'],4,r['target']) for r in rows]
original_code={fn:bytes(u.mem_read(fn,16)) for fn in set(original_routes) if fn}
patches={base+s['rva'] for s in manifest['sites']}
for s in manifest['sites']:
    address=base+s['rva']; expected=bytes.fromhex(s['original'])
    assert bytes(u.mem_read(address,len(expected)))==expected
    u.mem_write(address,b'\xc3')
assert [event(r['object'],4,r['target']) for r in rows]==original_routes
for r in rows:
    fn=int(r['function'],16)
    if r['target']!=25 and fn:
        assert fn not in patches
        assert bytes(u.mem_read(fn,16))==original_code[fn]
writes=[]
def memory_write(uc,access,address,size,value,user):
    if not STACK<=address<STACK+0x20000:writes.append((address,size))
hook=u.hook_add(UC_HOOK_MEM_WRITE,memory_write)
for r in bullet:
    invoke(int(r['function'],16),0xDEADBEEF,0xBAADF00D)
for s in manifest['sites']:
    invoke(base+s['rva'],0xDEADBEEF,0xBAADF00D)
assert not writes
u.hook_del(hook)
for s in manifest['sites']:u.mem_write(base+s['rva'],bytes.fromhex(s['original']))
for fn,before in original_code.items(): assert bytes(u.mem_read(fn,16))==before
for s in manifest['sites']: assert bytes(u.mem_read(base+s['rva'],5))==bytes.fromhex(s['original'])
report={
 'result':'PASS', 'game_sha256':manifest['sha256'],
 'patch_sites':len(patches),'collision_routes_checked':len(rows),
 'player_enemy_bullet_routes_verified':len(bullet),
 'unrelated_collision_routes_unchanged':len(rows)-len(bullet),
 'memory_writes_outside_emulator_stack':len(writes),
 'restoration':'all original bytes restored',
 'gameplay_tested':False,
 'scope':'x86 emulation and static dispatch verification; no game was launched or attached'
}
(ROOT.parent/'verification-offline.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
