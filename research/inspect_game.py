import sys, pathlib, struct, re, json
ROOT = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'pylibs'))
import pefile
from capstone import Cs, CS_ARCH_X86, CS_MODE_32
GAME = pathlib.Path(r'C:\Program Files (x86)\Steam\steamapps\common\hotline_miami')
data = (GAME / 'HotlineGL.exe').read_bytes()
pe = pefile.PE(data=data)
base = pe.OPTIONAL_HEADER.ImageBase
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True
def dis(va, size):
    off = pe.get_offset_from_rva(va-base)
    for i in md.disasm(data[off:off+size], va):
        print(f'{i.address:08x}  {i.bytes.hex():24s} {i.mnemonic:8s} {i.op_str}')
def refs(va):
    needle = struct.pack('<I',va)
    return [base+pe.get_rva_from_offset(m.start()) for m in re.finditer(re.escape(needle),data)]
def strings(pattern):
    for m in re.finditer(rb'[ -~]{5,}',data):
        if re.search(pattern,m.group(),re.I):
            va=base+pe.get_rva_from_offset(m.start())
            print(hex(va), m.group().decode(), [hex(x) for x in refs(va)])
def wad():
    f=(GAME/'HotlineMiami_GL.wad').open('rb')
    u32=lambda:struct.unpack('<I',f.read(4))[0]
    start,n=u32(),u32()
    entries=[]
    for _ in range(n):
        name=f.read(u32()).decode(); size,off=u32(),u32()
        entries.append(dict(name=name,size=size,offset=start+off))
    (ROOT/'wad_index.json').write_text(json.dumps(entries,indent=2))
    for e in entries:
        if e['name'].endswith('.bin'):
            print(e)
            f.seek(e['offset']); content=f.read(e['size'])
            (ROOT/pathlib.PurePosixPath(e['name']).name).write_bytes(content)
            print(content[:100].hex(' '))
if __name__=='__main__':
    if sys.argv[1]=='wad': wad()
    elif sys.argv[1]=='dis': dis(int(sys.argv[2],16), int(sys.argv[3],0))
    elif sys.argv[1]=='refs': print([hex(x) for x in refs(int(sys.argv[2],16))])
    elif sys.argv[1]=='strings': strings(sys.argv[2].encode())
    else:
        print(hex(base),[(s.Name,hex(s.VirtualAddress),hex(s.PointerToRawData),hex(s.SizeOfRawData)) for s in pe.sections])
