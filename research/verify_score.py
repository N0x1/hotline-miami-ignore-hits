"""Verify score storage and thresholds from the supported local executable."""
from functions import *
import hashlib
assert hashlib.sha256(data).hexdigest().upper() == json.loads((ROOT.parent/'patch-manifest.json').read_text())['sha256']
def raw(va,n): return data[pe.get_offset_from_rva(va-base):pe.get_offset_from_rva(va-base)+n]
# Global lookup returns [global table] + index*8; wrapper's +4 is its value object.
assert raw(0x407890,15).hex() == '558bec8b018b4d088d04c85dc20400'
assert raw(0x41ea90,10).hex() == '8b49048b018b5010ffe2'
assert struct.unpack('<I',raw(0xab8bf4+16,4))[0] == 0x8d4e90
assert struct.unpack('<I',raw(0x8d4fb4+4,4))[0] == 0x8d4ede
assert struct.unpack('<I',raw(0x8d4fb4+8,4))[0] == 0x8d4eca
assert raw(0x8d4ede,3).hex() == 'db4108' # kind 1: int32 at +8
assert raw(0x8d4eca,3).hex() == 'dd4108' # kind 2: double at +8
reset=instructions(0x805bc0)
global_indices=[]
for i,ins in enumerate(reset):
    if ins.mnemonic=='call' and ins.op_str=='0x407890':
        assert reset[i-1].op_str == 'ecx, 0xfffcd8'
        global_indices.append(int(reset[i-2].op_str,0))
assert global_indices[:10] == [0x254,0x69,0x243,0x28b,0x24a,0x246,0x2a6,0x295,0x296,0x29c]
# Native results screen resets mission score, reads kill score as bonus[0], then sums bonuses.
assert raw(0x5c0e5a,4).hex() == '6a006a69'
assert raw(0x5c0e6f,5).hex() == '684a020000'
assert raw(0x5c13be,2).hex() == '6a69'
# Mission time is 0x131: gameplay increments it by one and results use it
# in the 18000-time bonus. 0x24F was incorrectly identified from reset order.
assert raw(0x5d9e9b,7).hex() == '6a016831010000'
assert raw(0x5de0e1,7).hex() == '6a016831010000'
assert raw(0x5c0f92,5).hex() == '6831010000'
assert raw(0x6a7cb5,5).hex() == '6831010000'
thresholds=[]
for ins in instructions(0x8131f0):
    if ins.address>=0x814240:break
    if ins.mnemonic=='push' and ins.op_str.startswith('0x'):
        v=int(ins.op_str,16)
        if 10000<=v<=200000:thresholds.append(v)
assert len(thresholds)==17 and max(thresholds)==138000
assert 200000>max(thresholds)
report=dict(result='PASS', globals_pointer_rva='0xBFFCD8', numeric_vtable_rva='0x6B8BF4',
 fields={'killscore':'0x24A','myscore':'0x69','drawscore':'0x254','mission_time':'0x131'},
 native_threshold_values=thresholds, maximum_threshold=138000, boost_floor=200000,
 scope='Static native storage, timer increments, reset, tally and max-points verification. Live score confirmation is recorded separately in verification-live-score.txt; final A+ grade not verified.')
(ROOT.parent/'verification-score.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
