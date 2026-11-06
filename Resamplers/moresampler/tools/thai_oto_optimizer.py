#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""
thai_oto_optimizer.py
e:\Program Developing\Moresampler-main\thai_oto_optimizer.py
Made And Checked By DELTA SYNTH & Gemini AI
Original by DELTA SYNTH
Revision: 2.4
==============================================================================
Thai Oto.ini Optimizer for UTAU / OpenUtau (VCCV, VCV, C+V)
DELTA SYNTH — Audio & Phonetics Architecture Standard (Rule 10)
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

# Supported Encodings for Thai / International Voicebanks
ENCODINGS = ["utf-8-sig", "utf-8", "cp874", "tis-620", "shift_jis", "latin-1"]

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

            if wav_path not in _WAV_DURATION_CACHE or _WAV_DURATION_CACHE[wav_path] is None:
                _WAV_DURATION_CACHE[wav_path] = (nframes / float(sr)) * 1000.0

            return profile
    except Exception:
        _WAV_RMS_PROFILE_CACHE[wav_path] = None
        return None


def is_soft_thai_phoneme(alias: str) -> bool:
    """
    Returns True if the Thai phoneme contains an aspirated, voiceless, or fricative consonant
    (ph, th, kh, ch, s, f, h, and voiceless clusters) characteristic of thin/airy singing.
    """
    clean_alias = alias.strip().lower()
    parts = clean_alias.replace("-", " ").split()
    target = parts[-1] if parts else clean_alias
    soft_patterns = ['ph', 'th', 'kh', 'ch', 's', 'f', 'h', 'kl', 'kr', 'kw', 'khl', 'khr', 'khw', 'pl', 'pr', 'phl', 'phr', 'tr', 'thr', 'p', 't', 'k', 'c']
    return any(target.startswith(p) for p in soft_patterns)


def get_thai_lead_cushion(alias: str) -> float:
    """
    Returns acoustic attack cushion in milliseconds based on Thai phoneme type.
    - Plosives (p, t, k, b, d, c): 10ms (tight, punchy burst)
    - Fricatives/Aspirates/Clusters (ph, th, kh, ch, s, f, h, etc.): 12ms (preserves soft breath/frication)
    - Vowels/Sonorants: 15ms (natural acoustic attack)
    """
    clean_alias = alias.strip().lower()
    parts = clean_alias.replace("-", " ").split()
    target = parts[-1] if parts else clean_alias
    stop_patterns = ['p', 't', 'k', 'b', 'd', 'c']
    if any(target.startswith(p) for p in stop_patterns) and not any(target.startswith(p) for p in ['ph', 'th', 'kh', 'ch']):
        return 10.0
    fric_asp_patterns = ['ph', 'th', 'kh', 'ch', 's', 'f', 'h', 'kl', 'kr', 'kw', 'khl', 'khr', 'khw', 'pl', 'pr', 'phl', 'phr', 'tr', 'thr']
    if any(target.startswith(p) for p in fric_asp_patterns):
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


# Thai Vowel definitions (standard romanized & thai script)
THAI_VOWELS = set("aeiou123456789@")
THAI_VOWEL_PATTERNS = r'[aeiou136@]|(?:aw|ay|ai|ia|ua|uea|oe|or|ae|ee|oo|uu)'
THAI_CONSONANTS_PATTERNS = r'[bcdfghjklmnpqrstvwxyz]|(?:ch|kh|ph|th|ng|zh|sh|kl|kr|kw|khl|khr|khw|pl|pr|phl|phr|tr|thr|thw|bl|br|dr|fl|fr)'

class OtoEntry:
    def __init__(self, filename: str, alias: str, offset: float, consonant: float, cutoff: float, preutterance: float, overlap: float):
        self.filename = filename
        self.alias = alias
        self.offset = offset
        self.consonant = consonant
        self.cutoff = cutoff
        self.preutterance = preutterance
        self.overlap = overlap

    @classmethod
    def parse_line(cls, line: str) -> Optional['OtoEntry']:
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            return None
        parts = line.split("=", 1)
        filename = parts[0].strip()
        rest = parts[1].split(",")
        if len(rest) < 6:
            # Fill default parameters if missing
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


