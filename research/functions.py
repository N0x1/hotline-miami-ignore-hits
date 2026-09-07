from inspect_game import *
import collections, bisect
starts=sorted(set(base+pe.get_rva_from_offset(m.end()-3) for m in re.finditer(rb'\xcc+\x55\x8b\xec',data)))
def instructions(va):
    end=starts[bisect.bisect_right(starts,va)]
    off=pe.get_offset_from_rva(va-base)
    return list(md.disasm(data[off:off+end-va],va))
def calls(va):
    return collections.Counter(i.op_str for i in instructions(va) if i.mnemonic=='call')
if __name__=='__main__':
    for a in sys.argv[1:]:
        va=int(a,16)
        ins=instructions(va)
        (ROOT/(a+'.asm')).write_text('\n'.join(f'{i.address:08x} {i.mnemonic:8s} {i.op_str}' for i in ins))
        print(a,'end',hex(ins[-1].address),'calls',calls(va))
