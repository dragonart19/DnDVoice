# Prova di domenica 6 settembre 2026

[← README](../README.md) · [English](PLAYTEST_2026_09_06_EN.md) ·
[Roadmap V2](ROADMAP_2_0_IT.md) ·
[Kanban](https://github.com/users/dragonart19/projects/1/views/1)

## Obiettivo e scadenza

**Inizio: domenica 6 settembre, ore 10:30, Europe/Rome. Partecipanti: 7 in
totale, DM compreso.** Priorità: completare una sessione sulla base 2D Build
1.0 Discord Direct, con voce continua e mappa sincronizzata.

Il piano è stato preparato il 5 settembre. Una verifica automatica non
certifica la qualità della chiamata: serve una prova con i sette partecipanti
reali. L'esito della sessione è ancora da verificare.

## Base esaminata

- Repository: `dragonart19/DnDVoice`, branch `main`, commit `86e05a8`.
- Progetto Unity: sottocartella `DnDVoice/` della repository.
- Editor: `6000.3.8f1`; protocollo di rete: `6`.
- Relay: 7 connessioni oltre all'host, massimo 8 partecipanti totali. Il gruppo
  previsto occupa 6 connessioni oltre all'host.
- `BuildInfo.SupportedPlayers` contiene ancora `20`, ma non aumenta il limite
  effettivo di Relay e dei pacchetti, fissato a 8. Non usare quel valore come
  dichiarazione di capacità verificata.
- Voce: riproduzione Discord Direct; callback PCM per il pan quando il formato
  lo consente. Non introdurre modifiche a buffer, latenza o filtri per questa prova.
- Recovery: il codice segnala la perdita della sincronizzazione; non esiste un
  recupero automatico completo né la migrazione dell'host.
- Salvataggi: dimensioni, muri e porte; non ripristinano la sessione, gli account
  connessi o le posizioni delle pedine.
- La cartella locale delle build contiene artefatti datati. La sola etichetta
  `Build 1.0` non dimostra che un eseguibile corrisponda ai sorgenti esaminati.

## Lavori fattibili prima della prova

Le stime sono indicative, non scadenze garantite. I bug emersi hanno precedenza
sulle aggiunte. Gli ID seguenti possono diventare schede nel Kanban; questo
documento non implica che le schede online siano già state create o spostate.

| ID | Priorità | Attività | Tempo indicativo | Criterio di completamento |
| --- | --- | --- | --- | --- |
| SUN-01 | P0 | Compilazione e test EditMode della copia GitHub | 20–45 min, più eventuali fix | Report con test scoperti, eseguiti e tutti superati; nessun errore di compilazione |
| SUN-02 | P0 | Un solo pacchetto Windows identificabile | 20–45 min dopo SUN-01 | ZIP completo, SHA-256 registrato, avvio dall'archivio estratto su un altro PC |
| SUN-03 | P0 | Mappa preparata e copia di sicurezza | 15–30 min | Salva/carica verificati; copia separata del JSON; muri e porte corretti |
| SUN-04 | P0 | Prova prolungata a 7 account | 45–60 min con il gruppo | Checklist seguente completata, senza interruzioni vocali ricorrenti o perdita della mappa |
| SUN-05 | P0 | Uscita/rientro e procedura di emergenza | 10–15 min | Un giocatore rientra; tutti conoscono la procedura se cade il DM |
| SUN-06 | P0 | Documentazione e registro degli esiti IT/EN | durante ogni attività | Stato reale, prove e limiti aggiornati; commit e push eseguiti dall'utente |
| SUN-07 | P1, opzionale | Pulsante «Copia codice sessione» | 30–60 min con verifica UI | Copia il codice esatto; il clic non interagisce con la mappa sotto il menu |
| SUN-08 | P1, opzionale | Accesso rapido a log e salvataggi | 30–90 min con verifica UI | Apre solo le cartelle locali previste, anche quando non esistono ancora |

Per questa scadenza restano in roadmap V2: builder 3D, import di mappe/asset,
NPC impersonabili, community, rifacimento grafico, nuova architettura audio,
riconnessione automatica completa e migrazione host. Anche un'aggiunta piccola
richiede una nuova build e le verifiche dei comportamenti che può influenzare.

## Sequenza proposta

1. **Sabato:** verificare sorgenti e test, correggere soltanto problemi
   riproducibili che ostacolano la prova; preparare mappa e pacchetto candidato.
2. **Entro sabato sera, indicativamente le 21:00:** congelare le nuove funzioni
   e distribuire lo stesso ZIP ai sei giocatori. Conservare separatamente
   l'eventuale pacchetto già provato con successo e il backup della mappa.
3. **Domenica 09:15–10:00, se il gruppo è disponibile:** verifica a sette con
   almeno 30 minuti continuativi di conversazione e movimento sulla mappa.
4. **Domenica 10:00:** decisione sulla build da usare. Se un problema critico
   persiste, utilizzare una build precedente già verificata oppure il piano
   di emergenza. Evitare un cambio non verificato subito prima della sessione.
5. **10:30:** inizio della prova. Annotare l'ora precisa di eventuali problemi.

La disponibilità del gruppo prima delle 10:30 non è ancora confermata. Se non
è possibile eseguire SUN-04 prima, la sessione delle 10:30 sarà il primo test a
sette, non una release già validata per sette utenti.

## Checklist sul pacchetto candidato

Usare sette account Discord distinti sulle postazioni effettive. Il DM resta
host per tutta la verifica; cuffie su ogni postazione e nessuna seconda
chiamata Discord aperta in parallelo. La capacità di 8 include eventuali
istanze extra: non aprire un secondo client inutilmente durante la prova.

| Test | Procedura | Esito richiesto | Stato |
| --- | --- | --- | --- |
| T01 — Pacchetto | Tutti estraggono lo stesso ZIP e avviano l'EXE | Nessun file mancante, stessa identificazione del pacchetto | Da eseguire |
| T02 — Ingresso | Il DM crea, sei giocatori entrano | 7 partecipanti, voce e sincronizzazione pronte su tutti | Da eseguire |
| T03 — Movimento | Il DM muove tutte le pedine; i giocatori provano a trascinarle | Movimento fluido e coerente sui client; autorità di modifica solo al DM | Da eseguire |
| T04 — Mappa | Modificare muri, porta e dimensioni; far entrare un giocatore in ritardo | Stato uguale su tutti, incluso il nuovo arrivato | Da eseguire |
| T05 — Voce | Parlare a turno e in sovrapposizione, continuando a muovere pedine per almeno 30 min | Nessun taglio ricorrente, blocco permanente o eco da doppio ascolto | Da eseguire |
| T06 — Portata | Provare 1/2/3, avvicinare e allontanare le pedine | Portate 3/12/24 m coerenti; voce ritorna avvicinandosi | Da eseguire |
| T07 — Ostacoli | Confrontare muro sottile/spesso e porta aperta/chiusa | Attenuazione coerente su ogni ascoltatore | Da eseguire |
| T08 — Gruppi | Attivare e disattivare l'isolamento A/B/C | I gruppi corretti si sentono; dopo la disattivazione la voce ritorna | Da eseguire |
| T09 — UI | Usare menu, slider, zoom e barre sopra aree con pedine/muri | Nessun clic passa alla mappa; niente blocchi durante il parlato | Da eseguire |
| T10 — Salvataggi | Salvare una mappa di prova con nome nuovo, ricaricarla, riaprire l'app | Dimensioni, muri e porte conservati; pedine riposizionabili dal DM | Da eseguire |
| T11 — Rientro | Un giocatore esce e rientra; riattiva la voce | Stessa mappa, nessuna pedina duplicata, voce funzionante | Da eseguire |
| T12 — Recovery | Prima della sessione, interrompere brevemente la rete di un solo guest, poi ripristinarla | Errore comprensibile; rientro manuale verificato o limite annotato | Da eseguire |

Se la mappa resta diversa dopo più snapshot affidabili (circa 5 secondi in
condizioni normali), annotare il problema. È una soglia di verifica proposta,
non una garanzia sul tempo di riconnessione.

## Identificare e conservare la build

- Generare il candidato dal progetto interno della repository con
  **D&D Proximity Voice > Build Windows 1.0**, dopo SUN-01.
- Lo script usa sempre lo stesso percorso di output: copiare prima l'eventuale
  ZIP di fallback in una cartella distinta, senza sovrascriverlo.
- Distribuire l'intero ZIP, mai il solo EXE. Dare alla copia da condividere un
  nome distinguibile, per esempio `DnDVoice-1.0-playtest-20260906-rc1.zip`.
- Annotare commit dei sorgenti, eventuali modifiche locali incluse, data di
  compilazione, nome ZIP e hash SHA-256. L'hash identifica il pacchetto; non è
  una firma dell'autore. Non riusare `rc1` per contenuti differenti.
- Copiare i JSON da `Application.persistentDataPath/SavedMaps` in una cartella
  di backup del DM. Non includere automaticamente salvataggi personali nello ZIP.

Comando facoltativo per il checksum, dalla cartella che contiene lo ZIP:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath '.\DnDVoice-1.0-playtest-20260906-rc1.zip'
```

## Se qualcosa si blocca durante la sessione

1. **Solo voce:** verificare mute, distanza e isolamento gruppi. Fermare e
   riattivare la voce tramite i controlli esistenti. Se non basta, il giocatore
   esce e rientra nella sessione.
2. **Solo mappa:** controllare lo stato Relay. Il giocatore esce e rientra;
   non assumere che sentire la voce significhi avere la mappa sincronizzata.
3. **Caduta del DM:** il DM riapre l'app, crea una nuova sessione, carica la
   mappa salvata e comunica il nuovo codice. Deve riposizionare le pedine:
   il JSON attuale non salva quello stato.
4. **Problema persistente:** chiudere la voce dell'app su tutte le postazioni
   prima di usare una normale chiamata Discord. In questo fallback si rinuncia
   alla prossimità vocale; se serve, il DM condivide la schermata della mappa.

## Registrare gli esiti e aggiornare GitHub

Per ogni problema registrare: ID test, ora con fuso, ruolo DM/guest, numero di
partecipanti, identificazione dello ZIP, passaggi, atteso, risultato ed eventuale
soluzione temporanea. Conservare i log delle postazioni coinvolte prima di
riaprire ripetutamente l'app. Non pubblicare log integrali senza controllarli:
possono includere codici sessione, identificativi e percorsi personali.

Il percorso usato dal codice per le mappe è `Application.persistentDataPath`;
nelle build Windows il log standard è `Player.log` nella cartella persistente
del prodotto. Nell'esecuzione locale Unity ha risolto la cartella come
`%USERPROFILE%/AppData/LocalLow/DnD Proximity Voice/D_D Proximity Voice`;
`SavedMaps` si trova al suo interno.

| Verifica | Esito al 5 settembre |
| --- | --- |
| Repository e sorgenti della base individuati | Completato, `86e05a8` |
| Limite effettivo per il gruppo di 7 | Verificato nel codice: massimo 8; carico reale da provare |
| Test EditMode della base | **42/42 superati**, 0 falliti, 0 saltati; 5 settembre, 16:49:59 Europe/Rome |
| Nuova build Windows del candidato | Da generare e provare |
| Prova reale a sette e continuità vocale | Da eseguire |
| Commit e push di questa preparazione | A cura dell'utente |

La verifica è stata eseguita con Unity `6000.3.8f1` in modalità batch EditMode;
Unity ha terminato con codice `0`, senza errori di compilazione. Il report
locale è `DnDVoice/Logs/preflight-2026-09-05/editmode-results.xml` e il log è
`unity-tests.log` nella stessa cartella, entrambi esclusi da Git.

| Suite | Test superati |
| --- | ---: |
| SessionCodeTests | 3 |
| VoiceModeProfileTests | 7 |
| VoiceRangeCalculatorTests | 10 |
| WallAcousticsTests (inclusi snap, porte, stanze e salvataggi) | 13 |
| Pcm16ConverterTests | 2 |
| PcmRingBufferTests | 5 |
| RemotePcmDiagnosticTests | 1 |
| RemotePcmStreamTests | 1 |

I nove test delle quattro suite PCM verificano componenti locali, anche del
percorso sperimentale precedente: non misurano qualità o latenza della chiamata
Discord Direct. Il log contiene una segnalazione non bloccante del modulo di
licenza Unity sul rinnovo del proprio access token; non è un errore della voce
Discord e non ha impedito il completamento dei test.

Aggiornare questo documento e quello inglese quando cambia un esito; aggiornare
README e documentazione funzionale se cambia un comportamento. Ogni fix resta
piccolo e accompagnato dalla verifica pertinente. I test di logica locale non
sostituiscono T02–T12 sui client reali. Log, cache Unity e ZIP rimangono esclusi
dal commit; lo stato online si aggiorna dopo il commit/push dell'utente e
l'eventuale aggiornamento delle schede Kanban.