def classify_thai_phoneme(alias: str) -> str:
    """
    Classifies a Thai phoneme alias into standard categories:
    - VCCV_INIT: Silence to Consonant/Vowel (e.g. - ka, - a, - b)
    - VCCV_END: Vowel/Consonant ending to silence (e.g. a -, b -, a_, a -k)
    - VCCV_CC: Consonant cluster transitions (e.g. k l, p r, C_C, bl, br)
    - VCCV_VC: Vowel-Consonant transitions (e.g. a k, a ng, a-k, 4.A1, 5.M1)
    - VCCV_VV: Diphthongs / Vowel transitions (e.g. a i, a u)
    - VCV: Continuous VCV (e.g. a ka, i ka, u ka)
    - CV: Consonant + Vowel (e.g. ka, kha, C+V)
    - OTHER: General phoneme
    """
    alias_clean = alias.strip()

    # VCCV Initial: "- ka", "- k", "- a", "- b", "-a"
    if alias_clean.startswith("-"):
        return "VCCV_INIT"

    # Standalone pure vowel: "a", "i", "u", "e", "o", "aa", etc.
    if bool(re.search(r'^[aeiouæœəɤɨɯ136@]+$', alias_clean, re.IGNORECASE)):
        return "VCCV_INIT"

    # VCCV Ending: "a -", "b -", "a_", "k -"
    if alias_clean.endswith(" -") or alias_clean.endswith(" _") or alias_clean.endswith("-"):
        return "VCCV_END"


    parts = alias_clean.split()
    if len(parts) == 2:
        p1, p2 = parts[0], parts[1]
        is_p1_vowel = bool(re.search(r'^[aeiouæœəɤɨɯ136@]+$', p1, re.IGNORECASE))
        is_p2_pure_vowel = bool(re.search(r'^[aeiouæœəɤɨɯ136@]+$', p2, re.IGNORECASE))
        is_p2_cv = bool(re.search(r'^[b-df-hj-np-tv-z]+[aeiouæœəɤɨɯ136@]+', p2, re.IGNORECASE))

        if is_p1_vowel and is_p2_cv:
            return "VCV"
        elif is_p1_vowel and not is_p2_pure_vowel:
            return "VCCV_VC"
        elif not is_p1_vowel and not is_p2_pure_vowel:
            return "VCCV_CC"
        elif is_p1_vowel and is_p2_pure_vowel:
            return "VCCV_VV"
        elif not is_p1_vowel and is_p2_pure_vowel:
            return "CV"

    # Single syllable Thai VCV / CV
    if re.search(r'^[aeiou]\s+[a-z]+[aeiou]', alias_clean, re.IGNORECASE):
        return "VCV"

    if re.search(r'^[a-z]+\+?[aeiou]', alias_clean, re.IGNORECASE):
        return "CV"

    return "OTHER"


