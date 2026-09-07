# Design system UI V2 — Italiano

Questo documento descrive il linguaggio visivo e le regole di interazione della
V2 di D&D Proximity Voice. La sua implementazione corrente usa **Unity IMGUI**
perché tutta l'interfaccia del progetto è già generata a runtime da C#. La
scelta evita una migrazione rischiosa e mantiene intatti scene, networking e
logica della voce. I componenti sono centralizzati in `AppUiTheme` e possono
essere trasferiti in futuro a UI Toolkit senza cambiare il modello acustico.

## Principi

1. **La mappa è il contenuto principale.** I controlli avanzati restano in
   pannelli richiudibili e bloccano gli input diretti alla mappa sottostante.
2. **Fantasy sobrio.** Pergamena, ottone e superfici scure evocano il tavolo da
   gioco senza imitare pietra, legno o manoscritti in ogni elemento.
3. **Lo stato non dipende solo dal colore.** Simboli, etichette e barre
   segmentate distinguono parlato, mute, connessione e udibilità.
4. **Una sola azione primaria per area.** L'oro brillante è riservato a scelte
   o conferme importanti; i comandi secondari restano neutri.
5. **Feedback immediato.** Selezione, hover, focus, errore e operazioni audio
   mostrano uno stato riconoscibile senza finestre modali invasive.

## Token cromatici

| Token | Valore | Uso |
| --- | --- | --- |
| `Background` | `#07090A` | sfondo e vignettatura |
| `Surface` | `#121312` | pannelli principali |
| `SurfaceRaised` | `#1D1C18` | drawer e pannelli in primo piano |
| `SurfaceSoft` | `#26231D` | righe, gruppi di controlli, campi |
| `Stroke` | `#8B6A39` | bordi e separatori |
| `Text` | `#F7EFDA` | testo primario |
| `Muted` | `#C1B294` | descrizioni e metadati |
| `Accent` | `#AA742B` | azioni primarie |
| `AccentBright` | `#E8BE63` | focus, dettagli e selezione |
| `Success` | `#53BE89` | connesso, parlato, voce nitida |
| `Warning` | `#E6A443` | riconnessione e attenuazione |
| `Danger` | `#D25248` | errore, mute/deafen, fuori portata |
| `Info` | `#67A4CD` | informazioni neutrali |

Il contrasto viene ottenuto soprattutto con luminosità, testo e forma. Verde,
ambra e rosso non devono essere l'unico modo per comprendere uno stato.

## Tipografia e gerarchia

L'applicazione usa il font dinamico disponibile nel sistema Unity, così la
build non dipende da asset esterni. La scala è:

- display: 30 px, titoli delle schermate;
- titolo: 22 px, titoli di pannello;
- titolo compatto: 18 px, intestazione HUD;
- heading: 15 px, stato o sezione;
- corpo: 14 px, testo operativo;
- caption: 12 px, dettaglio secondario;
- eyebrow: 11 px in maiuscolo, etichette di sezione.

Le stringhe lunghe usano word wrap. Nomi di giocatori e dispositivi usano il
clipping in una sola riga per non deformare i pannelli.

## Spaziatura e dimensioni

La base è una griglia da 4 px: `4 / 8 / 12 / 20 / 32`. I controlli principali
sono alti 44 px e il target minimo interattivo è 40 px. Le card hanno raggio
visivo di circa 8–9 px, bordo sottile, linea superiore e angoli ornamentali.
Le ombre sono riservate ai pannelli che si sovrappongono alla mappa.

## Componenti condivisi

- `DrawCard`: superficie principale o rialzata con cornice sobria;
- `DrawStatusBadge`: simbolo, titolo e dettaglio per connessione e audio;
- `DrawPill`: ruolo, stato breve o scorciatoia;
- `DrawSegmentedMeter`: intensità leggibile anche senza colore;
- `DrawKeyHint`: tasto e azione associata;
- `DrawTooltip`: aiuto contestuale vicino al puntatore;
- `AppUiControls.IconButton`: pulsante coerente con icone Heroicons locali;
- `AppUiControls.BeginScrollView`: area scorrevole che limita correttamente hover e tooltip;
- `AppUiPointer`: puntatore interattivo condiviso, ripristinato quando l'app perde il focus;
- stili `PrimaryButton`, `SecondaryButton`, `DangerButton`, `IconButton`;
- stili di testo per display, titolo, corpo, caption, codice e pedine.

Gli stati hover, pressed, selected, focused e disabled usano differenze di
superficie e testo. Il focus usa lo stesso contrasto forte dell'hover.

