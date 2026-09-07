from inspect_game import *
d=(ROOT/'hotline_sprites.bin').read_bytes()
o=8+struct.unpack_from('<I',d)[0]*4
n=struct.unpack_from('<I',d,o-4)[0]
sp={struct.unpack_from('<I',d,o+148*i)[0]:d[o+148*i+56:o+148*(i+1)].split(b'\0')[0].decode() for i in range(n)}
d=(ROOT/'hotline_objects.bin').read_bytes()
o=8+struct.unpack_from('<I',d)[0]*4
n=struct.unpack_from('<I',d,o-4)[0]
ob={}
for i in range(n):
    r=struct.unpack_from('<10i',d,o+i*40)
    ob[r[0]]={'sprite':sp.get(r[1],str(r[1])),'parent':r[3],'depth':r[4]}
(ROOT/'objects.json').write_text(json.dumps(ob,indent=2))
for k,v in ob.items():
    if k in [25,8,31,21,9,191,59,44,6,173,29,594,747,649,320,193] or any(x in v['sprite'].lower() for x in ['bullet','player','unarmed','human','dog','panther']):
        print(k,v)
