window.startTypewriter = function () {
    const phrases = [
        "Ανάρτηση υλικού.",
        "Προβολή και αξιοποίηση υλικού.",
        "Διαχείριση προφίλ και αναρτήσεων."
    ];
    const textElement = document.getElementById("text");
    if (!textElement) return;

    let currentPhrase = 0;
    let currentChar = 0;
    let isDeleting = false;

    function type() {
        const current = phrases[currentPhrase];
        const visibleText = current.slice(0, currentChar);
        textElement.textContent = visibleText;

        if (!isDeleting && currentChar < current.length) {
            currentChar++;
            setTimeout(type, 100);
        } else if (isDeleting && currentChar > 0) {
            currentChar--;
            setTimeout(type, 50);
        } else {
            setTimeout(() => {
                isDeleting = !isDeleting;
                if (!isDeleting) {
                    currentPhrase = (currentPhrase + 1) % phrases.length;
                }

                type();
            }, 500);
        }
    }

    type();
};