## Stati della voce

Il pannello laterale comunica esplicitamente:

- voce connessa, in avvio, in riconnessione, arrestata o in errore;
- microfono attivo/disattivato;
- microfono disattivato dal DM, distinto dal mute scelto dall'utente;
- cuffie attive/disattivate;
- push-to-talk inattivo, in attesa o in trasmissione;
- numero di partecipanti Discord;
- messaggi di conferma o errore dei dispositivi audio.

Il giocatore che parla sale temporaneamente in cima alla lista e riceve barre
animate, testo **IN PAROLA** e alone sulla pedina. La selezione resta indicata
separatamente dall'accento dorato.

### Udibilità

La barra segmentata riassume lo stesso guadagno usato dalla voce: distanza,
modalità Sussurro/Normale/Urlo, occlusione dei muri e gruppo privato. Le
etichette sono:

- `NITIDA`: intensità almeno 56%;
- `ATTENUATA`: dal 18% al 56%;
- `DEBOLE`: sotto il 18% ma ancora udibile;
- `FUORI PORTATA`: guadagno nullo per distanza;
- `PRIVATA`: gruppo vocale non compatibile.

Queste etichette sono una presentazione del calcolo esistente e non modificano
il mix o la rete.

## Impostazioni audio

Il drawer **Impostazioni audio** consente di:

- scorrere i microfoni e le uscite rilevate dal Discord Social SDK;
- regolare volume microfono `0–100%`;
- regolare volume ascolto `0–200%`;
- usare rilevamento voce automatico o soglia manuale `-100–0 dB`;
- attivare push-to-talk con pressione prolungata di `V`;
- disattivare completamente l'audio ricevuto.

Il drawer è disponibile solo con la chiamata connessa. Le impostazioni sono
applicate dal manager Discord e la UI non manipola pacchetti PCM, code audio,
attenuazione o networking.

## Toolbar e menu contestuali del DM

Gli strumenti frequenti della mappa usano icone monocromatiche con tooltip,
stato selezionato e testo di supporto. Le azioni sull'elemento selezionato sono
mostrate vicino alla pedina o al muro, evitando pannelli permanenti. Conferme di
eliminazione, espulsione e uscita bloccano l'interfaccia sottostante. Il popup
consuma mouse e rotellina, perciò nessuna azione attraversa visivamente il menu.

## Input e accessibilità

- mouse: tutti i controlli e le pedine;
- tastiera: `1/2/3` per il modo voce, `V` per push-to-talk, `Invio` sul codice
  sessione, `Esc` per chiudere impostazioni o annullare la costruzione,
  `Canc/Backspace` per l'elemento selezionato;
- zoom: `Ctrl + rotellina`, centrato sul puntatore;
- controller: l'IMGUI corrente espone il focus dei controlli ma non include
  ancora un sistema di navigazione direzionale e rimappatura completo.

Le scorciatoie vocali non vengono intercettate mentre si scrive il nome di una
mappa. Quando un drawer è aperto, trascinamento, costruzione, zoom e scrollbar
della mappa sono sospesi.

## Risoluzione, safe area e prestazioni

`BeginResponsive` calcola la scala dalla safe area effettiva. Il limite massimo
è `2×`, quello minimo `0,72×`. I layout tattici sono verificati automaticamente
per `1366×768`, `1920×1080` e `2560×1440`; a tutte e tre le risoluzioni lo
spazio logico resta almeno `1280×720` circa.

Texture, gradienti, cornici e stili sono generati una sola volta e riutilizzati.
Il pannello giocatori riusa una lista e un ordinamento per inserimento, evitando
query LINQ e nuove collezioni in `OnGUI`. Il calcolo visivo dell'udibilità usa
una curva statica condivisa.

## Verifica e sostituzioni future

La suite EditMode controlla calcolo dell'udibilità, ordinamento, scala e script
mancanti nelle scene. Una verifica reale delle schermate connesse richiede una
sessione Discord, quindi prima di una release controllare manualmente:

1. login e selezione modalità;
2. creazione e ingresso sessione;
3. drawer giocatori con due o più account;
4. mute, deafen, push-to-talk e cambio dispositivi;
5. leggibilità alle tre risoluzioni supportate;
6. nessun clic passante verso la mappa.

Non sono stati aggiunti font, icone o immagini esterni. I simboli testuali e le
forme procedurali sono placeholder production-safe; per una futura release
commerciale si consiglia un font open source incorporato e un set di icone
vettoriali con licenza documentata.
