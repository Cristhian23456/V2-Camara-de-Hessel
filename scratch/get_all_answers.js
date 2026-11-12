const http = require('https');

function get(url) {
    return new Promise((resolve, reject) => {
        http.get(url, (res) => {
            let data = '';
            res.on('data', (chunk) => data += chunk);
            res.on('end', () => {
                try {
                    resolve(JSON.parse(data));
                } catch (e) {
                    reject(new Error(`Failed to parse: ${e.message}`));
                }
            });
        }).on('error', reject);
    });
}

async function run() {
    const phases = ['Inicial', 'Desarrollo', 'Final'];
    const caseId = 1;
    
    for (const phase of phases) {
        const dialogos = await get(`https://api-labpsicologia.onrender.com/api/get-dialogos?caso=${caseId}&fase=${phase}`);
        for (const d of dialogos) {
            if (d.tienePregunta && d.preguntaId) {
                const q = await get(`https://api-labpsicologia.onrender.com/api/get-questionsId/${d.preguntaId}`);
                console.log(`PREGUNTA ID: ${d.preguntaId}`);
                console.log(`Pregunta: ${q.pregunta}`);
                q.respuestas.forEach((resp, idx) => {
                    console.log(`  Option ${idx + 1}: ${resp.respuesta} -> ${resp.esCorrecta ? 'CORRECT' : 'INCORRECT'}`);
                });
                console.log('');
            }
        }
    }
}

run();