def optimize_entry(entry: OtoEntry, wav_duration: Optional[float] = None, acoustic_bounds: Optional[Tuple[Optional[float], Optional[float]]] = None, search_start: Optional[float] = None, next_offset: Optional[float] = None) -> Tuple[OtoEntry, List[str]]:
    """
    Applies DELTA SYNTH Rule 10 Optimization & Timing Safety Rules to an OtoEntry.
    Preserves exact acoustic preutterance anchors while harmonizing overlap, consonant area,
    Middle White Strip precision (10 <= W <= 30 ms), and acoustic noise floor / breath artifact trimming.

    OpenUtau / UTAU Nomenclature:
    - Green line = Overlap (OVL)
    - Red line = Preutterance (PRE)
    - Timing Safety: PRE >= OVL + 10ms (Red line comes after Green line on timeline)
    - Consonant End = Offset + Consonant (Pink shaded fixed region)
    - Cutoff Point = (Duration - |Cutoff|) if Cutoff < 0, else (Offset + Cutoff)
    - Middle White Strip W = Cutoff Point - Consonant End (10 <= W <= 30 ms)
    """
    fixes = []
    e = OtoEntry(entry.filename, entry.alias, entry.offset, entry.consonant, entry.cutoff, entry.preutterance, entry.overlap)
    phoneme_type = classify_thai_phoneme(e.alias)

    # Rule 0: Acoustic Noise Trimming & Soft Consonant Restoration (Thin-Singer Articulation Engine)
    shift = 0.0
    if acoustic_bounds is not None:
        sound_start, sound_end = acoustic_bounds
        lead_cushion = get_thai_lead_cushion(e.alias)
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
    MIN_SPACING = 10.0
    if e.preutterance <= e.overlap + MIN_SPACING:
        orig_pre = e.preutterance
        e.preutterance = e.overlap + MIN_SPACING
        fixes.append(f"Enforced Timing Safety: preutterance ({orig_pre:.1f} -> {e.preutterance:.1f}ms > overlap)")

    # Rule 3: Consonant Area Safety (Fixed area covers consonant attack & prevents resampler smearing)
    # Protect aspirated stops (ph, th, kh, ch), clusters, stops, and fricatives
    alias_lower = e.alias.strip().lower()
    is_soft = is_soft_thai_phoneme(e.alias)
    if any(alias_lower.startswith(c) for c in ['ph', 'th', 'kh', 'ch', 'kl', 'kr', 'kw', 'khl', 'khr', 'khw', 'pl', 'pr', 'phl', 'phr', 'tr', 'thr']):
        min_pad = 80.0
    elif is_soft or any(alias_lower.startswith(c) for c in ['p', 't', 'k', 's', 'f']):
        min_pad = 75.0
    else:
        min_pad = 70.0

    MIN_CONSONANT_PAD = min_pad
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

    # Rule 4: Thai Phonetic Timing Profiles & Boundary Midpoint Overlap (CV, VC, VCV, CC)
    if wav_duration is not None and wav_duration > 0:
        if e.cutoff < 0:
            cutoff_pos = max(0.0, wav_duration - abs(e.cutoff))
        else:
            cutoff_pos = e.offset + e.cutoff
        active_dur = max(50.0, cutoff_pos - e.offset)
    else:
        active_dur = max(100.0, abs(e.cutoff))

    if phoneme_type == "VCCV_CC":
        # Guideline: CC / C_C <= 5% (Leading consonant cluster ratio)
        max_cc_pre = max(25.0, active_dur * 0.05)
        if e.preutterance > max_cc_pre and e.preutterance > e.overlap + MIN_SPACING:
            orig_pre = e.preutterance
            e.preutterance = max(e.overlap + MIN_SPACING, round(max_cc_pre, 1))
            fixes.append(f"Thai CC Ratio: preutterance ({orig_pre:.1f} -> {e.preutterance:.1f}ms <= 5% ratio)")
        # Overlap (Green line) placed at boundary midpoint between consonants
        target_ov = max(10.0, round(e.preutterance - 25.0, 1))
        if abs(e.overlap - target_ov) > 8.0:
            orig_ov = e.overlap
            e.overlap = target_ov
            fixes.append(f"Thai CC Overlap attack gap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms, gap=25ms)")
        e.consonant = max(e.consonant, e.preutterance + 55.0)

    elif phoneme_type == "VCCV_VC":
        # Guideline: VC / VC- ≈ 35% ratio (based on precise benchmark)
        target_vc_pre = active_dur * 0.35
        if e.preutterance < target_vc_pre * 0.6 or e.preutterance > target_vc_pre * 1.6:
            orig_pre = e.preutterance
            e.preutterance = max(e.overlap + MIN_SPACING, round(target_vc_pre, 1))
            fixes.append(f"Thai VC Ratio: preutterance ({orig_pre:.1f} -> {e.preutterance:.1f}ms ~ 35% ratio)")
        # Overlap (Green line) placed to ensure consonant articulation gap
        target_ov = max(15.0, round(e.preutterance * 0.35, 1))
        if abs(e.overlap - target_ov) > 20.0:
            orig_ov = e.overlap
            e.overlap = target_ov
            fixes.append(f"Thai VC Overlap gap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms)")
        e.consonant = max(e.consonant, e.preutterance + 65.0)

    elif phoneme_type == "VCCV_VV":
        # Overlap (Green line) placed at exact midpoint between Vowel 1 and Vowel 2
        target_ov = round(e.preutterance * 0.50, 1)
        if abs(e.overlap - target_ov) > 15.0:
            orig_ov = e.overlap
            e.overlap = target_ov
            fixes.append(f"Thai VV Overlap midpoint balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms)")

    elif phoneme_type in ["CV", "VCV", "VCCV_CV"]:
        alias_clean = e.alias.strip()
        is_asp = any(alias_clean.lower().startswith(c) for c in ['ph', 'th', 'kh', 'ch', 's', 'f', 'h'])
        is_nasal = any(alias_clean.lower().startswith(c) for c in ['m', 'n', 'ng'])
        is_liquid = any(alias_clean.lower().startswith(c) for c in ['l', 'r', 'w', 'y'])
        
        if is_nasal:
            target_pre = 110.0
            target_ov = 35.0
            target_con = 330.0
            label = "Nasal"
        elif is_liquid:
            target_pre = 90.0
            target_ov = 25.0
            target_con = 320.0
            label = "Liquid"
        elif is_asp:
            target_pre = 87.0
            target_ov = 33.0
            target_con = 311.0
            label = "Aspirated/Fricative"
        else:
            target_pre = 87.0
            target_ov = 58.0
            target_con = 311.0
            label = "Unaspirated Stop"

        anchor = e.offset + e.preutterance
        min_allowed_off = search_start if search_start is not None else 0.0
        if (e.preutterance > target_pre + 15.0 or e.overlap > 65.0 or e.preutterance < target_pre - 20.0) and shift >= -5.0:
            new_off = max(min_allowed_off, round(anchor - target_pre, 1))
            orig_off = e.offset
            orig_pre = e.preutterance
            e.offset = new_off
            e.preutterance = round(anchor - e.offset, 1)
            e.overlap = min(target_ov, max(10.0, round(e.preutterance - MIN_SPACING, 1)))
            fixes.append(f"Thai CV/VCV ({label}) Offset & Preutterance calibrated to compact ({orig_off:.1f} -> {e.offset:.1f}ms, PRE {orig_pre:.1f} -> {e.preutterance:.1f}ms)")
        else:
            safe_ov = min(target_ov, max(10.0, round(e.preutterance - MIN_SPACING, 1)))
            if abs(e.overlap - safe_ov) > 5.0:
                orig_ov = e.overlap
                e.overlap = safe_ov
                fixes.append(f"Thai CV/VCV ({label}) Overlap gap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms)")

        # Defensive Timing Safety Guard
        if e.preutterance <= e.overlap + MIN_SPACING:
            e.preutterance = e.overlap + MIN_SPACING

        e.consonant = target_con

    elif phoneme_type == "VCCV_END":
        alias_clean = e.alias.strip().replace(" -", "").replace(" _", "").replace("-", "").strip()
        if bool(re.search(r'^[aeiouæœəɤɨɯ136@]+$', alias_clean, re.IGNORECASE)):
            target_end_ov = max(10.0, round(e.preutterance - 10.0, 1))
            if abs(e.overlap - target_end_ov) > 4.0:
                orig_ov = e.overlap
                e.overlap = target_end_ov
                fixes.append(f"Thai Vowel End Overlap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms ~ 10ms gap)")
        e.consonant = max(e.consonant, e.preutterance + 40.0)

    elif phoneme_type == "VCCV_INIT":
        alias_clean = e.alias.strip().replace("-", "").strip()
        is_vowel = bool(re.search(r'^[aeiouæœəɤɨɯ136@]+$', alias_clean.split()[0] if alias_clean else "", re.IGNORECASE))
        
        if is_vowel:
            # Pure Standalone Vowel & Initial Vowel (e.g. "- a", "a", "- aa"):
            target_pre = 105.4
            target_ov = 56.1
            target_con = 323.0

            anchor = e.offset + e.preutterance
            min_allowed_off = search_start if search_start is not None else 0.0
            
            if (e.preutterance > 140.0 or e.overlap > 90.0 or e.preutterance < 70.0) and shift >= -5.0:
                new_off = max(min_allowed_off, round(anchor - target_pre, 1))
                orig_off = e.offset
                orig_pre = e.preutterance
                e.offset = new_off
                e.preutterance = round(anchor - e.offset, 1)
                e.overlap = target_ov
                fixes.append(f"Thai Initial/Pure Vowel ({e.alias}) Offset calibrated to compact ({orig_off:.1f} -> {e.offset:.1f}ms, PRE {orig_pre:.1f} -> {e.preutterance:.1f}ms)")
            else:
                target_ov = max(10.0, round(e.preutterance - 49.3, 1))
                if abs(e.overlap - target_ov) > 5.0:
                    orig_ov = e.overlap
                    e.overlap = target_ov
                    fixes.append(f"Thai Initial/Pure Vowel Overlap gap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms ~ 50ms gap)")

            e.consonant = target_con
        else:
            # Initial Consonant (e.g. "- ka", "- pha", "- tha"):
            is_asp = any(alias_clean.lower().startswith(c) for c in ['ph', 'th', 'kh', 'ch', 's', 'f', 'h'])
            target_pre = 85.0 if is_asp else 87.0
            target_ov = 33.0 if is_asp else 58.0
            target_con = 311.0

            anchor = e.offset + e.preutterance
            min_allowed_off = search_start if search_start is not None else 0.0

            if (e.preutterance > target_pre + 15.0 or e.overlap > 65.0) and shift >= -5.0:
                new_off = max(min_allowed_off, round(anchor - target_pre, 1))
                orig_off = e.offset
                orig_pre = e.preutterance
                e.offset = new_off
                e.preutterance = round(anchor - e.offset, 1)
                e.overlap = target_ov
                fixes.append(f"Thai Initial Consonant ({e.alias}) Offset calibrated to compact ({orig_off:.1f} -> {e.offset:.1f}ms)")
            else:
                target_ov = max(10.0, round(e.preutterance - (target_pre - target_ov), 1))
                if abs(e.overlap - target_ov) > 6.0:
                    e.overlap = target_ov

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


