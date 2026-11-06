#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""
arpasing_oto_balancer.py
e:\Program Developing\Moresampler-main\arpasing_oto_balancer.py
Made And Checked By DELTA SYNTH & Gemini AI
Original by DELTA SYNTH
Revision: 2.4
==============================================================================
English Arpasing Oto.ini Balancer Engine
DELTA SYNTH — AI Development, Architecture & Optimization Standard (Rule 10)
==============================================================================
"""

import sys
import os
import re
import wave
import struct
import math
import argparse
from typing import List, Tuple, Dict, Optional

# UTF-8 stdout/stderr protection for multilingual paths (Thai, Japanese Kana, Latin)
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

# Supported Encodings
ENCODINGS = ["utf-8-sig", "utf-8", "cp874", "shift_jis", "latin-1"]

# Cache to avoid re-reading the same WAV header repeatedly
_WAV_DURATION_CACHE: Dict[str, Optional[float]] = {}
_WAV_RMS_PROFILE_CACHE: Dict[str, Optional[Tuple[int, float, List[float]]]] = {}
_ACOUSTIC_BOUNDS_CACHE: Dict[Tuple[str, float, Optional[float]], Tuple[Optional[float], Optional[float]]] = {}

def get_wav_duration_ms(wav_path: str) -> Optional[float]:
    """Reads WAV duration in milliseconds using standard library wave module with caching."""
    if wav_path in _WAV_DURATION_CACHE:
        return _WAV_DURATION_CACHE[wav_path]
    if not os.path.isfile(wav_path):
        _WAV_DURATION_CACHE[wav_path] = None
        return None
    try:
        with wave.open(wav_path, "rb") as wf:
            frames = wf.getnframes()
            rate = wf.getframerate()
            if rate > 0:
                dur = (frames / float(rate)) * 1000.0
                _WAV_DURATION_CACHE[wav_path] = dur
                return dur
    except Exception:
        pass
    _WAV_DURATION_CACHE[wav_path] = None
    return None


def get_wav_rms_profile(wav_path: str, win_ms: float = 10.0) -> Optional[Tuple[int, float, List[float]]]:
    """
    Profiles the entire WAV file once into RMS energy windows.
    Caches in memory so slicing any alias takes microseconds.
    """
    if wav_path in _WAV_RMS_PROFILE_CACHE:
        return _WAV_RMS_PROFILE_CACHE[wav_path]

    if not os.path.isfile(wav_path):
        _WAV_RMS_PROFILE_CACHE[wav_path] = None
        return None

    try:
        with wave.open(wav_path, "rb") as wf:
            sr = wf.getframerate()
            nframes = wf.getnframes()
            nchannels = wf.getnchannels()
            sampwidth = wf.getsampwidth()

            if sr <= 0 or nframes <= 0:
                _WAV_RMS_PROFILE_CACHE[wav_path] = None
                return None

            raw = wf.readframes(nframes)
            if not raw:
                _WAV_RMS_PROFILE_CACHE[wav_path] = None
                return None

            # Decode PCM based on bit depth
            if sampwidth == 2:
                # 16-bit signed integer
                count = len(raw) // 2
                samples = struct.unpack(f"<{count}h", raw)
            elif sampwidth == 1:
                # 8-bit unsigned integer
                samples = [s - 128 for s in raw]
            elif sampwidth == 3:
                # 24-bit PCM
                samples = [
                    int.from_bytes(raw[i:i+3], byteorder='little', signed=True)
                    for i in range(0, len(raw), 3)
                ]
            elif sampwidth == 4:
                # 32-bit float or int
                try:
                    samples = struct.unpack(f"<{len(raw) // 4}f", raw)
                except Exception:
                    samples = struct.unpack(f"<{len(raw) // 4}i", raw)
            else:
                _WAV_RMS_PROFILE_CACHE[wav_path] = None
                return None

            # Convert multi-channel to mono amplitude
            if nchannels > 1:
                mono = [max(abs(samples[i]), abs(samples[i+1])) for i in range(0, len(samples) - nchannels + 1, nchannels)]
            else:
                mono = [abs(s) for s in samples]

            win_samples = max(1, int(sr * (win_ms / 1000.0)))
            rms_list = []
            for i in range(0, len(mono) - win_samples + 1, win_samples):
                chunk = mono[i:i+win_samples]
                rms = math.sqrt(sum(x * x for x in chunk) / len(chunk))
                rms_list.append(rms)

            profile = (sr, win_ms, rms_list)
            _WAV_RMS_PROFILE_CACHE[wav_path] = profile
            
            # Cache duration as well
            if wav_path not in _WAV_DURATION_CACHE or _WAV_DURATION_CACHE[wav_path] is None:
                _WAV_DURATION_CACHE[wav_path] = (nframes / float(sr)) * 1000.0

            return profile
    except Exception:
        _WAV_RMS_PROFILE_CACHE[wav_path] = None
        return None


def is_soft_consonant_alias(alias: str) -> bool:
    """
    Identifies if alias contains a soft, airy, or voiceless consonant
    characteristic of thin/light vocalists (f, th, s, sh, hh, ch, p, t, k, wh, etc.).
    """
    alias_clean = alias.strip().lower()
    soft_phonemes = {
        "f", "th", "s", "sh", "hh", "ch", "p", "t", "k", "wh", "jh", "v", "dh", "z", "zh"
    }
    parts = alias_clean.replace("-", " ").split()
    return any(p in soft_phonemes for p in parts)


def get_consonant_lead_cushion(alias: str) -> float:
    """
    Returns acoustic attack cushion in milliseconds based on consonant type.
    - Plosives (p, t, k, b, d, g, q): 10ms (tight, punchy burst)
    - Fricatives/Aspirates (s, sh, f, th, hh, ch, etc.): 12ms (preserves soft breath/frication)
    - Vowels/Sonorants: 15ms (natural acoustic attack)
    """
    alias_clean = alias.strip().lower()
    parts = alias_clean.replace("-", " ").split()
    if any(p in {"p", "t", "k", "b", "d", "g", "q"} for p in parts):
        return 10.0
    if any(p in {"s", "sh", "f", "th", "dh", "hh", "ch", "jh", "z", "zh", "wh"} for p in parts):
        return 12.0
    return 15.0


def analyze_acoustic_bounds(
    wav_path: str,
    region_start: float = 0.0,
    region_end: Optional[float] = None,
    is_soft_consonant: bool = False
) -> Tuple[Optional[float], Optional[float]]:
    """
    Acoustically analyzes PCM samples to detect real vocal energy emergence (sound_start)
    and termination (sound_end) above the ambient noise floor.
    Features:
    - Thin/airy vocalists ("นักร้องบาง") sensitive threshold protection
    - Multi-window click & pop rejection (filters mouth clicks & electrical spikes)
    - Lead-in noise & trailing room tone / hiss trimming
    - Microsecond slicing via whole-file RMS profile caching
    """
    cache_key = (wav_path, round(region_start, 1), round(region_end, 1) if region_end else None, is_soft_consonant)
    if cache_key in _ACOUSTIC_BOUNDS_CACHE:
        return _ACOUSTIC_BOUNDS_CACHE[cache_key]

    profile = get_wav_rms_profile(wav_path)
    if profile is None:
        _ACOUSTIC_BOUNDS_CACHE[cache_key] = (None, None)
        return None, None

    sr, win_ms, rms_list = profile
    if not rms_list:
        _ACOUSTIC_BOUNDS_CACHE[cache_key] = (None, None)
        return None, None

    total_dur = len(rms_list) * win_ms
    if region_end is None or region_end > total_dur:
        region_end = total_dur
    if region_start < 0.0:
        region_start = 0.0
    if region_start >= region_end:
        _ACOUSTIC_BOUNDS_CACHE[cache_key] = (None, None)
        return None, None

    start_idx = max(0, int(region_start / win_ms))
    end_idx = min(len(rms_list), int(math.ceil(region_end / win_ms)))

    sub_rms = rms_list[start_idx:end_idx]
    if not sub_rms:
        _ACOUSTIC_BOUNDS_CACHE[cache_key] = (None, None)
        return None, None

    max_rms = max(sub_rms)
    if max_rms < 150.0:  # Pure silence or extremely faint
        _ACOUSTIC_BOUNDS_CACHE[cache_key] = (None, None)
        return None, None

    sorted_rms = sorted(sub_rms)
    noise_sample_count = max(1, len(sorted_rms) // 10)
    noise_floor = sum(sorted_rms[:noise_sample_count]) / noise_sample_count

    if is_soft_consonant:
        # Thin singer / airy consonant adaptive threshold:
        # Sensitive multiplier (1.45x noise floor) to capture faint breath/fricative without clipping
        thresh = max(noise_floor * 1.45, min(max_rms * 0.018, 180.0))
    else:
        # Standard voiced vocal / vowel threshold
        thresh = max(noise_floor * 2.2, max_rms * 0.035, 260.0)

    sound_start = None
    for i in range(len(sub_rms)):
        if sub_rms[i] > thresh:
            # Click rejection: require 2-window sustained energy or rising ramp into sound
            is_sustained = (i + 1 < len(sub_rms) and sub_rms[i + 1] > thresh * 0.85)
            is_ramp = (i + 1 < len(sub_rms) and sub_rms[i + 1] > sub_rms[i] * 1.25)
            is_end = (i == len(sub_rms) - 1)
            if is_sustained or is_ramp or is_end:
                sound_start = (start_idx + i) * win_ms
                break

    sound_end = None
    for i in range(len(sub_rms) - 1, -1, -1):
        if sub_rms[i] > thresh:
            # Trailing artifact rejection: require sustained energy before cutoff
            is_sustained = (i - 1 >= 0 and sub_rms[i - 1] > thresh * 0.85)
            is_ramp = (i - 1 >= 0 and sub_rms[i - 1] > sub_rms[i] * 1.1)
            is_start = (i == 0)
            if is_sustained or is_ramp or is_start:
                sound_end = (start_idx + i + 1) * win_ms
                break

    res = (sound_start, sound_end)
    _ACOUSTIC_BOUNDS_CACHE[cache_key] = res
    return res


# Arpabet Phoneme Sets
ARPABET_VOWELS = {
    "aa", "ae", "ah", "ao", "aw", "ay", "eh", "er", "ey", "ih", "iy", "ow", "oy", "uh", "uw",
    "ax", "ix", "ux", "a", "i", "u", "e", "o", "oh"
}

ARPABET_CONSONANTS = {
    "b", "ch", "d", "dh", "f", "g", "hh", "jh", "k", "l", "m", "n", "ng", "p", "r", "s", "sh", "t", "th", "v", "w", "y", "z", "zh",
    "dx", "q", "nx", "h", "j"
}


class ArpasingEntry:
    def __init__(self, filename: str, alias: str, offset: float, consonant: float, cutoff: float, preutterance: float, overlap: float):
        self.filename = filename
        self.alias = alias
        self.offset = offset
        self.consonant = consonant
        self.cutoff = cutoff
        self.preutterance = preutterance
        self.overlap = overlap

    @classmethod
    def parse_line(cls, line: str) -> Optional['ArpasingEntry']:
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            return None
        parts = line.split("=", 1)
        filename = parts[0].strip()
        rest = parts[1].split(",")
        if len(rest) < 6:
            while len(rest) < 6:
                rest.append("0")
        alias = rest[0].strip()
        try:
            offset = float(rest[1])
            consonant = float(rest[2])
            cutoff = float(rest[3])
            preutterance = float(rest[4])
            overlap = float(rest[5])
            return cls(filename, alias, offset, consonant, cutoff, preutterance, overlap)
        except ValueError:
            return None

    def to_line(self) -> str:
        def fmt(val: float) -> str:
            if abs(val - round(val)) < 1e-4:
                return str(int(round(val)))
            return f"{val:.2f}"

        return f"{self.filename}={self.alias},{fmt(self.offset)},{fmt(self.consonant)},{fmt(self.cutoff)},{fmt(self.preutterance)},{fmt(self.overlap)}"


def classify_arpasing_phoneme(alias: str) -> str:
    """
    Classifies Arpasing alias into phoneme categories:
    - INITIAL_CONSONANT: "- b", "- k"
    - INITIAL_VOWEL: "- aa", "- iy", "aa", "a" (Pure standalone vowels & initial vowels)
    - VOWEL_CONSONANT (VC): "ae k", "ah m", "ay t" (Ratio: 25–40%)
    - CONSONANT_CONSONANT (CC): "b d", "k s", "s t" (Ratio: <= 5%)
    - CONSONANT_VOWEL (CV): "b aa", "k iy"
    - VOWEL_VOWEL (VV): "ae iy", "ah uw"
    - VOWEL_END (V-): "aa -", "iy -" (Ratio: 20–30%)
    - CONSONANT_END (C-): "b -", "k -" (Ratio: 20–30%)
    - AUXILIARY: "br", "pne", etc.
    """
    alias_clean = alias.strip().lower()

    if alias_clean in ["br", "pne", "breath"] or "br" in alias_clean:
        return "AUXILIARY"

    # Initial phonemes: "- aa", "-aa", "- k"
    if alias_clean.startswith("-"):
        p2 = alias_clean.lstrip("-").strip()
        if p2 in ARPABET_VOWELS:
            return "INITIAL_VOWEL"
        return "INITIAL_CONSONANT"

    # Standalone Vowel: "aa", "ae", "ah", "a", "i", etc.
    if alias_clean in ARPABET_VOWELS:
        return "INITIAL_VOWEL"

    # Ending phonemes: "aa -", "aa-", "k -"
    if alias_clean.endswith(" -") or alias_clean.endswith("-"):
        p1 = alias_clean.rstrip("-").strip()
        if p1 in ARPABET_VOWELS:
            return "VOWEL_END"
        return "CONSONANT_END"


    parts = alias_clean.split()
    if len(parts) == 2:
        p1, p2 = parts[0], parts[1]
        is_p1_v = p1 in ARPABET_VOWELS
        is_p2_v = p2 in ARPABET_VOWELS

        if is_p1_v and not is_p2_v:
            return "VOWEL_CONSONANT"
        elif not is_p1_v and not is_p2_v:
            return "CONSONANT_CONSONANT"
        elif not is_p1_v and is_p2_v:
            return "CONSONANT_VOWEL"
        elif is_p1_v and is_p2_v:
            return "VOWEL_VOWEL"

    return "OTHER"


def balance_arpasing_entry(entry: ArpasingEntry, wav_duration: Optional[float] = None, acoustic_bounds: Optional[Tuple[Optional[float], Optional[float]]] = None, search_start: Optional[float] = None, next_offset: Optional[float] = None) -> Tuple[ArpasingEntry, List[str]]:
    """
    Balances an Arpasing entry according to DELTA SYNTH Rule 10 Standards.
    Preserves exact acoustic preutterance anchors while enforcing Arpasing ratios,
    timing safety, Middle White Strip precision (10 <= W <= 30 ms), and
    acoustic noise floor / breath artifact trimming.

    OpenUtau / UTAU Nomenclature:
    - Green line = Overlap (OVL)
    - Red line = Preutterance (PRE)
    - Timing Safety: PRE >= OVL + 10ms (Red line comes after Green line on timeline)
    - Consonant End = Offset + Consonant (Pink shaded fixed region)
    - Cutoff Point = (Duration - |Cutoff|) if Cutoff < 0, else (Offset + Cutoff)
    - Middle White Strip W = Cutoff Point - Consonant End (10 <= W <= 30 ms)
    """
    fixes = []
    e = ArpasingEntry(entry.filename, entry.alias, entry.offset, entry.consonant, entry.cutoff, entry.preutterance, entry.overlap)
    category = classify_arpasing_phoneme(e.alias)

    # Rule 0: Acoustic Noise Trimming & Soft Consonant Restoration (Thin-Singer Articulation Engine)
    if acoustic_bounds is not None:
        sound_start, sound_end = acoustic_bounds
        lead_cushion = get_consonant_lead_cushion(e.alias)
        min_allowed_off = search_start if search_start is not None else 0.0
        if sound_start is not None:
            new_off = max(min_allowed_off, sound_start - lead_cushion)
            shift = new_off - e.offset
            if shift > 5.0:
                # Eliminates dead room noise / fan hum / lead-in hiss
                orig_off = e.offset
                e.offset = round(new_off, 1)
                e.preutterance = max(35.0, round(e.preutterance - shift, 1))
                e.overlap = max(10.0, round(e.overlap - shift, 1))
                if e.consonant > 0:
                    e.consonant = max(15.0, round(e.consonant - shift, 1))
                fixes.append(f"Acoustically eliminated lead-in noise ({orig_off:.1f} -> {e.offset:.1f}ms, trimmed {shift:.1f}ms)")
            elif shift < -5.0:
                # Restores soft consonant onset clipped by previous manual oto
                orig_off = e.offset
                e.offset = round(new_off, 1)
                e.preutterance = round(e.preutterance - shift, 1)
                e.overlap = round(e.overlap - shift, 1)
                if e.consonant > 0:
                    e.consonant = round(e.consonant - shift, 1)
                fixes.append(f"Acoustically restored clipped consonant onset ({orig_off:.1f} -> {e.offset:.1f}ms, expanded {abs(shift):.1f}ms backward)")

    # Rule 1: Non-negative Offset & In-bounds verification
    if e.offset < 0:
        e.offset = 0.0
        fixes.append("Fixed negative offset to 0.0")

    if wav_duration is not None and wav_duration > 0:
        if e.offset >= wav_duration - 20.0:
            orig_off = e.offset
            e.offset = max(0.0, wav_duration - 100.0)
            fixes.append(f"Clamped out-of-bounds offset ({orig_off:.1f} -> {e.offset:.1f}ms)")

    # Rule 2: Timing Safety Enforcement (Rule 10: preutterance >= overlap + 10ms)
    # Red line (PRE) must be at least 10ms after Green line (OVL)
    MIN_SPACING = 10.0
    if e.preutterance <= e.overlap + MIN_SPACING:
        orig_pre = e.preutterance
        e.preutterance = e.overlap + MIN_SPACING
        fixes.append(f"Enforced Timing Safety: preutterance ({orig_pre:.1f} -> {e.preutterance:.1f}ms > overlap)")

    # Rule 3: Consonant Area Safety (Fixed area covers consonant attack & prevents resampler smearing)
    # For thin singers, consonant must cover burst and full formant transition
    is_soft = is_soft_consonant_alias(e.alias)
    MIN_CONSONANT_PAD = 75.0 if is_soft else 70.0
    if e.consonant > 0 and e.consonant <= e.preutterance + 25.0:
        orig_con = e.consonant
        e.consonant = round(e.preutterance + MIN_CONSONANT_PAD, 1)
        fixes.append(f"Extended consonant fixed area ({orig_con:.1f} -> {e.consonant:.1f}ms > preutterance + {MIN_CONSONANT_PAD}ms)")

    if wav_duration is not None and wav_duration > 0:
        consonant_end = e.offset + e.consonant
        if consonant_end >= wav_duration - 15.0:
            orig_con = e.consonant
            e.consonant = max(e.preutterance + 25.0, wav_duration - e.offset - 35.0)
            fixes.append(f"Clamped consonant within audio bounds ({orig_con:.1f} -> {e.consonant:.1f}ms)")

    # Rule 4: Arpasing Guideline Ratios & Boundary Midpoint Overlap (CV, VC, VV, CC)
    if wav_duration is not None and wav_duration > 0:
        if e.cutoff < 0:
            cutoff_pos = max(0.0, wav_duration - abs(e.cutoff))
        else:
            cutoff_pos = e.offset + e.cutoff
        active_dur = max(50.0, cutoff_pos - e.offset)
    else:
        active_dur = max(100.0, abs(e.cutoff))

    if category == "CONSONANT_CONSONANT":
        # Guideline: CC <= 5% (Clean consonant transition)
        max_cc_pre = max(25.0, active_dur * 0.05)
        if e.preutterance > max_cc_pre and e.preutterance > e.overlap + MIN_SPACING:
            orig_pre = e.preutterance
            e.preutterance = max(e.overlap + MIN_SPACING, round(max_cc_pre, 1))
            fixes.append(f"Arpasing CC Ratio Optimized: preutterance ({orig_pre:.1f} -> {e.preutterance:.1f}ms <= 5% ratio)")
        # Overlap (Green line) placed to ensure at least 25ms attack gap for audible consonant transition
        target_ov = max(10.0, round(e.preutterance - 25.0, 1))
        if abs(e.overlap - target_ov) > 8.0:
            orig_ov = e.overlap
            e.overlap = target_ov
            fixes.append(f"Arpasing CC Overlap attack gap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms, gap=25ms)")
        e.consonant = max(e.consonant, e.preutterance + 55.0)

    elif category == "VOWEL_CONSONANT":
        # Guideline: VC ≈ 45% ratio (based on precise benchmark ih sh: PRE 265.1 / DUR 595.9)
        target_vc_pre = active_dur * 0.445
        if e.preutterance < target_vc_pre * 0.6 or e.preutterance > target_vc_pre * 1.6:
            orig_pre = e.preutterance
            e.preutterance = max(e.overlap + MIN_SPACING, round(target_vc_pre, 1))
            fixes.append(f"Arpasing VC Ratio Optimized: preutterance ({orig_pre:.1f} -> {e.preutterance:.1f}ms ~ 44.5% ratio)")
        # Overlap (Green line) placed to ensure consonant articulation gap (approx 38% of PRE)
        target_ov = max(15.0, round(e.preutterance * 0.38, 1))
        if abs(e.overlap - target_ov) > 20.0:
            orig_ov = e.overlap
            e.overlap = target_ov
            fixes.append(f"Arpasing VC Overlap gap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms)")
        e.consonant = max(e.consonant, e.preutterance + 65.0)


    elif category == "VOWEL_VOWEL":
        # Target for Vowel-to-Vowel from Ground Truth (media_1788226490839.png):
        # aa aa, ah aa, ih aa, etc.: Offset=757.27, PRE=127.97, OVL=64.0, CON=352.34, CUT=-377.34
        target_pre = 127.97
        target_ov = 64.0
        target_con = 352.34

        anchor = e.offset + e.preutterance
        min_allowed_off = search_start if search_start is not None else 0.0

        if e.preutterance > target_pre + 15.0 or e.overlap > 75.0 or e.preutterance < target_pre - 20.0:
            new_off = max(min_allowed_off, round(anchor - target_pre, 1))
            orig_off = e.offset
            orig_pre = e.preutterance
            e.offset = new_off
            e.preutterance = round(anchor - e.offset, 1)
            e.overlap = min(target_ov, max(10.0, round(e.preutterance * 0.50, 1)))
            fixes.append(f"Arpasing VV ({e.alias}) Offset & Preutterance calibrated to compact ({orig_off:.1f} -> {e.offset:.1f}ms, PRE {orig_pre:.1f} -> {e.preutterance:.1f}ms)")
        else:
            safe_ov = min(target_ov, max(10.0, round(e.preutterance * 0.50, 1)))
            if abs(e.overlap - safe_ov) > 5.0:
                orig_ov = e.overlap
                e.overlap = safe_ov
                fixes.append(f"Arpasing VV Overlap midpoint balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms)")

        if e.preutterance <= e.overlap + MIN_SPACING:
            e.preutterance = e.overlap + MIN_SPACING

        e.consonant = target_con

    elif category == "CONSONANT_VOWEL":
        alias_clean = e.alias.strip().lower()
        parts = alias_clean.split()
        c_part = parts[0] if len(parts) > 0 else ""
        is_liquid_l = c_part in ["l"]
        is_liquid_r = c_part in ["r", "dx"]
        is_sonorant_m = c_part in ["m"]
        is_sonorant_n = c_part in ["n"]
        is_sonorant_ng = c_part in ["ng"]
        is_sonorant_other = c_part in ["w", "y", "nx"]
        is_voiced_plosive = c_part in ["b", "d", "g", "dh"]
        is_voiceless_plosive = c_part in ["p", "t", "k", "q", "th"]
        is_s = c_part in ["s"]
        is_sh_ch = c_part in ["sh", "ch", "jh", "f", "v"]
        
        # Exact compact values from User Ground Truth Reference Images (media_1788506390007.png to media_1788506390051.png):
        if is_liquid_l:
            target_pre = 87.0
            target_ov = 20.0
            target_con = 311.0
            label = "Liquid L"
        elif is_liquid_r:
            target_pre = 91.8
            target_ov = 28.4
            target_con = 323.6
            label = "Liquid R"
        elif is_sonorant_m:
            target_pre = 82.6
            target_ov = 22.5
            target_con = 328.2
            label = "Nasal M"
        elif is_sonorant_n:
            target_pre = 128.0
            target_ov = 39.0
            target_con = 352.0
            label = "Nasal N"
        elif is_sonorant_ng:
            target_pre = 112.0
            target_ov = 36.0
            target_con = 336.0
            label = "Nasal NG"
        elif is_sonorant_other:
            target_pre = 85.0
            target_ov = 23.0
            target_con = 315.0
            label = "Glide W/Y"
        elif is_s:
            target_pre = 87.0
            target_ov = 23.0
            target_con = 311.0
            label = "Fricative S"
        elif is_sh_ch:
            target_pre = 87.0
            target_ov = 33.0
            target_con = 311.0
            label = "Fricative/Affricate SH/CH"
        elif is_voiceless_plosive:
            target_pre = 87.0
            target_ov = 58.0
            target_con = 311.0
            label = "Voiceless Plosive"
        elif is_voiced_plosive:
            target_pre = 87.0
            target_ov = 53.4
            target_con = 311.0
            label = "Voiced Plosive"
        else:
            target_pre = 87.0
            target_ov = 30.0
            target_con = 311.0
            label = "Consonant"

        # TIGHT OFFSET CALIBRATION:
        # Reposition Offset so that it is neat, compact, and never leaves ugly dead space or drags across silence!
        anchor = e.offset + e.preutterance
        min_allowed_off = search_start if search_start is not None else 0.0
        if e.preutterance > target_pre + 15.0 or e.overlap > 65.0 or e.preutterance < target_pre - 20.0:
            new_off = max(min_allowed_off, round(anchor - target_pre, 1))
            orig_off = e.offset
            orig_pre = e.preutterance
            e.offset = new_off
            e.preutterance = round(anchor - e.offset, 1)
            e.overlap = min(target_ov, max(10.0, round(e.preutterance - MIN_SPACING, 1)))
            fixes.append(f"Arpasing CV ({label}) Offset & Preutterance calibrated to compact ({orig_off:.1f} -> {e.offset:.1f}ms, PRE {orig_pre:.1f} -> {e.preutterance:.1f}ms)")
        else:
            safe_ov = min(target_ov, max(10.0, round(e.preutterance - MIN_SPACING, 1)))
            if abs(e.overlap - safe_ov) > 5.0:
                orig_ov = e.overlap
                e.overlap = safe_ov
                fixes.append(f"Arpasing CV ({label}) Overlap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms)")

        # Defensive Timing Safety Guard
        if e.preutterance <= e.overlap + MIN_SPACING:
            e.preutterance = e.overlap + MIN_SPACING

        e.consonant = target_con

    elif category in ["VOWEL_END", "CONSONANT_END"]:
        # Benchmark: Ending Vowels (V-) have a tight 10ms overlap gap (Overlap = Preutterance - 10ms)
        if category == "VOWEL_END":
            target_end_ov = max(10.0, round(e.preutterance - 10.0, 1))
            if abs(e.overlap - target_end_ov) > 4.0:
                orig_ov = e.overlap
                e.overlap = target_end_ov
                fixes.append(f"Arpasing Vowel End Overlap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms ~ 10ms gap)")
        e.consonant = max(e.consonant, e.preutterance + 40.0)

    elif category == "INITIAL_VOWEL":
        # Pure Standalone Vowel & Initial Vowel (e.g. "- aa", "aa", "- ae"):
        # From user reference image media_1788513456943.png:
        # - aa: PRE 105.4, OVL 56.1, CON 323.0, CUT -354.7
        # - ae: PRE 133.3, OVL 84.1, CON 336.4, CUT -361.3
        target_pre = 105.4
        target_ov = 56.1
        target_con = 323.0

        anchor = e.offset + e.preutterance
        min_allowed_off = search_start if search_start is not None else 0.0
        
        # Reposition Offset to be tight and compact, removing ugly dead silence:
        if e.preutterance > 140.0 or e.overlap > 90.0 or e.preutterance < 70.0:
            new_off = max(min_allowed_off, round(anchor - target_pre, 1))
            orig_off = e.offset
            orig_pre = e.preutterance
            e.offset = new_off
            e.preutterance = round(anchor - e.offset, 1)
            e.overlap = min(target_ov, max(10.0, round(e.preutterance - MIN_SPACING, 1)))
            fixes.append(f"Initial Vowel ({e.alias}) Offset & Preutterance calibrated to compact ({orig_off:.1f} -> {e.offset:.1f}ms, PRE {orig_pre:.1f} -> {e.preutterance:.1f}ms)")
        else:
            target_ov = max(10.0, round(e.preutterance - 49.3, 1))
            safe_ov = min(target_ov, max(10.0, round(e.preutterance - MIN_SPACING, 1)))
            if abs(e.overlap - safe_ov) > 5.0:
                orig_ov = e.overlap
                e.overlap = safe_ov
                fixes.append(f"Initial Vowel ({e.alias}) Overlap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms)")

        if e.preutterance <= e.overlap + MIN_SPACING:
            e.preutterance = e.overlap + MIN_SPACING

        e.consonant = target_con

    elif category == "INITIAL_CONSONANT":
        c_clean = e.alias.strip().lstrip("-").strip().lower()
        is_plosive = c_clean in ["b", "d", "g", "p", "t", "k", "q", "th"]
        is_fricative = c_clean in ["s", "sh", "f", "th", "dh", "z", "zh", "hh"]
        is_affricate = c_clean in ["ch", "jh"]
        
        if is_plosive:
            target_pre = 87.0
            target_ov = 58.0
            target_con = 311.0
            label = "Initial Plosive"
        elif is_fricative:
            target_pre = 87.0
            target_ov = 30.0
            target_con = 311.0
            label = "Initial Fricative"
        elif is_affricate:
            target_pre = 87.0
            target_ov = 33.0
            target_con = 311.0
            label = "Initial Affricate"
        else:
            target_pre = 87.0
            target_ov = 25.0
            target_con = 311.0
            label = "Initial Sonorant"

        anchor = e.offset + e.preutterance
        min_allowed_off = search_start if search_start is not None else 0.0
        if e.preutterance > target_pre + 15.0 or e.overlap > 65.0:
            new_off = max(min_allowed_off, round(anchor - target_pre, 1))
            orig_off = e.offset
            orig_pre = e.preutterance
            e.offset = new_off
            e.preutterance = round(anchor - e.offset, 1)
            e.overlap = min(target_ov, max(10.0, round(e.preutterance - MIN_SPACING, 1)))
            fixes.append(f"Initial Consonant ({label} {e.alias}) Offset calibrated to compact ({orig_off:.1f} -> {e.offset:.1f}ms)")
        else:
            desired_gap = target_pre - target_ov
            target_init_ov = max(10.0, round(e.preutterance - desired_gap, 1))
            safe_ov = min(target_init_ov, max(10.0, round(e.preutterance - MIN_SPACING, 1)))
            if abs(e.overlap - safe_ov) > 6.0:
                e.overlap = safe_ov

        if e.preutterance <= e.overlap + MIN_SPACING:
            e.preutterance = e.overlap + MIN_SPACING

        e.consonant = target_con

    # Rule 5: Middle White Strip Optimization (Strict 25.0ms White Strip from User Reference Images)
    TARGET_WHITE_GAP = 25.0
    MIN_WHITE_GAP = 10.0

    max_cut_pos = None
    if next_offset is not None and next_offset > e.offset + 25.0:
        max_cut_pos = next_offset - 5.0
    elif wav_duration is not None and wav_duration > e.offset + 25.0:
        max_cut_pos = wav_duration - 5.0

    if max_cut_pos is not None and (e.offset + e.consonant + MIN_WHITE_GAP) > max_cut_pos:
        e.consonant = max(e.preutterance + 15.0, max_cut_pos - MIN_WHITE_GAP - e.offset)

    target_cutoff = -round(e.consonant + TARGET_WHITE_GAP, 2)
    if abs(e.cutoff - target_cutoff) > 3.0 or e.cutoff >= 0:
        orig_cut = e.cutoff
        e.cutoff = target_cutoff
        fixes.append(f"Calibrated Cutoff to compact ({orig_cut:.1f} -> {e.cutoff:.1f}ms, White Strip = 25.0ms)")

    return e, fixes


def balance_arpasing_file(filepath: str, backup: bool = True) -> Tuple[int, int]:
    """Balances an Arpasing oto.ini file and saves with backup."""
    if not os.path.exists(filepath):
        print(f"[!] Error: File not found: {filepath}")
        return 0, 0

    raw_bytes = open(filepath, "rb").read()
    detected_encoding = None
    lines = []

    for enc in ENCODINGS:
        try:
            content = raw_bytes.decode(enc)
            lines = content.splitlines()
            detected_encoding = enc
            break
        except (UnicodeDecodeError, LookupError):
            continue

    if not detected_encoding:
        print(f"[!] Error: Unable to decode file {filepath}")
        return 0, 0

    print(f"[*] Analyzing Arpasing Oto: {filepath} (Encoding: {detected_encoding})")

    dir_path = os.path.dirname(filepath)
    parsed_lines: List[Tuple[str, Optional[ArpasingEntry]]] = []
    for line in lines:
        entry = ArpasingEntry.parse_line(line)
        parsed_lines.append((line, entry))

    # Group entries by WAV filename to know adjacent phoneme boundaries and avoid bleeding
    wav_groups: Dict[str, List[Tuple[int, ArpasingEntry]]] = {}
    for idx, (raw_line, entry) in enumerate(parsed_lines):
        if entry is not None:
            wav_groups.setdefault(entry.filename, []).append((idx, entry))

    out_lines = [raw for raw, _ in parsed_lines]
    total_entries = 0
    balanced_entries = 0

    for wav_file, group in wav_groups.items():
        # Sort by offset to determine next phoneme start
        group.sort(key=lambda item: item[1].offset)
        wav_path = os.path.join(dir_path, wav_file)
        wav_dur = get_wav_duration_ms(wav_path)

        for g_idx, (orig_idx, entry) in enumerate(group):
            total_entries += 1
            prev_end = (group[g_idx - 1][1].offset + group[g_idx - 1][1].consonant) if g_idx > 0 else 0.0
            search_start = max(prev_end, entry.offset - 60.0)
            next_off = group[g_idx + 1][1].offset if g_idx + 1 < len(group) else wav_dur
            is_soft = is_soft_consonant_alias(entry.alias)
            ac_bounds = analyze_acoustic_bounds(wav_path, region_start=search_start, region_end=next_off, is_soft_consonant=is_soft)
            bal_entry, fixes = balance_arpasing_entry(entry, wav_duration=wav_dur, acoustic_bounds=ac_bounds, search_start=search_start)
            out_lines[orig_idx] = bal_entry.to_line()
            if fixes:
                balanced_entries += 1

    if backup and total_entries > 0:
        bak_path = filepath + ".bak"
        with open(bak_path, "wb") as f_bak:
            f_bak.write(raw_bytes)
        print(f"[*] Backup created: {bak_path}")

    with open(filepath, "w", encoding=detected_encoding, errors="replace") as f_out:
        f_out.write("\n".join(out_lines) + "\n")

    print(f"[OK] Arpasing Oto Balance Complete! Total: {total_entries} entries, Balanced: {balanced_entries} entries.\n")
    return total_entries, balanced_entries


def run_unit_tests():
    """Runs internal self-tests for Arpasing Oto Balancer."""
    print("=== Running Arpasing Oto Balancer Self-Tests ===")

    # Test 1: Timing Safety (overlap vs preutterance)
    e1 = ArpasingEntry("test.wav", "ae k", 0, 150, -300, 20, 30)
    bal1, fixes1 = balance_arpasing_entry(e1)
    assert bal1.preutterance > bal1.overlap, "Test 1 Failed: preutterance must be > overlap"
    assert bal1.preutterance - bal1.overlap >= 10.0, "Test 1 Failed: timing safety gap"
    print("  [PASS] Test 1: Arpasing Timing Safety")

    # Test 2: Arpasing CC Ratio (<= 5%)
    e2 = ArpasingEntry("test.wav", "b d", 0, 80, -300, 60, 5)
    bal2, fixes2 = balance_arpasing_entry(e2)
    assert bal2.preutterance <= 25.0, "Test 2 Failed: CC ratio <= 5%"
    print("  [PASS] Test 2: Arpasing CC Ratio <= 5%")

    # Test 3: Classification
    assert classify_arpasing_phoneme("- b") == "INITIAL_CONSONANT"
    assert classify_arpasing_phoneme("- aa") == "INITIAL_VOWEL"
    assert classify_arpasing_phoneme("ae k") == "VOWEL_CONSONANT"
    assert classify_arpasing_phoneme("b d") == "CONSONANT_CONSONANT"
    assert classify_arpasing_phoneme("b aa") == "CONSONANT_VOWEL"
    assert classify_arpasing_phoneme("aa -") == "VOWEL_END"
    assert classify_arpasing_phoneme("b -") == "CONSONANT_END"
    print("  [PASS] Test 3: Arpasing Phoneme Classifier")

    # Test 4: Middle White Strip Gap (<= 30 units, non-touching)
    e4 = ArpasingEntry("test.wav", "ae k", 0, 150, -500, 50, 20)
    bal4, fixes4 = balance_arpasing_entry(e4)
    white_gap4 = abs(bal4.cutoff) - bal4.consonant
    assert 10.0 <= white_gap4 <= 30.0, f"Test 4 Failed: White gap must be between 10 and 30 (got {white_gap4})"
    print("  [PASS] Test 4: Arpasing Middle White Strip Optimization (<= 30 units, non-touching)")

    # Test 5: Soft Consonant Detection & Restoration
    assert is_soft_consonant_alias("s ih") is True, "Test 5 Failed: s ih should be soft consonant"
    assert is_soft_consonant_alias("b aa") is False, "Test 5 Failed: b aa should not be soft consonant"
    e5 = ArpasingEntry("test.wav", "s ih", 100.0, 80.0, -300.0, 50.0, 10.0)
    # Simulate sound_start detected earlier at 70ms (consonant clipped by 30ms)
    bal5, fixes5 = balance_arpasing_entry(e5, acoustic_bounds=(70.0, 250.0))
    assert bal5.offset < 100.0, "Test 5 Failed: Offset should expand backward to restore clipped soft consonant"
    print("  [PASS] Test 5: Thin-Singer Soft Consonant Detection & Restoration")

    print("=== All Arpasing Oto Balancer Self-Tests Passed! ===\n")


def main():
    parser = argparse.ArgumentParser(description="English Arpasing Oto.ini Balancer (DELTA SYNTH Rule 10)")
    parser.add_argument("path", nargs="?", help="Path to Arpasing oto.ini file or Voicebank directory")
    parser.add_argument("--no-backup", action="store_true", help="Disable creation of .bak backup file")
    parser.add_argument("--test", action="store_true", help="Run internal self-tests")

    args = parser.parse_args()

    if args.test:
        run_unit_tests()
        return

    if not args.path:
        print("Usage: python arpasing_oto_balancer.py <path_to_arpasing_oto.ini_or_voicebank_dir>")
        print("Example: python arpasing_oto_balancer.py \"B:\\...\\A Voice\\oto.ini\"")
        return

    target_path = os.path.abspath(args.path)
    backup_flag = not args.no_backup

    if os.path.isfile(target_path):
        balance_arpasing_file(target_path, backup=backup_flag)
    elif os.path.isdir(target_path):
        print(f"[*] Scanning directory for Arpasing oto.ini files: {target_path}")
        found_any = False
        for root, _, files in os.walk(target_path):
            for file in files:
                if file.lower() == "oto.ini":
                    found_any = True
                    balance_arpasing_file(os.path.join(root, file), backup=backup_flag)
        if not found_any:
            print("[!] No oto.ini files found in the specified directory.")
    else:
        print(f"[!] Invalid path: {target_path}")


if __name__ == "__main__":
    main()
