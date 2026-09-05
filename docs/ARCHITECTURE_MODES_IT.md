# Architettura delle modalità — issue #3

[← README](../README.md) · [English](ARCHITECTURE_MODES_EN.md) ·
[Roadmap V2](ROADMAP_2_0_IT.md) ·
[Issue #3](https://github.com/dragonart19/DnDVoice/issues/3)

## Obiettivo

Questa prima fondazione separa esplicitamente tre concetti:

- **Build 1.0 stabile:** rimane su `main` e non riceve funzioni V2 non validate;
- **Tavolo 2D:** usa la mappa, la sessione, il Relay e la voce già esistenti;
- **World Builder 3D:** ha un'identità distinta ma resta disabilitato finché
  camera, dati e builder non saranno implementati nelle issue dedicate.

Non viene ancora creata una scena 3D e non vengono spostati i sistemi 2D. Lo
scopo è impedire che una funzione futura attivi accidentalmente il flusso
sbagliato o venga presentata come pronta.

## Flusso applicativo

```text
Avvio
  ↓
Accesso Discord
  ↓
Scelta modalità
  ├── Tavolo 2D ──→ Crea/entra ──→ Mappa e voce Build 1.0
  └── World Builder 3D ──→ Disabilitato: roadmap V2
```

`ProductModeManager` è l'unica sorgente dello stato della modalità. Parte da
`None`, accetta soltanto modalità dichiarate disponibili nel
`ProductModeCatalog` e notifica i cambiamenti. Attualmente soltanto
`Tabletop2D` è disponibile.

## Confini introdotti

| Area | Responsabilità |
| --- | --- |
| `ProductMode` | Identificatori stabili `None`, `Tabletop2D`, `WorldBuilder3D` |
| `ProductModeCatalog` | Disponibilità e nome leggibile delle modalità |
| `ProductModeManager` | Selezione centrale e ritorno alla schermata modalità |
| `ProductModeOverlay` | Scelta visibile dopo l'accesso Discord |
| `BuildInfo` | Identifica i branch V2 come `2.0-dev` / `V2 Preview` |
| Build Windows | Usa nome e ZIP V2 Preview, distinti dal comando Build 1.0 di `main` |
| `DiscordSessionManager` | Rifiuta crea/entra se non è selezionato il Tavolo 2D |
| `DiscordSessionOverlay` | Compare solo in 2D e permette di cambiare modalità |
| `ProximityMapOverlay` | Compare solo in una sessione 2D già entrata |

Il futuro modulo 3D non deve dipendere da `ProximityMapOverlay` o
`RoomManager`. Potrà condividere autenticazione Discord, tema UI e servizi
generali, ma avrà dati, visuale e strumenti propri.

## Criteri di accettazione della issue #3

- `main` continua a identificare la Build 1.0 stabile;
- una build generata dal branch V2 è identificata come V2 Preview e non può
  essere confusa con il pacchetto Build 1.0;
- dopo il login compare una sola schermata di scelta modalità;
- il Tavolo 2D conduce al flusso precedente di creazione/ingresso;
- il World Builder 3D è visibile ma non selezionabile;
- «Cambia modalità» torna alla scelta prima di entrare in una sessione;
- crea/entra non può partire senza la modalità 2D;
- mappa, Relay e voce Build 1.0 restano invariati dopo la scelta 2D;
- documentazione italiana e inglese sono coerenti.

## Test da eseguire a cura dell'utente

1. Apri il branch `feature/3-mode-boundaries` con Unity `6000.3.8f1`.
2. Attendi che la Console termini la compilazione e verifica che non ci siano
   errori rossi.
3. Entra in Play Mode e completa l'accesso Discord.
4. Verifica che compaiano `Tavolo 2D` e `World Builder 3D`.
5. Verifica che `IN SVILUPPO` del 3D sia disabilitato.
6. Premi `CONTINUA IN 2D`, poi `CAMBIA MODALITÀ`, e controlla il ritorno alla
   schermata precedente.
7. Rientra in 2D, crea una sessione e verifica mappa, pedina e voce.
8. Con un secondo account entra tramite codice e verifica che il comportamento
   della Build 1.0 non sia cambiato.
9. Esegui la suite EditMode dal Test Runner: ai 47 test precedenti si aggiungono
   i 4 test `ProductModeTests`; il risultato atteso è **51 superati**.
10. Dal menu Unity verifica che sia presente **Build Windows V2 Preview** e che
    non venga generato un pacchetto chiamato Build 1.0 da questo branch.

Codex non ha eseguito questi test, come concordato. L'issue passa in revisione
solo dopo che l'utente comunica gli esiti.
