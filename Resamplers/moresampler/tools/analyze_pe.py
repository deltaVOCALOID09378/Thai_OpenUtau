import struct
import sys
import os

exe_path = r"D:\Program Developing\moresampler-0.8.4\Moresampler-main\moresampler.exe"

with open(exe_path, "rb") as f:
    data = f.read()

print(f"File size: {len(data)} bytes")

# DOS Header
if data[:2] != b"MZ":
    print("Not a valid PE file (missing MZ)")
    sys.exit(1)

e_lfanew = struct.unpack_from("<I", data, 0x3C)[0]
print(f"PE Header offset: 0x{e_lfanew:X}")

pe_sig = data[e_lfanew:e_lfanew+4]
if pe_sig != b"PE\x00\x00":
    print("Invalid PE signature")
    sys.exit(1)

machine, num_sections, timedate, symtab, num_syms, opt_hdr_size, characteristics = struct.unpack_from("<HHIIIHH", data, e_lfanew+4)
machine_str = "x86 (32-bit)" if machine == 0x14C else ("x64 (64-bit)" if machine == 0x8664 else f"0x{machine:X}")
print(f"Machine: {machine_str}")
print(f"Sections count: {num_sections}")
print(f"Characteristics: 0x{characteristics:X}")

opt_hdr_offset = e_lfanew + 24
magic = struct.unpack_from("<H", data, opt_hdr_offset)[0]
magic_str = "PE32" if magic == 0x10B else ("PE32+" if magic == 0x20B else f"0x{magic:X}")
print(f"Optional Header Magic: {magic_str}")

entry_point = struct.unpack_from("<I", data, opt_hdr_offset + 16)[0]
image_base = struct.unpack_from("<I" if magic == 0x10B else "<Q", data, opt_hdr_offset + (28 if magic == 0x10B else 24))[0]
print(f"Entry Point RVA: 0x{entry_point:X}")
print(f"Image Base: 0x{image_base:X}")

# Section headers
sec_hdr_offset = opt_hdr_offset + opt_hdr_size
print("\nSections:")
sections = []
for i in range(num_sections):
    sec = data[sec_hdr_offset + i*40 : sec_hdr_offset + (i+1)*40]
    name = sec[:8].rstrip(b"\x00").decode("latin1")
    vsize, vrva, rsize, roffset = struct.unpack_from("<IIII", sec, 8)
    flags = struct.unpack_from("<I", sec, 36)[0]
    print(f"  {name:8} VSize: 0x{vsize:06X}  VRVA: 0x{vrva:08X}  RawSize: 0x{rsize:06X}  RawOffset: 0x{roffset:08X}  Flags: 0x{flags:08X}")
    sections.append((name, vrva, vsize, roffset, rsize))

# Search for interesting strings
import re
print("\nSearching strings...")
ascii_strings = re.findall(b"[ -~]{5,}", data)
print(f"Total ASCII strings (len>=5): {len(ascii_strings)}")

keywords = [b"LLSM", b"llsm", b"world", b"Harvest", b"Dio", b"D4C", b"StoneMask", b"moresampler", b"wavtool", b"resampler", b"lrhsmm", b"gvps", b"fastlz", b"cJSON", b"lua", b"ooura", b"fft", b"pitch", b"spectrum", b"harmonic", b"oto.ini", b"flags", b"f0", b"formant"]

for kw in keywords:
    matches = [s.decode('latin1', errors='ignore') for s in ascii_strings if kw.lower() in s.lower()]
    print(f"Keyword '{kw.decode()}': found {len(matches)} occurrences. Sample: {matches[:3]}")
