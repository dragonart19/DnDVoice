# D&D Proximity Voice

Companion desktop per sessioni di Dungeons & Dragons online: Discord gestisce
accesso e trasporto vocale, mentre una mappa tattica condivisa determina chi
può sentire chi in base a distanza, modalità di voce, muri, porte e gruppi
privati.

> **Stato:** Build 1.0 · Discord Direct — prototipo Windows funzionante, ancora
> in sviluppo e da validare con più client dopo ogni modifica alla rete o alla
> voce.

[Documentazione completa in italiano](docs/README_IT.md) ·
[Full documentation in English](docs/README_EN.md)

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
└── README_EN.md                English documentation
```

## Sicurezza e licenza

Non inserire mai client secret, token OAuth o credenziali nella repository.
L'applicazione usa PKCE e non salva i token tra un avvio e il successivo.

Al momento non è presente un file `LICENSE`: prima di distribuire o accettare
contributi pubblici va scelta e aggiunta una licenza esplicita.
