VOWELS = {
    'a', 'i', 'u', 'e', 'o', '@', 'Q', '3', '6', '1',
    'ia', 'ua', 'ay', 'aw', 'am',
}

def is_vowel(phoneme: str) -> bool:
    # If the phoneme is exactly in the set, or has any common vowel character
    if phoneme in VOWELS:
        return True
    return any(char in phoneme for char in 'aiueo@Q361')

def is_consonant(phoneme: str) -> bool:
    return not is_vowel(phoneme)
