const badWords = [
    "profanity", "curse", "ugly", "stupid", "idiot", "dumb", "hell", "damn", "ass", "bitch", "shit", "fuck", "bastard",

    "cunt", "whore", "slut", "pussy", "dick", "cock", "vagina", "penis", "balls", "asshole", "motherfucker", "fucker", "faggot",
    "nigger", "nigga", "tard", "retard", "spic", "chink", "pedo", "porn", "sex", "rape", "kill", "bomb", "suicide",

    "putangina", "tangina", "tang ina", "pota", "puta", "ina mo", "gago", "siraulo", "tanga",
    "hayop", "animal", "lintik", "demonyo", "leche",
    "ulol", "bobo", "tarantado", "kupal", "burat", "kikiam", "pekpek", "puke", "bading",
    "bayot", "bakla", "pokpok"
];

function filterProfanity(text) {
    if (!text) return text;

    const pattern = new RegExp(`\\b(${badWords.join('|')})\\b`, 'gi');

    return text.replace(pattern, (match) => {
        return '*'.repeat(match.length);
    });
}

window.filterProfanity = filterProfanity;