def process_oto_file(filepath: str, backup: bool = True) -> Tuple[int, int]:
    """
    Processes an oto.ini file, optimizing all entries and saving with backup.
    Returns (total_entries, fixed_entries)
    """
    if not os.path.exists(filepath):
        print(f"[!] Error: File not found: {filepath}")
        return 0, 0

    # Detect encoding
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
        print(f"[!] Error: Could not decode {filepath} with supported encodings.")
        return 0, 0

    print(f"[*] Processing: {filepath} (Encoding: {detected_encoding})")

    dir_path = os.path.dirname(filepath)
    parsed_lines: List[Tuple[str, Optional[OtoEntry]]] = []
    for line in lines:
        entry = OtoEntry.parse_line(line)
        parsed_lines.append((line, entry))

    # Group entries by WAV filename to know adjacent boundaries and avoid bleeding
    wav_groups: Dict[str, List[Tuple[int, OtoEntry]]] = {}
    for idx, (raw_line, entry) in enumerate(parsed_lines):
        if entry is not None:
            wav_groups.setdefault(entry.filename, []).append((idx, entry))

    out_lines = [raw for raw, _ in parsed_lines]
    total_entries = 0
    fixed_entries = 0

    for wav_file, group in wav_groups.items():
        group.sort(key=lambda item: item[1].offset)
        wav_path = os.path.join(dir_path, wav_file)
        wav_dur = get_wav_duration_ms(wav_path)

        for g_idx, (orig_idx, entry) in enumerate(group):
            total_entries += 1
            prev_end = (group[g_idx - 1][1].offset + group[g_idx - 1][1].consonant) if g_idx > 0 else 0.0
            search_start = max(prev_end, entry.offset - 60.0)
            next_off = group[g_idx + 1][1].offset if g_idx + 1 < len(group) else wav_dur
            is_soft = is_soft_thai_phoneme(entry.alias)
            ac_bounds = analyze_acoustic_bounds(wav_path, region_start=search_start, region_end=next_off, is_soft_consonant=is_soft)
            opt_entry, fixes = optimize_entry(entry, wav_duration=wav_dur, acoustic_bounds=ac_bounds, search_start=search_start)
            out_lines[orig_idx] = opt_entry.to_line()
            if fixes:
                fixed_entries += 1

    # Create backup before saving
    if backup and total_entries > 0:
        bak_path = filepath + ".bak"
        with open(bak_path, "wb") as f_bak:
            f_bak.write(raw_bytes)
        print(f"[*] Backup created: {bak_path}")

    # Write optimized file
    with open(filepath, "w", encoding=detected_encoding, errors="replace") as f_out:
        f_out.write("\n".join(out_lines) + "\n")

    print(f"[OK] Complete! Total: {total_entries} entries, Optimized: {fixed_entries} entries.\n")
    return total_entries, fixed_entries


