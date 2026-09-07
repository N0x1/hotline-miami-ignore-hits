from inspect_game import *
from unicorn import Uc,UC_ARCH_X86,UC_MODE_32
from unicorn.x86_const import *
u=Uc(UC_ARCH_X86,UC_MODE_32)
u.mem_map(base, (pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095)
u.mem_write(base,pe.get_memory_mapped_image())
STACK=0x10000000
u.mem_map(STACK,0x20000)
def invoke(fn,*args):
    esp=STACK+0x10000
    u.mem_write(esp,struct.pack('<'+'I'*(len(args)+1),STACK,*args))
    u.reg_write(UC_X86_REG_ESP,esp)
    u.reg_write(UC_X86_REG_EBP,0)
    u.emu_start(fn,STACK,count=2000)
    assert u.reg_read(UC_X86_REG_EIP)==STACK
    assert u.reg_read(UC_X86_REG_ESP)==esp+4
    return u.reg_read(UC_X86_REG_EAX)
def event(id,typ,sub): return invoke(0x7074d0,id,typ,sub)
if __name__=='__main__':
    d=(ROOT/'hotline_collision_events.bin').read_bytes()
    n=struct.unpack_from('<I',d)[0];o=4;rows=[]
    for _ in range(n):
        id,c=struct.unpack_from('<II',d,o);o+=8
        for target in struct.unpack_from('<'+'I'*c,d,o):
            fn=event(id,4,target)
            rows.append(dict(object=id,target=target,function=hex(fn)))
        o+=4*c
    (ROOT/'collision_functions.json').write_text(json.dumps(rows,indent=2))
    print('Mapped',len(rows),'collision handlers')
    print([r for r in rows if r['target']==25])
