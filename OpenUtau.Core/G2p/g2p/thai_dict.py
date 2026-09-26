VOWELS = {
    'a', 'aa', 'i', 'ii', 'u', 'uu', 'e', 'ee', 'o', 'oo',
    'ae', 'aee', 'oe', 'oee', 'ue', 'uee', 'ia', 'ua', 'uea',
    'ɛ', 'ɔ', 'ɯ', 'ɤ', 'a:', 'i:', 'u:', 'e:', 'o:', 'ɛ:', 'ɔ:', 'ɯ:', 'ɤ:',
    'aw', 'ay', 'am'
}

def is_vowel(phoneme: str) -> bool:
    # If the phoneme is exactly in the set, or has any common vowel character
    if phoneme in VOWELS:
        return True
    return any(char in phoneme for char in 'aiueoɛɔɯɤ')

def is_consonant(phoneme: str) -> bool:
    return not is_vowel(phoneme)
