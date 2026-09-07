from inspect_game import *
import hashlib
sites = [
 (0x447c70, 'Jacket enemy-bullet collision'),
 (0x452e70, 'Human-shield enemy-bullet collision'),
 (0x466f60, 'Alternate Jacket enemy-bullet collision'),
 (0x474ca0, 'Biker enemy-bullet collision'),
 (0x47cfb0, 'Biker-derived state enemy-bullet collision'),
 (0x4b9d60, 'Alternate player state enemy-bullet collision'),
 (0x4bf060, 'Execution state enemy-bullet collision'),
 (0x4cd700, 'Special execution state enemy-bullet collision'),
 (0x642a40, 'Shared execution enemy-bullet collision wrapper'),
 (0x744b40, 'Shared player melee death'),
 (0x749000, 'Player dog death'),
 (0x74b270, 'Player panther death'),
 (0x74d2f0, 'Alternate player dog death'),
]
manifest = dict(executable='HotlineGL.exe', sha256=hashlib.sha256(data).hexdigest().upper(), sites=[])
for va,label in sites:
 off=pe.get_offset_from_rva(va-base)
 manifest['sites'].append(dict(name=label,rva=va-base,original=data[off:off+5].hex(' ').upper(),enabled='C3'))
out=ROOT.parent
(out/'src').mkdir(exist_ok=True)
(out/'patch-manifest.json').write_text(json.dumps(manifest,indent=2))
cs='// Generated from the installed executable by research/make_manifest.py.\n'
cs+='internal static class Definitions {\n public const string Hash = "'+manifest['sha256']+'";\n public static readonly PatchSite[] Sites = {\n'
for s in manifest['sites']:
 cs+='  new PatchSite(0x%X, "%s", new byte[] {%s}),\n'%(s['rva'],s['name'],','.join('0x'+x for x in s['original'].split()))
cs+=' };\n}\n'
(out/'src'/'Definitions.cs').write_text(cs)
print('Wrote',len(sites),'patch definitions; game files unchanged.')
