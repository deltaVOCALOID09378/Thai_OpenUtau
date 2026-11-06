import struct
import sys
import re

exe_path = r"D:\Program Developing\moresampler-0.8.4\Moresampler-main\moresampler.exe"

with open(exe_path, "rb") as f:
    data = f.read()

e_lfanew = struct.unpack_from("<I", data, 0x3C)[0]
opt_hdr_offset = e_lfanew + 24
magic = struct.unpack_from("<H", data, opt_hdr_offset)[0]
opt_hdr_size = struct.unpack_from("<H", data, e_lfanew + 20)[0]
num_sections = struct.unpack_from("<H", data, e_lfanew + 6)[0]

# Data directories
# Import Table is entry 1 (offset 96 from opt_hdr_offset for 32-bit, or 104 for 64-bit)
# For PE32 (32-bit):
# Optional header standard fields: 28 bytes
# NT-specific fields: 68 bytes
# Total to DataDirectory: 96 bytes
dd_offset = opt_hdr_offset + (96 if magic == 0x10B else 112)
import_rva, import_size = struct.unpack_from("<II", data, dd_offset + 8)
rsrc_rva, rsrc_size = struct.unpack_from("<II", data, dd_offset + 16)

sec_hdr_offset = opt_hdr_offset + opt_hdr_size
sections = []
for i in range(num_sections):
    sec = data[sec_hdr_offset + i*40 : sec_hdr_offset + (i+1)*40]
    name = sec[:8].rstrip(b"\x00").decode("latin1")
    vsize, vrva, rsize, roffset = struct.unpack_from("<IIII", sec, 8)
    sections.append((name, vrva, vsize, roffset, rsize))

def rva_to_offset(rva):
    for name, vrva, vsize, roffset, rsize in sections:
        if vrva <= rva < vrva + vsize:
            return roffset + (rva - vrva)
    return None

print(f"Import Table RVA: 0x{import_rva:X}, Size: 0x{import_size:X}")
import_offset = rva_to_offset(import_rva)
print(f"Import Table File Offset: {hex(import_offset) if import_offset else 'None'}")

# Parse imports
if import_offset:
    curr = import_offset
    print("\n--- IMPORTED DLLS & FUNCTIONS ---")
    while True:
        orig_first_thunk, timedate, forwarder, name_rva, first_thunk = struct.unpack_from("<IIIII", data, curr)
        if name_rva == 0:
            break
        dll_name_off = rva_to_offset(name_rva)
        dll_name = data[dll_name_off:data.find(b"\x00", dll_name_off)].decode("latin1")
        print(f"\nDLL: {dll_name}")
        
        # Thunk table
        thunk_rva = orig_first_thunk if orig_first_thunk != 0 else first_thunk
        thunk_off = rva_to_offset(thunk_rva)
        func_names = []
        if thunk_off:
            t_curr = thunk_off
            while True:
                val = struct.unpack_from("<I", data, t_curr)[0]
                if val == 0:
                    break
                if val & 0x80000000: # Ordinal
                    func_names.append(f"Ordinal_{val & 0xFFFF}")
                else:
                    fn_off = rva_to_offset(val)
                    if fn_off:
                        fn_name = data[fn_off+2:data.find(b"\x00", fn_off+2)].decode("latin1")
                        func_names.append(fn_name)
                t_curr += 4
        print(f"  Imported {len(func_names)} functions: {', '.join(func_names[:10])}...")
        curr += 20

# Search for embedded Lua scripts
print("\n--- SEARCHING FOR EMBEDDED LUA SCRIPTS ---")
# Lua bytecode header signature is \x1bLua
lua_sig = b"\x1bLua"
pos = 0
found_lua = []
while True:
    pos = data.find(lua_sig, pos)
    if pos == -1:
        break
    found_lua.append(pos)
    pos += 4

print(f"Found {len(found_lua)} compiled Lua bytecode signatures at file offsets: {[hex(p) for p in found_lua]}")

# Look for Lua source code patterns
lua_kw = [b"function", b"end", b"local ", b"return "]
# Scan for chunks of text containing lua code
text_chunks = re.findall(b"(?:[^\x00\r\n]{3,}\r?\n){5,}", data)
lua_text_chunks = [c for c in text_chunks if b"function" in c and (b"end" in c or b"local" in c)]
print(f"Found {len(lua_text_chunks)} possible Lua source text blocks!")
for i, chunk in enumerate(lua_text_chunks[:5]):
    print(f"\n--- Potential Lua chunk #{i+1} ---")
    print(chunk[:500].decode("latin1", errors="replace"))

# Scan for internal function / debug strings in Moresampler
print("\n--- MORESAMPLER LOG & FUNCTION STRINGS ---")
all_strings = [s.decode('latin1', errors='ignore') for s in re.findall(b"[A-Za-z0-9_.:/\\-]{4,}", data)]
ms_strings = set()
for s in all_strings:
    if any(k in s for k in ["main_", "lmix_", "llsm", "f0_", "oto", "vcv", "wavtool", "resampler", "gvps", "hsmm", "pitch", "spectral", "synthesize", "estimate_", "calc_"]):
        ms_strings.add(s)

print(f"Total matching function/internal identifier strings: {len(ms_strings)}")
for s in sorted(list(ms_strings))[:60]:
    print(f"  {s}")
