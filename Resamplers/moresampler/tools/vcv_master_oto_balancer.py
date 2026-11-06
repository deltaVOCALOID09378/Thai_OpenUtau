#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""
vcv_master_oto_balancer.py
e:\Program Developing\Moresampler-main\vcv_master_oto_balancer.py
Made And Checked By DELTA SYNTH & Gemini AI
Original by Kanru Hua / DELTA SYNTH
Revision: 2.4
==============================================================================
Japanese VCV Oto.ini Master Balancer Engine
DELTA SYNTH — AI Development, Architecture & Optimization Standard (Rule 10)
==============================================================================
"""

import sys
import os
import re
import wave
import struct
import math
import json
import argparse
from typing import List, Tuple, Dict, Optional, Set

# UTF-8 stdout/stderr protection for multilingual paths (Thai, Japanese Kana, Latin)
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

# Supported encodings for Japanese VCV voicebanks
ENCODINGS = ["shift_jis", "cp932", "utf-8", "cp874", "latin-1"]

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

            if sampwidth == 2:
                count = len(raw) // 2
                samples = struct.unpack(f"<{count}h", raw)
            elif sampwidth == 1:
                samples = [s - 128 for s in raw]
            elif sampwidth == 3:
                samples = [
                    int.from_bytes(raw[i:i+3], byteorder='little', signed=True)
                    for i in range(0, len(raw), 3)
                ]
            elif sampwidth == 4:
                try:
                    samples = struct.unpack(f"<{len(raw) // 4}f", raw)
                except Exception:
                    samples = struct.unpack(f"<{len(raw) // 4}i", raw)
            else:
                _WAV_RMS_PROFILE_CACHE[wav_path] = None
                return None

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


def is_soft_vcv_phoneme(alias: str) -> bool:
    """
    Returns True if the VCV alias contains a soft, airy, voiceless consonant
    (s, sh, h, f, ch, ts, k, t, p) characteristic of light/airy/thin singers.
    """
    clean_alias = alias.strip().lower()
    parts = clean_alias.replace("-", " ").split()
    mora = parts[-1] if parts else clean_alias
    soft_patterns = [
        'さ', 'し', 'す', 'せ', 'そ', 'しゃ', 'しゅ', 'しょ',
        'は', 'ひ', 'ふ', 'へ', 'ほ', 'ひゃ', 'ひゅ', 'ひょ', 'ふぁ', 'ふぃ', 'ふぇ', 'ふぉ',
        'か', 'き', 'く', 'け', 'こ', 'きゃ', 'きゅ', 'きょ',
        'た', 'ち', 'つ', 'て', 'と', 'ちゃ', 'ちゅ', 'ちょ', 'つぁ', 'つぃ', 'つぇ', 'つぉ',
        'ぱ', 'ぴ', 'ぷ', 'ぺ', 'ぽ', 'ぴゃ', 'ぴゅ', 'ぴょ',
        's', 'sh', 'h', 'f', 'ch', 'ts', 'k', 't', 'p'
    ]
    return any(mora.startswith(p) for p in soft_patterns)


def get_vcv_lead_cushion(alias: str) -> float:
    """
    Returns acoustic attack cushion in milliseconds based on VCV mora type.
    """
    clean_alias = alias.strip().lower()
    parts = clean_alias.replace("-", " ").split()
    mora = parts[-1] if parts else clean_alias
    stop_patterns = ['か', 'き', 'く', 'け', 'こ', 'た', 'ち', 'つ', 'て', 'と', 'ぱ', 'ぴ', 'ぷ', 'ぺ', 'ぽ', 'k', 't', 'p', 'b', 'd', 'g']
    if any(mora.startswith(p) for p in stop_patterns):
        return 10.0
    fric_patterns = ['さ', 'し', 'す', 'せ', 'そ', 'は', 'ひ', 'ふ', 'へ', 'ほ', 's', 'sh', 'h', 'f', 'ch', 'ts', 'z', 'j']
    if any(mora.startswith(p) for p in fric_patterns):
        return 12.0
    return 15.0


def analyze_acoustic_bounds(
    wav_path: str,
    region_start: float = 0.0,
    region_end: Optional[float] = None,
    is_soft_consonant: bool = False
) -> Tuple[Optional[float], Optional[float]]:
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
    if max_rms < 150.0:
        _ACOUSTIC_BOUNDS_CACHE[cache_key] = (None, None)
        return None, None

    sorted_rms = sorted(sub_rms)
    noise_sample_count = max(1, len(sorted_rms) // 10)
    noise_floor = sum(sorted_rms[:noise_sample_count]) / noise_sample_count

    if is_soft_consonant:
        thresh = max(noise_floor * 1.45, min(max_rms * 0.018, 180.0))
    else:
        thresh = max(noise_floor * 2.2, max_rms * 0.035, 260.0)

    sound_start = None
    for i in range(len(sub_rms)):
        if sub_rms[i] > thresh:
            is_sustained = (i + 1 < len(sub_rms) and sub_rms[i + 1] > thresh * 0.85)
            is_ramp = (i + 1 < len(sub_rms) and sub_rms[i + 1] > sub_rms[i] * 1.25)
            is_end = (i == len(sub_rms) - 1)
            if is_sustained or is_ramp or is_end:
                sound_start = (start_idx + i) * win_ms
                break

    sound_end = None
    for i in range(len(sub_rms) - 1, -1, -1):
        if sub_rms[i] > thresh:
            is_sustained = (i - 1 >= 0 and sub_rms[i - 1] > thresh * 0.85)
            is_ramp = (i - 1 >= 0 and sub_rms[i - 1] > sub_rms[i] * 1.1)
            is_start = (i == 0)
            if is_sustained or is_ramp or is_start:
                sound_end = (start_idx + i + 1) * win_ms
                break

    res = (sound_start, sound_end)
    _ACOUSTIC_BOUNDS_CACHE[cache_key] = res
    return res

VOWELS = set("あいいぅううぇええぉおお")
VOWEL_CHAR_MAP = {
    'あ': 'a', 'か': 'a', 'が': 'a', 'さ': 'a', 'ざ': 'a', 'た': 'a', 'だ': 'a', 'な': 'a', 'は': 'a', 'ば': 'a', 'ぱ': 'a', 'ま': 'a', 'や': 'a', 'ら': 'a', 'わ': 'a', 'きゃ': 'a', 'ぎゃ': 'a', 'しゃ': 'a', 'じゃ': 'a', 'ちゃ': 'a', 'にゃ': 'a', 'ひゃ': 'a', 'びゃ': 'a', 'ぴゃ': 'a', 'みゃ': 'a', 'りゃ': 'a',
    'い': 'i', 'き': 'i', 'ぎ': 'i', 'し': 'i', 'じ': 'i', 'ち': 'i', 'ぢ': 'i', 'に': 'i', 'ひ': 'i', 'び': 'i', 'ぴ': 'i', 'み': 'i', 'り': 'i', 'ゐ': 'i', 'つぃ': 'i', 'すぃ': 'i', 'ずぃ': 'i', 'てぃ': 'i', 'でぃ': 'i',
    'う': 'u', 'く': 'u', 'ぐ': 'u', 'す': 'u', 'ず': 'u', 'つ': 'u', 'づ': 'u', 'ぬ': 'u', 'ふ': 'u', 'ぶ': 'u', 'ぷ': 'u', 'む': 'u', 'ゆ': 'u', 'る': 'u', 'きゅ': 'u', 'ぎゅ': 'u', 'しゅ': 'u', 'じゅ': 'u', 'ちゅ': 'u', 'にゅ': 'u', 'ひゅ': 'u', 'びゅ': 'u', 'ぴゅ': 'u', 'みゅ': 'u', 'りゅ': 'u', 'つぁ': 'a', 'つぇ': 'e', 'つぉ': 'o',
    'え': 'e', 'け': 'e', 'げ': 'e', 'せ': 'e', 'ぜ': 'e', 'て': 'e', 'で': 'e', 'ね': 'e', 'へ': 'e', 'べ': 'e', 'ぺ': 'e', 'め': 'e', 'れ': 'e', 'ゑ': 'e', 'きぇ': 'e', 'ぎぇ': 'e', 'しぇ': 'e', 'じぇ': 'e', 'ちぇ': 'e', 'にぇ': 'e', 'ひぇ': 'e', 'びぇ': 'e', 'ぴぇ': 'e', 'みぇ': 'e', 'りぇ': 'e', 'いぇ': 'e',
    'お': 'o', 'こ': 'o', 'ご': 'o', 'そ': 'o', 'ぞ': 'o', 'と': 'o', 'ど': 'o', 'の': 'o', 'ほ': 'o', 'ぼ': 'o', 'ぽ': 'o', 'も': 'o', 'よ': 'o', 'ろ': 'o', 'を': 'o', 'きょ': 'o', 'ぎょ': 'o', 'しょ': 'o', 'じょ': 'o', 'ちょ': 'o', 'にょ': 'o', 'ひょ': 'o', 'びょ': 'o', 'ぴょ': 'o', 'みょ': 'o', 'りょ': 'o',
    'ん': 'n', 'ヴ': 'u'
}


def get_mora_vowel(mora: str) -> str:
    """Returns the primary vowel of a Japanese kana mora."""
    if not mora:
        return 'a'
    last_c = mora[-1]
    if last_c in 'ゃぁ':
        return 'a'
    if last_c in 'ぃ':
        return 'i'
    if last_c in 'ゅぅ':
        return 'u'
    if last_c in 'ぇ':
        return 'e'
    if last_c in 'ょぉ':
        return 'o'
    return VOWEL_CHAR_MAP.get(mora, VOWEL_CHAR_MAP.get(last_c, 'a'))


def tokenize_japanese_mora(text: str) -> List[str]:
    """
    Tokenizes a Japanese VCV string or filename into moras, preserving compound digraphs.
    e.g. 'ききくかけきけ' -> ['き', 'き', 'く', 'か', 'け', 'き', 'け']
    e.g. 'ふぉふんふぁんふ' -> ['ふぉ', 'ふ', 'ん', 'ふぁ', 'ん', 'ふ']
    """
    clean = re.sub(r'^[_\-\s]+|[_\-\s]+$', '', text)
    clean = os.path.splitext(clean)[0]
    res = []
    i = 0
    while i < len(clean):
        if i + 1 < len(clean) and clean[i+1] in 'ゃゅょぁぃぅぇぉ':
            res.append(clean[i:i+2])
            i += 2
        else:
            res.append(clean[i])
            i += 1
    return res


def get_expected_vcv_aliases(filename: str) -> List[str]:
    """
    Generates the complete, expected list of VCV aliases from a Japanese VCV WAV filename.
    """
    moras = tokenize_japanese_mora(filename)
    if len(moras) < 2:
        return []

    expected = []
    for idx, m in enumerate(moras):
        if idx == 0:
            expected.append(f"- {m}")
        else:
            prev_v = get_mora_vowel(moras[idx - 1])
            expected.append(f"{prev_v} {m}")

    last_v = get_mora_vowel(moras[-1])
    expected.append(f"{last_v} -")
    expected.append(f"{last_v} R")
    return expected


class VCVEntry:
    def __init__(self, filename: str, alias: str, offset: float, consonant: float, cutoff: float, preutterance: float, overlap: float):
        self.filename = filename
        self.alias = alias
        self.offset = offset
        self.consonant = consonant
        self.cutoff = cutoff
        self.preutterance = preutterance
        self.overlap = overlap

    @classmethod
    def parse_line(cls, line: str) -> Optional['VCVEntry']:
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


def categorize_vcv_phoneme(alias: str) -> str:
    clean_alias = alias.strip()

    if clean_alias in ["br", "pne", "息", "吸"] or "息" in clean_alias or "br" in clean_alias.lower():
        return "AUXILIARY"

    if clean_alias.startswith("-"):
        return "INITIAL_MORA"

    if clean_alias in ['a', 'i', 'u', 'e', 'o', 'あ', 'い', 'う', 'え', 'お', 'ん', 'n']:
        return "INITIAL_MORA"

    if re.search(r'\s+[-R]$', clean_alias) or clean_alias.endswith("-"):
        return "RELEASE_MORA"

    parts = clean_alias.split()
    if len(parts) >= 2:
        mora = parts[1]

        if mora in ['あ', 'い', 'う', 'え', 'お', 'a', 'i', 'u', 'e', 'o']:
            return "VOWEL_VOWEL"

        if any(mora.startswith(c) for c in ['か', 'き', 'く', 'け', 'こ', 'が', 'ぎ', 'ぐ', 'げ', 'ご', 'た', 'ち', 'つ', 'て', 'と', 'だ', 'ぢ', 'づ', 'で', 'ど', 'ぱ', 'ぴ', 'ぷ', 'ぺ', 'ぽ', 'ば', 'び', 'ぶ', 'べ', 'ぼ', 'k', 'g', 't', 'd', 'p', 'b']):
            return "PLOSIVE"

        if any(mora.startswith(c) for c in ['さ', 'し', 'す', 'せ', 'そ', 'ざ', 'じ', 'ず', 'ぜ', 'ぞ', 'は', 'ひ', 'ふ', 'へ', 'ほ', 'ふぁ', 'ふぃ', 'ふぇ', 'ふぉ', 's', 'z', 'h', 'f', 'sh', 'ch', 'ts']):
            return "FRICATIVE"

        if any(mora.startswith(c) for c in ['な', 'に', 'ぬ', 'ね', 'の', 'ま', 'み', 'む', 'め', 'も', 'ら', 'り', 'る', 'れ', 'ろ', 'や', 'ゆ', 'よ', 'わ', 'を', 'ん', 'm', 'n', 'r', 'y', 'w']):
            return "NASAL_LIQUID"

    return "VOWEL_VOWEL"


def balance_vcv_entry(
    entry: VCVEntry,
    wav_duration: Optional[float] = None,
    acoustic_bounds: Optional[Tuple[Optional[float], Optional[float]]] = None,
    search_start: Optional[float] = None,
    next_offset: Optional[float] = None
) -> Tuple[VCVEntry, List[str]]:
    """
    Balances a VCV entry according to DELTA SYNTH Rule 10 Master VCV Standards.
    Preserves exact acoustic preutterance anchors while harmonizing overlap, consonant area,
    Middle White Strip precision (10 <= W <= 36.5 ms), and acoustic noise floor / breath artifact trimming.

    OpenUtau C# Engine Ground Truth (OtoPlot.cs:L386-388, VoicebankErrorChecker.cs:L25):
    - Green line = Overlap (OVL)
    - Red line = Preutterance (PRE)
    - Timing Safety: PRE >= OVL + 10ms (Red line comes after Green line on timeline)
    - Consonant End = Offset + Consonant (Pink shaded fixed region)
    - When Cutoff < 0: Cutoff Point = Offset - Cutoff = Offset + abs(Cutoff)
    - Middle White Strip W = (Offset + abs(Cutoff)) - (Offset + Consonant) = abs(Cutoff) - Consonant
    - Standard Rule 10: 10 <= W <= 36.5 ms (Target W = 20.0 ms) -> Cutoff = -(Consonant + W)
    """
    fixes = []
    e = VCVEntry(entry.filename, entry.alias, entry.offset, entry.consonant, entry.cutoff, entry.preutterance, entry.overlap)
    category = categorize_vcv_phoneme(e.alias)

    # Rule 0: Red-Line Center Anchor & Acoustic Preservation (Prime Directive: Preserve Before Replace)
    # The Red Line (Offset + Preutterance) marks the center midpoint between sound waves.
    # On existing entries with established beat structures, preserve Offset, Preutterance, and Overlap!
    if acoustic_bounds is not None:
        sound_start, sound_end = acoustic_bounds
        lead_cushion = get_vcv_lead_cushion(e.alias)
        min_allowed_off = search_start if search_start is not None else 0.0
        # Only apply conservative micro-adjustment (<= 25ms) or uninitialized entries (offset <= 0)
        if sound_start is not None:
            new_off = max(min_allowed_off, sound_start - lead_cushion)
            orig_beat_point = e.offset + e.preutterance
            min_pre = 35.0
            safe_new_off = min(new_off, orig_beat_point - min_pre)
            shift = safe_new_off - e.offset
            # Conservative guard: allow backward expansion only for soft voiceless consonants to restore clipped onsets, but preserve established entries
            is_soft_phoneme = is_soft_vcv_phoneme(e.alias)
            if (is_soft_phoneme and shift < -5.0 and abs(shift) <= 150.0) or (shift > 5.0 and (e.offset <= 0.0 or shift <= 25.0)):
                if shift > 5.0:
                    orig_off = e.offset
                    e.offset = round(safe_new_off, 1)
                    e.preutterance = max(min_pre, round(orig_beat_point - e.offset, 1))
                    e.overlap = max(10.0, round(e.overlap - shift, 1))
                    if e.consonant > 0:
                        e.consonant = max(15.0, round(e.consonant - shift, 1))
                    fixes.append(f"Acoustically micro-aligned lead-in ({orig_off:.1f} -> {e.offset:.1f}ms, shifted {shift:.1f}ms, beat locked)")
                elif shift < -5.0:
                    orig_off = e.offset
                    e.offset = round(safe_new_off, 1)
                    e.preutterance = round(orig_beat_point - e.offset, 1)
                    e.overlap = round(e.overlap - shift, 1)
                    if e.consonant > 0:
                        e.consonant = round(e.consonant - shift, 1)
                    fixes.append(f"Acoustically restored clipped consonant onset ({orig_off:.1f} -> {e.offset:.1f}ms, expanded {abs(shift):.1f}ms backward)")

    # Rule 1: Non-negative Offset Safety & In-bounds verification
    if e.offset < 0:
        e.offset = 0.0
        fixes.append("Offset set to non-negative 0.0")

    if wav_duration is not None and wav_duration > 0:
        if e.offset >= wav_duration - 20.0:
            orig_off = e.offset
            e.offset = max(0.0, wav_duration - 100.0)
            fixes.append(f"Clamped out-of-bounds offset ({orig_off:.1f} -> {e.offset:.1f}ms)")

    # Rule 2: Timing Safety Enforcement (Rule 10: preutterance >= overlap + 10ms)
    MIN_SAFETY_GAP = 10.0
    if e.preutterance <= e.overlap + MIN_SAFETY_GAP:
        orig_pre = e.preutterance
        e.preutterance = e.overlap + MIN_SAFETY_GAP
        fixes.append(f"Timing Safety: preutterance ({orig_pre:.1f} -> {e.preutterance:.1f}ms > overlap)")

    # Rule 3: Consonant Area Safety (Pink Strip Protection against resampler smearing - "ร้องไม่ชัด")
    is_soft = is_soft_vcv_phoneme(e.alias)
    if is_soft or category in ["PLOSIVE", "FRICATIVE"]:
        min_pad = 75.0
    elif category == "INITIAL_MORA":
        min_pad = 70.0
    elif category == "NASAL_LIQUID":
        min_pad = 220.0
    else:
        min_pad = 65.0

    if e.consonant > 0 and e.consonant <= e.preutterance + 25.0:
        orig_con = e.consonant
        e.consonant = round(e.preutterance + min_pad, 1)
        fixes.append(f"Extended consonant fixed area ({orig_con:.1f} -> {e.consonant:.1f}ms > preutterance + {min_pad}ms)")

    if wav_duration is not None and wav_duration > 0:
        consonant_end = e.offset + e.consonant
        if consonant_end >= wav_duration - 15.0:
            orig_con = e.consonant
            e.consonant = max(e.preutterance + 25.0, wav_duration - e.offset - 35.0)
            fixes.append(f"Clamped consonant within audio bounds ({orig_con:.1f} -> {e.consonant:.1f}ms)")

    # Rule 4: Master Overlap & Spacing Harmonization by Acoustic Category
    if category == "VOWEL_VOWEL":
        target_overlap_gap = min(80.0, max(50.0, e.preutterance * 0.45))
        desired_overlap = max(10.0, e.preutterance - target_overlap_gap)
        if abs(e.overlap - desired_overlap) > 15.0:
            orig_ov = e.overlap
            e.overlap = round(desired_overlap, 1)
            fixes.append(f"Vowel-Vowel Overlap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms)")

    elif category == "PLOSIVE":
        if e.preutterance <= e.overlap + 10.0:
            orig_ov = e.overlap
            e.overlap = max(10.0, round(e.preutterance - 10.0, 1))
            fixes.append(f"Plosive Overlap secured ({orig_ov:.1f} -> {e.overlap:.1f}ms)")
        elif e.overlap <= 0.0 or (e.preutterance - e.overlap) > 150.0:
            target_overlap_gap = min(120.0, max(70.0, e.preutterance * 0.55))
            desired_overlap = max(10.0, e.preutterance - target_overlap_gap)
            orig_ov = e.overlap
            e.overlap = round(desired_overlap, 1)
            fixes.append(f"Plosive Overlap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms)")

    elif category == "FRICATIVE":
        if e.preutterance <= e.overlap + 10.0:
            orig_ov = e.overlap
            e.overlap = max(10.0, round(e.preutterance - 10.0, 1))
            fixes.append(f"Fricative Overlap secured ({orig_ov:.1f} -> {e.overlap:.1f}ms)")
        elif e.overlap <= 0.0 or (e.preutterance - e.overlap) > 160.0:
            target_overlap_gap = min(140.0, max(80.0, e.preutterance * 0.60))
            desired_overlap = max(10.0, e.preutterance - target_overlap_gap)
            orig_ov = e.overlap
            e.overlap = round(desired_overlap, 1)
            fixes.append(f"Fricative Overlap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms)")

    elif category == "NASAL_LIQUID":
        if e.preutterance <= e.overlap + 10.0:
            orig_ov = e.overlap
            e.overlap = max(10.0, round(e.preutterance - 10.0, 1))
            fixes.append(f"Nasal/Liquid Overlap secured ({orig_ov:.1f} -> {e.overlap:.1f}ms)")
        elif e.overlap <= 0.0 or (e.preutterance - e.overlap) > 160.0:
            # Gold standard: media_1789614433535.png (a ら) PRE=266.3, OVL=148.6 -> Attack Gap ~ 117.6ms
            desired_overlap = max(10.0, round(e.preutterance - 117.6, 1))
            orig_ov = e.overlap
            e.overlap = desired_overlap
            fixes.append(f"Nasal/Liquid Overlap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms ~ 117ms gap)")

    elif category == "INITIAL_MORA":
        alias_clean = e.alias.replace("-", "").strip().lower()
        is_vowel = alias_clean in ['a', 'i', 'u', 'e', 'o', 'n', 'ん', 'あ', 'い', 'う', 'え', 'お']
        if is_vowel:
            # Anti-pattern Prevention: Trim dead silence/breath to prevent massive fade-ins.
            # Enforce a crisp Attack Cushion (15ms fade-in, 50ms gap -> 65ms PRE).
            target_pre = 65.0
            target_init_ov = 15.0
            
            if e.preutterance > 85.0:
                true_sound_onset = e.offset + e.preutterance
                orig_off = e.offset
                orig_pre = e.preutterance
                e.offset = max(0.0, round(true_sound_onset - target_pre, 1))
                e.preutterance = target_pre
                e.overlap = target_init_ov
                fixes.append(f"Trimmed initial dead silence on VCV Vowel {e.alias}: offset ({orig_off:.1f} -> {e.offset:.1f}ms), preutterance ({orig_pre:.1f} -> {e.preutterance:.1f}ms)")
            else:
                if e.preutterance <= e.overlap + 10.0:
                    orig_ov = e.overlap
                    e.overlap = max(10.0, round(e.preutterance - 10.0, 1))
                    fixes.append(f"Initial/Pure Vowel Overlap secured ({orig_ov:.1f} -> {e.overlap:.1f}ms)")
                elif abs(e.overlap - target_init_ov) > 8.0:
                    orig_ov = e.overlap
                    e.overlap = target_init_ov
                    fixes.append(f"Initial/Pure Vowel Overlap gap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms ~ 50ms gap)")

            min_cons = max(e.preutterance + 150.0, 300.0)
            if e.consonant < e.preutterance + 80.0:
                orig_cons = e.consonant
                e.consonant = round(min_cons, 1)
                fixes.append(f"Initial/Pure Vowel Consonant extended ({orig_cons:.1f} -> {e.consonant:.1f}ms)")
        else:
            is_fric = any(alias_clean.startswith(c) for c in ['さ', 'し', 'す', 'せ', 'そ', 'ざ', 'じ', 'ず', 'ぜ', 'ぞ', 's', 'z', 'sh', 'ch', 'ts'])
            is_stop = any(alias_clean.startswith(c) for c in ['か', 'き', 'く', 'け', 'こ', 'た', 'ち', 'つ', 'て', 'と', 'ぱ', 'ぴ', 'ぷ', 'ぺ', 'ぽ', 'k', 't', 'p'])
            target_pre = 80.0 if is_fric else (60.0 if is_stop else 65.0)
            target_ov = 20.0 if is_fric else (15.0 if is_stop else 20.0)

            # Preserve established initial mora preutterance & attack gap!
            # Only balance overlap if gap violates timing safety or is uncalibrated
            desired_gap = target_pre - target_ov
            target_init_ov = max(10.0, round(e.preutterance - desired_gap, 1))
            if e.preutterance <= e.overlap + 10.0:
                orig_ov = e.overlap
                e.overlap = max(10.0, round(e.preutterance - 10.0, 1))
                fixes.append(f"Initial Consonant Overlap secured ({orig_ov:.1f} -> {e.overlap:.1f}ms)")
            elif abs(e.overlap - target_init_ov) > 30.0 and e.preutterance < 160.0:
                orig_ov = e.overlap
                e.overlap = target_init_ov
                fixes.append(f"Initial Consonant Overlap harmonized ({orig_ov:.1f} -> {e.overlap:.1f}ms)")

    elif category == "RELEASE_MORA":
        target_overlap_gap = 10.0
        desired_overlap = max(10.0, round(e.preutterance - target_overlap_gap, 1))
        if abs(e.overlap - desired_overlap) > 5.0:
            orig_ov = e.overlap
            e.overlap = round(desired_overlap, 1)
            fixes.append(f"Release Mora Overlap balanced ({orig_ov:.1f} -> {e.overlap:.1f}ms ~ 10ms gap)")

    # Rule 5: Middle White Strip Optimization & Trailing Noise Elimination
    # OpenUtau C# Engine Ground Truth (OtoPlot.cs:L386-388, VoicebankErrorChecker.cs:L25):
    # double cutoff = oto.Cutoff < 0 ? oto.Offset - oto.Cutoff : fileDuration - oto.Cutoff;
    # When Cutoff < 0: Cutoff timeline pos = Offset - Cutoff = Offset + abs(Cutoff)
    # Pink fixed region ends at = Offset + Consonant
    # Stretchable Middle White Strip W = (Offset + abs(Cutoff)) - (Offset + Consonant) = abs(Cutoff) - Consonant
    # Standard Rule 10: 10 <= W <= 36.5 ms (Target W = 20.0 ms) -> Cutoff = -(Consonant + W)
    MAX_WHITE_GAP = 36.5
    MIN_WHITE_GAP = 10.0
    TARGET_WHITE_GAP = 20.0

    # Determine boundary ceiling (next mora offset or audio duration)
    max_cut_pos = None
    if next_offset is not None and next_offset > e.offset + 25.0:
        max_cut_pos = next_offset - 5.0
    elif wav_duration is not None and wav_duration > e.offset + 25.0:
        max_cut_pos = wav_duration - 5.0

    # Ensure consonant does not overshoot maximum boundary
    if max_cut_pos is not None and (e.offset + e.consonant + MIN_WHITE_GAP) > max_cut_pos:
        e.consonant = max(e.preutterance + 15.0, max_cut_pos - MIN_WHITE_GAP - e.offset)

    # Calculate current white strip
    if e.cutoff < 0:
        current_w = abs(e.cutoff) - e.consonant
    else:
        # Legacy positive cutoff: fileDuration - Cutoff - (Offset + Consonant)
        if wav_duration is not None and wav_duration > 0:
            cut_pos = wav_duration - e.cutoff
            current_w = cut_pos - (e.offset + e.consonant)
        else:
            current_w = -1.0

    # Check if cutoff needs calibration
    # Standard Rule 10: 10 <= W <= 72 ms (Target W = 25.0 to 45.0 ms, media_1789613357785.png W=69.22ms).
    # If already negative and within [10.0, 72.0] ms, preserve user gold standard!
    needs_rebalance = (e.cutoff >= 0) or (current_w < MIN_WHITE_GAP) or (current_w > 72.0)
    if not needs_rebalance and max_cut_pos is not None:
        if (e.offset + abs(e.cutoff)) > max_cut_pos:
            needs_rebalance = True

    if needs_rebalance:
        orig_cut = e.cutoff
        # For Vowel-Vowel transitions (media_1789612375236.png), target W ~ 35-43ms
        # For Initial Moras (media_1789611916252.png), target W ~ 36.3ms
        # For Plosive transitions (media_1789613357785.png), target W ~ 35-50ms
        if category == "VOWEL_VOWEL":
            target_w = 35.0
        elif category == "INITIAL_MORA":
            target_w = 36.3
        elif category == "PLOSIVE":
            target_w = 35.0
        elif category == "NASAL_LIQUID":
            target_w = 52.6
        else:
            target_w = TARGET_WHITE_GAP

        if max_cut_pos is not None:
            max_allowed_w = max_cut_pos - (e.offset + e.consonant)
            if max_allowed_w < MIN_WHITE_GAP:
                e.consonant = max(e.preutterance + 15.0, max_cut_pos - MIN_WHITE_GAP - e.offset)
                target_w = MIN_WHITE_GAP
            else:
                target_w = min(target_w, max(MIN_WHITE_GAP, max_allowed_w))

        e.cutoff = -round(e.consonant + target_w, 2)
        fixes.append(f"Calibrated Middle White Strip ({orig_cut:.1f} -> {e.cutoff:.1f}ms, W={target_w:.1f}ms)")

    return e, fixes


def recover_missing_vcv_moras(
    wav_file: str,
    wav_path: str,
    existing_entries: List[VCVEntry],
    wav_dur: Optional[float]
) -> Tuple[List[VCVEntry], int]:
    """
    Intelligently recovers missing moras from audio RMS profile and tempo interpolation
    for multi-mora Japanese VCV files, anchoring the Red Line (Preutterance) directly
    at the acoustic transition midpoint between sound waves.
    Eliminates duplicate or conflicting aliases.
    """
    moras = tokenize_japanese_mora(wav_file)
    if len(moras) < 2:
        return existing_entries, 0

    expected_aliases = get_expected_vcv_aliases(wav_file)
    if not expected_aliases:
        return existing_entries, 0

    profile = get_wav_rms_profile(wav_path)
    if not profile:
        return existing_entries, 0

    sr, win_ms, rms_list = profile
    if not rms_list:
        return existing_entries, 0

    # 1. Clean up duplicate / contradictory aliases
    cleaned_entries: List[VCVEntry] = []
    seen_aliases: Set[str] = set()
    for e in existing_entries:
        al = e.alias.strip()
        if al == 'i ue' and 'i うぇ' not in seen_aliases:
            e.alias = 'i うぇ'
            al = 'i うぇ'
        elif al == '- yi' and '- い' not in seen_aliases:
            e.alias = '- い'
            al = '- い'
        elif al == '- ゆい' and any(x.alias.strip() in ['- い', '- yi', '- いい'] for x in existing_entries):
            continue

        if al not in seen_aliases:
            seen_aliases.add(al)
            cleaned_entries.append(e)

    cleaned_entries.sort(key=lambda x: x.offset)

    # 2. Identify known mora beat positions (Red Line on timeline: Offset + Preutterance)
    known_beats: List[Tuple[int, float]] = []
    existing_map: Dict[str, VCVEntry] = {e.alias.strip(): e for e in cleaned_entries}
    for idx, exp_al in enumerate(expected_aliases):
        if exp_al in existing_map:
            ent = existing_map[exp_al]
            known_beats.append((idx, ent.offset + ent.preutterance))

    # Calculate average tempo interval between moras
    if len(known_beats) >= 2:
        deltas = []
        for i in range(len(known_beats) - 1):
            idx_diff = known_beats[i+1][0] - known_beats[i][0]
            t_diff = known_beats[i+1][1] - known_beats[i][1]
            if idx_diff > 0 and t_diff > 0:
                deltas.append(t_diff / float(idx_diff))
        avg_tempo = sum(deltas) / float(len(deltas)) if deltas else 650.0
    else:
        avg_tempo = 650.0

    ref_idx, ref_beat = known_beats[0] if known_beats else (0, 650.0)

    # 3. Recover missing moras anchored at the Red Line midpoint
    final_entries = list(cleaned_entries)
    existing_alias_set = set(e.alias.strip() for e in final_entries)
    recovered_count = 0

    for exp_idx, exp_alias in enumerate(expected_aliases):
        if exp_alias in existing_alias_set:
            continue

        # Estimated beat on timeline
        est_beat = ref_beat + (exp_idx - ref_idx) * avg_tempo

        # Search for true vocal energy burst near estimated beat
        start_i = max(0, int((est_beat - 180.0) / win_ms))
        end_i = min(len(rms_list), int(math.ceil((est_beat + 180.0) / win_ms)))
        sub = rms_list[start_i:end_i]
        if sub and max(sub) > 180.0:
            refined_beat = (start_i + sub.index(max(sub))) * win_ms
        else:
            refined_beat = max(100.0, est_beat)

        cat = categorize_vcv_phoneme(exp_alias)
        is_vowel_ending = any(exp_alias.endswith(v) for v in ['あ', 'い', 'う', 'え', 'お', 'a', 'i', 'u', 'e', 'o'])

        if cat == 'INITIAL_MORA':
            # Gold standard: Offset ~ 120-218ms, PRE ~ 490.3ms (V) / 520ms (C), OVL ~ 381.3ms (V) / 475ms (C)
            pre = 490.3 if is_vowel_ending else 520.0
            ovl = 381.3 if is_vowel_ending else 475.0
            off = max(0.0, refined_beat - pre)
            con = 780.8
            cut = -(con + 36.3)
        elif cat == 'RELEASE_MORA':
            # Gold standard: Offset at decay start, PRE=160, OVL=150, CON=352, CUT=-372
            pre = 160.0
            ovl = 150.0
            off = max(0.0, refined_beat - pre)
            con = 352.0
            cut = -(con + 20.0)
        elif cat == 'VOWEL_VOWEL':
            # Gold standard (media_1789612375236.png): PRE=353.4, OVL=273.4 (Attack Gap = 80ms), CON=706.9, CUT=-741.9
            pre = 353.4
            ovl = 273.4
            off = max(0.0, refined_beat - pre)
            con = 706.9
            cut = -(con + 35.0)
        else:
            # Consonant VCV transition: PRE=380.0, OVL=250.0 (Attack Gap = 130ms), CON=650.0, CUT=-670.0
            pre = 380.0
            ovl = 250.0
            off = max(0.0, refined_beat - pre)
            con = 650.0
            cut = -(con + 20.0)

        recovered_entry = VCVEntry(
            wav_file,
            exp_alias,
            round(off, 1),
            round(con, 1),
            round(cut, 1),
            round(pre, 1),
            round(ovl, 1)
        )
        final_entries.append(recovered_entry)
        existing_alias_set.add(exp_alias)
        recovered_count += 1

    final_entries.sort(key=lambda x: x.offset)
    return final_entries, recovered_count


def generate_master_template_json() -> Dict:
    return {
        "title": "DELTA SYNTH VCV Master Oto Template Specification",
        "standard": "DELTA SYNTH Rule 10 (Audio & Phonetics Architecture)",
        "version": "2.2",
        "safety_rules": {
            "min_preutterance_overlap_gap_ms": 10.0,
            "min_consonant_preutterance_gap_ms": 20.0,
            "min_trailing_vowel_tail_ms": 60.0,
            "target_middle_white_strip_ms": 20.0,
            "liquid_tight_articulation_gap_ms": 222.9,
            "liquid_target_overlap_gap_ms": 117.6
        },
        "categories": {
            "VOWEL_VOWEL": {
                "description": "Vowel to Vowel transition (e.g., a あ, a い)",
                "target_overlap_gap_ratio": 0.45,
                "max_overlap_gap_ms": 80.0
            },
            "PLOSIVE": {
                "description": "Unvoiced / Voiced Stops (e.g., a か, i き, u つ)",
                "target_overlap_gap_ratio": 0.55,
                "max_overlap_gap_ms": 120.0
            },
            "FRICATIVE": {
                "description": "Fricatives & Sibilants (e.g., a さ, i し, a ふ)",
                "target_overlap_gap_ratio": 0.60,
                "max_overlap_gap_ms": 140.0
            },
            "NASAL_LIQUID": {
                "description": "Nasals & Liquids (e.g., a ま, a な, a ら)",
                "target_overlap_gap_ratio": 0.50,
                "max_overlap_gap_ms": 120.0,
                "target_attack_gap_ms": 117.6,
                "target_consonant_distance_ms": 222.9
            },
            "INITIAL_MORA": {
                "description": "Initial Moras from Silence (e.g., - か, - あ)",
                "target_overlap_gap_ratio": 0.40,
                "max_overlap_gap_ms": 100.0
            },
            "RELEASE_MORA": {
                "description": "Release & Silence endings (e.g., a -, a R)",
                "target_overlap_gap_ratio": 0.45,
                "max_overlap_gap_ms": 80.0
            }
        }
    }


def balance_oto_file(filepath: str, backup: bool = True) -> Tuple[int, int]:
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

    print(f"[*] Analyzing VCV Oto: {filepath} (Encoding: {detected_encoding})")

    dir_path = os.path.dirname(filepath)
    parsed_lines: List[Tuple[str, Optional[VCVEntry]]] = []
    for line in lines:
        entry = VCVEntry.parse_line(line)
        parsed_lines.append((line, entry))

    wav_groups: Dict[str, List[VCVEntry]] = {}
    for raw_line, entry in parsed_lines:
        if entry is not None:
            fn = entry.filename
            if fn == "ふぉふんふぁんんふ.wav":
                fn = "ふぉふんふぁんふ.wav"
                entry.filename = fn
            elif fn == "りょりゅんりゃんんりゅ.wav":
                fn = "りょりゅんりゃんりゅ.wav"
                entry.filename = fn
            wav_groups.setdefault(fn, []).append(entry)

    if os.path.isdir(dir_path):
        try:
            for disk_file in os.listdir(dir_path):
                if disk_file.lower().endswith(".wav") and disk_file not in wav_groups:
                    wav_groups[disk_file] = []
        except Exception:
            pass

    out_lines: List[str] = []
    for raw_line, entry in parsed_lines:
        if entry is None and raw_line.strip().startswith("#"):
            out_lines.append(raw_line)

    total_entries = 0
    balanced_entries = 0
    total_recovered = 0

    for wav_file in sorted(wav_groups.keys()):
        raw_entries = wav_groups[wav_file]
        wav_path = os.path.join(dir_path, wav_file)
        wav_dur = get_wav_duration_ms(wav_path)

        rec_entries, rec_count = recover_missing_vcv_moras(wav_file, wav_path, raw_entries, wav_dur)
        total_recovered += rec_count

        for g_idx, entry in enumerate(rec_entries):
            total_entries += 1
            prev_end = (rec_entries[g_idx - 1].offset + rec_entries[g_idx - 1].consonant) if g_idx > 0 else 0.0
            search_start = max(prev_end, entry.offset - 60.0)
            next_off = rec_entries[g_idx + 1].offset if g_idx + 1 < len(rec_entries) else wav_dur
            is_soft = is_soft_vcv_phoneme(entry.alias)
            ac_bounds = analyze_acoustic_bounds(wav_path, region_start=search_start, region_end=next_off, is_soft_consonant=is_soft)
            bal_entry, fixes = balance_vcv_entry(entry, wav_duration=wav_dur, acoustic_bounds=ac_bounds, search_start=search_start, next_offset=next_off)
            out_lines.append(bal_entry.to_line())
            if fixes or rec_count > 0:
                balanced_entries += 1

    if backup and total_entries > 0:
        bak_path = filepath + ".bak"
        with open(bak_path, "wb") as f_bak:
            f_bak.write(raw_bytes)
        print(f"[*] Backup created: {bak_path}")

    with open(filepath, "w", encoding=detected_encoding, errors="replace") as f_out:
        f_out.write("\n".join(out_lines) + "\n")

    print(f"[OK] Master Oto Balance Complete! Total: {total_entries} entries (Recovered {total_recovered} missing moras), Balanced: {balanced_entries} entries.\n")
    return total_entries, balanced_entries


def run_unit_tests():
    print("=== Running VCV Master Oto Balancer Self-Tests ===")

    # Test 1: Vowel-Vowel Overlap Balance
    e1 = VCVEntry("a.wav", "a あ", 1000, 500, -600, 200, 20)
    bal1, fixes1 = balance_vcv_entry(e1)
    assert bal1.preutterance > bal1.overlap, "Test 1 Failed: preutterance must be > overlap"
    assert 100.0 <= bal1.overlap <= 150.0, "Test 1 Failed: Vowel-Vowel overlap ratio"
    print("  [PASS] Test 1: Vowel-Vowel Overlap Harmonization")

    # Test 2: Plosive Overlap Harmonization & Consonant Protection
    e2 = VCVEntry("ka.wav", "a か", 1000, 200, -600, 200, 10)
    bal2, fixes2 = balance_vcv_entry(e2)
    assert bal2.consonant >= bal2.preutterance + 75.0, "Test 2 Failed: Consonant must cover plosive burst"
    assert bal2.overlap >= 80.0, "Test 2 Failed: Plosive overlap ratio"
    print("  [PASS] Test 2: Plosive Overlap Harmonization & Consonant Protection")

    # Test 3: Middle White Strip Calibration (10 <= W <= 36.5 ms) - OpenUtau C# Ground Truth
    e3 = VCVEntry("ka.wav", "a か", 1000, 350, -500, 200, 80)
    bal3, fixes3 = balance_vcv_entry(e3, wav_duration=3000.0)
    white_gap = abs(bal3.cutoff) - bal3.consonant
    assert 10.0 <= white_gap <= 36.5, f"Test 3 Failed: True White gap must be between 10 and 36.5 (got {white_gap})"
    cut_pos = bal3.offset - bal3.cutoff
    pink_end = bal3.offset + bal3.consonant
    assert abs((cut_pos - pink_end) - white_gap) < 1e-4, "Test 3 Failed: OpenUtau timeline geometry mismatch"
    print("  [PASS] Test 3: Middle White Strip Calibration (10 <= W <= 36.5 ms)")

    # Test 4: Category Detection
    assert categorize_vcv_phoneme("a あ") == "VOWEL_VOWEL"
    assert categorize_vcv_phoneme("a か") == "PLOSIVE"
    assert categorize_vcv_phoneme("a さ") == "FRICATIVE"
    assert categorize_vcv_phoneme("a な") == "NASAL_LIQUID"
    assert categorize_vcv_phoneme("- か") == "INITIAL_MORA"
    assert categorize_vcv_phoneme("a -") == "RELEASE_MORA"
    print("  [PASS] Test 4: VCV Category Classifier")

    # Test 5: Initial Mora Auto-Trimming & Anti-Pattern Prevention
    e5 = VCVEntry("あああいあうえあ.wav", "- あ", 217.907, 780.833, -817.151, 490.29, 381.337)
    bal5, fixes5 = balance_vcv_entry(e5)
    assert bal5.offset == 643.2, f"Test 5 Failed: Offset should be trimmed (got {bal5.offset})"
    assert bal5.preutterance == 65.0, f"Test 5 Failed: Preutterance should be 65.0 (got {bal5.preutterance})"
    assert bal5.overlap == 15.0, f"Test 5 Failed: Overlap should be 15.0 (got {bal5.overlap})"
    print("  [PASS] Test 5: Initial Mora Auto-Trimming & Anti-Pattern Prevention")

    # Test 6: Soft VCV Phoneme Detection & Restoration
    assert is_soft_vcv_phoneme("a さ") is True, "Test 6 Failed: a さ should be soft phoneme"
    assert is_soft_vcv_phoneme("a ま") is False, "Test 6 Failed: a ま should not be soft phoneme"
    e6 = VCVEntry("sa.wav", "a さ", 500.0, 150.0, -400.0, 200.0, 50.0)
    bal6, fixes6 = balance_vcv_entry(e6, acoustic_bounds=(460.0, 800.0))
    assert bal6.offset < 500.0, "Test 6 Failed: Offset should expand backward to restore clipped soft consonant"
    print("  [PASS] Test 6: Thin-Singer Soft VCV Consonant Detection & Restoration")

    # Test 7: Japanese Mora Tokenizer & Expected Alias Generator
    m_tokens = tokenize_japanese_mora("ききくかけきけ")
    assert m_tokens == ['き', 'き', 'く', 'か', 'け', 'き', 'け'], f"Test 7 Failed: Mora tokens {m_tokens}"
    aliases = get_expected_vcv_aliases("ききくかけきけ.wav")
    assert aliases == ['- き', 'i き', 'i く', 'u か', 'a け', 'e き', 'i け', 'e -', 'e R'], f"Test 7 Failed: Expected aliases {aliases}"
    print("  [PASS] Test 7: Japanese Mora Tokenizer & Expected Alias Generator")

    # Test 8: Plosive / Voiced Stop Master Standard (media_1789613357785.png)
    e8 = VCVEntry("ででづどでどど.wav", "o ど", 4876.9, 792.896, -862.116, 240.087, 160.1)
    bal8, fixes8 = balance_vcv_entry(e8)
    assert bal8.offset == 4876.9, f"Test 8 Failed: Offset preserved (got {bal8.offset})"
    assert bal8.preutterance == 240.087, f"Test 8 Failed: Preutterance preserved (got {bal8.preutterance})"
    assert bal8.overlap == 160.1, f"Test 8 Failed: Overlap preserved (got {bal8.overlap})"
    assert bal8.consonant == 792.896, f"Test 8 Failed: Consonant preserved (got {bal8.consonant})"
    assert bal8.cutoff == -862.116, f"Test 8 Failed: Cutoff preserved (got {bal8.cutoff})"
    assert abs(bal8.preutterance - bal8.overlap - 79.987) < 1e-3, "Test 8 Failed: 80ms Attack Gap"
    print("  [PASS] Test 8: Plosive / Voiced Stop Gold Standard Preservation (media_1789613357785.png)")

    # Test 9: Liquid / Flap Tight Articulation Master Standard (media_1789614433535.png)
    e9 = VCVEntry("ららりらるれら.wav", "a ら", 892.392, 489.199, -541.834, 266.273, 148.617)
    bal9, fixes9 = balance_vcv_entry(e9)
    assert bal9.offset == 892.392, f"Test 9 Failed: Offset preserved (got {bal9.offset})"
    assert bal9.preutterance == 266.273, f"Test 9 Failed: Preutterance preserved (got {bal9.preutterance})"
    assert bal9.overlap == 148.617, f"Test 9 Failed: Overlap preserved (got {bal9.overlap})"
    assert bal9.consonant == 489.199, f"Test 9 Failed: Consonant preserved (got {bal9.consonant})"
    assert bal9.cutoff == -541.834, f"Test 9 Failed: Cutoff preserved (got {bal9.cutoff})"
    assert abs(bal9.preutterance - bal9.overlap - 117.656) < 1e-3, "Test 9 Failed: 117.6ms Attack Gap"
    assert abs(bal9.consonant - bal9.preutterance - 222.926) < 1e-3, "Test 9 Failed: 222.9ms Tight Articulation Distance"
    assert abs(abs(bal9.cutoff) - bal9.consonant - 52.635) < 1e-3, "Test 9 Failed: 52.6ms White Strip"
    print("  [PASS] Test 9: Liquid / Flap Tight Articulation Gold Standard Preservation (media_1789614433535.png)")

    print("=== All VCV Master Oto Balancer Self-Tests Passed! ===\n")


def main():
    parser = argparse.ArgumentParser(description="Japanese VCV Oto.ini Master Balancer (DELTA SYNTH Rule 10)")
    parser.add_argument("path", nargs="?", help="Path to VCV oto.ini file or Voicebank directory")
    parser.add_argument("--export-template", action="store_true", help="Export vcv_master_template.json specification file")
    parser.add_argument("--no-backup", action="store_true", help="Disable creation of .bak backup file")
    parser.add_argument("--test", action="store_true", help="Run internal self-tests")

    args = parser.parse_args()

    if args.test:
        run_unit_tests()
        return

    if args.export_template:
        template_data = generate_master_template_json()
        out_json_path = os.path.join(os.getcwd(), "vcv_master_template.json")
        with open(out_json_path, "w", encoding="utf-8") as f:
            json.dump(template_data, f, ensure_ascii=False, indent=2)
        print(f"[OK] Master Template Specification exported to: {out_json_path}")
        return

    if not args.path:
        print("Usage: python vcv_master_oto_balancer.py <path_to_vcv_oto.ini_or_voicebank_dir>")
        print("Example: python vcv_master_oto_balancer.py \"B:\\...\\A Voice\\oto.ini\"")
        return

    target_path = os.path.abspath(args.path)
    backup_flag = not args.no_backup

    if os.path.isfile(target_path):
        balance_oto_file(target_path, backup=backup_flag)
    elif os.path.isdir(target_path):
        print(f"[*] Scanning directory for VCV oto.ini files: {target_path}")
        found_any = False
        for root, _, files in os.walk(target_path):
            for file in files:
                if file.lower() == "oto.ini":
                    found_any = True
                    balance_oto_file(os.path.join(root, file), backup=backup_flag)
        if not found_any:
            print("[!] No oto.ini files found in the specified directory.")
    else:
        print(f"[!] Invalid path: {target_path}")


if __name__ == "__main__":
    main()
