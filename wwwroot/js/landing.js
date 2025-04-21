// Typewriter animation for landing page
window.startTypewriter = function() {
    const textElement = document.getElementById('text');
    if (!textElement) return;
    
    const phrases = [
        "Ανάρτηση υλικού.",
        "Προβολή και αξιοποίηση υλικού.",
        "Διαχείριση προφίλ και αναρτήσεων."
    ];
    
    let currentPhrase = 0;
    let currentChar = 0;
    let isDeleting = false;
    let typingSpeed = 100;
    
    function type() {
        // Get current phrase
        const phrase = phrases[currentPhrase];
        
        // Determine if typing or deleting
        if (isDeleting) {
            // Remove a character
            textElement.textContent = phrase.substring(0, currentChar - 1);
            currentChar--;
            typingSpeed = 50; // Delete faster than typing
        } else {
            // Add a character
            textElement.textContent = phrase.substring(0, currentChar + 1);
            currentChar++;
            typingSpeed = 100; // Normal typing speed
        }
        
        // If completed typing the phrase
        if (!isDeleting && currentChar === phrase.length) {
            // Pause at end of phrase
            isDeleting = true;
            typingSpeed = 1000; // Wait before deleting
        }
        
        // If deleted the whole phrase
        else if (isDeleting && currentChar === 0) {
            isDeleting = false;
            currentPhrase = (currentPhrase + 1) % phrases.length; // Move to next phrase
            typingSpeed = 500; // Pause before typing new phrase
        }
        
        // Schedule next character
        setTimeout(type, typingSpeed);
    }
    
    // Start the animation
    setTimeout(type, 1000);
};
