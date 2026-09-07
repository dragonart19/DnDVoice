# Build 1.0.1 Hotfix — occlusione e stabilità audio

[← README](../README.md) · [English](HOTFIX_1_0_1_EN.md) ·
[Esito playtest](PLAYTEST_2026_09_06_IT.md) ·
[Kanban](https://github.com/users/dragonart19/projects/1/views/1)

## Origine

Il 6 settembre 2026 il candidato Build 1.0 è stato controllato poco prima di
una sessione con sette partecipanti. Due problemi ne hanno impedito l'uso per
la partita completa:

1. un muro impostato allo spessore massimo lasciava la voce troppo udibile;
2. dopo un intervallo non annotato si sono verificate disconnessioni della
   voce e della sessione, incluso il DM secondo il resoconto.

Il gruppo ha completato la partita usando una build precedente e il giudizio
generale sul prodotto è stato molto positivo. Il log locale del candidato
contiene `JoinTimeout`, disconnessione Relay e lobby Discord non disponibile,
ma non contiene timestamp assoluti per ogni riga e non dimostra una singola
causa per tutti gli eventi.

## Modifiche della 1.0.1

- un muro da `0,2 m` applica un'occlusione minima più evidente ma lascia la
  conversazione distinguibile, con guadagno finale di circa `0,66` prima della
  moltiplicazione per la distanza;
- lo spessore massimo da `2 m` porta il guadagno del muro a circa `0,02`, quindi
  la voce deve risultare quasi impercettibile;
- gli ostacoli multipli continuano a sommare l'occlusione;
- durante gli stati temporanei `Connecting`/`Reconnecting` del client Discord,
  una sessione già entrata conserva lobby e Relay invece di azzerarli subito;
- gli errori transitori della chiamata, incluso `JoinTimeout`, avviano fino a
  tre tentativi automatici dopo `2`, `4` e `6` secondi;
- gli errori `Forbidden` restano terminali e richiedono intervento dell'utente;
- i log riportano lo stato Discord e il tempo trascorso dall'avvio per rendere
  confrontabili le prossime segnalazioni;
- build, cartella e interfaccia sono identificate come `1.0.1 Hotfix`.

## Cosa non risolve

- nessuna migrazione automatica del DM verso un altro partecipante;
- nessuna garanzia di recupero se la lobby Discord viene realmente eliminata;
- nessuna ricreazione automatica dell'allocazione Relay del DM;
- nessun filtro passa-basso nel percorso Discord Direct;
- nessuna prova automatica può certificare qualità, latenza o continuità della
  voce su Internet.

## Verifica richiesta all'utente

Il 7 settembre 2026 l'utente ha confermato il completamento dell'intera
checklist A–D: profilo dei muri a due client, ripristino dopo interruzione breve
e prova di almeno 30 minuti. Alle 13:16 UTC la suite è stata rieseguita in
batch con Unity `6000.3.8f1`: **53/53 test EditMode superati**, zero falliti o
saltati e nessun errore di compilazione. Il report locale è
`DnDVoice/Logs/hotfix-1.0.1-editmode-results.xml` ed è escluso da Git. Gli orari
della prova manuale e l'hash del pacchetto 1.0.1 non sono registrati e non
vengono dedotti retroattivamente.

Le sezioni seguenti restano come procedura ripetibile per le prossime build.

### A. Compilazione e suite locale

1. Apri la sottocartella Unity `DnDVoice` con Unity `6000.3.8f1`.
2. Attendi che la Console non mostri errori rossi.
3. Apri **Window > General > Test Runner > EditMode**.
4. Esegui tutti i test: il totale atteso è **53**, tutti verdi.

### B. Profilo dei muri con due client

Usa cuffie, due PC/account, modalità **Normale**, gruppi privati disattivati e
pedine alla stessa distanza per ogni confronto.

1. Senza muro: la voce è il riferimento pieno.
2. Muro `0,2 m`: voce ridotta ma ancora comprensibile.
3. Muro circa `1 m`: voce molto attenuata.
4. Muro `2 m`: voce quasi impercettibile, senza sparire per errori di routing.
5. Porta aperta: la voce torna come se l'ostacolo non esistesse.
6. Porta chiusa: attenuazione evidente ma meno estrema del muro massimo.
7. Muovi una pedina avanti e indietro sullo stesso muro e verifica che il
   volume cambi dal lato corretto su entrambi i client.

### C. Ripristino della chiamata

1. Avvia la voce su entrambi i client.
2. Sul guest interrompi la rete per 5–10 secondi e ripristinala.
3. La UI può mostrare la riconnessione; non deve tornare immediatamente alla
   schermata di creazione/ingresso.
4. Verifica il ritorno della voce. Se i tre tentativi falliscono, deve apparire
   un errore e deve restare disponibile **Riprova voce**.
5. Ripeti una volta sul DM soltanto dopo aver salvato la mappa. La conservazione
   della sessione copre transizioni brevi, non la perdita definitiva dell'host.

### D. Durata

Mantieni due client collegati per almeno **30 minuti**, parlando e muovendo
periodicamente le pedine. Annota ora di avvio, ora dell'eventuale problema,
ruolo della macchina e testo visibile nella UI.

### E. Pacchetto

Dal menu Unity usa:

```text
D&D Proximity Voice > Build Windows 1.0.1 Hotfix
```

Distribuisci lo ZIP completo generato come
`Builds/DnDProximityVoice-Windows-BUILD-1.0.1-HOTFIX.zip`. Dopo i test registra
commit, dimensione e SHA-256; non sovrascrivere il pacchetto di fallback.

## Git

Il lavoro resta su `hotfix/1.0-playtest-audio-stability`, derivato da `main`.
Il commit `9ba9a1c` è stato pubblicato dall'utente sul branch remoto e la
validazione A–D è stata confermata. Il merge in `main` è stato completato con
il commit `77a9bf4`; la stessa base è stata poi integrata in `develop/v2`.
La suite combinata V2 + hotfix ha superato **57/57 test EditMode** il 7
settembre alle 13:43 UTC. Il report locale, escluso da Git, è
`DnDVoice/Logs/v2-integration-editmode-results.xml`.
