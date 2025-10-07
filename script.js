const hebrewAlphabet = ['א', 'ב', 'ג', 'ד', 'ה', 'ו', 'ז', 'ח', 'ט', 'י', 'כ', 'ל', 'מ', 'נ', 'ס', 'ע', 'פ', 'צ', 'ק', 'ר', 'ש', 'ת'];

const letterDisplay = document.getElementById('letter-display');
const animalsContainer = document.getElementById('animals-container');
const feedback = document.getElementById('feedback');

let currentLetter = '';
let correctAnimal = '';

function getRandomLetter(excludeLetter) {
    let letter;
    do {
        letter = hebrewAlphabet[Math.floor(Math.random() * hebrewAlphabet.length)];
    } while (letter === excludeLetter);
    return letter;
}

function newRound() {
    feedback.textContent = '';
    currentLetter = hebrewAlphabet[Math.floor(Math.random() * hebrewAlphabet.length)];
    letterDisplay.textContent = currentLetter;

    animalsContainer.innerHTML = '';

    const animals = ['penguin', 'rabbit'];
    correctAnimal = animals[Math.floor(Math.random() * animals.length)];

    const animalElements = [];

    const correctAnimalDiv = document.createElement('div');
    correctAnimalDiv.classList.add('animal');
    correctAnimalDiv.dataset.letter = currentLetter;
    correctAnimalDiv.innerHTML = `
        <img src="images/${correctAnimal}.png" alt="${correctAnimal}">
        <div class="letter">${currentLetter}</div>
    `;
    animalElements.push(correctAnimalDiv);


    const incorrectLetter = getRandomLetter(currentLetter);
    const incorrectAnimal = correctAnimal === 'penguin' ? 'rabbit' : 'penguin';
    const incorrectAnimalDiv = document.createElement('div');
    incorrectAnimalDiv.classList.add('animal');
    incorrectAnimalDiv.dataset.letter = incorrectLetter;
    incorrectAnimalDiv.innerHTML = `
        <img src="images/${incorrectAnimal}.png" alt="${incorrectAnimal}">
        <div class="letter">${incorrectLetter}</div>
    `;
    animalElements.push(incorrectAnimalDiv);

    // Randomize the order of animals
    animalElements.sort(() => Math.random() - 0.5);
    animalElements.forEach(el => animalsContainer.appendChild(el));


    document.querySelectorAll('.animal').forEach(animal => {
        animal.addEventListener('click', checkAnswer);
    });
}

function checkAnswer(event) {
    const selectedLetter = event.currentTarget.dataset.letter;

    if (selectedLetter === currentLetter) {
        feedback.textContent = 'Correct!';
        feedback.className = 'correct';
        setTimeout(newRound, 1500);
    } else {
        feedback.textContent = 'Try Again!';
        feedback.className = 'incorrect';
    }
}

document.addEventListener('DOMContentLoaded', () => {
    newRound();
});