def run_unit_tests():
    """Run internal self-tests for DELTA SYNTH Rule 10 rules."""
    print("=== Running Thai Oto Optimizer Self-Tests ===")

    # Test 1: Timing Safety (overlap >= preutterance)
    e1 = OtoEntry("test.wav", "ka", 0, 100, -200, 30, 40)
    opt1, fixes1 = optimize_entry(e1)
    assert opt1.preutterance > opt1.overlap, "Test 1 Failed: preutterance should be > overlap"
    assert opt1.preutterance - opt1.overlap >= 10.0, "Test 1 Failed: min spacing rule violated"
    print("  [PASS] Test 1: Timing Safety (Preutterance vs Overlap)")

    # Test 2: Positive Cutoff Correction
    e2 = OtoEntry("1.D.wav", "- d", 218, 537, 1190, 227, 20)
    opt2, fixes2 = optimize_entry(e2)
    assert opt2.cutoff < 0, "Test 2 Failed: Positive cutoff must be converted to negative span"
    print("  [PASS] Test 2: Positive Cutoff Auto-Correction")

    # Test 3: VCCV VC Ratio (~25-40%)
    e3 = OtoEntry("test.wav", "a k", 0, 150, -300, 10, 5)
    opt3, fixes3 = optimize_entry(e3)
    assert 50.0 <= opt3.preutterance <= 150.0, "Test 3 Failed: VC ratio target ~30%"
    print("  [PASS] Test 3: VCCV VC Ratio ~25-40%")

    # Test 4: Phoneme Classification
    assert classify_thai_phoneme("- b") == "VCCV_INIT"
    assert classify_thai_phoneme("b -") == "VCCV_END"
    assert classify_thai_phoneme("k l") == "VCCV_CC"
    assert classify_thai_phoneme("a k") == "VCCV_VC"
    assert classify_thai_phoneme("a ka") == "VCV"
    print("  [PASS] Test 4: Thai Phoneme Classifier")

    # Test 5: Middle White Strip Gap (10 <= W <= 36.5) - OpenUtau C# Ground Truth
    e5 = OtoEntry("test.wav", "a k", 1000, 350, -500, 200, 80)
    opt5, fixes5 = optimize_entry(e5, wav_duration=3000.0)
    white_gap = abs(opt5.cutoff) - opt5.consonant
    assert 10.0 <= white_gap <= 36.5, f"Test 5 Failed: True White gap must be between 10 and 36.5 (got {white_gap})"
    cut_pos = opt5.offset - opt5.cutoff
    pink_end = opt5.offset + opt5.consonant
    assert abs((cut_pos - pink_end) - white_gap) < 1e-4, "Test 5 Failed: OpenUtau timeline geometry mismatch"
    print("  [PASS] Test 5: Thai Middle White Strip Optimization (10 <= W <= 36.5)")

    # Test 6: Initial Consonant Silence Trimming & Beat Alignment
    e6 = OtoEntry("1.KH.wav", "- kha", 1200, 500, -600, 550, 100)
    opt6, fixes6 = optimize_entry(e6)
    assert opt6.preutterance <= 85.0, f"Test 6 Failed: Initial consonant preutterance trimmed (got {opt6.preutterance})"
    assert opt6.preutterance > opt6.overlap, "Test 6 Failed: timing safety"
    print("  [PASS] Test 6: Thai Initial Consonant Beat Alignment & Silence Trimming")

    # Test 7: Soft Thai Phoneme Detection & Restoration
    assert is_soft_thai_phoneme("- pha") is True, "Test 7 Failed: - pha should be soft phoneme"
    assert is_soft_thai_phoneme("a ma") is False, "Test 7 Failed: a ma should not be soft phoneme"
    e7 = OtoEntry("pha.wav", "- pha", 400.0, 150.0, -350.0, 180.0, 50.0)
    opt7, fixes7 = optimize_entry(e7, acoustic_bounds=(360.0, 700.0))
    assert opt7.offset < 400.0, "Test 7 Failed: Offset should expand backward to restore clipped soft consonant"
    print("  [PASS] Test 7: Thin-Singer Soft Thai Consonant Detection & Restoration")

    print("=== All Thai Oto Optimizer Self-Tests Passed! ===\n")


