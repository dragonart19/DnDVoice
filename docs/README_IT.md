# D&D Proximity Voice — documentazione italiana

[← README principale](../README.md) · [English documentation](README_EN.md) ·
[Roadmap prodotto 2.0](ROADMAP_2_0_IT.md) ·
[Kanban GitHub](https://github.com/users/dragonart19/projects/1/views/1)

## 1. Visione del progetto

D&D Proximity Voice è un companion desktop per sessioni di Dungeons & Dragons
online. Lo scopo è far percepire la conversazione come se i personaggi fossero
veramente nello stesso luogo: la voce cambia con la distanza, la posizione e
gli ostacoli disegnati dal Dungeon Master.

Discord fornisce identità, lobby e trasporto vocale. Unity visualizza il tavolo
tattico, sincronizza lo stato e calcola un mix diverso per ogni ascoltatore.
L'app non sostituisce un VTT completo: si concentra su posizione e voce e può
essere usata accanto agli strumenti già scelti dal gruppo.

Principi di progetto:

- ingresso semplice, idealmente in meno di un minuto;
- il DM controlla la scena, i giocatori devono solo entrare e parlare;
- regole acustiche comprensibili e leggibili sulla mappa;
- interfaccia fantasy moderna, senza pannelli tecnici durante il gioco;
- nessuna credenziale privata nel client o nella repository;
- stabilità della voce prima della ricerca della latenza minima assoluta.

## 2. Stato della Build 1.0

La Build 1.0 è un prototipo giocabile per Windows. Il flusso completo esiste:
accesso Discord, sessione con codice, mappa condivisa, pedine, voce di
prossimità, muri, porte, stanze, gruppi e salvataggi locali.

Questa non è ancora una release finale. Ogni modifica al networking o al
percorso audio deve essere verificata con almeno due PC e due account Discord.

| Area | Stato | Dettaglio |
| --- | :---: | --- |
| OAuth Discord | ✅ | PKCE, Public Client e redirect locale |
| Sessioni | ✅ | Crea/entra con codice di 6 caratteri |
| Voce Discord Direct | ✅ | Chiamata nativa senza coda audio Unity |
| Attenuazione per distanza | ✅ | Calcolo locale per ogni partecipante |
| Direzione stereo | 🟡 | Disponibile solo quando il callback offre almeno due canali PCM |
| Mappa e pedine | ✅ | Stato autorevole del DM e interpolazione lato client |
| Muri, porte e stanze | ✅ | Disegno, snap, spessore, stati porta e rilevamento stanze |
| Occlusione | 🟡 | Attenuazione del volume attiva; filtro passa-basso non attivo |
| Gruppi privati | ✅ | Gruppi A/B/C applicati come regola audio locale |
| Mappe salvate | ✅ | Persistenza JSON locale al computer |
| Riconnessione | 🟡 | Gestione errori di base; recupero completo ancora da irrobustire |
| Funzioni immersive avanzate | ⬜ | Telepatia, ambienti sonori, riverbero e preset |

Legenda: ✅ disponibile · 🟡 parziale o da validare · ⬜ pianificato.

## 3. Requisiti

### Per usare una build

- Windows 10 o 11 x64;
- Discord desktop installato, avviato e connesso;
- cuffie consigliate per evitare eco acustico tra casse e microfono;
- connessione Internet per Discord e Unity Relay.

### Per sviluppare

- Unity `6000.3.8f1`;
- modulo **Windows Build Support** compatibile con la configurazione x64;
- Git, se si vuole contribuire tramite repository;
- un'applicazione nel Discord Developer Portal abilitata al Social SDK.

Dipendenze principali:

- Discord Social SDK `1.10.18687`, incluso come pacchetto locale;
- Netcode for GameObjects `2.7.0`;
- Unity Multiplayer Services `1.2.0`;
- Universal Render Pipeline `17.0.1`;
- Input System `1.18.0`;
- Test Framework `1.4.2`.

## 4. Configurazione Discord

Il progetto corrente è configurato per l'Application ID
`1541099026571722772` e per il redirect OAuth:

```text
http://127.0.0.1/callback
```

Nel Discord Developer Portal:

1. abilita il Social SDK per l'applicazione;
2. configura esattamente il redirect URI indicato sopra;
3. abilita **Public Client** per il flusso OAuth2 PKCE;
4. usa gli scope di comunicazione richiesti dal Social SDK;
5. non copiare mai il client secret nel progetto Unity.

Il login apre Discord per la conferma. Access token e refresh token non sono
scritti nei log, nei salvataggi o nei file del progetto. Alla riapertura può
essere richiesta una nuova autorizzazione: è una scelta prudente della versione
attuale, non un errore.

Per usare una propria applicazione Discord bisogna aggiornare la configurazione
in `Assets/_Project/Runtime/Discord/DiscordConfiguration.cs` e mantenere coerente
il redirect nel portale.

## 5. Aprire ed eseguire il progetto

1. Clona o scarica la repository.
2. Apri Unity Hub e scegli **Add project from disk**.
3. Seleziona la sottocartella `DnDVoice` che contiene `Assets`, `Packages` e
   `ProjectSettings`.
4. Scegli Unity `6000.3.8f1`.
5. Attendi la ricostruzione della cartella `Library` e il ripristino pacchetti.
6. Apri la scena principale e premi Play.
7. Consenti l'accesso al microfono e completa l'autorizzazione Discord.

Non selezionare la cartella esterna della repository in Unity Hub: il progetto
Unity vero e proprio è la cartella interna `DnDVoice`.

## 6. Flusso per DM e giocatori

### Creare una sessione

1. Il DM avvia l'app e sceglie **Continua con Discord**.
2. Dopo il login crea una nuova sessione.
3. L'app genera un codice di 6 caratteri evitando simboli ambigui.
4. Il DM condivide solo quel codice con il proprio gruppo.
5. Quando gli altri entrano, le pedine compaiono nella mappa condivisa.

### Entrare in una sessione

1. Il giocatore avvia la stessa build e autorizza Discord.
2. Sceglie di entrare in una sessione.
3. Inserisce il codice comunicato dal DM.
4. Attende che connessione Discord e Relay risultino pronte.

Il codice individua la lobby Discord; un segreto deterministico viene derivato
dal codice. La lobby pubblica metadati di applicazione, codice, host e versione
del protocollo. Il protocollo attuale è la versione `6`.

Se il DM esce, viene meno l'autorità della mappa e la sessione non offre ancora
una migrazione automatica dell'host.

## 7. Comandi dell'interfaccia

| Azione | Comando |
| --- | --- |
| Sussurro | `1` |
| Voce normale | `2` |
| Urlo | `3` |
| Zoom mappa | `Ctrl + rotellina` |
| Scorrimento verticale | Rotellina |
| Scorrimento orizzontale | `Shift + rotellina` |
| Selezionare una pedina | Clic sulla pedina |
| Spostare una pedina | Trascinamento, con autorità DM |
| Annullare una costruzione | `Esc` |
| Eliminare l'elemento selezionato | `Canc` o `Backspace` |

Il menu burger in alto a sinistra contiene gli strumenti di costruzione e il
pannello richiudibile dei giocatori connessi, così la mappa resta libera. I
pannelli intercettano i clic: un comando UI non deve muovere una pedina o
disegnare un muro sottostante.

## 8. Mappa tattica

La mappa parte da `48 × 48 m`. Ogni casella rappresenta un metro. Il DM può
modificarne larghezza e altezza a incrementi di 8 metri, entro i limiti attuali
di `32 × 32 m` e `96 × 96 m`.

La visuale dispone di barre di scorrimento verticali e orizzontali. Lo zoom è
centrato sul puntatore e va indicativamente dal 43% al 300% della scala base.
Selezionando una pedina viene mostrato il suo raggio vocale.

Il DM è autorevole per lo stato della mappa e per lo spostamento delle pedine.
I client ricevono snapshot tramite Relay e interpolano la posizione verso il
bersaglio, riducendo gli scatti visivi.

## 9. Muri, porte e stanze

### Disegnare muri

- scegli **Muri** dal menu;
- il primo clic imposta il punto iniziale e il secondo quello finale;
- gli estremi si agganciano alla griglia da 1 metro;
- vicino a un muro esistente lo snap usa anche segmenti ed estremi già creati;
- la lunghezza minima di un segmento è `0,5 m`;
- lo slider regola lo spessore da `0,2 m` a `2 m`;
- si possono creare fino a 44 segmenti nella configurazione corrente;
- `Esc` annulla solo la costruzione in corso;
- selezionando un vecchio elemento e premendo `Canc` o `Backspace` lo si elimina.

Lo snap rende coerenti anche i segmenti verticali e orizzontali e consente di
chiudere davvero gli angoli, requisito necessario per riconoscere una stanza.

### Stanze

Una stanza non è un rettangolo predefinito: viene ricostruita come poligono dal
grafo dei muri chiusi. L'area minima riconosciuta è `1 m²`. Il comando **Chiudi
stanza** aiuta a collegare il tratto finale al punto iniziale della catena.

### Porte

Una porta viene inserita dentro un muro esistente dividendone il segmento. La
lunghezza di riferimento è `2 m`. Gli stati sono:

- **Aperta**: nessuna occlusione;
- **Chiusa**: attenuazione della voce;
- **Bloccata**: chiusa e marcata come non attraversabile per la logica futura.

Il clic sulla porta ne cambia lo stato secondo i controlli disponibili al DM.
La porta chiusa usa attualmente un fattore di occlusione di circa `0,58`.

### Effetto acustico

Ogni segmento che interseca la linea tra oratore e ascoltatore riduce il volume.
L'effetto dei muri cresce con lo spessore. Le porte aperte non ostacolano la
voce; quelle chiuse o bloccate sì.

Il progetto originale prevedeva anche un filtro passa-basso per rendere la voce
ovattata. In modalità Discord Direct il volume è applicato direttamente al
partecipante della chiamata, ma il filtro non è ancora inserito nel percorso
audio corrente. È quindi una funzione pianificata, non completata.

## 10. Modello della voce

Ogni client calcola localmente il volume degli altri partecipanti usando:

1. distanza tra la propria pedina e quella dell'oratore;
2. curva di attenuazione;
3. modalità Sussurro/Normale/Urlo;
4. muri e porte attraversati;
5. gruppo privato attivo;
6. guadagno finale applicato al partecipante Discord.

| Modalità | Distanza minima | Portata massima | Guadagno base |
| --- | ---: | ---: | ---: |
| Sussurro | 0,75 m | 3 m | 0,72 |
| Normale | 2 m | 12 m | 1,00 |
| Urlo | 3 m | 24 m | 1,00 |

La curva normalizzata usa questi riferimenti: 100% a distanza zero, 80% al 20%
della portata, 55% al 40%, 30% al 60%, 10% all'80% e 0% al limite massimo.

### Perché “Discord Direct”

Una versione precedente copiava il PCM in una coda Unity per ottenere pieno
controllo su pan e filtri. Riducendo troppo quella coda la voce iniziava a
interrompersi per underflow. La Build 1.0 affida continuità e jitter buffer al
percorso nativo Discord e applica il volume per partecipante.

Questo elimina la fragile coda personalizzata e privilegia la stabilità. Una
latenza end-to-end di `1 ms` non è realistica su Internet: acquisizione,
codifica, rete, jitter buffer e riproduzione richiedono tempo. L'obiettivo è una
conversazione stabile con il minor ritardo pratico, non un numero irraggiungibile.

Il pan stereo può essere calcolato quando Discord consegna almeno due canali PCM;
con un callback mono la voce rimane centrata. Questa parte va sempre validata
con due client reali dopo gli aggiornamenti del SDK.

### Eco

L'eco può dipendere da casse aperte, microfono molto sensibile, doppio ascolto
Discord/app o elaborazione del dispositivo. Per provarlo correttamente:

- entrambi usano cuffie;
- si evita di restare anche in un canale vocale Discord separato;
- si controlla che sia attivo un solo microfono per postazione;
- si confronta il comportamento con soppressione eco Discord attiva/disattiva.

## 11. Gruppi privati

Ogni giocatore può appartenere a Nessun gruppo, A, B o C. Quando l'isolamento è
attivo, un membro sente solo altri partecipanti dello stesso gruppo non vuoto.
Questa funzione serve per sottogruppi, stanze narrative o conversazioni private
gestite dal DM.

È importante il confine di sicurezza: tutti restano nella stessa chiamata e il
client applica il mix. Non è isolamento crittografico e non va presentato come
protezione contro un client modificato.

## 12. Networking e sincronizzazione

Discord gestisce autenticazione, lobby e voce; Unity Relay con DTLS trasporta lo
stato della mappa tramite Netcode for GameObjects.

```text
Discord OAuth ──> identità ──> lobby/chiamata
                                  │
DM ──> stato autorevole ──> Unity Relay ──> client
       pedine, modalità, mappa, muri, porte
                                  │
client ──> distanza/occlusione ──> volume Discord locale
```

Dettagli attuali:

- snapshot posizione e mappa fino a 15 Hz quando lo stato cambia;
- snapshot affidabile ogni 2 secondi e all'ingresso di un nuovo client;
- pacchetti frequenti non affidabili per contenere latenza e traffico;
- snapshot periodici affidabili per riallineare lo stato;
- interpolazione visiva sul client;
- host autorevole;
- Relay configurato per 7 connessioni oltre all'host: 8 partecipanti totali.

I modelli hanno un valore obiettivo di 20 giocatori, ma questo non modifica il
limite Relay reale della Build 1.0. Prima di dichiarare supporto a 20 occorrono
un nuovo limite, test di banda, test voce e verifica dell'interfaccia.

## 13. Salvataggi

Il DM può salvare, elencare, caricare ed eliminare mappe locali. I file sono JSON
versione `1` e si trovano sotto:

```text
Application.persistentDataPath/SavedMaps
```

Un salvataggio include:

- nome, fino a 32 caratteri;
- larghezza e altezza della mappa;
- muri con identificatore, estremi e spessore;
- tipo di segmento;
- stato delle porte.

Le stanze vengono ricalcolate dai muri quando la mappa è caricata. Il file è
locale alla macchina del DM; i client online ricevono lo stato caricato tramite
Relay, ma non una copia persistente. Backup, esportazione e cloud non sono
ancora implementati.

## 14. Architettura del codice

```text
Assets/_Project/Runtime/
├── Bootstrap/   avvio e composizione dei servizi
├── Core/        stato applicazione e informazioni build
├── Discord/     configurazione, OAuth e utente Discord
├── Session/     lobby, codici e partecipanti
├── Realtime/    Unity Relay, snapshot e sincronizzazione
├── Players/     modello giocatore e registro pedine
├── Map/         rendering UI, input, muri, porte, stanze e salvataggi
├── Voice/       modalità, distanza, occlusione e integrazione chiamata
└── UI/          tema e componenti grafici condivisi
```

Responsabilità principali:

- `DiscordAuthManager`: inizializzazione SDK e login PKCE;
- `DiscordSessionManager`: lobby, codice sessione e membership;
- `PositionSyncManager`: Relay e snapshot autorevoli;
- `PlayerManager`: stato dei partecipanti e movimento interpolato;
- `ProximityMapOverlay`: visuale tattica, menu e strumenti di costruzione;
- `RoomManager`: segmenti, porte, stanze e persistenza;
- `DiscordVoiceManager`: chiamata, volumi per utente e regole acustiche;
- `VoiceRangeCalculator`: curva di distanza e portata.

Le classi `PcmRingBuffer`, `RemotePcmStream` e `VoiceAudioSource` restano nel
progetto come base sperimentale e per i test del vecchio percorso PCM. Non sono
la coda di riproduzione principale della modalità Discord Direct.

## 15. Build Windows

Dal menu Unity usa:

```text
D&D Proximity Voice > Build Windows 1.0
```

Lo script crea una build release x64 nella cartella:

```text
Builds/DnDProximityVoice-Windows-BUILD-1.0
```

e prepara anche un archivio ZIP condivisibile. Distribuire l'intera cartella o
lo ZIP, non soltanto il file `.exe`, perché Unity necessita della cartella
`*_Data` e delle librerie associate.

L'eseguibile non è firmato. Windows SmartScreen può mostrare un avviso: per una
distribuzione pubblica serviranno firma del codice, pagina release, checksum e
un processo ripetibile di pubblicazione.

## 16. Test e verifica manuale

In Unity apri **Window > General > Test Runner**, seleziona **EditMode** ed
esegui tutti i test. La suite copre le aree core, tra cui:

- generazione e normalizzazione del codice sessione;
- portate e curve delle modalità vocali;
- intersezioni e attenuazione degli ostacoli;
- conversione PCM e comportamento bounded della vecchia coda audio;
- logica dei dati mappa e stanze dove coperta dalla suite.

Checklist minima prima di condividere una nuova build:

1. progetto senza errori di compilazione;
2. tutti gli EditMode test verdi;
3. login su due account Discord;
4. host crea e guest entra con codice;
5. movimento pedine fluido e sincronizzato in entrambe le istanze;
6. cambio `1/2/3` visibile e udibile;
7. volume diverso dentro/fuori portata;
8. muro spesso attenua più di uno sottile;
9. porta aperta/chiusa cambia l'audio e si sincronizza;
10. salvataggio, caricamento ed eliminazione mappa;
11. uscita pulita e nuovo ingresso senza riavviare Discord;
12. conversazione continua per almeno 10–15 minuti senza tagli ricorrenti.

## 17. Risoluzione dei problemi

### Unity Hub dice “No projects found”

Seleziona la sottocartella `DnDVoice`, quella che contiene le cartelle `Assets`,
`Packages` e `ProjectSettings`.

### Errori dentro `Library/PackageCache`

Chiudi Unity. Elimina solo le cartelle generate `Library`, `Temp` e `obj` del
progetto, poi riaprilo con la versione Unity corretta. Non eliminare `Assets`,
`Packages` o `ProjectSettings`.

### Il giocatore non trova la sessione

- ricontrolla tutti i 6 caratteri del codice;
- verifica che entrambi usino la stessa build/protocollo;
- lascia Discord aperto e connesso;
- controlla firewall e connessione Internet;
- fai ricreare la sessione al DM se la lobby precedente è rimasta incoerente.

### La mappa non si aggiorna

- verifica che lo stato Relay sia pronto in entrambe le istanze;
- conferma che il DM sia ancora connesso;
- evita build con versioni protocollo differenti;
- prova prima con un piccolo movimento e attendi lo snapshot affidabile.

### La voce si interrompe

- usa la Build 1.0 Discord Direct, non una build sperimentale con coda Unity;
- controlla stabilità della rete e carico CPU;
- usa cuffie e chiudi eventuali doppi canali vocali;
- conserva il log completo di entrambe le macchine con ora dell'interruzione.

### L'audio non è direzionale

L'attenuazione del volume può funzionare anche quando il flusso fornito dal SDK
è mono. Il pan stereo richiede un percorso PCM multicanale compatibile e resta
un'area da validare e migliorare.

## 18. Limiti conosciuti

- solo Windows è stato assunto come target della prima release;
- massimo pratico corrente: 8 partecipanti totali;
- niente migrazione host automatica;
- niente recupero completo dopo cambio rete o dispositivo audio;
- pan stereo dipendente dal formato PCM disponibile;
- nessun filtro passa-basso/reverb nel percorso Discord Direct;
- nessuna selezione di microfono e uscita dentro l'app;
- nessun controllo volume master o per singolo utente nell'interfaccia;
- salvataggi solo locali;
- niente avatar Discord completi: la visuale usa soprattutto iniziali e colori;
- interfaccia testuale principalmente italiana;
- gruppi privati non crittografici;
- applicazione non firmata e nessun installer;
- Application ID Discord attualmente incorporato nella configurazione client.

## 19. Roadmap completa

### Priorità 1 — affidabilità

- test automatico e manuale multi-client ripetibile;
- riconnessione a lobby, Relay e chiamata dopo perdita rete;
- gestione della disconnessione del DM e possibile migrazione host;
- telemetria locale sicura per dropout, jitter e stato SDK, senza token;
- conferma del limite giocatori e prove di carico da 4 a 8 utenti;
- regressioni automatiche per muri verticali, orizzontali, snap e cancellazione.

### Priorità 2 — controlli audio

- scelta del microfono e del dispositivo di uscita;
- test microfono e indicatore di livello;
- volume master, volume per giocatore e mute manuale;
- intensità audio spaziale configurabile;
- filtro passa-basso attraverso muri e porte;
- profili anti-eco e diagnostica del doppio ascolto;
- studio di un pan stereo stabile senza reintrodurre una coda fragile.

### Priorità 3 — strumenti del DM

- selezione multipla e movimento di gruppo;
- teletrasporto e blocco pedine;
- mute/isola per singolo giocatore;
- editor più ricco per porte, nomi stanza e proprietà acustiche;
- visualizzazione “chi sente chi” con indicatori chiari verde/giallo/rosso;
- annulla/ripristina e cronologia delle modifiche;
- import/export delle mappe.

### Priorità 4 — funzioni D&D

- telepatia e canali magici indipendenti dalla distanza;
- comunicazioni segrete DM-giocatore;
- sorgenti audio ambientali posizionali, musica e suoni di scena;
- riverbero e profili acustici per taverna, dungeon, grotta, esterno e tempio;
- preset di portata e ambiente;
- campagne con più mappe e sessioni salvate.

### Priorità 5 — esperienza e pubblicazione

- avatar Discord, animazione di chi parla e transizioni UI;
- tooltip, onboarding e scorciatoie rimappabili;
- scala UI, contrasto, modalità daltonismo e navigazione tastiera;
- localizzazione completa italiano/inglese;
- build macOS/Linux dopo verifica del supporto SDK;
- installer, firma digitale, aggiornamenti e release GitHub automatizzate;
- backend/confidential OAuth appropriato per una distribuzione pubblica;
- licenza open source o commerciale esplicita.

## 20. Idee escluse dalla Build 1.0 ma conservate

Il concept originale comprendeva inoltre: linee e archi animati tra chi parla e
chi ascolta, pulsazione delle pedine, alone colorato per volume, visualizzatore
audio di debug, effetti ambientali dinamici, profili di ambiente, overlay di
diagnostica e gestione campagne. Sono idee valide, ma non vanno confuse con le
funzioni già disponibili.

Gli strumenti di test in singolo giocatore sono stati rimossi dalla UI pubblica
per mantenere la Build 1.0 pulita. Le verifiche di sviluppo restano nella suite
EditMode e possono essere ampliate senza riportare pulsanti tecnici nel flusso
normale.

## 21. Contribuire

Prima di una modifica:

1. apri una branch dedicata;
2. non aggiungere `Library`, `Temp`, `Logs`, build o credenziali;
3. mantieni separate le responsabilità Discord, rete, mappa e voce;
4. aggiorna test e documentazione quando cambia un comportamento;
5. prova con due client se tocchi sessione, sincronizzazione o audio;
6. descrivi nel commit cosa cambia per il giocatore, non solo i file modificati.

Prima di pubblicare il repository va aggiunto un file `LICENSE` coerente con il
tipo di distribuzione desiderato.
