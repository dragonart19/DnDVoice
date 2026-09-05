# D&D Proximity Voice — roadmap prodotto 2.0

[← README principale](../README.md) · [English version](ROADMAP_2_0_EN.md) ·
[Kanban GitHub](https://github.com/users/dragonart19/projects/1/views/1)

## Priorità immediata — prova del 6 settembre

Prima delle nuove funzioni V2 si verifica la base 2D per **7 partecipanti,
domenica 6 settembre 2026 alle 10:30 Europe/Rome**. Le attività SUN-01–SUN-08,
i criteri di accettazione e gli esiti sono nel [piano della prova](PLAYTEST_2026_09_06_IT.md).
Le priorità P0 della V2 qui sotto sono relative alla fase successiva: non sono
impegni da completare prima di domenica. L'utente esegue commit e push.

## Obiettivo

La versione 2.0 evolve D&D Proximity Voice da companion vocale 2D a tavolo
virtuale ibrido 2D/3D, progettato soprattutto per il Dungeon Master. Il punto
distintivo non è soltanto costruire una scena: il DM può **diventare ogni voce**
della scena, scegliendo da quale personaggio o punto dello spazio parlare.

> **Promessa di prodotto:** Costruisci la scena. Diventa ogni voce.

La Build 1.0 rimane la base stabile e giocabile. Le nuove funzioni vengono
sviluppate in moduli separati e integrate soltanto dopo test a due o più client.

## Decisioni già definite

- Il prodotto offre due modalità distinte: tavolo **2D** e mondo **3D**.
- Una mappa importata da carta, immagine o disegno rimane 2D; non viene
  convertita automaticamente in 3D.
- Esistono un builder 2D e un world builder 3D separati, nello stesso prodotto.
- Solo il DM sposta pedine, personaggi e NPC.
- Gli NPC non sono intelligenze artificiali: sono identità che il DM può
  impersonare e usare come origine della propria voce.
- Il DM usa un'edizione completa; i giocatori usano un client gratuito per
  entrare, vedere la scena e parlare.
- La prima distribuzione avviene fuori da Steam e senza costi anticipati.
- La community di asset arriva per fasi: prima catalogo ufficiale, poi invii
  moderati, infine eventualmente un hub pubblico.

## Esperienza prevista

### Tavolo 2D

- importazione di mappe disegnate su carta o create digitalmente;
- builder interno per griglia, muri, porte, stanze e pedine;
- voce di prossimità legata a distanza, ostacoli e piani;
- salvataggio di campagne composte da più scene.

### World builder 3D

- scene finite e caricabili, non un unico open world infinito;
- costruzione modulare di pavimenti, muri, soffitti, porte e luci;
- posizionamento di oggetti da un catalogo controllato;
- importazione locale sicura di asset GLB/glTF;
- proprietà funzionali: porte apribili, materiali acustici, luci, suoni e
  collegamenti tra piani;
- navigazione libera per i giocatori, con autorità di modifica riservata al DM.

### Regia vocale del DM

Il punto da cui il DM parla e il punto da cui ascolta sono due concetti
separati. Il DM può scegliere una modalità chiara:

- **Narratore:** voce globale, non legata a una pedina;
- **DM ambientale:** voce proveniente dalla pedina o posizione del DM;
- **Impersona NPC:** la voce nasce dalla posizione dell'NPC selezionato;
- **Sussurro privato:** comunicazione diretta a uno o più giocatori;
- **Voce magica o divina:** voce con portata ed effetto speciali;
- **Voce fuori scena:** origine narrativa non visibile sulla mappa.

Durante l'impersonificazione l'interfaccia deve mostrare sempre un avviso
inequivocabile, per esempio **“Stai parlando come: Oste”**. Il DM può ascoltare
in modo onnisciente oppure dal punto dell'NPC impersonato.

### NPC

Ogni NPC può contenere:

- nome, ritratto o modello 3D e colore;
- posizione, piano e visibilità;
- note private del DM e fazione;
- portata vocale e profilo acustico;
- effetto opzionale della voce;
- suoni associati;
- stato nascosto o visibile ai giocatori.

## Architettura di prodotto

```text
Campagna
├── Scene 2D
│   ├── immagine o disegno
│   ├── muri, porte e zone
│   └── pedine e sorgenti audio
└── Scene 3D
    ├── struttura modulare
    ├── asset e luci
    ├── NPC e sorgenti audio
    └── materiali acustici e collegamenti tra piani
```

Il Relay sincronizza solo lo stato leggero della scena: trasformazioni, porte,
pedine, modalità vocali e azioni. I file 3D non devono attraversare il Relay.
Ogni asset ha un identificatore, una versione e un checksum; i client lo
scaricano o lo leggono dalla cache prima di entrare. Se manca, viene mostrato
un segnaposto sicuro invece di interrompere la sessione.

## Asset e sicurezza

### Prima fase

- catalogo ufficiale di 20–30 asset CC0 selezionati;
- import locale GLB/glTF;
- niente script, DLL, shader personalizzati o codice eseguibile negli asset;
- limiti a dimensioni, poligoni, texture e materiali;
- validazione automatica prima dell'uso;
- manifest con autore, licenza, versione, anteprima, collider e proprietà
  acustiche.

### Community futura

1. pacchetti locali e catalogo ufficiale;
2. invio manuale con revisione e licenza verificata;
3. catalogo online con download, cache e segnalazioni;
4. hub pubblico soltanto dopo aver validato domanda, moderazione e costi.

Un marketplace a pagamento non appartiene all'MVP: pagamenti, tasse,
moderazione, rimborsi e diritti degli autori lo rendono un prodotto separato.

## Modello commerciale a costo iniziale zero

- **Player Client:** gratuito;
- **DM Edition:** acquisto una tantum;
- nessun abbonamento nella prima versione;
- pacchetto base ufficiale gratuito;
- eventuali pacchetti premium solo dopo la validazione del prodotto;
- prima distribuzione tramite itch.io o download GitHub, senza Steam;
- Unity Personal, Blender, GitHub e asset CC0 per evitare costi anticipati;
- niente dominio, installer firmato o infrastruttura a pagamento finché non
  esistono utenti o ricavi che li giustificano.

“Costo zero” significa nessuna spesa obbligatoria prima di validare il prodotto.
Firma del codice, hosting più ampio, consulenza legale e servizi commerciali
potranno diventare costi reali solo quando il progetto cresce.

## Vertical slice da validare

La prima demo 3D deve essere piccola ma completa:

- una taverna 3D costruita con moduli;
- 20–30 asset gratuiti e verificati;
- una porta funzionante, luci e un camino con suono posizionale;
- due client connessi;
- pedine mosse soltanto dal DM;
- NPC oste impersonabile;
- modalità Narratore;
- voce 3D attenuata da distanza, muri, porte e piani;
- salvataggio e caricamento locale della scena.

Se questa esperienza non è divertente, stabile e comprensibile, non si passa
alla piattaforma community.

## Fasi e criteri di uscita

| Fase | Risultato | Criterio di uscita |
| --- | --- | --- |
| Fondamenta | confini V2, dati e pipeline definiti | Build 1.0 ancora stabile e formati documentati |
| Vertical slice 3D | taverna completa giocabile | sessione a due client senza blocchi critici |
| Alpha chiusa | campagne, import e recovery | tester esterni completano una sessione senza assistenza |
| MVP commerciale | DM Edition + Player Client | build distribuibile, licenze e privacy verificate |
| Community | catalogo moderato | workflow di invio, revisione e rimozione sostenibile |

## Backlog ordinato

### P0 — indispensabile

- definire confini tecnici tra Build 1.0, modalità 2D e modalità 3D;
- progettare dati versionati per campagne e scene 3D;
- realizzare camera, selezione e trasformazione oggetti 3D;
- creare il builder modulare di stanze 3D;
- preparare il catalogo CC0 iniziale;
- creare dati, editor e posizionamento degli NPC;
- implementare impersonificazione NPC e identità vocale sempre visibile;
- estendere la voce spaziale a X/Y/Z, muri, porte e piani;
- sincronizzare scena, porte, pedine e NPC tramite Relay;
- separare DM Edition e Player Client gratuito;
- completare e testare la vertical slice della taverna.

### P1 — necessario per l'alpha

- modalità Narratore, privata, magica e fuori scena;
- sorgenti sonore ambientali posizionali;
- campagne con più scene, autosave e recovery;
- importatore locale GLB/glTF con validazione;
- manifest, versione, checksum, cache e segnaposto degli asset;
- controllo dipendenze prima dell'ingresso in sessione;
- ricostruzione UI, accessibilità e impostazioni di qualità;
- budget prestazionali, LOD, culling e test di carico;
- riconnessione e recupero di host/sessione;
- piano QA automatico e manuale multi-client;
- pipeline di distribuzione senza costi anticipati;
- verifica di privacy, Discord, licenze degli asset e termini commerciali.

### P2 — dopo la validazione

- strategia di firma del codice;
- specifica pubblica dei pacchetti asset;
- invio e revisione manuale di asset community;
- catalogo online con download e cache;
- sistema di segnalazione, moderazione e rimozione.

### P3 — solo con trazione reale

- hub pubblico di contenuti generati dagli utenti;
- monetizzazione dei pacchetti community;
- backend scalabile e servizi commerciali dedicati;
- marketplace con pagamenti e ripartizione dei ricavi.

## Regole di sviluppo

- Ogni attività vive in GitHub e ha priorità, area, milestone e criterio di
  accettazione.
- `main` rimane la linea stabile della Build 1.x; lo sviluppo della V2 viene
  integrato nel branch `develop/v2`, creato da `main` al commit `6d2304a`.
- Ogni scheda scelta dal Kanban viene prima convertita da bozza a issue della
  repository, poi sviluppata in un branch `feature/<numero>-<nome-breve>` creato
  da `develop/v2`. Una sola issue per branch; modifiche piccole e verificabili.
- L'utente esegue i test e comunica l'esito. Per ogni consegna Codex fornisce
  una checklist e i comandi necessari, ma non avvia suite o build salvo richiesta
  esplicita dell'utente.
- Un'issue viene chiusa soltanto dopo: criteri di accettazione soddisfatti,
  documentazione aggiornata, test confermati dall'utente e modifica integrata
  in `develop/v2`. Spostare una scheda in `Done` non sostituisce la chiusura
  dell'issue collegata.
- Le funzioni di rete o voce richiedono sempre un test con almeno due client.
- Nessun asset entra nel progetto senza origine e licenza registrate.
- Le nuove funzioni non devono rompere la Build 1.0.
- Prima viene validata l'esperienza del DM, poi viene ampliata la piattaforma.

## Cose intenzionalmente escluse dall'MVP

- conversione automatica di una foto 2D in mondo 3D;
- NPC controllati da IA;
- open world infinito;
- marketplace pubblico a pagamento;
- asset con script o codice eseguibile;
- abbonamento obbligatorio;
- distribuzione iniziale su Steam.
