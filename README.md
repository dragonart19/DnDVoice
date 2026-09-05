# D&D Proximity Voice

Companion desktop per sessioni di Dungeons & Dragons online: Discord gestisce
accesso e trasporto vocale, mentre una mappa tattica condivisa determina chi
può sentire chi in base a distanza, modalità di voce, muri, porte e gruppi
privati.

> **Stato:** Build 1.0 · Discord Direct — prototipo Windows funzionante, ancora
> in sviluppo e da validare con più client dopo ogni modifica alla rete o alla
> voce.

[Documentazione completa in italiano](docs/README_IT.md) ·
[Full documentation in English](docs/README_EN.md) ·
[Kanban GitHub V2](https://github.com/users/dragonart19/projects/1/views/1) ·
[Roadmap prodotto 2.0](docs/ROADMAP_2_0_IT.md) ·
[Product Roadmap 2.0](docs/ROADMAP_2_0_EN.md)

## Prossima prova: 6 settembre, ore 10:30

La priorità immediata è verificare la Build 1.0 **con 7 partecipanti totali,
DM compreso**, prima di estendere il progetto. Il limite attuale di Relay è 8;
il test reale a sette resta da eseguire. Il piano include controlli della voce,
sincronizzazione, salvataggi, rientro e un pacchetto Windows identificabile.

[Piano e checklist in italiano](docs/PLAYTEST_2026_09_06_IT.md) ·
[Playtest plan and checklist in English](docs/PLAYTEST_2026_09_06_EN.md).
La roadmap V2 resta valida; le nuove funzioni 3D seguono questa verifica.
Modifiche ed esiti vengono documentati nella repository; **commit e push
rimangono a cura dell'utente**.

## Direzione 2.0

La Build 1.0 resta la base stabile. La prossima fase trasforma il progetto in
un tavolo virtuale ibrido **2D/3D** in cui il DM costruisce scene, controlla
pedine e NPC e può parlare dal punto di vista di qualunque personaggio.

Il piano commerciale parte senza costi anticipati: **Player Client gratuito**,
**DM Edition con acquisto una tantum**, strumenti e asset CC0, distribuzione
iniziale fuori da Steam. La prima prova sarà una taverna 3D completa con builder
modulare, NPC impersonabile, suono ambientale e voce spaziale a due client.

La lista ordinata, i criteri di uscita e le funzioni intenzionalmente escluse
dall'MVP sono nella [roadmap italiana](docs/ROADMAP_2_0_IT.md) e nella
[roadmap inglese](docs/ROADMAP_2_0_EN.md). L'avanzamento operativo è gestito
nel [Kanban GitHub ufficiale](https://github.com/users/dragonart19/projects/1/views/1).

## Funzioni disponibili

| Area | Stato | Funzione |
| --- | :---: | --- |
| Accesso | ✅ | OAuth2 Discord con PKCE, senza client secret nel progetto |
| Sessioni | ✅ | Creazione e ingresso tramite codice di 6 caratteri |
| Voce | ✅ | Chiamata Discord con attenuazione per distanza e modalità Sussurro/Normale/Urlo |
| Audio spaziale | 🟡 | Volume posizionale attivo; la direzione stereo dipende dai canali PCM forniti dal SDK |
| Mappa | ✅ | Mappa condivisa, pedine sincronizzate, trascinamento DM e raggio vocale |
| Navigazione | ✅ | Mappa ridimensionabile, barre di scorrimento e zoom con `Ctrl + rotellina` |
| Costruzione | ✅ | Muri a spessore variabile, aggancio alla griglia, porte e stanze chiuse |
| Acustica | 🟡 | Muri e porte attenuano il volume; filtro passa-basso e riverbero sono pianificati |
| Gruppi | ✅ | Gruppi vocali privati A/B/C come regola di mix dell'app |
| Salvataggi | ✅ | Salvataggio, caricamento ed eliminazione locale delle mappe |
| Interfaccia | ✅ | Tema fantasy, menu laterale e pannello giocatori richiudibili |
| Utilità | ✅ | Copia codice sessione, apertura cartella log e accesso DM ai JSON delle mappe dal menu |

Legenda: ✅ disponibile · 🟡 parziale/da rifinire · ⬜ pianificato.

## Avvio rapido

Requisiti principali:

- Windows 10/11 x64;
- Unity `6000.3.8f1` per lavorare al progetto;
- Discord desktop installato e avviato;
- un'applicazione Discord con Social SDK e redirect OAuth
  `http://127.0.0.1/callback`.

Per aprire il progetto:

1. clona o scarica questa repository;
2. in Unity Hub scegli **Add project from disk**;
3. seleziona la cartella `DnDVoice` contenuta nella repository;
4. aprila con Unity `6000.3.8f1` e attendi il ripristino dei pacchetti;
5. avvia la scena principale e autorizza Discord.

Per provare una sessione reale servono due istanze su due account Discord:
il DM crea la sessione e condivide il codice, l'altro giocatore sceglie di
entrare e inserisce lo stesso codice.

## Comandi essenziali

- `1`, `2`, `3`: Sussurro, Normale, Urlo.
- Trascinamento pedina: spostamento sulla mappa; il DM controlla le pedine.
- `Ctrl + rotellina`: zoom centrato sul puntatore.
- Rotellina: scorrimento verticale; `Shift + rotellina`: orizzontale.
- `Esc`: annulla il muro o la porta in costruzione.
- `Canc`/`Backspace`: elimina l'elemento di costruzione selezionato.
- Menu burger → **COPIA**: copia il codice sessione senza spazi.
- Menu burger → **UTILITÀ**: apre i log locali; il DM può aprire anche le mappe salvate.

Con il menu aperto, trascinamento, barre di scorrimento e rotellina della mappa
sono sospesi per evitare interazioni con gli elementi sottostanti.

## Limiti importanti della Build 1.0

- Il target verificato è Windows; l'eseguibile non è firmato e SmartScreen può
  mostrare un avviso.
- Il Relay è configurato per un massimo pratico di **8 partecipanti totali**
  (host + 7 connessioni), anche se i modelli interni sono pensati per crescere
  fino a 20.
- La modalità Discord Direct usa il jitter buffer nativo per privilegiare una
  voce continua. Non promette latenza di 1 ms e non applica ancora un filtro
  passa-basso attraverso i muri.
- I salvataggi sono locali al computer del DM; non esiste ancora una libreria
  cloud delle campagne.
- I gruppi privati sono una regola audio dell'app, non una separazione
  crittografica in chiamate Discord differenti.

## Roadmap sintetica

- diagnostica di riconnessione e test multi-client ripetibili;
- selezione microfono/uscita, test livello e controlli volume;
- indicatore chi-sente-chi più leggibile;
- telepatia e comunicazioni magiche;
- sorgenti ambientali posizionali, riverbero e acustica avanzata;
- selezione multipla, movimento di gruppo e teletrasporto DM;
- campagne, preset di ambientazione e salvataggi cloud/esportabili;
- accessibilità, ridimensionamento UI, localizzazione e build multipiattaforma.

La roadmap dettagliata, l'architettura, il comportamento acustico, il formato
dei salvataggi, la procedura di build e la risoluzione dei problemi sono nella
[documentazione italiana](docs/README_IT.md). La stessa documentazione è
disponibile [in inglese](docs/README_EN.md).

## Struttura

```text
DnDVoice/
├── Assets/_Project/Runtime/    codice dell'app
├── Assets/_Project/Tests/      test Edit Mode
├── Assets/_Project/Editor/     comando per la build Windows
├── Packages/                   dipendenze Unity e Discord Social SDK
└── ProjectSettings/            configurazione Unity
docs/
├── README_IT.md                documentazione italiana
├── README_EN.md                English documentation
├── ROADMAP_2_0_IT.md            visione, fasi e backlog V2
├── ROADMAP_2_0_EN.md            V2 vision, phases, and backlog
├── PLAYTEST_2026_09_06_IT.md    piano, checklist ed esiti della prova a 7
└── PLAYTEST_2026_09_06_EN.md    seven-person playtest plan and results
```

## Sicurezza e licenza

Non inserire mai client secret, token OAuth o credenziali nella repository.
L'applicazione usa PKCE e non salva i token tra un avvio e il successivo.

Al momento non è presente un file `LICENSE`: prima di distribuire o accettare
contributi pubblici va scelta e aggiunta una licenza esplicita.
