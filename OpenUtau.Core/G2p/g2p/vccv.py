from typing import List
from .thai_dict import is_vowel, is_consonant

def generate_vccv_transitions(syllables_phonemes: List[List[str]]) -> List[str]:
    """
    Generate VCCV transition pairs from a list of syllables 
    (where each syllable is a list of phonemes).
    """
    transitions = []
    
    if not syllables_phonemes:
        return []
        
    linear_phonemes = []
    for syl in syllables_phonemes:
        linear_phonemes.extend(syl)
        
    if not linear_phonemes:
        return []
        
    # Silence to first phoneme
    transitions.append(f"[- {linear_phonemes[0]}]")
    
    # Adjacent phoneme pairs
    for i in range(len(linear_phonemes) - 1):
        curr = linear_phonemes[i]
        nxt = linear_phonemes[i+1]
        transitions.append(f"[{curr} {nxt}]")
        
    # Last phoneme to silence
    transitions.append(f"[{linear_phonemes[-1]} -]")
    
    return transitions