def main():
    parser = argparse.ArgumentParser(description="Thai Oto.ini Optimizer for UTAU / OpenUtau (VCCV, VCV, C+V)")
    parser.add_argument("path", nargs="?", help="Path to oto.ini file or Voicebank directory")
    parser.add_argument("--no-backup", action="store_true", help="Disable creation of .bak backup file")
    parser.add_argument("--test", action="store_true", help="Run internal unit tests")

    args = parser.parse_args()

    if args.test:
        run_unit_tests()
        return

    if not args.path:
        print("Usage: python thai_oto_optimizer.py <path_to_oto.ini_or_voicebank_dir>")
        print("Example: python thai_oto_optimizer.py C:\\UTAU\\Voice\\ThaiVCCV\\oto.ini")
        return

    target_path = os.path.abspath(args.path)
    backup_flag = not args.no_backup

    if os.path.isfile(target_path):
        process_oto_file(target_path, backup=backup_flag)
    elif os.path.isdir(target_path):
        print(f"[*] Scanning directory for oto.ini files: {target_path}")
        found_any = False
        for root, _, files in os.walk(target_path):
            for file in files:
                if file.lower() == "oto.ini":
                    found_any = True
                    process_oto_file(os.path.join(root, file), backup=backup_flag)
        if not found_any:
            print("[!] No oto.ini files found in the specified directory.")
    else:
        print(f"[!] Invalid path: {target_path}")


if __name__ == "__main__":
    main()
